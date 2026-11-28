using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace ShiaPrayerTimes
{
    /// <summary>
    /// Manages Windows hardware wake timers (CreateWaitableTimer / SetWaitableTimer with fResume=true),
    /// Power Manager requests (PowerCreateRequest / PowerSetRequest), and execution states
    /// so the PC can wake from Sleep for upcoming prayer alerts, wake display/HDMI audio,
    /// and optionally stay awake instead of falling back to unattended sleep after 2 minutes.
    /// </summary>
    public class SleepWakeManager : IDisposable
    {
        [Flags]
        private enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
        }

        private enum POWER_REQUEST_TYPE : int
        {
            PowerRequestDisplayRequired = 0,
            PowerRequestSystemRequired = 1,
            PowerRequestAwayModeRequired = 2,
            PowerRequestExecutionRequired = 3
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct REASON_CONTEXT
        {
            public uint Version;
            public uint Flags;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string SimpleReasonString;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateWaitableTimer(IntPtr lpTimerAttributes, bool bManualReset, string lpTimerName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetWaitableTimer(
            IntPtr hTimer,
            [In] ref long pDueTime,
            int lPeriod,
            IntPtr pfnCompletionRoutine,
            IntPtr lpArgToCompletionRoutine,
            bool fResume);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CancelWaitableTimer(IntPtr hTimer);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr PowerCreateRequest(ref REASON_CONTEXT Context);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool PowerSetRequest(IntPtr PowerRequest, POWER_REQUEST_TYPE RequestType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool PowerClearRequest(IntPtr PowerRequest, POWER_REQUEST_TYPE RequestType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public InputUnion u;
        }

        private const int INPUT_MOUSE = 0;
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xFFFF);
        private const uint WM_SYSCOMMAND = 0x0112;
        private static readonly IntPtr SC_MONITORPOWER = new IntPtr(0xF170);
        private static readonly IntPtr MONITOR_ON = new IntPtr(-1);

        private sealed class NativeWaitHandle : WaitHandle
        {
            public NativeWaitHandle(IntPtr handle)
            {
                this.SafeWaitHandle = new SafeWaitHandle(handle, true);
            }
        }

        private readonly Dispatcher _dispatcher;
        private readonly Action _onWakeCallback;
        private readonly AutoResetEvent _signalEvent = new AutoResetEvent(false);
        private readonly object _lock = new object();

        private NativeWaitHandle _timerWaitHandle;
        private IntPtr _timerRawHandle = IntPtr.Zero;
        private IntPtr _powerRequestHandle = IntPtr.Zero;
        private bool _powerSystemReqActive = false;
        private bool _powerDisplayReqActive = false;

        private Thread _waitThread;
        private DispatcherTimer _wakeHoldTimer;

        private bool _disposed = false;
        private bool _wakeHoldActive = false;
        private bool _playbackActive = false;
        private bool _keepAwakeAfterWake = true;
        private bool _postWakeStayAwakeActive = false;
        private DateTime _scheduledWakeLocal = DateTime.MinValue;
        private DateTime _lastArmedAlertLocal = DateTime.MinValue;

        public DateTime ScheduledWakeLocal { get { return _scheduledWakeLocal; } }

        public SleepWakeManager(Dispatcher dispatcher, Action onWakeCallback)
        {
            _dispatcher = dispatcher;
            _onWakeCallback = onWakeCallback;

            try
            {
                var ctx = new REASON_CONTEXT
                {
                    Version = 0,
                    Flags = 1,
                    SimpleReasonString = "Shia Prayer Times Adhan Wake & Playback"
                };
                IntPtr pr = PowerCreateRequest(ref ctx);
                if (pr != IntPtr.Zero && pr != new IntPtr(-1))
                {
                    _powerRequestHandle = pr;
                }
            }
            catch { }

            _timerRawHandle = CreateWaitableTimer(IntPtr.Zero, false, null);
            if (_timerRawHandle != IntPtr.Zero && _timerRawHandle != new IntPtr(-1))
            {
                _timerWaitHandle = new NativeWaitHandle(_timerRawHandle);
                _waitThread = new Thread(WaitLoop)
                {
                    IsBackground = true,
                    Name = "SleepWakeTimerThread"
                };
                _waitThread.Start();
            }

            _wakeHoldTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(25) };
            _wakeHoldTimer.Tick += (s, e) =>
            {
                _wakeHoldTimer.Stop();
                _wakeHoldActive = false;
                UpdateExecutionStateOnUiThread();
            };

            SystemEvents.PowerModeChanged += OnPowerModeChanged;
        }

        public void SetKeepAwakeAfterWake(bool keepAwake)
        {
            _keepAwakeAfterWake = keepAwake;
            if (!keepAwake && _postWakeStayAwakeActive)
            {
                _postWakeStayAwakeActive = false;
                UpdateExecutionStateOnUiThread();
            }
        }

        private void NudgeMonitorWake()
        {
            try { PostMessage(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, MONITOR_ON); } catch { }
            try
            {
                // 0-delta mouse move helps exit Windows Unattended Wake without moving cursor or unlocking
                var inputs = new INPUT[]
                {
                    new INPUT { type = INPUT_MOUSE, u = new InputUnion { mi = new MOUSEINPUT { dx = 0, dy = 0, dwFlags = MOUSEEVENTF_MOVE } } }
                };
                SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
            }
            catch { }
        }

        private void WaitLoop()
        {
            WaitHandle[] handles = new WaitHandle[] { _signalEvent, _timerWaitHandle };
            while (!_disposed)
            {
                try
                {
                    int idx = WaitHandle.WaitAny(handles);
                    if (_disposed) break;

                    if (idx == 1)
                    {
                        // Nudge system & display idle timers immediately so monitor/HDMI audio wake up before UI runs
                        SetThreadExecutionState(EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_DISPLAY_REQUIRED);
                        NudgeMonitorWake();
                        AppLogger.Log("WakeTimer", "Win32 WaitableTimer signaled! Armed target was: " + _lastArmedAlertLocal.ToString("HH:mm:ss"));

                        if (_dispatcher != null && !_disposed)
                        {
                            _dispatcher.BeginInvoke((Action)(() =>
                            {
                                ActivateWakeHold(true);
                                if (_onWakeCallback != null) _onWakeCallback();
                            }));
                        }
                    }
                }
                catch
                {
                    if (_disposed) break;
                    Thread.Sleep(500);
                }
            }
        }

        /// <summary>
        /// Arms the Win32 wakeable timer to wake Windows 3 seconds prior to targetAlertLocal.
        /// Passing DateTime.MaxValue or DateTime.MinValue cancels any armed wake timer.
        /// </summary>
        public void ScheduleWake(DateTime targetAlertLocal)
        {
            lock (_lock)
            {
                if (_disposed || _timerRawHandle == IntPtr.Zero || _timerWaitHandle == null) return;

                if (targetAlertLocal == DateTime.MaxValue || targetAlertLocal == DateTime.MinValue)
                {
                    CancelWakeTimerInternal();
                    return;
                }

                // Skip redundant re-arm if already scheduled for this exact prayer/reminder time
                if (_lastArmedAlertLocal != DateTime.MinValue &&
                    Math.Abs((_lastArmedAlertLocal - targetAlertLocal).TotalSeconds) < 1.0)
                {
                    return;
                }

                // Wake 3 seconds before prayer/reminder time so hardware & audio drivers initialize
                DateTime wakeLocal = targetAlertLocal.AddSeconds(-3);
                DateTime now = DateTime.Now;
                if (wakeLocal <= now)
                {
                    wakeLocal = now.AddSeconds(1);
                }

                long dueTimeFileTime = wakeLocal.ToUniversalTime().ToFileTimeUtc();
                bool fResumeOk = SetWaitableTimer(_timerRawHandle, ref dueTimeFileTime, 0, IntPtr.Zero, IntPtr.Zero, true);
                if (!fResumeOk)
                {
                    // Fallback to non-wake timer if OS/hardware rejects fResume=true
                    SetWaitableTimer(_timerRawHandle, ref dueTimeFileTime, 0, IntPtr.Zero, IntPtr.Zero, false);
                }
                _scheduledWakeLocal = wakeLocal;
                _lastArmedAlertLocal = targetAlertLocal;
                AppLogger.Log("WakeTimer", string.Format("Armed for {0:yyyy-MM-dd HH:mm:ss} (wake at {1:HH:mm:ss}, fResume={2})",
                    targetAlertLocal, wakeLocal, fResumeOk));
            }
        }

        public void CancelWakeTimer()
        {
            lock (_lock)
            {
                CancelWakeTimerInternal();
            }
        }

        private void CancelWakeTimerInternal()
        {
            if (_timerRawHandle != IntPtr.Zero && _timerWaitHandle != null)
            {
                try { CancelWaitableTimer(_timerRawHandle); } catch { }
            }
            _scheduledWakeLocal = DateTime.MinValue;
            _lastArmedAlertLocal = DateTime.MinValue;
        }

        /// <summary>
        /// Prevents Windows from entering idle sleep while long Adhan playback is active.
        /// Must be called on the UI Dispatcher thread.
        /// </summary>
        public void SetPlaybackActive(bool isPlaying)
        {
            if (_disposed) return;
            _playbackActive = isPlaying;
            UpdateExecutionStateOnUiThread();
        }

        private void ActivateWakeHold(bool fromWakeEvent)
        {
            if (_disposed) return;
            _wakeHoldActive = true;
            if (fromWakeEvent && _keepAwakeAfterWake)
            {
                _postWakeStayAwakeActive = true;
                AppLogger.Log("WakeTimer", "Holding system awake after prayer wake (KeepAwakeAfterPrayerWake=True).");
            }
            _wakeHoldTimer.Stop();
            _wakeHoldTimer.Start();
            NudgeMonitorWake();
            UpdateExecutionStateOnUiThread();
        }

        private void UpdateExecutionStateOnUiThread()
        {
            if (_disposed) return;
            try
            {
                bool wantSystemAwake = _wakeHoldActive || _playbackActive || (_keepAwakeAfterWake && _postWakeStayAwakeActive);
                bool wantDisplayAwake = _wakeHoldActive || _playbackActive;

                if (_powerRequestHandle != IntPtr.Zero)
                {
                    if (wantSystemAwake && !_powerSystemReqActive)
                    {
                        if (PowerSetRequest(_powerRequestHandle, POWER_REQUEST_TYPE.PowerRequestSystemRequired))
                            _powerSystemReqActive = true;
                    }
                    else if (!wantSystemAwake && _powerSystemReqActive)
                    {
                        PowerClearRequest(_powerRequestHandle, POWER_REQUEST_TYPE.PowerRequestSystemRequired);
                        _powerSystemReqActive = false;
                    }

                    if (wantDisplayAwake && !_powerDisplayReqActive)
                    {
                        if (PowerSetRequest(_powerRequestHandle, POWER_REQUEST_TYPE.PowerRequestDisplayRequired))
                            _powerDisplayReqActive = true;
                    }
                    else if (!wantDisplayAwake && _powerDisplayReqActive)
                    {
                        PowerClearRequest(_powerRequestHandle, POWER_REQUEST_TYPE.PowerRequestDisplayRequired);
                        _powerDisplayReqActive = false;
                    }
                }

                if (wantDisplayAwake)
                {
                    SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_DISPLAY_REQUIRED);
                }
                else if (wantSystemAwake)
                {
                    SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED);
                }
                else
                {
                    SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
                }
            }
            catch { }
        }

        private void ClearAllPowerRequests()
        {
            try
            {
                if (_powerRequestHandle != IntPtr.Zero)
                {
                    if (_powerDisplayReqActive)
                    {
                        PowerClearRequest(_powerRequestHandle, POWER_REQUEST_TYPE.PowerRequestDisplayRequired);
                        _powerDisplayReqActive = false;
                    }
                    if (_powerSystemReqActive)
                    {
                        PowerClearRequest(_powerRequestHandle, POWER_REQUEST_TYPE.PowerRequestSystemRequired);
                        _powerSystemReqActive = false;
                    }
                }
                SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
            }
            catch { }
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (_disposed) return;
            if (e.Mode == PowerModes.Suspend && _dispatcher != null)
            {
                // User or OS initiated sleep: release stay-awake lock so manual sleep works cleanly,
                // then re-enable once the prayer wake timer wakes the PC again.
                _dispatcher.BeginInvoke((Action)(() =>
                {
                    _postWakeStayAwakeActive = false;
                    _wakeHoldActive = false;
                    _wakeHoldTimer.Stop();
                    ClearAllPowerRequests();
                }));
            }
            else if (e.Mode == PowerModes.Resume && _dispatcher != null)
            {
                _dispatcher.BeginInvoke((Action)(() =>
                {
                    ActivateWakeHold(true);
                    if (_onWakeCallback != null) _onWakeCallback();
                }));
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
                CancelWakeTimerInternal();
                try { SystemEvents.PowerModeChanged -= OnPowerModeChanged; } catch { }
                if (_wakeHoldTimer != null) _wakeHoldTimer.Stop();
                ClearAllPowerRequests();
                if (_powerRequestHandle != IntPtr.Zero)
                {
                    try { CloseHandle(_powerRequestHandle); } catch { }
                    _powerRequestHandle = IntPtr.Zero;
                }
                try { _signalEvent.Set(); } catch { }
                if (_timerWaitHandle != null)
                {
                    try { _timerWaitHandle.Close(); } catch { }
                    _timerWaitHandle = null;
                    _timerRawHandle = IntPtr.Zero;
                }
            }
        }
    }
}

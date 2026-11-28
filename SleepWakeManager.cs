using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace ShiaPrayerTimes
{
    /// <summary>
    /// Manages Windows hardware wake timers (CreateWaitableTimer / SetWaitableTimer with fResume=true)
    /// and system execution states (SetThreadExecutionState) so the PC can wake from Sleep for
    /// upcoming prayer alerts and stay awake until Adhan playback completes.
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
        private Thread _waitThread;
        private DispatcherTimer _wakeHoldTimer;

        private bool _disposed = false;
        private bool _wakeHoldActive = false;
        private bool _playbackActive = false;
        private DateTime _scheduledWakeLocal = DateTime.MinValue;
        private DateTime _lastArmedAlertLocal = DateTime.MinValue;

        public DateTime ScheduledWakeLocal { get { return _scheduledWakeLocal; } }

        public SleepWakeManager(Dispatcher dispatcher, Action onWakeCallback)
        {
            _dispatcher = dispatcher;
            _onWakeCallback = onWakeCallback;

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
                        AppLogger.Log("WakeTimer", "Win32 WaitableTimer signaled! Armed target was: " + _lastArmedAlertLocal.ToString("HH:mm:ss"));

                        if (_dispatcher != null && !_disposed)
                        {
                            _dispatcher.BeginInvoke((Action)(() =>
                            {
                                ActivateWakeHold();
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

        private void ActivateWakeHold()
        {
            if (_disposed) return;
            _wakeHoldActive = true;
            _wakeHoldTimer.Stop();
            _wakeHoldTimer.Start();
            UpdateExecutionStateOnUiThread();
        }

        private void UpdateExecutionStateOnUiThread()
        {
            if (_disposed) return;
            try
            {
                if (_wakeHoldActive || _playbackActive)
                {
                    // Keep both system & display active during wake/Adhan playback so HDMI/DisplayPort audio
                    // endpoints stay powered on and Windows doesn't drop back into unattended sleep.
                    SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_DISPLAY_REQUIRED);
                }
                else
                {
                    SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
                }
            }
            catch { }
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (_disposed) return;
            if (e.Mode == PowerModes.Resume && _dispatcher != null)
            {
                _dispatcher.BeginInvoke((Action)(() =>
                {
                    ActivateWakeHold();
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
                try { SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS); } catch { }
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

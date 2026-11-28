using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;

namespace ShiaPrayerTimes
{
    public class AudioNotifier
    {
        private const string MciAlias = "ShiaPrayerAudio";

        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        private static extern int mciSendString(string command, StringBuilder returnValue, int returnLength, IntPtr winHandle);

        private MediaPlayer _player;
        private WaveOutPlayer _waveOut;
        private NotifyIcon _notifyIcon;
        private AppSettings _settings;
        private Dispatcher _dispatcher;
        private DispatcherTimer _mciPollTimer;
        private bool _usingWaveOut = false;
        private int _activeWaveSessionId = 0;
        private bool _usingMci = false;
        private bool _isPlaying = false;
        private string _currentTrackName = "";

        public event Action<bool, string> PlaybackStateChanged;

        public bool IsPlaying { get { return _isPlaying; } }
        public string CurrentTrackName { get { return _currentTrackName; } }

        public NotifyIcon NotifyIcon
        {
            get { return _notifyIcon; }
            set { _notifyIcon = value; }
        }

        public AudioNotifier(AppSettings settings, NotifyIcon notifyIcon)
        {
            _settings = settings;
            _notifyIcon = notifyIcon;
            _dispatcher = Dispatcher.CurrentDispatcher;

            _waveOut = new WaveOutPlayer();
            _waveOut.PlaybackFinished += (sessionId, stoppedByUser) =>
            {
                if (_dispatcher != null)
                {
                    _dispatcher.BeginInvoke((Action)(() =>
                    {
                        if (sessionId == _activeWaveSessionId && _usingWaveOut)
                        {
                            _usingWaveOut = false;
                            _isPlaying = false;
                            _currentTrackName = "";
                            if (PlaybackStateChanged != null)
                            {
                                PlaybackStateChanged(false, "");
                            }
                        }
                    }));
                }
            };

            _player = new MediaPlayer();
            _player.MediaEnded += Player_MediaEnded;
            _player.MediaFailed += Player_MediaFailed;

            _mciPollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            _mciPollTimer.Tick += MciPollTimer_Tick;

            UpdateVolume();
        }

        private void MciPollTimer_Tick(object sender, EventArgs e)
        {
            if (!_usingMci || !_isPlaying)
            {
                _mciPollTimer.Stop();
                return;
            }

            var mode = new StringBuilder(128);
            int res = mciSendString("status " + MciAlias + " mode", mode, mode.Capacity, IntPtr.Zero);
            string status = mode.ToString().Trim();
            if (res != 0 || (!status.Equals("playing", StringComparison.OrdinalIgnoreCase) && !status.Equals("seeking", StringComparison.OrdinalIgnoreCase)))
            {
                _mciPollTimer.Stop();
                CloseMci();
                _isPlaying = false;
                _currentTrackName = "";
                if (PlaybackStateChanged != null)
                {
                    PlaybackStateChanged(false, "");
                }
            }
        }

        private void CloseMci()
        {
            try
            {
                mciSendString("stop " + MciAlias, null, 0, IntPtr.Zero);
                mciSendString("close " + MciAlias, null, 0, IntPtr.Zero);
            }
            catch { }
            _usingMci = false;
        }

        public void UpdateSettings(AppSettings settings)
        {
            _settings = settings;
            UpdateVolume();
        }

        public void UpdateVolume()
        {
            if (_waveOut != null)
            {
                _waveOut.SetVolume(_settings.Volume, _settings.IsMuted);
            }
            if (_usingMci && _isPlaying)
            {
                int mciVol = _settings.IsMuted ? 0 : Math.Max(0, Math.Min(1000, _settings.Volume * 10));
                try { mciSendString("setaudio " + MciAlias + " volume to " + mciVol, null, 0, IntPtr.Zero); } catch { }
            }
            if (_player != null)
            {
                _player.Volume = _settings.IsMuted ? 0.0 : (_settings.Volume / 100.0);
            }
        }

        public void PlayChime()
        {
            if (_settings.IsMuted) return;

            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string chimeFile = Path.Combine(appDir, "Audio", "chime.wav");
            if (!File.Exists(chimeFile))
            {
                string cfgChime = Path.Combine(AppSettings.GetConfigDirectory(), "Audio", "chime.wav");
                if (File.Exists(cfgChime)) chimeFile = cfgChime;
            }
            if (!File.Exists(chimeFile))
            {
                chimeFile = @"C:\Windows\Media\Windows Notify.wav";
                if (!File.Exists(chimeFile)) chimeFile = @"C:\Windows\Media\chimes.wav";
            }

            if (File.Exists(chimeFile))
            {
                PlayAudioFile(chimeFile, "Chime");
            }
            else
            {
                System.Media.SystemSounds.Asterisk.Play();
            }
        }

        public void PlayBeep()
        {
            if (_settings.IsMuted) return;
            try
            {
                System.Media.SystemSounds.Beep.Play();
            }
            catch
            {
                try { Console.Beep(800, 250); } catch { }
            }
        }

        public void PlayReminderSound(string soundId = "chime", string customPath = "")
        {
            if (_settings.IsMuted) return;

            if (string.IsNullOrEmpty(soundId) || soundId.Equals("chime", StringComparison.OrdinalIgnoreCase))
            {
                PlayChime();
            }
            else if (soundId.Equals("beep", StringComparison.OrdinalIgnoreCase))
            {
                PlayBeep();
            }
            else if (soundId.Equals("silent", StringComparison.OrdinalIgnoreCase) || soundId.Equals("off", StringComparison.OrdinalIgnoreCase))
            {
                // Silent / Visual Toast only
            }
            else
            {
                string audioPath = _settings.ResolveSoundPath("Reminder", soundId, customPath);
                if (!string.IsNullOrEmpty(audioPath) && File.Exists(audioPath))
                {
                    PlayAudioFile(audioPath, "تذكير بالصلاة");
                }
                else
                {
                    PlayChime();
                }
            }
        }

        public void PlayAdhan(string prayerName, string soundId = "", string customPath = "")
        {
            if (_settings.IsMuted) return;

            string audioPath = _settings.ResolveSoundPath(prayerName, soundId, customPath);
            if (!string.IsNullOrEmpty(audioPath) && File.Exists(audioPath))
            {
                PlayAudioFile(audioPath, "الأذان (" + prayerName + ")");
            }
            else
            {
                PlayChime();
            }
        }

        public void PlayAudioFile(string filePath, string trackName = "")
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    AppLogger.Log("Audio", "Missing audio file: " + filePath);
                    return;
                }

                _usingWaveOut = false;
                if (_waveOut != null) _waveOut.Stop();
                _mciPollTimer.Stop();
                CloseMci();
                if (_player != null)
                {
                    try { _player.Stop(); _player.Close(); } catch { }
                }

                string resolvedName = string.IsNullOrEmpty(trackName) ? Path.GetFileName(filePath) : trackName;
                AppLogger.Log("Audio", "Playing: " + filePath + " (vol=" + _settings.Volume + ", muted=" + _settings.IsMuted + ")");

                // Primary engine: MediaFoundation -> winmm waveOutWrite (raw PCM hardware mixer).
                // Works reliably when Windows is locked, monitor is off, or waking from Sleep.
                int sessionId;
                if (_waveOut != null && _waveOut.PlayAsync(filePath, _settings.Volume, _settings.IsMuted, out sessionId))
                {
                    _activeWaveSessionId = sessionId;
                    _usingWaveOut = true;
                    _isPlaying = true;
                    _currentTrackName = resolvedName;

                    if (PlaybackStateChanged != null)
                    {
                        PlaybackStateChanged(true, _currentTrackName);
                    }
                    return;
                }

                // Secondary engine: winmm.dll MCI
                int openRes = mciSendString("open \"" + filePath + "\" type mpegvideo alias " + MciAlias, null, 0, IntPtr.Zero);
                if (openRes != 0)
                {
                    openRes = mciSendString("open \"" + filePath + "\" alias " + MciAlias, null, 0, IntPtr.Zero);
                }

                if (openRes == 0)
                {
                    int mciVol = _settings.IsMuted ? 0 : Math.Max(0, Math.Min(1000, _settings.Volume * 10));
                    mciSendString("setaudio " + MciAlias + " volume to " + mciVol, null, 0, IntPtr.Zero);

                    int playRes = mciSendString("play " + MciAlias, null, 0, IntPtr.Zero);
                    if (playRes == 0)
                    {
                        _usingMci = true;
                        _isPlaying = true;
                        _currentTrackName = resolvedName;
                        _mciPollTimer.Start();
                        AppLogger.Log("Audio", "Playing via MCI fallback: " + filePath);

                        if (PlaybackStateChanged != null)
                        {
                            PlaybackStateChanged(true, _currentTrackName);
                        }
                        return;
                    }
                    CloseMci();
                }

                // Final fallback: WPF MediaPlayer
                _usingMci = false;
                _player.Open(new Uri(filePath, UriKind.Absolute));
                UpdateVolume();
                _player.Play();
                _isPlaying = true;
                _currentTrackName = resolvedName;
                AppLogger.Log("Audio", "Playing via WPF MediaPlayer fallback: " + filePath);

                if (PlaybackStateChanged != null)
                {
                    PlaybackStateChanged(true, _currentTrackName);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log("Audio", "Audio play error: " + ex.Message);
                Console.WriteLine("Audio play error: " + ex.Message);
                _isPlaying = false;
                if (PlaybackStateChanged != null) PlaybackStateChanged(false, "");
            }
        }

        public void Stop()
        {
            _usingWaveOut = false;
            if (_waveOut != null) _waveOut.Stop();
            _mciPollTimer.Stop();
            CloseMci();
            try
            {
                if (_player != null)
                {
                    _player.Stop();
                    _player.Close();
                }
            }
            catch { }
            _isPlaying = false;
            _currentTrackName = "";
            if (PlaybackStateChanged != null)
            {
                PlaybackStateChanged(false, "");
            }
        }

        private void Player_MediaEnded(object sender, EventArgs e)
        {
            _isPlaying = false;
            _currentTrackName = "";
            if (PlaybackStateChanged != null)
            {
                PlaybackStateChanged(false, "");
            }
        }

        private void Player_MediaFailed(object sender, ExceptionEventArgs e)
        {
            Console.WriteLine("Media Failed: " + (e.ErrorException != null ? e.ErrorException.Message : "Unknown"));
            _isPlaying = false;
            _currentTrackName = "";
            if (PlaybackStateChanged != null)
            {
                PlaybackStateChanged(false, "");
            }
        }

        public void ShowNotification(string title, string body, string iconSymbol = "🕌")
        {
            bool isDark = _settings.Theme == "Dark";
            bool isAr = _settings.Language == "ar";
            int duration = _settings.NotificationDurationSeconds;
            string style = _settings.NotificationStyle ?? "CustomToast";
            string position = _settings.ToastPosition ?? "BottomRight";
            string size = _settings.ToastSize ?? "Medium";
            double cLeft = _settings.CustomToastLeft;
            double cTop = _settings.CustomToastTop;

            if (style.Equals("WindowsNative", StringComparison.OrdinalIgnoreCase))
            {
                if (_notifyIcon != null)
                {
                    int ms = duration <= 0 ? 30000 : Math.Max(5000, duration * 1000);
                    _notifyIcon.ShowBalloonTip(ms, title, body, ToolTipIcon.Info);
                }
            }
            else
            {
                ToastNotificationWindow.ShowToast(title, body, duration, isDark, isAr, iconSymbol, position, size, cLeft, cTop, false, null, () =>
                {
                    try
                    {
                        if (System.Windows.Application.Current != null && System.Windows.Application.Current.MainWindow != null)
                        {
                            var mw = System.Windows.Application.Current.MainWindow;
                            if (mw.WindowState == System.Windows.WindowState.Minimized)
                            {
                                mw.WindowState = System.Windows.WindowState.Normal;
                            }
                            mw.Show();
                            mw.Activate();
                        }
                    }
                    catch { }
                });
            }
        }

        public void TriggerPrayerNotification(string prayerNameAr, string prayerNameEn, string timeStr, string mode, string soundId, string customAudioPath)
        {
            if (mode == "Off") return;

            bool isPrayer = PrayerCalculator.IsActualPrayer(prayerNameEn);
            string title = _settings.Language == "ar"
                ? (isPrayer ? "حان الآن موعد الصلاة" : "حان الآن الموعد")
                : (isPrayer ? "Prayer Time" : "Scheduled Time");

            string body;
            if (_settings.Language == "ar")
            {
                body = isPrayer
                    ? string.Format("حان الآن موعد صلاة {0} ({1})", prayerNameAr, timeStr)
                    : string.Format("حان الآن موعد {0} ({1})", prayerNameAr, timeStr);
            }
            else
            {
                body = isPrayer
                    ? string.Format("It is now time for {0} Prayer ({1})", prayerNameEn, timeStr)
                    : string.Format("It is now time for {0} ({1})", prayerNameEn, timeStr);
            }

            if (mode == "Chime")
            {
                PlayChime();
            }
            else if (mode == "Adhan")
            {
                PlayAdhan(prayerNameEn, soundId, customAudioPath);
            }

            try { ShowNotification(title, body, isPrayer ? "🕌" : "⏰"); } catch { }
        }

        public void TriggerReminderNotification(string prayerNameAr, string prayerNameEn, int minutesBefore, string reminderSoundId = "chime", string customPath = "")
        {
            bool isPrayer = PrayerCalculator.IsActualPrayer(prayerNameEn);
            string title = _settings.Language == "ar"
                ? (isPrayer ? "تذكير باقتراب الصلاة" : "تذكير باقتراب الموعد")
                : (isPrayer ? "Prayer Reminder" : "Time Reminder");

            string body;
            if (_settings.Language == "ar")
            {
                body = isPrayer
                    ? string.Format("اقترب موعد صلاة {0} بعد {1} دقيقة", prayerNameAr, minutesBefore)
                    : string.Format("اقترب موعد {0} بعد {1} دقيقة", prayerNameAr, minutesBefore);
            }
            else
            {
                body = isPrayer
                    ? string.Format("{0} prayer will be in {1} minutes", prayerNameEn, minutesBefore)
                    : string.Format("{0} will be in {1} minutes", prayerNameEn, minutesBefore);
            }

            if (reminderSoundId != "silent" && reminderSoundId != "off" && reminderSoundId != "Off")
            {
                PlayReminderSound(reminderSoundId, customPath);
            }

            try { ShowNotification(title, body, "⏳"); } catch { }
        }

        public void TestVisualNotification(string prayerNameAr = "الظهر", string prayerNameEn = "Dhuhr", string timeStr = "12:00 م")
        {
            string title = _settings.Language == "ar" ? "تجربة: إشعار مرئي فقط" : "Test: Visual Toast Only";
            string body = _settings.Language == "ar"
                ? string.Format("حان الآن موعد صلاة {0} ({1})", prayerNameAr, timeStr)
                : string.Format("It is now time for {0} ({1})", prayerNameEn, timeStr);

            ShowNotification(title, body, "👁️");
        }

        public void TestChimeNotification(string prayerNameAr = "الظهر", string prayerNameEn = "Dhuhr", string timeStr = "12:00 م")
        {
            string title = _settings.Language == "ar" ? "تجربة: نغمة تنبيه (Chime)" : "Test: Chime Notification";
            string body = _settings.Language == "ar"
                ? string.Format("حان الآن موعد صلاة {0} ({1})", prayerNameAr, timeStr)
                : string.Format("It is now time for {0} ({1})", prayerNameEn, timeStr);

            ShowNotification(title, body, "🎵");
            PlayChime();
        }

        public void TestAdhanNotification(string prayerNameEn = "Dhuhr", string prayerNameAr = "الظهر", string timeStr = "12:00 م", string soundId = "default")
        {
            string title = _settings.Language == "ar" ? "تجربة: أذان كامل (Adhan)" : "Test: Full Adhan Alert";
            string body = _settings.Language == "ar"
                ? string.Format("حان الآن موعد أذان صلاة {0} ({1})", prayerNameAr, timeStr)
                : string.Format("It is now time for {0} Adhan ({1})", prayerNameEn, timeStr);

            ShowNotification(title, body, "📢");

            if ((string.IsNullOrEmpty(soundId) || soundId == "default") && _settings.Prayers != null && _settings.Prayers.ContainsKey(prayerNameEn))
            {
                var cfg = _settings.Prayers[prayerNameEn];
                if (!string.IsNullOrEmpty(cfg.SoundId)) soundId = cfg.SoundId;
            }

            PlayAdhan(prayerNameEn, soundId, "");
        }

        public void TestReminderNotification(string prayerNameAr = "الظهر", string prayerNameEn = "Dhuhr", int minutesBefore = 10, string reminderSoundId = "chime")
        {
            string title = _settings.Language == "ar" ? "تجربة: تذكير مسبق بالصلاة" : "Test: Pre-Prayer Reminder";
            string body = _settings.Language == "ar"
                ? string.Format("اقترب موعد صلاة {0} بعد {1} دقائق", prayerNameAr, minutesBefore)
                : string.Format("{0} prayer will be in {1} minutes", prayerNameEn, minutesBefore);

            ShowNotification(title, body, "⏳");

            if (reminderSoundId != "silent" && reminderSoundId != "off" && reminderSoundId != "Off")
            {
                PlayReminderSound(reminderSoundId);
            }
        }
    }
}

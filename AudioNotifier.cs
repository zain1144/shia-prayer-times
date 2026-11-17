using System;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media;

namespace ShiaPrayerTimes
{
    public class AudioNotifier
    {
        private MediaPlayer _player;
        private NotifyIcon _notifyIcon;
        private AppSettings _settings;
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
            _player = new MediaPlayer();
            _player.MediaEnded += Player_MediaEnded;
            _player.MediaFailed += Player_MediaFailed;
            UpdateVolume();
        }

        public void UpdateSettings(AppSettings settings)
        {
            _settings = settings;
            UpdateVolume();
        }

        public void UpdateVolume()
        {
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
                if (!File.Exists(filePath)) return;

                _player.Stop();
                _player.Open(new Uri(filePath, UriKind.Absolute));
                UpdateVolume();
                _player.Play();
                _isPlaying = true;
                _currentTrackName = string.IsNullOrEmpty(trackName) ? Path.GetFileName(filePath) : trackName;

                if (PlaybackStateChanged != null)
                {
                    PlaybackStateChanged(true, _currentTrackName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Audio play error: " + ex.Message);
                _isPlaying = false;
                if (PlaybackStateChanged != null) PlaybackStateChanged(false, "");
            }
        }

        public void Stop()
        {
            try
            {
                if (_player != null)
                {
                    _player.Stop();
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

        public void TriggerPrayerNotification(string prayerNameAr, string prayerNameEn, string timeStr, string mode, string soundId, string customAudioPath)
        {
            string title = _settings.Language == "ar" ? "حان الآن موعد الصلاة" : "Prayer Time";
            string body = _settings.Language == "ar"
                ? string.Format("حان الآن موعد صلاة {0} ({1})", prayerNameAr, timeStr)
                : string.Format("It is now time for {0} ({1})", prayerNameEn, timeStr);

            if (mode == "Off") return;

            if (_notifyIcon != null)
            {
                _notifyIcon.ShowBalloonTip(10000, title, body, ToolTipIcon.Info);
            }

            if (mode == "Chime")
            {
                PlayChime();
            }
            else if (mode == "Adhan")
            {
                PlayAdhan(prayerNameEn, soundId, customAudioPath);
            }
        }

        public void TriggerReminderNotification(string prayerNameAr, string prayerNameEn, int minutesBefore, string reminderSoundId = "chime", string customPath = "")
        {
            string title = _settings.Language == "ar" ? "تذكير باقتراب الصلاة" : "Prayer Reminder";
            string body = _settings.Language == "ar"
                ? string.Format("اقترب موعد صلاة {0} بعد {1} دقيقة", prayerNameAr, minutesBefore)
                : string.Format("{0} prayer will be in {1} minutes", prayerNameEn, minutesBefore);

            if (_notifyIcon != null)
            {
                _notifyIcon.ShowBalloonTip(7000, title, body, ToolTipIcon.Info);
            }

            if (reminderSoundId != "silent" && reminderSoundId != "off" && reminderSoundId != "Off")
            {
                PlayReminderSound(reminderSoundId, customPath);
            }
        }

        public void TestVisualNotification(string prayerNameAr = "الظهر", string prayerNameEn = "Dhuhr", string timeStr = "12:00 م")
        {
            string title = _settings.Language == "ar" ? "تجربة: إشعار مرئي فقط" : "Test: Visual Toast Only";
            string body = _settings.Language == "ar"
                ? string.Format("حان الآن موعد صلاة {0} ({1})", prayerNameAr, timeStr)
                : string.Format("It is now time for {0} ({1})", prayerNameEn, timeStr);

            if (_notifyIcon != null)
            {
                _notifyIcon.ShowBalloonTip(7000, title, body, ToolTipIcon.Info);
            }
        }

        public void TestChimeNotification(string prayerNameAr = "الظهر", string prayerNameEn = "Dhuhr", string timeStr = "12:00 م")
        {
            string title = _settings.Language == "ar" ? "تجربة: نغمة تنبيه (Chime)" : "Test: Chime Notification";
            string body = _settings.Language == "ar"
                ? string.Format("حان الآن موعد صلاة {0} ({1})", prayerNameAr, timeStr)
                : string.Format("It is now time for {0} ({1})", prayerNameEn, timeStr);

            if (_notifyIcon != null)
            {
                _notifyIcon.ShowBalloonTip(7000, title, body, ToolTipIcon.Info);
            }
            PlayChime();
        }

        public void TestAdhanNotification(string prayerNameEn = "Dhuhr", string prayerNameAr = "الظهر", string timeStr = "12:00 م", string soundId = "default")
        {
            string title = _settings.Language == "ar" ? "تجربة: أذان كامل (Adhan)" : "Test: Full Adhan Alert";
            string body = _settings.Language == "ar"
                ? string.Format("حان الآن موعد أذان صلاة {0} ({1})", prayerNameAr, timeStr)
                : string.Format("It is now time for {0} Adhan ({1})", prayerNameEn, timeStr);

            if (_notifyIcon != null)
            {
                _notifyIcon.ShowBalloonTip(10000, title, body, ToolTipIcon.Info);
            }
            PlayAdhan(prayerNameEn, soundId, "");
        }

        public void TestReminderNotification(string prayerNameAr = "الظهر", string prayerNameEn = "Dhuhr", int minutesBefore = 10, string reminderSoundId = "chime")
        {
            string title = _settings.Language == "ar" ? "تجربة: تذكير مسبق بالصلاة" : "Test: Pre-Prayer Reminder";
            string body = _settings.Language == "ar"
                ? string.Format("اقترب موعد صلاة {0} بعد {1} دقائق", prayerNameAr, minutesBefore)
                : string.Format("{0} prayer will be in {1} minutes", prayerNameEn, minutesBefore);

            if (_notifyIcon != null)
            {
                _notifyIcon.ShowBalloonTip(7000, title, body, ToolTipIcon.Info);
            }

            if (reminderSoundId != "silent" && reminderSoundId != "off" && reminderSoundId != "Off")
            {
                PlayReminderSound(reminderSoundId);
            }
        }
    }
}

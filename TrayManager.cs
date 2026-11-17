using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace ShiaPrayerTimes
{
    public class TrayManager : IDisposable
    {
        private NotifyIcon _notifyIcon;
        private Window _mainWindow;
        private AppSettings _settings;
        private AudioNotifier _audioNotifier;
        private ToolStripMenuItem _nextPrayerMenuItem;
        private ToolStripMenuItem _stopAudioMenuItem;
        private ToolStripMenuItem _muteMenuItem;

        public NotifyIcon NotifyIcon { get { return _notifyIcon; } }

        public TrayManager(Window mainWindow, AppSettings settings, AudioNotifier audioNotifier)
        {
            _mainWindow = mainWindow;
            _settings = settings;
            _audioNotifier = audioNotifier;

            _notifyIcon = new NotifyIcon();
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "أوقات الصلاة الشيعية - Shia Prayer Times";

            LoadIcon();

            _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();

            BuildContextMenu();

            _audioNotifier.PlaybackStateChanged += (isPlaying, track) =>
            {
                if (_stopAudioMenuItem != null)
                {
                    _stopAudioMenuItem.Visible = isPlaying;
                }
            };
        }

        private void LoadIcon()
        {
            try
            {
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string iconPath = Path.Combine(appDir, "app_new.ico");
                if (!File.Exists(iconPath)) iconPath = Path.Combine(appDir, "app.ico");

                if (File.Exists(iconPath))
                {
                    byte[] bytes = File.ReadAllBytes(iconPath);
                    using (var ms = new MemoryStream(bytes))
                    {
                        _notifyIcon.Icon = new Icon(ms);
                    }
                }
                else
                {
                    _notifyIcon.Icon = SystemIcons.Application;
                }
            }
            catch
            {
                _notifyIcon.Icon = SystemIcons.Application;
            }
        }

        public void BuildContextMenu()
        {
            var menu = new ContextMenuStrip();
            bool isAr = _settings.Language == "ar";

            _nextPrayerMenuItem = new ToolStripMenuItem(isAr ? "الموعد القادم: ..." : "Next: ...");
            _nextPrayerMenuItem.Font = new Font(_nextPrayerMenuItem.Font, System.Drawing.FontStyle.Bold);
            _nextPrayerMenuItem.Enabled = false;
            menu.Items.Add(_nextPrayerMenuItem);

            menu.Items.Add(new ToolStripSeparator());

            var showItem = new ToolStripMenuItem(isAr ? "فتح البرنامج" : "Show Window", null, (s, e) => ShowMainWindow());
            menu.Items.Add(showItem);

            _stopAudioMenuItem = new ToolStripMenuItem(isAr ? "إيقاف الصوت" : "Stop Audio", null, (s, e) => _audioNotifier.Stop());
            _stopAudioMenuItem.Visible = _audioNotifier.IsPlaying;
            menu.Items.Add(_stopAudioMenuItem);

            _muteMenuItem = new ToolStripMenuItem(_settings.IsMuted ? (isAr ? "إلغاء الكتم" : "Unmute") : (isAr ? "كتم الصوت" : "Mute"), null, (s, e) =>
            {
                _settings.IsMuted = !_settings.IsMuted;
                _settings.Save();
                _audioNotifier.UpdateSettings(_settings);
                UpdateMuteMenuItem();
            });
            menu.Items.Add(_muteMenuItem);

            menu.Items.Add(new ToolStripSeparator());

            var settingsItem = new ToolStripMenuItem(isAr ? "الإعدادات..." : "Settings...", null, (s, e) =>
            {
                ShowMainWindow();
                var main = _mainWindow as MainWindow;
                if (main != null) main.OpenSettingsDialog();
            });
            menu.Items.Add(settingsItem);

            var testMenu = new ToolStripMenuItem(isAr ? "اختبار التنبيهات" : "Test Notifications");
            testMenu.DropDownItems.Add(new ToolStripMenuItem(isAr ? "👁️ إشعار مرئي فقط" : "👁️ Visual Toast Only", null, (s, e) => _audioNotifier.TestVisualNotification()));
            testMenu.DropDownItems.Add(new ToolStripMenuItem(isAr ? "🎵 نغمة تنبيه (Chime)" : "🎵 Chime Alert", null, (s, e) => _audioNotifier.TestChimeNotification()));
            testMenu.DropDownItems.Add(new ToolStripMenuItem(isAr ? "📢 أذان كامل (Adhan)" : "📢 Full Adhan Alert", null, (s, e) => _audioNotifier.TestAdhanNotification()));
            testMenu.DropDownItems.Add(new ToolStripMenuItem(isAr ? "⏳ تذكير مسبق (10 دقائق)" : "⏳ Pre-Prayer Reminder (10 min)", null, (s, e) => _audioNotifier.TestReminderNotification()));
            menu.Items.Add(testMenu);

            var exitItem = new ToolStripMenuItem(isAr ? "خروج" : "Exit", null, (s, e) =>
            {
                _notifyIcon.Visible = false;
                System.Windows.Application.Current.Shutdown();
            });
            menu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = menu;
        }

        public void UpdateMuteMenuItem()
        {
            if (_muteMenuItem != null)
            {
                bool isAr = _settings.Language == "ar";
                _muteMenuItem.Text = _settings.IsMuted ? (isAr ? "إلغاء الكتم" : "Unmute") : (isAr ? "كتم الصوت" : "Mute");
            }
        }

        public void UpdateTrayTooltipAndMenu(string prayerNameAr, string prayerNameEn, string countdownStr)
        {
            try
            {
                bool isAr = _settings.Language == "ar";
                string pName = isAr ? prayerNameAr : prayerNameEn;
                bool isActualPrayer = PrayerCalculator.IsActualPrayer(prayerNameEn);
                string text = isActualPrayer
                    ? (isAr ? string.Format("الصلاة القادمة: {0} ({1})", pName, countdownStr) : string.Format("Next Prayer: {0} in {1}", pName, countdownStr))
                    : (isAr ? string.Format("الموعد القادم: {0} ({1})", pName, countdownStr) : string.Format("Next Event: {0} in {1}", pName, countdownStr));

                if (_nextPrayerMenuItem != null)
                {
                    _nextPrayerMenuItem.Text = text;
                }

                // NotifyIcon.Text max length is 63 chars in Windows Forms
                string tip = string.Format("{0}: {1}", pName, countdownStr);
                if (tip.Length > 63) tip = tip.Substring(0, 63);
                _notifyIcon.Text = tip;
            }
            catch { }
        }

        public void ShowMainWindow()
        {
            if (_mainWindow != null)
            {
                _mainWindow.Show();
                if (_mainWindow.WindowState == WindowState.Minimized)
                {
                    _mainWindow.WindowState = WindowState.Normal;
                }
                _mainWindow.Activate();
            }
        }

        public void Dispose()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
        }
    }
}

using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace ShiaPrayerTimes
{
    public class TrayHoverFlyout : Window
    {
        private TextBlock _iconText;
        private TextBlock _titleText;
        private TextBlock _countdownText;
        private TextBlock _timeText;
        private DispatcherTimer _autoHideTimer;
        private System.Drawing.Point _lastHoverPos;

        public TrayHoverFlyout(bool isDark, bool isAr)
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = System.Windows.Media.Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            ShowActivated = false;
            Focusable = false;
            Width = 240;
            Height = 90;
            FlowDirection = isAr ? System.Windows.FlowDirection.RightToLeft : System.Windows.FlowDirection.LeftToRight;

            var card = new Border
            {
                Background = UIThemeHelper.GetBrush(isDark ? System.Windows.Media.Color.FromRgb(26, 32, 44) : System.Windows.Media.Color.FromRgb(255, 255, 255)),
                BorderBrush = UIThemeHelper.GetBrush(UIThemeHelper.AccentGold),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 8, 14, 8),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 315,
                    ShadowDepth = 3,
                    Opacity = isDark ? 0.6 : 0.25,
                    BlurRadius = 10
                }
            };

            var stack = new StackPanel();

            var headerStack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
            _iconText = new TextBlock
            {
                Text = "🕌 ",
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            headerStack.Children.Add(_iconText);

            _titleText = new TextBlock
            {
                Text = isAr ? "الصلاة القادمة" : "Next Prayer",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextPrimary : UIThemeHelper.LightTextPrimary),
                VerticalAlignment = VerticalAlignment.Center
            };
            headerStack.Children.Add(_titleText);
            stack.Children.Add(headerStack);

            _countdownText = new TextBlock
            {
                Text = "00:00:00",
                FontSize = 21,
                FontWeight = FontWeights.Bold,
                FontFamily = new System.Windows.Media.FontFamily("Consolas, Segoe UI"),
                Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentGold),
                Margin = new Thickness(0, 2, 0, 2)
            };
            stack.Children.Add(_countdownText);

            _timeText = new TextBlock
            {
                Text = "",
                FontSize = 11,
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextSecondary : UIThemeHelper.LightTextSecondary)
            };
            stack.Children.Add(_timeText);

            card.Child = stack;
            Content = card;

            _autoHideTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _autoHideTimer.Interval = TimeSpan.FromMilliseconds(120);
            _autoHideTimer.Tick += (s, e) =>
            {
                var cur = System.Windows.Forms.Cursor.Position;
                double dx = cur.X - _lastHoverPos.X;
                double dy = cur.Y - _lastHoverPos.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                bool overFlyout = (cur.X >= Left && cur.X <= Left + ActualWidth &&
                                   cur.Y >= Top && cur.Y <= Top + ActualHeight);

                if (dist > 60 && !overFlyout)
                {
                    HideFlyout();
                }
            };
        }

        public void UpdateData(string icon, string title, string countdown, string timeDetail)
        {
            _iconText.Text = icon;
            _titleText.Text = title;
            _countdownText.Text = countdown;
            _timeText.Text = timeDetail;
        }

        public void ShowAt(System.Drawing.Point mousePos)
        {
            _lastHoverPos = mousePos;

            double w = 240;
            double h = 90;

            double screenW = SystemParameters.PrimaryScreenWidth;
            double screenH = SystemParameters.PrimaryScreenHeight;

            double left = mousePos.X - (w / 2.0);
            if (left + w > screenW - 8) left = screenW - w - 8;
            if (left < 8) left = 8;

            double top = mousePos.Y - h - 10;
            if (top < 10) top = mousePos.Y + 22;

            Left = left;
            Top = top;

            if (!IsVisible)
            {
                Show();
            }
            _autoHideTimer.Start();
        }

        public void HideFlyout()
        {
            _autoHideTimer.Stop();
            Hide();
        }
    }

    public class TrayManager : IDisposable
    {
        private NotifyIcon _notifyIcon;
        private Window _mainWindow;
        private AppSettings _settings;
        private AudioNotifier _audioNotifier;
        private ToolStripMenuItem _nextPrayerMenuItem;
        private ToolStripMenuItem _stopAudioMenuItem;
        private ToolStripMenuItem _muteMenuItem;
        private TrayHoverFlyout _hoverFlyout;

        private string _lastPrayerNameAr = "";
        private string _lastPrayerNameEn = "";
        private string _lastCountdownStr = "";
        private string _lastNextTimeStr = "";

        public NotifyIcon NotifyIcon { get { return _notifyIcon; } }

        public TrayManager(Window mainWindow, AppSettings settings, AudioNotifier audioNotifier)
        {
            _mainWindow = mainWindow;
            _settings = settings;
            _audioNotifier = audioNotifier;

            _notifyIcon = new NotifyIcon();
            _notifyIcon.Visible = true;
            _notifyIcon.Text = ""; // Keep empty so Windows Explorer doesn't trigger its sluggish 1-second tooltip delay!

            LoadIcon();

            // Pre-create and warm up the hover flyout immediately at startup for instantaneous 0ms display!
            bool isDark = _settings.Theme == "Dark";
            bool isAr = _settings.Language == "ar";
            _hoverFlyout = new TrayHoverFlyout(isDark, isAr);

            _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();
            _notifyIcon.MouseMove += (s, e) => OnTrayMouseMove();
            _notifyIcon.MouseDown += (s, e) => HideHoverFlyout();

            BuildContextMenu();

            _audioNotifier.PlaybackStateChanged += (isPlaying, track) =>
            {
                if (_stopAudioMenuItem != null)
                {
                    _stopAudioMenuItem.Visible = isPlaying;
                }
            };
        }

        private void OnTrayMouseMove()
        {
            try
            {
                var cur = System.Windows.Forms.Cursor.Position;
                if (_hoverFlyout == null)
                {
                    bool isDark = _settings.Theme == "Dark";
                    bool isAr = _settings.Language == "ar";
                    _hoverFlyout = new TrayHoverFlyout(isDark, isAr);
                }

                bool isArLang = _settings.Language == "ar";
                string pName = isArLang ? _lastPrayerNameAr : _lastPrayerNameEn;
                bool isActual = PrayerCalculator.IsActualPrayer(_lastPrayerNameEn);
                string title = isActual
                    ? (isArLang ? string.Format("الصلاة القادمة: {0}", pName) : string.Format("Next Prayer: {0}", pName))
                    : (isArLang ? string.Format("الموعد القادم: {0}", pName) : string.Format("Next Event: {0}", pName));

                string countdownText = isArLang ? string.Format("المتبقي: {0}", _lastCountdownStr) : string.Format("Remaining: {0}", _lastCountdownStr);
                string timeDetail = string.Format("⏰ {0}  |  📍 {1}", _lastNextTimeStr, _settings.Location != null ? _settings.Location.Name : "");

                _hoverFlyout.UpdateData(isActual ? "🕌 " : "⏰ ", title, countdownText, timeDetail);
                _hoverFlyout.ShowAt(cur);
            }
            catch { }
        }

        public void HideHoverFlyout()
        {
            try
            {
                if (_hoverFlyout != null)
                {
                    _hoverFlyout.HideFlyout();
                }
            }
            catch { }
        }

        private void LoadIcon()
        {
            try
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();

                // 1. Try embedded manifest resource (app_new.ico or app.ico)
                using (var stream = asm.GetManifestResourceStream("app_new.ico"))
                {
                    if (stream != null)
                    {
                        _notifyIcon.Icon = new Icon(stream);
                        return;
                    }
                }

                using (var stream = asm.GetManifestResourceStream("app.ico"))
                {
                    if (stream != null)
                    {
                        _notifyIcon.Icon = new Icon(stream);
                        return;
                    }
                }

                // 2. Try AppData extracted icon
                string cfgIcon = Path.Combine(AppSettings.GetConfigDirectory(), "app_new.ico");
                if (File.Exists(cfgIcon))
                {
                    using (var fs = new FileStream(cfgIcon, FileMode.Open, FileAccess.Read))
                    {
                        _notifyIcon.Icon = new Icon(fs);
                        return;
                    }
                }

                // 3. Try BaseDirectory
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string iconPath = Path.Combine(appDir, "app_new.ico");
                if (!File.Exists(iconPath)) iconPath = Path.Combine(appDir, "app.ico");
                if (File.Exists(iconPath))
                {
                    using (var fs = new FileStream(iconPath, FileMode.Open, FileAccess.Read))
                    {
                        _notifyIcon.Icon = new Icon(fs);
                        return;
                    }
                }

                // 4. Try Win32 associated icon extracted from the running EXE itself
                string exePath = asm.Location;
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    Icon exeIcon = Icon.ExtractAssociatedIcon(exePath);
                    if (exeIcon != null)
                    {
                        _notifyIcon.Icon = exeIcon;
                        return;
                    }
                }

                _notifyIcon.Icon = SystemIcons.Application;
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

        public void UpdateTrayTooltipAndMenu(string prayerNameAr, string prayerNameEn, string countdownStr, string nextTimeFormatted = "")
        {
            try
            {
                _lastPrayerNameAr = prayerNameAr;
                _lastPrayerNameEn = prayerNameEn;
                _lastCountdownStr = countdownStr;
                _lastNextTimeStr = nextTimeFormatted;

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

                // If live hover flyout is visible, update it dynamically so countdown ticks constantly!
                if (_hoverFlyout != null && _hoverFlyout.IsVisible)
                {
                    string title = isActualPrayer
                        ? (isAr ? string.Format("الصلاة القادمة: {0}", pName) : string.Format("Next Prayer: {0}", pName))
                        : (isAr ? string.Format("الموعد القادم: {0}", pName) : string.Format("Next Event: {0}", pName));

                    string countdownText = isAr ? string.Format("المتبقي: {0}", countdownStr) : string.Format("Remaining: {0}", countdownStr);
                    string timeDetail = string.Format("⏰ {0}  |  📍 {1}", _lastNextTimeStr, _settings.Location != null ? _settings.Location.Name : "");
                    _hoverFlyout.UpdateData(isActualPrayer ? "🕌 " : "⏰ ", title, countdownText, timeDetail);
                }
            }
            catch { }
        }

        public void ShowMainWindow()
        {
            HideHoverFlyout();
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
            if (_hoverFlyout != null)
            {
                try { _hoverFlyout.Close(); } catch { }
                _hoverFlyout = null;
            }
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
        }
    }
}

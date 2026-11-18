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
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private Border _card;
        private TextBlock _titleText;
        private TextBlock _timeText;
        private TextBlock _countdownText;
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
            SizeToContent = SizeToContent.WidthAndHeight;
            MinWidth = 205;
            FlowDirection = isAr ? System.Windows.FlowDirection.RightToLeft : System.Windows.FlowDirection.LeftToRight;

            _card = new Border
            {
                Background = UIThemeHelper.GetBrush(isDark ? System.Windows.Media.Color.FromRgb(26, 32, 44) : System.Windows.Media.Color.FromRgb(255, 255, 255)),
                BorderBrush = UIThemeHelper.GetBrush(UIThemeHelper.AccentGold),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(18, 10, 18, 10),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 315,
                    ShadowDepth = 3,
                    Opacity = isDark ? 0.6 : 0.25,
                    BlurRadius = 10
                }
            };

            var stack = new StackPanel
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };

            // Line 1: Prayer / Event Title (Centered, no icon)
            _titleText = new TextBlock
            {
                Text = isAr ? "الصلاة القادمة" : "Next Prayer",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextPrimary : UIThemeHelper.LightTextPrimary),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                TextAlignment = System.Windows.TextAlignment.Center
            };
            stack.Children.Add(_titleText);

            // Line 2: Time and Live Countdown (Centered)
            var timeStack = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            };

            _timeText = new TextBlock
            {
                Text = "",
                FontSize = 16.5,
                FontWeight = FontWeights.Bold,
                Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentGold),
                VerticalAlignment = VerticalAlignment.Center
            };
            timeStack.Children.Add(_timeText);

            _countdownText = new TextBlock
            {
                Text = "",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                FontFamily = new System.Windows.Media.FontFamily("Consolas, Segoe UI"),
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextSecondary : UIThemeHelper.LightTextSecondary),
                Margin = new Thickness(8, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            timeStack.Children.Add(_countdownText);
            stack.Children.Add(timeStack);

            _card.Child = stack;
            Content = _card;

            Loaded += (s, e) => EnsureInsideScreen();

            _autoHideTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _autoHideTimer.Interval = TimeSpan.FromMilliseconds(150);
            _autoHideTimer.Tick += (s, e) =>
            {
                var cur = System.Windows.Forms.Cursor.Position;
                double dx = cur.X - _lastHoverPos.X;
                double dy = cur.Y - _lastHoverPos.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                bool overFlyout = (cur.X >= Left - 6 && cur.X <= Left + ActualWidth + 6 &&
                                   cur.Y >= Top - 6 && cur.Y <= Top + ActualHeight + 6);

                // Hide if mouse moved to neighboring icon (dist > 40) and not over the flyout card
                if (dist > 40 && !overFlyout)
                {
                    HideFlyout();
                }
            };
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            try
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);
                SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
            }
            catch { }
        }

        public void UpdateData(string title, string timeStr, string countdownStr)
        {
            _titleText.Text = title;
            if (!string.IsNullOrEmpty(timeStr))
            {
                _timeText.Text = timeStr;
                _timeText.Visibility = Visibility.Visible;
                _countdownText.Text = string.Format("({0})", countdownStr);
            }
            else
            {
                _timeText.Visibility = Visibility.Collapsed;
                _countdownText.Text = countdownStr;
            }
            if (IsVisible)
            {
                EnsureInsideScreen();
            }
        }

        public void ShowNearTray(System.Drawing.Point mousePos)
        {
            _lastHoverPos = mousePos;

            _card.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            // Add margin for shadow/border so clamping safely includes the full card width inside the monitor
            double w = Math.Max(MinWidth, _card.DesiredSize.Width) + 28;
            double h = Math.Max(50, _card.DesiredSize.Height) + 20;

            var screen = System.Windows.Forms.Screen.FromPoint(mousePos);
            var wa = screen.WorkingArea;

            double left = mousePos.X - (w / 2.0);
            double top = wa.Bottom - h - 10;

            if (wa.Bottom < screen.Bounds.Bottom)
            {
                top = wa.Bottom - h - 10;
                left = mousePos.X - (w / 2.0);
            }
            else if (wa.Top > screen.Bounds.Top)
            {
                top = wa.Top + 10;
                left = mousePos.X - (w / 2.0);
            }
            else if (wa.Right < screen.Bounds.Right)
            {
                left = wa.Right - w - 10;
                top = mousePos.Y - (h / 2.0);
            }
            else if (wa.Left > screen.Bounds.Left)
            {
                left = wa.Left + 10;
                top = mousePos.Y - (h / 2.0);
            }

            // Strictly clamp inside the working area of the monitor where the cursor is!
            if (left + w > wa.Right - 10) left = wa.Right - w - 10;
            if (left < wa.Left + 10) left = wa.Left + 10;
            if (top + h > wa.Bottom - 8) top = wa.Bottom - h - 8;
            if (top < wa.Top + 8) top = wa.Top + 8;

            Left = left;
            Top = top;

            if (!IsVisible)
            {
                Show();
            }

            EnsureInsideScreen();

            _autoHideTimer.Start();
        }

        private void EnsureInsideScreen()
        {
            try
            {
                var screen = System.Windows.Forms.Screen.FromPoint(_lastHoverPos);
                var wa = screen.WorkingArea;
                double curW = ActualWidth > 0 ? ActualWidth : (DesiredSize.Width > 0 ? DesiredSize.Width : 205);
                double curH = ActualHeight > 0 ? ActualHeight : (DesiredSize.Height > 0 ? DesiredSize.Height : 60);

                if (Left + curW > wa.Right - 10)
                {
                    Left = wa.Right - curW - 10;
                }
                if (Left < wa.Left + 10)
                {
                    Left = wa.Left + 10;
                }
                if (Top + curH > wa.Bottom - 8)
                {
                    Top = wa.Bottom - curH - 8;
                }
                if (Top < wa.Top + 8)
                {
                    Top = wa.Top + 8;
                }
            }
            catch { }
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
        private DispatcherTimer _hoverTimer;
        private System.Drawing.Point _pendingHoverPos;

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
            _notifyIcon.Text = ""; // Suppress native Windows Explorer tooltip so only our live ticking flyout shows without duplicates!

            LoadIcon();

            // Pre-create and warm up the hover flyout immediately at startup
            bool isDark = _settings.Theme == "Dark";
            bool isAr = _settings.Language == "ar";
            _hoverFlyout = new TrayHoverFlyout(isDark, isAr);

            // Hover stabilization timer (250ms delay to settle mouse before showing)
            _hoverTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _hoverTimer.Interval = TimeSpan.FromMilliseconds(250);
            _hoverTimer.Tick += (s, e) =>
            {
                _hoverTimer.Stop();
                try
                {
                    var cur = System.Windows.Forms.Cursor.Position;
                    double dx = cur.X - _pendingHoverPos.X;
                    double dy = cur.Y - _pendingHoverPos.Y;
                    double dist = Math.Sqrt(dx * dx + dy * dy);

                    // Check that the mouse is hovering around the icon (generous 45px tolerance)
                    if (dist < 45)
                    {
                        ShowHoverFlyoutAt(cur);
                    }
                }
                catch { }
            };

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
                _pendingHoverPos = cur;

                // If flyout is already visible, keep it visible
                if (_hoverFlyout != null && _hoverFlyout.IsVisible)
                {
                    return;
                }

                // Restart hover delay timer (waits for mouse to settle, but faster than Windows default)
                _hoverTimer.Stop();
                _hoverTimer.Start();
            }
            catch { }
        }

        private void ShowHoverFlyoutAt(System.Drawing.Point pos)
        {
            try
            {
                if (_hoverFlyout == null)
                {
                    bool isDark = _settings.Theme == "Dark";
                    bool isAr = _settings.Language == "ar";
                    _hoverFlyout = new TrayHoverFlyout(isDark, isAr);
                }

                bool isArLang = _settings.Language == "ar";
                string pName = isArLang ? _lastPrayerNameAr : _lastPrayerNameEn;
                if (string.IsNullOrEmpty(pName)) return;

                bool isActual = PrayerCalculator.IsActualPrayer(_lastPrayerNameEn);
                string title = isActual
                    ? (isArLang ? string.Format("الصلاة القادمة: {0}", pName) : string.Format("Next Prayer: {0}", pName))
                    : (isArLang ? string.Format("الموعد القادم: {0}", pName) : string.Format("Next Event: {0}", pName));

                _hoverFlyout.UpdateData(title, _lastNextTimeStr, _lastCountdownStr);
                _hoverFlyout.ShowNearTray(pos);
            }
            catch { }
        }

        public void HideHoverFlyout()
        {
            try
            {
                if (_hoverTimer != null)
                {
                    _hoverTimer.Stop();
                }
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

                // Line 1: Next Prayer or Next Event
                string line1 = isActualPrayer
                    ? (isAr ? string.Format("الصلاة القادمة: {0}", pName) : string.Format("Next Prayer: {0}", pName))
                    : (isAr ? string.Format("الموعد القادم: {0}", pName) : string.Format("Next Event: {0}", pName));

                // Line 2: Time and countdown
                string line2 = !string.IsNullOrEmpty(nextTimeFormatted)
                    ? string.Format("{0} ({1})", nextTimeFormatted, countdownStr)
                    : countdownStr;

                if (_nextPrayerMenuItem != null)
                {
                    _nextPrayerMenuItem.Text = string.Format("{0}: {1}", line1, line2);
                }

                // If live hover flyout is visible, update it dynamically so countdown ticks constantly!
                if (_hoverFlyout != null && _hoverFlyout.IsVisible)
                {
                    _hoverFlyout.UpdateData(line1, nextTimeFormatted, countdownStr);
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
            if (_hoverTimer != null)
            {
                _hoverTimer.Stop();
                _hoverTimer = null;
            }
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

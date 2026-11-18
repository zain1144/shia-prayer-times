using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
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

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const int SW_HIDE = 0;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpfn, IntPtr lParam);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out TrayManager.RECT lpRect);

        private Border _card;
        private TextBlock _titleText;
        private TextBlock _timeText;
        private TextBlock _countdownText;
        private DispatcherTimer _autoHideTimer;
        private System.Drawing.Point _lastHoverPos;
        private TrayManager _trayManager;
        private Rect _iconRect;
        private double _iconCenterX;
        private double _slotHalfWidth;

        public TrayHoverFlyout(bool isDark, bool isAr, TrayManager trayManager = null)
        {
            _trayManager = trayManager;
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
                Padding = new Thickness(18, 10, 18, 10)
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

            _lastHoverPos = System.Windows.Forms.Cursor.Position;
            Loaded += (s, e) => EnsureInsideScreen();

            _autoHideTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _autoHideTimer.Interval = TimeSpan.FromMilliseconds(50);
            _autoHideTimer.Tick += (s, e) =>
            {
                var cur = System.Windows.Forms.Cursor.Position;
                double scaleX, scaleY;
                GetDpiScale(out scaleX, out scaleY);

                double dipCurX = cur.X / scaleX;
                double dipCurY = cur.Y / scaleY;

                // 1. Is cursor inside the flyout card itself?
                // Strictly bounded inside the card; does NOT reach down into the taskbar!
                bool overCard = (dipCurX >= Left - 4 && dipCurX <= Left + ActualWidth + 4 &&
                                 dipCurY >= Top - 4 && dipCurY <= Top + ActualHeight + 2);

                // 2. Is cursor over our tray icon slot?
                // Narrow horizontal range of our single icon slot (matching the OS highlight box)
                double iconLeft = _iconCenterX - _slotHalfWidth - 3.0;
                double iconRight = _iconCenterX + _slotHalfWidth + 3.0;
                double iconTop = _iconRect.Top - 2.0;
                double iconBottom = _iconRect.Bottom + 4.0;

                bool onOurIcon = (dipCurX >= iconLeft && dipCurX <= iconRight &&
                                  dipCurY >= iconTop && dipCurY <= iconBottom);

                // 3. Is cursor on the vertical bridge between our icon and the card?
                // Allows vertical cursor transition, but ONLY within our icon's narrow column!
                double bridgeTop = Math.Min(Top + ActualHeight - 4.0, iconTop);
                double bridgeBottom = Math.Max(Top + ActualHeight + 4.0, iconTop + 4.0);

                bool onBridge = (dipCurX >= iconLeft && dipCurX <= iconRight &&
                                 dipCurY >= bridgeTop && dipCurY <= bridgeBottom);

                if (!overCard && !onOurIcon && !onBridge)
                {
                    HideFlyout();
                }
                else
                {
                    SuppressNativeTooltips();
                }
            };
        }

        private void SuppressNativeTooltips()
        {
            try
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                if (helper.Handle != IntPtr.Zero)
                {
                    SetWindowPos(helper.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                }

                double scaleX, scaleY;
                GetDpiScale(out scaleX, out scaleY);
                var screen = System.Windows.Forms.Screen.FromPoint(_lastHoverPos);
                double waBottom = screen.WorkingArea.Bottom / scaleY;

                EnumWindows((hWnd, lParam) =>
                {
                    if (IsWindowVisible(hWnd))
                    {
                        StringBuilder sb = new StringBuilder(64);
                        GetClassName(hWnd, sb, 64);
                        string cls = sb.ToString();
                        if (cls == "Xaml_WindowedPopupClass" || cls == "tooltips_class32")
                        {
                            TrayManager.RECT r;
                            if (GetWindowRect(hWnd, out r))
                            {
                                int w = r.right - r.left;
                                int h = r.bottom - r.top;
                                double dipLeft = r.left / scaleX;
                                double dipRight = r.right / scaleX;
                                double dipBottom = r.bottom / scaleY;

                                if (w > 0 && w < 300 && h > 0 && h < 120 &&
                                    dipRight >= _iconCenterX - 150 && dipLeft <= _iconCenterX + 150 &&
                                    dipBottom >= waBottom - 120)
                                {
                                    ShowWindow(hWnd, SW_HIDE);
                                }
                            }
                        }
                    }
                    return true;
                }, IntPtr.Zero);
            }
            catch { }
        }

        public static void GetDpiScale(out double scaleX, out double scaleY)
        {
            try
            {
                scaleX = (double)Screen.PrimaryScreen.Bounds.Width / SystemParameters.PrimaryScreenWidth;
                scaleY = (double)Screen.PrimaryScreen.Bounds.Height / SystemParameters.PrimaryScreenHeight;
                if (scaleX <= 0.1) scaleX = 1.0;
                if (scaleY <= 0.1) scaleY = 1.0;
            }
            catch
            {
                scaleX = 1.0;
                scaleY = 1.0;
            }
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

            double scaleX, scaleY;
            GetDpiScale(out scaleX, out scaleY);

            var screen = System.Windows.Forms.Screen.FromPoint(mousePos);
            double dipMouseX = mousePos.X / scaleX;
            double dipMouseY = mousePos.Y / scaleY;

            double waLeft = screen.WorkingArea.Left / scaleX;
            double waTop = screen.WorkingArea.Top / scaleY;
            double waRight = screen.WorkingArea.Right / scaleX;
            double waBottom = screen.WorkingArea.Bottom / scaleY;

            double boundsLeft = screen.Bounds.Left / scaleX;
            double boundsTop = screen.Bounds.Top / scaleY;
            double boundsRight = screen.Bounds.Right / scaleX;
            double boundsBottom = screen.Bounds.Bottom / scaleY;

            // Determine accurate icon slot bounds (matching the OS highlight box)
            Rect osRect;
            if (_trayManager != null && _trayManager.TryGetIconRect(out osRect))
            {
                _iconRect = osRect;
                _iconCenterX = osRect.Left + (osRect.Width / 2.0);
                _slotHalfWidth = Math.Max(10.0, osRect.Width / 2.0);
            }
            else
            {
                _iconCenterX = dipMouseX;
                // Standard Windows 11 taskbar button is ~40-48 physical px wide -> in DIPs = 22 / scaleX
                _slotHalfWidth = Math.Max(10.0, 22.0 / scaleX);
                _iconRect = new Rect(_iconCenterX - _slotHalfWidth, waBottom, _slotHalfWidth * 2.0, Math.Max(36.0, boundsBottom - waBottom));
            }

            _card.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            double w = Math.Max(MinWidth, _card.DesiredSize.Width);
            double h = Math.Max(50, _card.DesiredSize.Height);

            // Center horizontally over our icon
            double left = _iconCenterX - (w / 2.0);
            double top = waBottom - h - 2;

            if (waBottom < boundsBottom)
            {
                top = waBottom - h - 2;
                left = _iconCenterX - (w / 2.0);
            }
            else if (waTop > boundsTop)
            {
                top = waTop + 2;
                left = _iconCenterX - (w / 2.0);
            }
            else if (waRight < boundsRight)
            {
                left = waRight - w - 2;
                top = dipMouseY - (h / 2.0);
            }
            else if (waLeft > boundsLeft)
            {
                left = waLeft + 2;
                top = dipMouseY - (h / 2.0);
            }

            // Strictly clamp inside the working area of the monitor where the cursor is!
            if (left + w > waRight - 8) left = waRight - w - 8;
            if (left < waLeft + 8) left = waLeft + 8;
            if (top + h > waBottom - 2) top = waBottom - h - 2;
            if (top < waTop + 2) top = waTop + 2;

            Left = left;
            Top = top;

            if (!IsVisible)
            {
                Show();
            }

            EnsureInsideScreen();
            SuppressNativeTooltips();

            _autoHideTimer.Start();
        }

        private void EnsureInsideScreen()
        {
            try
            {
                double scaleX, scaleY;
                GetDpiScale(out scaleX, out scaleY);

                var screen = System.Windows.Forms.Screen.FromPoint(_lastHoverPos);
                double waLeft = screen.WorkingArea.Left / scaleX;
                double waTop = screen.WorkingArea.Top / scaleY;
                double waRight = screen.WorkingArea.Right / scaleX;
                double waBottom = screen.WorkingArea.Bottom / scaleY;

                double curW = ActualWidth > 0 ? ActualWidth : (DesiredSize.Width > 0 ? DesiredSize.Width : 205);
                double curH = ActualHeight > 0 ? ActualHeight : (DesiredSize.Height > 0 ? DesiredSize.Height : 60);

                if (Left + curW > waRight - 8)
                {
                    Left = waRight - curW - 8;
                }
                if (Left < waLeft + 8)
                {
                    Left = waLeft + 8;
                }
                if (Top + curH > waBottom - 2)
                {
                    Top = waBottom - curH - 2;
                }
                if (Top < waTop + 2)
                {
                    Top = waTop + 2;
                }
            }
            catch { }
        }

        public void UpdateHoverPosition(System.Drawing.Point pos)
        {
            _lastHoverPos = pos;
            Rect osRect;
            if (_trayManager != null && _trayManager.TryGetIconRect(out osRect))
            {
                _iconRect = osRect;
                _iconCenterX = osRect.Left + (osRect.Width / 2.0);
                _slotHalfWidth = Math.Max(10.0, osRect.Width / 2.0);
            }
        }

        public void HideFlyout()
        {
            _autoHideTimer.Stop();
            Hide();
        }
    }

    public class TrayManager : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NOTIFYICONIDENTIFIER
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uID;
            public Guid guidItem;
        }

        [DllImport("shell32.dll", SetLastError = true)]
        public static extern int Shell_NotifyIconGetRect(ref NOTIFYICONIDENTIFIER identifier, out RECT iconLocation);

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

        public bool TryGetIconRect(out Rect rectInDips)
        {
            rectInDips = Rect.Empty;
            try
            {
                if (_notifyIcon == null) return false;
                var fiId = typeof(NotifyIcon).GetField("id", BindingFlags.NonPublic | BindingFlags.Instance);
                var fiWnd = typeof(NotifyIcon).GetField("window", BindingFlags.NonPublic | BindingFlags.Instance);
                if (fiId == null || fiWnd == null) return false;

                int id = (int)fiId.GetValue(_notifyIcon);
                NativeWindow wnd = (NativeWindow)fiWnd.GetValue(_notifyIcon);
                if (wnd == null || wnd.Handle == IntPtr.Zero) return false;

                NOTIFYICONIDENTIFIER nid = new NOTIFYICONIDENTIFIER();
                nid.cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONIDENTIFIER));
                nid.hWnd = wnd.Handle;
                nid.uID = (uint)id;
                nid.guidItem = Guid.Empty;

                RECT r;
                int hr = Shell_NotifyIconGetRect(ref nid, out r);
                if (hr == 0 && r.right > r.left && r.bottom > r.top)
                {
                    double scaleX, scaleY;
                    TrayHoverFlyout.GetDpiScale(out scaleX, out scaleY);
                    rectInDips = new Rect(r.left / scaleX, r.top / scaleY, (r.right - r.left) / scaleX, (r.bottom - r.top) / scaleY);
                    return true;
                }
            }
            catch { }
            return false;
        }

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
            _hoverFlyout = new TrayHoverFlyout(isDark, isAr, this);

            // Hover stabilization timer (60ms delay, beats Windows 11 400ms tooltip)
            _hoverTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _hoverTimer.Interval = TimeSpan.FromMilliseconds(60);
            _hoverTimer.Tick += (s, e) =>
            {
                _hoverTimer.Stop();
                try
                {
                    var cur = System.Windows.Forms.Cursor.Position;
                    double scaleX, scaleY;
                    TrayHoverFlyout.GetDpiScale(out scaleX, out scaleY);

                    var screen = System.Windows.Forms.Screen.FromPoint(cur);
                    double waBottom = screen.WorkingArea.Bottom / scaleY;

                    double dx = Math.Abs((cur.X - _pendingHoverPos.X) / scaleX);
                    double curY = cur.Y / scaleY;

                    // Ensure cursor is still in taskbar area near our pending hover position
                    if (dx <= 16 && curY >= waBottom - 4)
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

                // If flyout is already visible, update position and keep it alive!
                if (_hoverFlyout != null && _hoverFlyout.IsVisible)
                {
                    _hoverFlyout.UpdateHoverPosition(cur);
                    return;
                }

                // Start hover timer if not already running (prevents continuous resetting on mouse move)
                if (!_hoverTimer.IsEnabled)
                {
                    _pendingHoverPos = cur;
                    _hoverTimer.Start();
                }
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
                    _hoverFlyout = new TrayHoverFlyout(isDark, isAr, this);
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

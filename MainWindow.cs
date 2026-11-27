using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ShiaPrayerTimes
{
    public class MainWindow : Window
    {
        private AppSettings _settings;
        private AudioNotifier _audioNotifier;
        private TrayManager _trayManager;
        private SleepWakeManager _sleepWakeManager;
        private DispatcherTimer _clockTimer;

        private DateTime _viewDate = DateTime.Today;
        private PrayerTimesResult _viewTimesResult;
        private PrayerTimesResult _todayTimesResult;
        private DateTime _todayDate = DateTime.MinValue;

        // UI Header controls
        private TextBlock _gregorianDateText;
        private TextBlock _hijriDateText;
        private TextBlock _locationBadgeText;
        private ComboBox _quickLocationCombo;
        private Button _muteBtn;
        private Button _themeBtn;
        private Button _langBtn;
        private Border _audioPlayingBanner;
        private TextBlock _audioPlayingText;
        private Button _stopAudioBtn;

        // Next prayer card controls
        private TextBlock _nextPrayerTitleText;
        private TextBlock _nextPrayerTimeText;
        private TextBlock _countdownText;
        private TextBlock _currentPeriodText;
        private ProgressBar _periodProgressBar;

        // Prayer list panel
        private StackPanel _prayerListPanel;
        private DatePicker _datePicker;

        // Track fired notifications
        private HashSet<string> _firedAlerts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public MainWindow(AppSettings settings)
        {
            _settings = settings;

            InitializeApp();
        }

        private void InitializeApp()
        {
            _audioNotifier = new AudioNotifier(_settings, null);
            _trayManager = new TrayManager(this, _settings, _audioNotifier);
            _audioNotifier.NotifyIcon = _trayManager.NotifyIcon;
            _sleepWakeManager = new SleepWakeManager(Dispatcher, OnSystemWakeOrResume);

            if (Application.Current != null)
            {
                Application.Current.Exit += (s, e) =>
                {
                    if (_sleepWakeManager != null) _sleepWakeManager.Dispose();
                };
            }

            _audioNotifier.PlaybackStateChanged += (isPlaying, trackName) =>
            {
                Dispatcher.Invoke((Action)(() =>
                {
                    if (_sleepWakeManager != null)
                    {
                        _sleepWakeManager.SetPlaybackActive(isPlaying);
                    }
                    if (_audioPlayingBanner != null)
                    {
                        _audioPlayingBanner.Visibility = isPlaying ? Visibility.Visible : Visibility.Collapsed;
                        if (_audioPlayingText != null)
                        {
                            _audioPlayingText.Text = (_settings.Language == "ar" ? "جاري تشغيل: " : "Playing: ") + trackName;
                        }
                    }
                }));
            };

            SetupWindowUI();
            RecalculateAndRefreshUI();

            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += ClockTimer_Tick;
            _clockTimer.Start();

            // Delayed once-daily silent update check so app startup remains instant
            var startupUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            startupUpdateTimer.Tick += (s, e) =>
            {
                startupUpdateTimer.Stop();
                AppUpdater.CheckForUpdatesAsync(_settings, false, this);
            };
            startupUpdateTimer.Start();
        }

        private void SetupWindowUI()
        {
            bool isAr = _settings.Language == "ar";
            bool isDark = _settings.Theme == "Dark";

            Title = isAr ? "أوقات الصلاة الشيعية - الجعفرية (Pray Times)" : "Shia (Jafari) Prayer Times Desktop";
            var wa = SystemParameters.WorkArea;
            Width = Math.Min(980, Math.Max(720, wa.Width * 0.90));
            Height = Math.Min(760, Math.Max(520, wa.Height * 0.92));
            MinWidth = Math.Min(720, wa.Width * 0.80);
            MinHeight = Math.Min(500, wa.Height * 0.80);
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = isAr ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

            WindowStyle = WindowStyle.None;

            Background = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkBg : UIThemeHelper.LightBg);
            Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextPrimary : UIThemeHelper.LightTextPrimary);

            // Load Window Icon
            try
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                using (var stream = asm.GetManifestResourceStream("app_new.ico"))
                {
                    if (stream != null)
                    {
                        Icon = System.Windows.Media.Imaging.BitmapFrame.Create(stream);
                    }
                }

                if (Icon == null)
                {
                    using (var stream = asm.GetManifestResourceStream("app.ico"))
                    {
                        if (stream != null)
                        {
                            Icon = System.Windows.Media.Imaging.BitmapFrame.Create(stream);
                        }
                    }
                }

                if (Icon == null)
                {
                    string cfgIcon = Path.Combine(AppSettings.GetConfigDirectory(), "app_new.ico");
                    if (File.Exists(cfgIcon))
                    {
                        Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri(cfgIcon, UriKind.Absolute));
                    }
                }

                if (Icon == null)
                {
                    string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_new.ico");
                    if (!File.Exists(iconPath)) iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                    if (File.Exists(iconPath))
                    {
                        Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri(iconPath, UriKind.Absolute));
                    }
                }
            }
            catch { }

            var mainBorder = new Border
            {
                BorderBrush = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkBorder : UIThemeHelper.LightBorder),
                BorderThickness = new Thickness(1),
                Background = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkBg : UIThemeHelper.LightBg)
            };

            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Integrated Title Bar
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Audio Banner
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Date & Nav Bar
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Next Prayer Countdown Card
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Prayers List

            // 1. Integrated Title Bar (Title, Actions & Window Controls)
            rootGrid.Children.Add(CreateIntegratedHeader(isDark, isAr));

            // 2. Audio Playing Banner
            _audioPlayingBanner = new Border
            {
                Background = UIThemeHelper.GetBrush(UIThemeHelper.AccentRed),
                Padding = new Thickness(16, 8, 16, 8),
                Visibility = Visibility.Collapsed
            };
            var audioBannerGrid = new Grid();
            audioBannerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            audioBannerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _audioPlayingText = new TextBlock
            {
                Text = isAr ? "جاري تشغيل الأذان..." : "Playing Adhan...",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_audioPlayingText, 0);
            audioBannerGrid.Children.Add(_audioPlayingText);

            _stopAudioBtn = new Button
            {
                Content = isAr ? "إيقاف الصوت ⏹" : "Stop Audio ⏹",
                Background = Brushes.White,
                Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentRed),
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(14, 4, 14, 4),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            _stopAudioBtn.Click += (s, e) => _audioNotifier.Stop();
            Grid.SetColumn(_stopAudioBtn, 1);
            audioBannerGrid.Children.Add(_stopAudioBtn);

            _audioPlayingBanner.Child = audioBannerGrid;
            Grid.SetRow(_audioPlayingBanner, 1);
            rootGrid.Children.Add(_audioPlayingBanner);

            // 3. Date & Navigation Bar
            rootGrid.Children.Add(CreateDateBar(isDark, isAr));

            // 4. Next Prayer Countdown Card
            rootGrid.Children.Add(CreateCountdownCard(isDark, isAr));

            // 5. Prayer Times Schedule List
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(24, 6, 24, 18)
            };
            _prayerListPanel = new StackPanel();
            scrollViewer.Content = _prayerListPanel;
            Grid.SetRow(scrollViewer, 4);
            rootGrid.Children.Add(scrollViewer);

            mainBorder.Child = rootGrid;
            Content = mainBorder;
        }

        private UIElement CreateIntegratedHeader(bool isDark, bool isAr)
        {
            var headerBar = new Border
            {
                Height = 50,
                Background = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkHeaderBg : UIThemeHelper.LightHeaderBg),
                BorderBrush = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkBorder : UIThemeHelper.LightBorder),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(16, 0, 6, 0)
            };

            // Window drag on title bar
            headerBar.MouseDown += (s, e) =>
            {
                if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                {
                    if (e.ClickCount == 2)
                    {
                        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                    }
                    else
                    {
                        DragMove();
                    }
                }
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Left: Logo & App Title
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Middle: Actions
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Right: Window Buttons

            // Left: Crescent & Title
            var titleStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var crescentText = new TextBlock
            {
                Text = "🌙",
                FontSize = 20,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            var titleText = new TextBlock
            {
                Text = isAr ? "أوقات الصلاة (جعفري)" : "Pray Times (Jafari)",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentGold),
                VerticalAlignment = VerticalAlignment.Center
            };
            titleStack.Children.Add(crescentText);
            titleStack.Children.Add(titleText);
            Grid.SetColumn(titleStack, 0);
            grid.Children.Add(titleStack);

            // Middle Action Bar
            var actionStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = isAr ? System.Windows.HorizontalAlignment.Left : System.Windows.HorizontalAlignment.Right,
                Margin = new Thickness(16, 0, 16, 0)
            };

            // Quick Location Preset Dropdown
            _quickLocationCombo = new ComboBox
            {
                Width = 250,
                Height = 32,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = isAr ? "اختيار المدينة السريع" : "Quick city preset"
            };
            UIThemeHelper.StyleComboBox(_quickLocationCombo, isDark);

            foreach (var p in _settings.Presets)
            {
                _quickLocationCombo.Items.Add(p);
            }
            int activePresetIdx = 0;
            for (int i = 0; i < _settings.Presets.Count; i++)
            {
                if (Math.Abs(_settings.Presets[i].Latitude - _settings.Location.Latitude) < 0.001 &&
                    Math.Abs(_settings.Presets[i].Longitude - _settings.Location.Longitude) < 0.001)
                {
                    activePresetIdx = i;
                    break;
                }
            }
            _quickLocationCombo.SelectedIndex = activePresetIdx;
            _quickLocationCombo.SelectionChanged += (s, e) =>
            {
                var sel = _quickLocationCombo.SelectedItem as LocationPreset;
                if (sel != null)
                {
                    _settings.Location = sel;
                    _settings.Save();
                    _todayTimesResult = null;
                    RecalculateAndRefreshUI();
                }
            };
            actionStack.Children.Add(_quickLocationCombo);

            // Mute Button
            _muteBtn = CreateHeaderIconButton(_settings.IsMuted ? "🔇" : "🔊", isAr ? "كتم / إلغاء الكتم" : "Mute / Unmute", isDark);
            _muteBtn.Click += (s, e) =>
            {
                _settings.IsMuted = !_settings.IsMuted;
                _settings.Save();
                _audioNotifier.UpdateSettings(_settings);
                _muteBtn.Content = _settings.IsMuted ? "🔇" : "🔊";
                _trayManager.UpdateMuteMenuItem();
            };
            actionStack.Children.Add(_muteBtn);

            // Theme Toggle Button
            _themeBtn = CreateHeaderIconButton(isDark ? "☀️" : "🌙", isAr ? "تغيير المظهر" : "Toggle Theme", isDark);
            _themeBtn.Click += (s, e) =>
            {
                _settings.Theme = (_settings.Theme == "Dark") ? "Light" : "Dark";
                _settings.Save();
                RefreshThemeAndLanguage();
            };
            actionStack.Children.Add(_themeBtn);

            // Language Toggle Button
            _langBtn = CreateHeaderIconButton(isAr ? "EN" : "عربي", isAr ? "English" : "العربية", isDark);
            _langBtn.Click += (s, e) =>
            {
                _settings.Language = (_settings.Language == "ar") ? "en" : "ar";
                _settings.Save();
                RefreshThemeAndLanguage();
            };
            actionStack.Children.Add(_langBtn);

            // Settings Button
            var settingsBtn = CreateHeaderIconButton("⚙️", isAr ? "الإعدادات" : "Settings", isDark);
            settingsBtn.Click += (s, e) => OpenSettingsDialog();
            actionStack.Children.Add(settingsBtn);

            Grid.SetColumn(actionStack, 1);
            grid.Children.Add(actionStack);

            // Right: Integrated Window Control Buttons (Minimize, Maximize, Close)
            var winBtnStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            Color textCol = isDark ? UIThemeHelper.DarkTextPrimary : UIThemeHelper.LightTextPrimary;
            Color btnHoverBg = isDark ? Color.FromRgb(51, 65, 85) : Color.FromRgb(226, 232, 240);

            // Minimize
            var minBtn = CreateWinButton("—", "تصغير / Minimize", textCol, btnHoverBg, 42, 34);
            minBtn.Click += (s, e) => WindowState = WindowState.Minimized;
            winBtnStack.Children.Add(minBtn);

            // Maximize / Restore
            var maxBtn = CreateWinButton(WindowState == WindowState.Maximized ? "🗗" : "🗖", "تكبير / Maximize", textCol, btnHoverBg, 42, 34);
            maxBtn.Click += (s, e) =>
            {
                if (WindowState == WindowState.Maximized)
                {
                    WindowState = WindowState.Normal;
                    maxBtn.Content = "🗖";
                }
                else
                {
                    WindowState = WindowState.Maximized;
                    maxBtn.Content = "🗗";
                }
            };
            StateChanged += (s, e) =>
            {
                maxBtn.Content = WindowState == WindowState.Maximized ? "🗗" : "🗖";
            };
            winBtnStack.Children.Add(maxBtn);

            // Close
            var closeBtn = CreateWinButton("✕", "إغلاق / Close", textCol, UIThemeHelper.AccentRed, 46, 34, true);
            closeBtn.Click += (s, e) => Close();
            winBtnStack.Children.Add(closeBtn);

            Grid.SetColumn(winBtnStack, 2);
            grid.Children.Add(winBtnStack);

            headerBar.Child = grid;
            Grid.SetRow(headerBar, 0);
            return headerBar;
        }

        private Button CreateWinButton(string symbol, string tooltip, Color textCol, Color hoverBg, double w, double h, bool isClose = false)
        {
            var btn = new Button
            {
                Content = symbol,
                Width = w,
                Height = h,
                Background = Brushes.Transparent,
                Foreground = UIThemeHelper.GetBrush(textCol),
                BorderThickness = new Thickness(0),
                FontSize = 13,
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = tooltip,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };

            btn.MouseEnter += (s, e) =>
            {
                btn.Background = UIThemeHelper.GetBrush(hoverBg);
                if (isClose) btn.Foreground = Brushes.White;
            };
            btn.MouseLeave += (s, e) =>
            {
                btn.Background = Brushes.Transparent;
                btn.Foreground = UIThemeHelper.GetBrush(textCol);
            };

            return btn;
        }

        private Button CreateHeaderIconButton(string iconText, string tooltip, bool isDark)
        {
            var btn = new Button
            {
                Content = iconText,
                Width = 38,
                Height = 32,
                Margin = new Thickness(3, 0, 3, 0),
                Background = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(51, 65, 85) : Color.FromRgb(226, 232, 240)),
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextPrimary : UIThemeHelper.LightTextPrimary),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = tooltip,
                FontSize = 13,
                FontWeight = FontWeights.Bold
            };
            return btn;
        }

        private UIElement CreateDateBar(bool isDark, bool isAr)
        {
            var border = new Border
            {
                Margin = new Thickness(24, 14, 24, 6)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Dates & City badge
            var dateStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            var hijriRow = new StackPanel { Orientation = Orientation.Horizontal };
            _hijriDateText = new TextBlock
            {
                Text = "",
                FontSize = 19,
                FontWeight = FontWeights.Bold,
                Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentGold)
            };
            hijriRow.Children.Add(_hijriDateText);

            _locationBadgeText = new TextBlock
            {
                Text = "",
                FontSize = 12,
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextSecondary : UIThemeHelper.LightTextSecondary),
                Margin = new Thickness(14, 4, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            hijriRow.Children.Add(_locationBadgeText);
            dateStack.Children.Add(hijriRow);

            _gregorianDateText = new TextBlock
            {
                Text = "",
                FontSize = 13,
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextSecondary : UIThemeHelper.LightTextSecondary),
                Margin = new Thickness(0, 3, 0, 0)
            };
            dateStack.Children.Add(_gregorianDateText);

            Grid.SetColumn(dateStack, 0);
            grid.Children.Add(dateStack);

            // Date Navigation Buttons (< Today > DatePicker)
            var navStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            var prevBtn = new Button
            {
                Content = isAr ? "◀ السابق" : "◀ Prev",
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(4, 0, 4, 0),
                Background = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkCardBg : UIThemeHelper.LightCardBg),
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextPrimary : UIThemeHelper.LightTextPrimary),
                BorderBrush = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkBorder : UIThemeHelper.LightBorder),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 12
            };
            prevBtn.Click += (s, e) =>
            {
                _viewDate = _viewDate.AddDays(-1);
                RecalculateAndRefreshUI();
            };
            navStack.Children.Add(prevBtn);

            var todayBtn = new Button
            {
                Content = isAr ? "اليوم" : "Today",
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(4, 0, 4, 0),
                Background = UIThemeHelper.GetBrush(UIThemeHelper.AccentEmerald),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 12
            };
            todayBtn.Click += (s, e) =>
            {
                _viewDate = DateTime.Today;
                RecalculateAndRefreshUI();
            };
            navStack.Children.Add(todayBtn);

            var nextBtn = new Button
            {
                Content = isAr ? "التالي ▶" : "Next ▶",
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(4, 0, 4, 0),
                Background = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkCardBg : UIThemeHelper.LightCardBg),
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextPrimary : UIThemeHelper.LightTextPrimary),
                BorderBrush = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkBorder : UIThemeHelper.LightBorder),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 12
            };
            nextBtn.Click += (s, e) =>
            {
                _viewDate = _viewDate.AddDays(1);
                RecalculateAndRefreshUI();
            };
            navStack.Children.Add(nextBtn);

            // Date picker
            _datePicker = new DatePicker
            {
                SelectedDate = _viewDate,
                Width = 120,
                Height = 30,
                Margin = new Thickness(6, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            _datePicker.SelectedDateChanged += (s, e) =>
            {
                if (_datePicker.SelectedDate.HasValue && _datePicker.SelectedDate.Value.Date != _viewDate.Date)
                {
                    _viewDate = _datePicker.SelectedDate.Value.Date;
                    RecalculateAndRefreshUI();
                }
            };
            navStack.Children.Add(_datePicker);

            Grid.SetColumn(navStack, 1);
            grid.Children.Add(navStack);

            border.Child = grid;
            Grid.SetRow(border, 2);
            return border;
        }

        private UIElement CreateCountdownCard(bool isDark, bool isAr)
        {
            var cardBorder = new Border
            {
                Margin = new Thickness(24, 8, 24, 14),
                Padding = new Thickness(20),
                CornerRadius = new CornerRadius(12),
                Background = new LinearGradientBrush(
                    isDark ? UIThemeHelper.DarkCardBg : Color.FromRgb(241, 245, 249),
                    isDark ? UIThemeHelper.DarkHeaderBg : Color.FromRgb(226, 232, 240),
                    new Point(0, 0), new Point(1, 1)),
                BorderBrush = UIThemeHelper.GetBrush(UIThemeHelper.AccentEmerald),
                BorderThickness = new Thickness(1.5)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Next Prayer info
            var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            _currentPeriodText = new TextBlock
            {
                Text = "",
                FontSize = 13,
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextSecondary : UIThemeHelper.LightTextSecondary),
                Margin = new Thickness(0, 0, 0, 4)
            };
            infoStack.Children.Add(_currentPeriodText);

            _nextPrayerTitleText = new TextBlock
            {
                Text = isAr ? "الموعد القادم: ..." : "Next: ...",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextPrimary : UIThemeHelper.LightTextPrimary)
            };
            infoStack.Children.Add(_nextPrayerTitleText);

            _nextPrayerTimeText = new TextBlock
            {
                Text = "",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentGold),
                Margin = new Thickness(0, 4, 0, 8)
            };
            infoStack.Children.Add(_nextPrayerTimeText);

            // Progress bar
            _periodProgressBar = new ProgressBar
            {
                Height = 8,
                Maximum = 100,
                Value = 0,
                Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentEmerald),
                Background = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(51, 65, 85) : Color.FromRgb(203, 213, 225)),
                BorderThickness = new Thickness(0)
            };
            infoStack.Children.Add(_periodProgressBar);

            Grid.SetColumn(infoStack, 0);
            grid.Children.Add(infoStack);

            // Live Countdown Timer
            var timerStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = isAr ? System.Windows.HorizontalAlignment.Left : System.Windows.HorizontalAlignment.Right
            };

            var remLabel = new TextBlock
            {
                Text = isAr ? "الوقت المتبقي:" : "Time Remaining:",
                FontSize = 12,
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextSecondary : UIThemeHelper.LightTextSecondary),
                HorizontalAlignment = isAr ? System.Windows.HorizontalAlignment.Left : System.Windows.HorizontalAlignment.Right
            };
            timerStack.Children.Add(remLabel);

            _countdownText = new TextBlock
            {
                Text = "00:00:00",
                FontSize = 38,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Consolas, Segoe UI"),
                Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentEmerald),
                HorizontalAlignment = isAr ? System.Windows.HorizontalAlignment.Left : System.Windows.HorizontalAlignment.Right
            };
            timerStack.Children.Add(_countdownText);

            Grid.SetColumn(timerStack, 1);
            grid.Children.Add(timerStack);

            cardBorder.Child = grid;
            Grid.SetRow(cardBorder, 3);
            return cardBorder;
        }

        public void RecalculateAndRefreshUI()
        {
            bool isAr = _settings.Language == "ar";
            bool isDark = _settings.Theme == "Dark";

            // Update dates
            var hijriDate = HijriCalendarHelper.GetHijriDate(_viewDate, _settings.HijriOffset);
            _hijriDateText.Text = hijriDate.Format(isAr ? "ar" : "en");

            string gregFormat = isAr ? "dddd، d MMMM yyyy" : "dddd, MMMM d, yyyy";
            var culture = isAr ? new System.Globalization.CultureInfo("ar-SA") : new System.Globalization.CultureInfo("en-US");
            _gregorianDateText.Text = _viewDate.ToString(gregFormat, culture);

            _locationBadgeText.Text = string.Format("📍 {0} ({1:0.00}°N, {2:0.00}°E, UTC{3:+0.0;-0.0})",
                _settings.Location.Name, _settings.Location.Latitude, _settings.Location.Longitude, _settings.Location.Timezone);

            if (_datePicker != null)
            {
                _datePicker.SelectedDate = _viewDate;
            }

            // Astronomical calculation for viewed date
            var calcParams = _settings.ToCalculationParameters();
            var offsets = _settings.GetOffsets();
            _viewTimesResult = PrayerCalculator.Calculate(
                _viewDate,
                _settings.Location.Latitude,
                _settings.Location.Longitude,
                _settings.Location.Timezone,
                _settings.Location.Elevation,
                calcParams,
                offsets);

            // Ensure today's calculation is always ready for next prayer countdown, alarms, and tray
            EnsureTodayTimesCalculated();

            // Rebuild Prayer Schedule List for viewed date
            BuildPrayerScheduleCards(isDark, isAr);

            // Update Countdown and highlights (always based on today!)
            UpdateCountdownAndHighlights();

            // Arm or update Windows sleep wake timer for next enabled prayer/reminder
            RefreshWakeSchedule();
        }

        private void EnsureTodayTimesCalculated()
        {
            DateTime today = DateTime.Today;
            if (_todayTimesResult == null || _todayDate != today)
            {
                var calcParams = _settings.ToCalculationParameters();
                var offsets = _settings.GetOffsets();
                _todayTimesResult = PrayerCalculator.Calculate(
                    today,
                    _settings.Location.Latitude,
                    _settings.Location.Longitude,
                    _settings.Location.Timezone,
                    _settings.Location.Elevation,
                    calcParams,
                    offsets);
                _todayDate = today;
            }
        }

        private void BuildPrayerScheduleCards(bool isDark, bool isAr)
        {
            _prayerListPanel.Children.Clear();

            string[] events = new string[] { "Imsak", "Fajr", "Sunrise", "Dhuhr", "Asr", "Sunset", "Maghrib", "Isha", "Midnight" };
            Dictionary<string, string> namesAr = new Dictionary<string, string>
            {
                { "Imsak", "الإمساك" }, { "Fajr", "الفجر (الصباح)" }, { "Sunrise", "الشروق" },
                { "Dhuhr", "الظهر" }, { "Asr", "العصر" }, { "Sunset", "الغروب" },
                { "Maghrib", "المغرب" }, { "Isha", "العشاء" }, { "Midnight", "منتصف الليل الشرعي" }
            };
            Dictionary<string, string> icons = new Dictionary<string, string>
            {
                { "Imsak", "⏳" }, { "Fajr", "🌄" }, { "Sunrise", "🌅" },
                { "Dhuhr", "☀️" }, { "Asr", "🌤️" }, { "Sunset", "🌇" },
                { "Maghrib", "🌆" }, { "Isha", "🌙" }, { "Midnight", "🌌" }
            };

            var timesDict = _viewTimesResult.ToDictionary();

            foreach (var ev in events)
            {
                PrayerEventConfig cfg;
                if (!_settings.Prayers.TryGetValue(ev, out cfg))
                {
                    cfg = new PrayerEventConfig();
                }

                // If hidden, skip showing in main schedule
                if (!cfg.Visible) continue;

                DateTime time = timesDict[ev];
                string timeFormatted = _settings.TimeFormat24H ? time.ToString("HH:mm") : time.ToString("hh:mm tt");

                var itemBorder = new Border
                {
                    Margin = new Thickness(0, 4, 0, 4),
                    Padding = new Thickness(16, 12, 16, 12),
                    CornerRadius = new CornerRadius(8),
                    Background = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkCardBg : UIThemeHelper.LightCardBg),
                    BorderBrush = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkBorder : UIThemeHelper.LightBorder),
                    BorderThickness = new Thickness(1),
                    Tag = ev
                };

                var itemGrid = new Grid();
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(45) });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(210) });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Icon
                var iconTb = new TextBlock
                {
                    Text = icons[ev],
                    FontSize = 20,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(iconTb, 0);
                itemGrid.Children.Add(iconTb);

                // Name (Arabic + English) - High Contrast White in Dark Mode!
                var nameStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                var mainName = new TextBlock
                {
                    Text = isAr ? namesAr[ev] : ev,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextPrimary : UIThemeHelper.LightTextPrimary)
                };
                var subName = new TextBlock
                {
                    Text = isAr ? ev : namesAr[ev],
                    FontSize = 11,
                    Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextSecondary : UIThemeHelper.LightTextSecondary)
                };
                nameStack.Children.Add(mainName);
                nameStack.Children.Add(subName);
                Grid.SetColumn(nameStack, 1);
                itemGrid.Children.Add(nameStack);

                // Badges (Offset, Alert Mode, Reminder)
                var badgeStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

                if (cfg.OffsetMinutes != 0)
                {
                    badgeStack.Children.Add(CreateBadge((cfg.OffsetMinutes > 0 ? "+" : "") + cfg.OffsetMinutes + "m", Color.FromRgb(59, 130, 246), isDark));
                }

                // Alert Mode Badge
                string modeLabel = "📢 Adhan";
                Color modeColor = UIThemeHelper.AccentEmerald;
                if (cfg.NotificationMode == "Off") { modeLabel = "🔕 Off"; modeColor = Color.FromRgb(148, 163, 184); }
                else if (cfg.NotificationMode == "Toast") { modeLabel = "💬 Toast"; modeColor = Color.FromRgb(192, 132, 252); }
                else if (cfg.NotificationMode == "Chime") { modeLabel = "🔔 Chime"; modeColor = UIThemeHelper.AccentGold; }
                badgeStack.Children.Add(CreateBadge(modeLabel, modeColor, isDark));

                if (cfg.ReminderMinutes > 0)
                {
                    badgeStack.Children.Add(CreateBadge("⏰ -" + cfg.ReminderMinutes + "m", Color.FromRgb(251, 146, 60), isDark));
                }

                Grid.SetColumn(badgeStack, 2);
                itemGrid.Children.Add(badgeStack);

                // Time Display - High Contrast White in Dark Mode!
                var timeTb = new TextBlock
                {
                    Text = timeFormatted,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    FontFamily = new FontFamily("Consolas, Segoe UI"),
                    Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextPrimary : UIThemeHelper.LightTextPrimary),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(timeTb, 3);
                itemGrid.Children.Add(timeTb);

                itemBorder.Child = itemGrid;
                _prayerListPanel.Children.Add(itemBorder);
            }
        }

        private Border CreateBadge(string text, Color color, bool isDark)
        {
            return new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(50, color.R, color.G, color.B)),
                BorderBrush = new SolidColorBrush(color),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(4, 0, 4, 0),
                Child = new TextBlock
                {
                    Text = text,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(color)
                }
            };
        }

        private void ClockTimer_Tick(object sender, EventArgs e)
        {
            if (DateTime.Today != _todayDate)
            {
                _todayTimesResult = null;
                EnsureTodayTimesCalculated();
                RefreshWakeSchedule();
            }
            UpdateCountdownAndHighlights();
            CheckAndTriggerAlarms();
        }

        private void UpdateCountdownAndHighlights()
        {
            EnsureTodayTimesCalculated();
            if (_todayTimesResult == null) return;

            DateTime now = DateTime.Now;
            bool isAr = _settings.Language == "ar";
            bool isDark = _settings.Theme == "Dark";

            string[] allEvents = new string[] { "Imsak", "Fajr", "Sunrise", "Dhuhr", "Asr", "Sunset", "Maghrib", "Isha", "Midnight" };
            Dictionary<string, string> namesAr = new Dictionary<string, string>
            {
                { "Imsak", "الإمساك" }, { "Fajr", "الفجر" }, { "Sunrise", "الشروق" },
                { "Dhuhr", "الظهر" }, { "Asr", "العصر" }, { "Sunset", "الغروب" },
                { "Maghrib", "المغرب" }, { "Isha", "العشاء" }, { "Midnight", "منتصف الليل" }
            };

            var timesDict = _todayTimesResult.ToDictionary();

            string nextPrayerName = "";
            DateTime nextPrayerTime = DateTime.MaxValue;
            string prevPrayerName = "";
            DateTime prevPrayerTime = DateTime.MinValue;

            foreach (var ev in allEvents)
            {
                PrayerEventConfig cfg;
                if (_settings.Prayers.TryGetValue(ev, out cfg) && !cfg.Visible) continue;

                DateTime t = timesDict[ev];
                if (t > now && t < nextPrayerTime)
                {
                    nextPrayerTime = t;
                    nextPrayerName = ev;
                }
                if (t <= now && t > prevPrayerTime)
                {
                    prevPrayerTime = t;
                    prevPrayerName = ev;
                }
            }

            if (string.IsNullOrEmpty(nextPrayerName))
            {
                var tomorrow = DateTime.Today.AddDays(1);
                var pParams = _settings.ToCalculationParameters();
                var offsets = _settings.GetOffsets();
                var tomRes = PrayerCalculator.Calculate(tomorrow, _settings.Location.Latitude, _settings.Location.Longitude, _settings.Location.Timezone, _settings.Location.Elevation, pParams, offsets);
                var tomDict = tomRes.ToDictionary();

                foreach (var ev in allEvents)
                {
                    PrayerEventConfig cfg;
                    if (_settings.Prayers.TryGetValue(ev, out cfg) && !cfg.Visible) continue;
                    nextPrayerName = ev;
                    nextPrayerTime = tomDict[ev];
                    break;
                }
            }

            // Update countdown card (always fixed to the true upcoming prayer of today!)
            if (!string.IsNullOrEmpty(nextPrayerName))
            {
                TimeSpan diff = nextPrayerTime - now;
                if (diff.TotalSeconds < 0) diff = TimeSpan.Zero;

                string countdownStr = string.Format("{0:00}:{1:00}:{2:00}", (int)diff.TotalHours, diff.Minutes, diff.Seconds);
                _countdownText.Text = countdownStr;

                string pNameAr = namesAr.ContainsKey(nextPrayerName) ? namesAr[nextPrayerName] : nextPrayerName;
                bool isActualPrayer = PrayerCalculator.IsActualPrayer(nextPrayerName);
                if (isActualPrayer)
                {
                    _nextPrayerTitleText.Text = isAr ? string.Format("الصلاة القادمة: {0}", pNameAr) : string.Format("Next Prayer: {0}", nextPrayerName);
                }
                else
                {
                    _nextPrayerTitleText.Text = isAr ? string.Format("الموعد القادم: {0}", pNameAr) : string.Format("Next Event: {0}", nextPrayerName);
                }

                string formattedTime = _settings.TimeFormat24H ? nextPrayerTime.ToString("HH:mm:ss") : nextPrayerTime.ToString("hh:mm:ss tt");
                _nextPrayerTimeText.Text = formattedTime;

                string shortTime = _settings.TimeFormat24H
                    ? nextPrayerTime.ToString("HH:mm")
                    : (isAr ? nextPrayerTime.ToString("hh:mm tt", new System.Globalization.CultureInfo("ar-SA")) : nextPrayerTime.ToString("hh:mm tt"));

                if (!string.IsNullOrEmpty(prevPrayerName))
                {
                    string prevAr = namesAr.ContainsKey(prevPrayerName) ? namesAr[prevPrayerName] : prevPrayerName;
                    _currentPeriodText.Text = isAr ? string.Format("الفترة الحالية: {0}", prevAr) : string.Format("Current Period: {0}", prevPrayerName);

                    double totalWindowSec = (nextPrayerTime - prevPrayerTime).TotalSeconds;
                    double elapsedSec = (now - prevPrayerTime).TotalSeconds;
                    if (totalWindowSec > 0)
                    {
                        double pct = Math.Max(0, Math.Min(100, (elapsedSec / totalWindowSec) * 100));
                        _periodProgressBar.Value = pct;
                    }
                }
                else
                {
                    _currentPeriodText.Text = "";
                    _periodProgressBar.Value = 0;
                }

                // Tray tooltip & menu ALWAYS stay anchored to the true upcoming prayer & countdown!
                _trayManager.UpdateTrayTooltipAndMenu(pNameAr, nextPrayerName, countdownStr, shortTime);
            }

            // Highlight items in prayer schedule list ONLY when viewing today
            bool isViewingToday = (_viewDate.Date == DateTime.Today);
            foreach (UIElement child in _prayerListPanel.Children)
            {
                var border = child as Border;
                if (border == null) continue;
                string ev = border.Tag as string;
                if (string.IsNullOrEmpty(ev)) continue;

                if (isViewingToday && ev.Equals(nextPrayerName, StringComparison.OrdinalIgnoreCase))
                {
                    // Next upcoming: Gold border highlight
                    border.BorderBrush = UIThemeHelper.GetBrush(UIThemeHelper.AccentGold);
                    border.BorderThickness = new Thickness(2);
                    border.Background = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(45, 55, 72) : Color.FromRgb(254, 243, 199));
                }
                else if (isViewingToday && ev.Equals(prevPrayerName, StringComparison.OrdinalIgnoreCase))
                {
                    // Current period: Emerald border highlight
                    border.BorderBrush = UIThemeHelper.GetBrush(UIThemeHelper.AccentEmerald);
                    border.BorderThickness = new Thickness(1.5);
                    border.Background = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(30, 41, 59) : Color.FromRgb(241, 245, 249));
                }
                else
                {
                    border.BorderBrush = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkBorder : UIThemeHelper.LightBorder);
                    border.BorderThickness = new Thickness(1);
                    border.Background = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkCardBg : UIThemeHelper.LightCardBg);
                }
            }
        }

        private void CheckAndTriggerAlarms()
        {
            EnsureTodayTimesCalculated();
            if (_todayTimesResult == null) return;

            DateTime now = DateTime.Now;

            string[] events = new string[] { "Imsak", "Fajr", "Sunrise", "Dhuhr", "Asr", "Sunset", "Maghrib", "Isha", "Midnight" };
            Dictionary<string, string> namesAr = new Dictionary<string, string>
            {
                { "Imsak", "الإمساك" }, { "Fajr", "الفجر" }, { "Sunrise", "الشروق" },
                { "Dhuhr", "الظهر" }, { "Asr", "العصر" }, { "Sunset", "الغروب" },
                { "Maghrib", "المغرب" }, { "Isha", "العشاء" }, { "Midnight", "منتصف الليل" }
            };

            var timesDict = _todayTimesResult.ToDictionary();
            bool anyFired = false;

            foreach (var ev in events)
            {
                PrayerEventConfig cfg;
                if (!_settings.Prayers.TryGetValue(ev, out cfg)) continue;

                DateTime pTime = timesDict[ev];
                string todayStr = now.ToString("yyyyMMdd");

                // 1. Check exact prayer time (Must never fire before prayer time arrives)
                double secPast = (now - pTime).TotalSeconds;
                if (secPast >= 0 && secPast <= 60)
                {
                    string alertKey = "PRAYER_" + ev + "_" + todayStr;
                    if (!_firedAlerts.Contains(alertKey))
                    {
                        _firedAlerts.Add(alertKey);
                        anyFired = true;
                        string tStr = _settings.TimeFormat24H ? pTime.ToString("HH:mm") : pTime.ToString("hh:mm tt");
                        _audioNotifier.TriggerPrayerNotification(namesAr[ev], ev, tStr, cfg.NotificationMode, cfg.SoundId, cfg.CustomAudioPath);
                    }
                }

                // 2. Check pre-prayer reminder (Must never fire before reminder time arrives)
                if (cfg.ReminderMinutes > 0)
                {
                    DateTime reminderTime = pTime.AddMinutes(-cfg.ReminderMinutes);
                    double remSecPast = (now - reminderTime).TotalSeconds;
                    if (remSecPast >= 0 && remSecPast <= 60)
                    {
                        string remKey = "REM_" + ev + "_" + todayStr;
                        if (!_firedAlerts.Contains(remKey))
                        {
                            _firedAlerts.Add(remKey);
                            anyFired = true;
                            _audioNotifier.TriggerReminderNotification(namesAr[ev], ev, cfg.ReminderMinutes, cfg.ReminderSoundId, cfg.CustomReminderAudioPath);
                        }
                    }
                }
            }

            if (anyFired)
            {
                RefreshWakeSchedule();
            }
        }

        private void OnSystemWakeOrResume()
        {
            if (DateTime.Today != _todayDate)
            {
                _todayTimesResult = null;
                EnsureTodayTimesCalculated();
            }
            UpdateCountdownAndHighlights();
            CheckAndTriggerAlarms();
            RefreshWakeSchedule();
        }

        private void RefreshWakeSchedule()
        {
            if (_sleepWakeManager == null) return;
            DateTime nextWake = GetNextAlertTimeLocal();
            _sleepWakeManager.ScheduleWake(nextWake);
        }

        private DateTime GetNextAlertTimeLocal()
        {
            if (!_settings.WakeFromSleepForAlerts) return DateTime.MaxValue;

            EnsureTodayTimesCalculated();
            DateTime now = DateTime.Now;
            DateTime earliest = DateTime.MaxValue;
            string[] events = new string[] { "Imsak", "Fajr", "Sunrise", "Dhuhr", "Asr", "Sunset", "Maghrib", "Isha", "Midnight" };

            for (int dayOffset = 0; dayOffset <= 1; dayOffset++)
            {
                DateTime targetDate = DateTime.Today.AddDays(dayOffset);
                PrayerTimesResult res = (dayOffset == 0)
                    ? _todayTimesResult
                    : PrayerCalculator.Calculate(
                        targetDate,
                        _settings.Location.Latitude,
                        _settings.Location.Longitude,
                        _settings.Location.Timezone,
                        _settings.Location.Elevation,
                        _settings.ToCalculationParameters(),
                        _settings.GetOffsets());

                if (res == null) continue;
                var timesDict = res.ToDictionary();
                string dateKey = targetDate.ToString("yyyyMMdd");

                foreach (var ev in events)
                {
                    PrayerEventConfig cfg;
                    if (!_settings.Prayers.TryGetValue(ev, out cfg)) continue;

                    DateTime pTime = timesDict[ev];

                    if (!string.Equals(cfg.NotificationMode, "Off", StringComparison.OrdinalIgnoreCase))
                    {
                        string alertKey = "PRAYER_" + ev + "_" + dateKey;
                        if (!_firedAlerts.Contains(alertKey) && pTime > now && pTime < earliest)
                        {
                            earliest = pTime;
                        }
                    }

                    if (cfg.ReminderMinutes > 0)
                    {
                        DateTime remTime = pTime.AddMinutes(-cfg.ReminderMinutes);
                        string remKey = "REM_" + ev + "_" + dateKey;
                        if (!_firedAlerts.Contains(remKey) && remTime > now && remTime < earliest)
                        {
                            earliest = remTime;
                        }
                    }
                }

                if (earliest != DateTime.MaxValue) break;
            }

            return earliest;
        }

        public void OpenSettingsDialog()
        {
            var dlg = new SettingsWindow(_settings, _audioNotifier, () =>
            {
                RefreshThemeAndLanguage();
                _todayTimesResult = null;
                RecalculateAndRefreshUI();
            });
            dlg.Owner = this;
            dlg.ShowDialog();
        }

        private void RefreshThemeAndLanguage()
        {
            bool isAr = _settings.Language == "ar";
            bool isDark = _settings.Theme == "Dark";

            FlowDirection = isAr ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            Background = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkBg : UIThemeHelper.LightBg);
            Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextPrimary : UIThemeHelper.LightTextPrimary);

            _muteBtn.Content = _settings.IsMuted ? "🔇" : "🔊";
            _themeBtn.Content = isDark ? "☀️" : "🌙";
            _langBtn.Content = isAr ? "EN" : "عربي";

            SetupWindowUI();
            RecalculateAndRefreshUI();
            _trayManager.BuildContextMenu();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_settings.MinimizeToTrayOnClose)
            {
                e.Cancel = true;
                Hide();
                if (!_settings.HasShownTrayMinimizeHint)
                {
                    _settings.HasShownTrayMinimizeHint = true;
                    _settings.Save();
                    if (_trayManager != null && _trayManager.NotifyIcon != null)
                    {
                        bool isAr = _settings.Language == "ar";
                        _trayManager.NotifyIcon.ShowBalloonTip(3000,
                            isAr ? "أوقات الصلاة تعمل في الخلفية" : "Shia Prayer Times",
                            isAr ? "تم تصغير البرنامج إلى شريط المهام بجانب الساعة." : "Application minimized to system tray.",
                            System.Windows.Forms.ToolTipIcon.Info);
                    }
                }
            }
            else
            {
                _clockTimer.Stop();
                _audioNotifier.Stop();
                if (_sleepWakeManager != null) _sleepWakeManager.Dispose();
                _trayManager.Dispose();
                base.OnClosing(e);
            }
        }
    }
}

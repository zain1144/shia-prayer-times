using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;

namespace ShiaPrayerTimes
{
    public class SettingsWindow : Window
    {
        private AppSettings _settings;
        private AudioNotifier _audioNotifier;
        private Action _onSettingsSaved;

        // UI Controls for Prayer Matrix
        private Dictionary<string, System.Windows.Controls.CheckBox> _visChecks = new Dictionary<string, System.Windows.Controls.CheckBox>();
        private Dictionary<string, System.Windows.Controls.Slider> _offsetSliders = new Dictionary<string, System.Windows.Controls.Slider>();
        private Dictionary<string, TextBlock> _offsetLabels = new Dictionary<string, TextBlock>();
        private Dictionary<string, System.Windows.Controls.ComboBox> _notifCombos = new Dictionary<string, System.Windows.Controls.ComboBox>();
        private Dictionary<string, System.Windows.Controls.ComboBox> _reminderCombos = new Dictionary<string, System.Windows.Controls.ComboBox>();
        private Dictionary<string, System.Windows.Controls.ComboBox> _soundSelectCombos = new Dictionary<string, System.Windows.Controls.ComboBox>();

        // Calculation Controls
        private System.Windows.Controls.ComboBox _methodCombo;
        private System.Windows.Controls.TextBox _fajrAngleBox;
        private System.Windows.Controls.TextBox _maghribAngleBox;
        private System.Windows.Controls.TextBox _ishaAngleBox;
        private System.Windows.Controls.ComboBox _midnightCombo;
        private System.Windows.Controls.Slider _imsakSlider;
        private TextBlock _imsakLabel;
        private System.Windows.Controls.ComboBox _highLatsCombo;
        private System.Windows.Controls.ComboBox _timeFormatCombo;
        private StackPanel _customAnglesPanel;

        // Location Controls
        private System.Windows.Controls.ComboBox _presetCombo;
        private System.Windows.Controls.TextBox _locNameBox;
        private System.Windows.Controls.TextBox _latBox;
        private System.Windows.Controls.TextBox _lonBox;
        private System.Windows.Controls.TextBox _tzBox;
        private System.Windows.Controls.TextBox _elvBox;
        private System.Windows.Controls.Slider _hijriSlider;
        private TextBlock _hijriLabel;
        private TextBlock _hijriPreviewLabel;
        private TextBlock _detectStatusLabel;

        // Audio & System Controls
        private System.Windows.Controls.Slider _volSlider;
        private TextBlock _volLabel;
        private System.Windows.Controls.CheckBox _muteCheck;
        private StackPanel _adhanLibraryListPanel;
        private System.Windows.Controls.CheckBox _startupCheck;
        private System.Windows.Controls.CheckBox _closeToTrayCheck;
        private System.Windows.Controls.ComboBox _themeCombo;
        private System.Windows.Controls.ComboBox _langCombo;

        public SettingsWindow(AppSettings settings, AudioNotifier audioNotifier, Action onSettingsSaved)
        {
            _settings = settings;
            _audioNotifier = audioNotifier;
            _onSettingsSaved = onSettingsSaved;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            bool isAr = _settings.Language == "ar";
            bool isDark = _settings.Theme == "Dark";

            Title = isAr ? "إعدادات أوقات الصلاة" : "Prayer Times Settings";
            Width = 960;
            Height = 700;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = isAr ? System.Windows.FlowDirection.RightToLeft : System.Windows.FlowDirection.LeftToRight;

            WindowStyle = WindowStyle.None;

            Background = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkBg : UIThemeHelper.LightBg);
            Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextPrimary : UIThemeHelper.LightTextPrimary);

            var mainBorder = new Border
            {
                BorderBrush = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkBorder : UIThemeHelper.LightBorder),
                BorderThickness = new Thickness(1),
                Background = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkBg : UIThemeHelper.LightBg)
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Integrated Title Bar
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Tabs
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64) }); // Bottom Save/Cancel

            // Integrated Title Bar (drag & close)
            var titleBar = UIThemeHelper.CreateIntegratedTitleBar(
                this,
                isAr ? "إعدادات أوقات الصلاة" : "Prayer Times Settings",
                isAr ? "المعايير الجعفرية والتخصيص المتقدم" : "Jafari Standards & Advanced Customization",
                isDark,
                isAr,
                null,
                false);
            Grid.SetRow(titleBar, 0);
            mainGrid.Children.Add(titleBar);

            var tabControl = new System.Windows.Controls.TabControl
            {
                Margin = new Thickness(16, 10, 16, 6),
                Background = Brushes.Transparent,
                BorderBrush = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkBorder : UIThemeHelper.LightBorder),
                BorderThickness = new Thickness(1)
            };

            // 1. Prayer Settings Matrix Tab
            var tabMatrix = CreateTabItem(isAr ? " 📋 مصفوفة الصلوات والتنبيهات " : " 📋 Prayers & Alerts Matrix ", CreateMatrixTabContent(isDark, isAr), isDark);
            tabControl.Items.Add(tabMatrix);

            // 2. Astronomy & Calculation Tab
            var tabCalc = CreateTabItem(isAr ? " 🔭 الحسابات الفلكية " : " 🔭 Astronomical Calculation ", CreateCalcTabContent(isDark, isAr), isDark);
            tabControl.Items.Add(tabCalc);

            // 3. Location & Calendar Tab
            var tabLocation = CreateTabItem(isAr ? " 📍 الموقع والتقويم " : " 📍 Location & Calendar ", CreateLocationTabContent(isDark, isAr), isDark);
            tabControl.Items.Add(tabLocation);

            // 4. Audio & System Tab
            var tabSystem = CreateTabItem(isAr ? " 🔊 الأصوات والمكتبة " : " 🔊 Audio Library & System ", CreateAudioSystemTabContent(isDark, isAr), isDark);
            tabControl.Items.Add(tabSystem);

            Grid.SetRow(tabControl, 1);
            mainGrid.Children.Add(tabControl);

            // Bottom Buttons (Save & Cancel)
            var buttonPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new Thickness(16, 8, 16, 14)
            };

            var saveBtn = new System.Windows.Controls.Button
            {
                Content = isAr ? "حفظ التغييرات ✓" : "Save Changes ✓",
                Width = 150,
                Height = 38,
                Margin = new Thickness(8, 0, 0, 0),
                Background = UIThemeHelper.GetBrush(UIThemeHelper.AccentEmerald),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 13
            };
            saveBtn.Click += SaveBtn_Click;

            var cancelBtn = new System.Windows.Controls.Button
            {
                Content = isAr ? "إلغاء" : "Cancel",
                Width = 100,
                Height = 38,
                Margin = new Thickness(8, 0, 0, 0),
                Background = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(51, 65, 85) : Color.FromRgb(203, 213, 225)),
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextPrimary : UIThemeHelper.LightTextPrimary),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 13
            };
            cancelBtn.Click += (s, e) => { _audioNotifier.Stop(); Close(); };

            buttonPanel.Children.Add(cancelBtn);
            buttonPanel.Children.Add(saveBtn);

            Grid.SetRow(buttonPanel, 2);
            mainGrid.Children.Add(buttonPanel);

            mainBorder.Child = mainGrid;
            Content = mainBorder;
        }

        private TabItem CreateTabItem(string header, UIElement content, bool isDark)
        {
            var tab = new TabItem
            {
                Header = header,
                Content = content,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextPrimary : UIThemeHelper.LightTextPrimary),
                Background = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkCardBg : UIThemeHelper.LightCardBg),
                Padding = new Thickness(14, 8, 14, 8)
            };
            return tab;
        }

        private UIElement CreateMatrixTabContent(bool isDark, bool isAr)
        {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var panel = new StackPanel { Margin = new Thickness(16) };

            var introText = new TextBlock
            {
                Text = isAr ?
                    "حدد لكل وقت صلاة إمكانية العرض في الجدول، والضبط بالدقائق (-15..+15)، ونوع التنبيه، والتذكير المسبق، والأذان المخصص من مكتبتك:" :
                    "Configure visibility, offsets (-15 to +15 min), notification mode, pre-prayer reminder, and dedicated Adhan sound per prayer:",
                Margin = new Thickness(0, 0, 0, 16),
                FontSize = 13,
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextSecondary : UIThemeHelper.LightTextSecondary),
                TextWrapping = TextWrapping.Wrap
            };
            panel.Children.Add(introText);

            // Table Header
            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(165) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(135) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            headerGrid.Children.Add(MakeHeaderLabel(isAr ? "الوقت" : "Event", 0));
            headerGrid.Children.Add(MakeHeaderLabel(isAr ? "عرض" : "Show", 1));
            headerGrid.Children.Add(MakeHeaderLabel(isAr ? "الضبط (دقيقة)" : "Offset (min)", 2));
            headerGrid.Children.Add(MakeHeaderLabel(isAr ? "التنبيه" : "Alert Mode", 3));
            headerGrid.Children.Add(MakeHeaderLabel(isAr ? "تذكير مسبق" : "Reminder", 4));
            headerGrid.Children.Add(MakeHeaderLabel(isAr ? "الصوت المخصص" : "Sound Choice", 5));

            panel.Children.Add(headerGrid);

            panel.Children.Add(new Border
            {
                Height = 1,
                Background = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkBorder : UIThemeHelper.LightBorder),
                Margin = new Thickness(0, 0, 0, 8)
            });

            string[] events = new string[] { "Imsak", "Fajr", "Sunrise", "Dhuhr", "Asr", "Sunset", "Maghrib", "Isha", "Midnight" };
            Dictionary<string, string> namesAr = new Dictionary<string, string>
            {
                { "Imsak", "الإمساك" }, { "Fajr", "الفجر" }, { "Sunrise", "الشروق" },
                { "Dhuhr", "الظهر" }, { "Asr", "العصر" }, { "Sunset", "الغروب" },
                { "Maghrib", "المغرب" }, { "Isha", "العشاء" }, { "Midnight", "منتصف الليل" }
            };

            foreach (var ev in events)
            {
                PrayerEventConfig cfg;
                if (!_settings.Prayers.TryGetValue(ev, out cfg))
                {
                    cfg = new PrayerEventConfig();
                }

                var rowGrid = new Grid { Margin = new Thickness(0, 6, 0, 6) };
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(165) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(135) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Event Name with High Contrast Pure White
                var nameBlock = new TextBlock
                {
                    Text = isAr ? string.Format("{0} ({1})", namesAr[ev], ev) : string.Format("{0} ({1})", ev, namesAr[ev]),
                    FontWeight = FontWeights.Bold,
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextPrimary : UIThemeHelper.LightTextPrimary)
                };
                Grid.SetColumn(nameBlock, 0);
                rowGrid.Children.Add(nameBlock);

                // Show/Hide Toggle
                var visCheck = new System.Windows.Controls.CheckBox
                {
                    IsChecked = cfg.Visible,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                    Content = isAr ? "تفعيل" : "Enabled"
                };
                UIThemeHelper.StyleCheckBox(visCheck, isDark);
                _visChecks[ev] = visCheck;
                Grid.SetColumn(visCheck, 1);
                rowGrid.Children.Add(visCheck);

                // Offset Slider (-15 to +15)
                var offsetPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                var slider = new System.Windows.Controls.Slider
                {
                    Minimum = -15,
                    Maximum = 15,
                    Value = cfg.OffsetMinutes,
                    Width = 100,
                    IsSnapToTickEnabled = true,
                    TickFrequency = 1,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var valText = new TextBlock
                {
                    Text = (cfg.OffsetMinutes > 0 ? "+" : "") + cfg.OffsetMinutes + (isAr ? " د" : " min"),
                    Width = 55,
                    Margin = new Thickness(6, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextPrimary : UIThemeHelper.LightTextPrimary)
                };
                slider.ValueChanged += (s, e) =>
                {
                    int val = (int)Math.Round(slider.Value);
                    valText.Text = (val > 0 ? "+" : "") + val + (isAr ? " د" : " min");
                };
                _offsetSliders[ev] = slider;
                _offsetLabels[ev] = valText;
                offsetPanel.Children.Add(slider);
                offsetPanel.Children.Add(valText);
                Grid.SetColumn(offsetPanel, 2);
                rowGrid.Children.Add(offsetPanel);

                // Notification Mode
                var notifCombo = new System.Windows.Controls.ComboBox
                {
                    Width = 135,
                    Height = 30,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left
                };
                UIThemeHelper.StyleComboBox(notifCombo, isDark);
                notifCombo.Items.Add(new ComboBoxItem { Content = isAr ? "صامت (إيقاف)" : "Silent (Off)", Tag = "Off" });
                notifCombo.Items.Add(new ComboBoxItem { Content = isAr ? "إشعار مرئي فقط" : "Visual Toast Only", Tag = "Toast" });
                notifCombo.Items.Add(new ComboBoxItem { Content = isAr ? "نغمة تنبيه / Chime" : "Short Chime / Beep", Tag = "Chime" });
                notifCombo.Items.Add(new ComboBoxItem { Content = isAr ? "أذان كامل / Adhan" : "Full Adhan Audio", Tag = "Adhan" });

                string mode = cfg.NotificationMode;
                int selIdx = 3;
                if (mode == "Off") selIdx = 0;
                else if (mode == "Toast") selIdx = 1;
                else if (mode == "Chime") selIdx = 2;
                notifCombo.SelectedIndex = selIdx;
                _notifCombos[ev] = notifCombo;
                Grid.SetColumn(notifCombo, 3);
                rowGrid.Children.Add(notifCombo);

                // Reminder Dropdown
                var remCombo = new System.Windows.Controls.ComboBox
                {
                    Width = 120,
                    Height = 30,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left
                };
                UIThemeHelper.StyleComboBox(remCombo, isDark);
                remCombo.Items.Add(new ComboBoxItem { Content = isAr ? "إيقاف" : "Off", Tag = 0 });
                remCombo.Items.Add(new ComboBoxItem { Content = isAr ? "قبل 5 دقائق" : "5 min before", Tag = 5 });
                remCombo.Items.Add(new ComboBoxItem { Content = isAr ? "قبل 10 دقائق" : "10 min before", Tag = 10 });
                remCombo.Items.Add(new ComboBoxItem { Content = isAr ? "قبل 15 دقيقة" : "15 min before", Tag = 15 });
                remCombo.Items.Add(new ComboBoxItem { Content = isAr ? "قبل 20 دقيقة" : "20 min before", Tag = 20 });
                remCombo.Items.Add(new ComboBoxItem { Content = isAr ? "قبل 30 دقيقة" : "30 min before", Tag = 30 });

                int remIdx = 0;
                int[] remVals = new int[] { 0, 5, 10, 15, 20, 30 };
                for (int ri = 0; ri < remVals.Length; ri++)
                {
                    if (remVals[ri] == cfg.ReminderMinutes) { remIdx = ri; break; }
                }
                remCombo.SelectedIndex = remIdx;
                _reminderCombos[ev] = remCombo;
                Grid.SetColumn(remCombo, 4);
                rowGrid.Children.Add(remCombo);

                // Sound Selection Dropdown (Adhan Library Choice) + Play Button
                var soundPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                var soundCombo = new System.Windows.Controls.ComboBox
                {
                    Width = 155,
                    Height = 30,
                    VerticalAlignment = VerticalAlignment.Center
                };
                UIThemeHelper.StyleComboBox(soundCombo, isDark);

                // Populate with library sounds
                PopulateSoundCombo(soundCombo, cfg.SoundId);
                _soundSelectCombos[ev] = soundCombo;

                var playBtn = new System.Windows.Controls.Button
                {
                    Content = "▶",
                    Width = 32,
                    Height = 30,
                    Margin = new Thickness(6, 0, 0, 0),
                    Background = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(51, 65, 85) : Color.FromRgb(226, 232, 240)),
                    Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentGold),
                    BorderThickness = new Thickness(0),
                    FontWeight = FontWeights.Bold,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    ToolTip = isAr ? "تجربة الصوت المختار" : "Test Play Selected Sound"
                };
                string evKey = ev;
                playBtn.Click += (s, e) =>
                {
                    if (_audioNotifier.IsPlaying)
                    {
                        _audioNotifier.Stop();
                        playBtn.Content = "▶";
                    }
                    else
                    {
                        var selItem = soundCombo.SelectedItem as ComboBoxItem;
                        string sId = selItem != null ? (selItem.Tag as string) : "default";
                        _audioNotifier.PlayAdhan(evKey, sId, cfg.CustomAudioPath);
                        playBtn.Content = "⏹";
                    }
                };

                soundPanel.Children.Add(soundCombo);
                soundPanel.Children.Add(playBtn);
                Grid.SetColumn(soundPanel, 5);
                rowGrid.Children.Add(soundPanel);

                panel.Children.Add(rowGrid);
            }

            scroll.Content = panel;
            return scroll;
        }

        private void PopulateSoundCombo(System.Windows.Controls.ComboBox combo, string selectedSoundId)
        {
            combo.Items.Clear();
            int selIdx = 0;
            int idx = 0;

            if (_settings.AdhanLibrary != null)
            {
                foreach (var sound in _settings.AdhanLibrary)
                {
                    var item = new ComboBoxItem { Content = sound.Name, Tag = sound.Id };
                    combo.Items.Add(item);
                    if (sound.Id == selectedSoundId)
                    {
                        selIdx = idx;
                    }
                    idx++;
                }
            }

            if (combo.Items.Count > 0)
            {
                combo.SelectedIndex = selIdx;
            }
        }

        private TextBlock MakeHeaderLabel(string text, int col)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentGold)
            };
            Grid.SetColumn(tb, col);
            return tb;
        }

        private UIElement CreateCalcTabContent(bool isDark, bool isAr)
        {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var panel = new StackPanel { Margin = new Thickness(24) };

            panel.Children.Add(UIThemeHelper.CreateLabel(
                isAr ? "طريقة الحساب الفلكي المعتمدة (المعايير الجعفرية):" : "Astronomical Calculation Method (Jafari Standards):",
                isDark, true, 14));

            _methodCombo = new System.Windows.Controls.ComboBox { Width = 600, Height = 34, Margin = new Thickness(0, 8, 0, 0), HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
            UIThemeHelper.StyleComboBox(_methodCombo, isDark);

            // 1. Tubalani (explicitly requested by user)
            _methodCombo.Items.Add(new ComboBoxItem
            {
                Content = isAr ? "تقويم السيد هاشم التوبلاني - Sayyid Hashim al-Tubalani [فجر: 18°، مغرب: 4°، عشاء: 14°]" :
                                 "Sayyid Hashim al-Tubalani Calendar [Fajr: 18°, Maghrib: 4°, Isha: 14°]",
                Tag = "Tubalani"
            });
            // 2. Leva Institute, Qom
            _methodCombo.Items.Add(new ComboBoxItem
            {
                Content = isAr ? "معهد لواء للأبحاث، قم - Shia Ithna-Ashari (Leva Institute, Qom) [فجر: 16°، مغرب: 4°، عشاء: 14°]" :
                                 "Shia Ithna-Ashari, Leva Institute, Qom [Fajr: 16°, Maghrib: 4°, Isha: 14°]",
                Tag = "Jafari"
            });
            // 3. Tehran University
            _methodCombo.Items.Add(new ComboBoxItem
            {
                Content = isAr ? "معهد الجيوفيزياء بجامعة طهران - Tehran University [فجر: 17.7°، مغرب: 4.5°، عشاء: 14°]" :
                                 "Institute of Geophysics, University of Tehran [Fajr: 17.7°, Maghrib: 4.5°, Isha: 14°]",
                Tag = "Tehran"
            });
            // 4. Custom
            _methodCombo.Items.Add(new ComboBoxItem
            {
                Content = isAr ? "زوايا مخصصة يدوياً - Custom Astronomical Angles" : "Custom Astronomical Angles",
                Tag = "Custom"
            });

            int selMethodIdx = 0;
            if (_settings.CalculationMethodName == "Jafari") selMethodIdx = 1;
            else if (_settings.CalculationMethodName == "Tehran") selMethodIdx = 2;
            else if (_settings.CalculationMethodName == "Custom") selMethodIdx = 3;
            _methodCombo.SelectedIndex = selMethodIdx;

            _methodCombo.SelectionChanged += (s, e) =>
            {
                bool isCustom = _methodCombo.SelectedIndex == 3;
                if (_customAnglesPanel != null)
                {
                    _customAnglesPanel.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
                }
            };
            panel.Children.Add(_methodCombo);

            // Custom Angles Panel
            _customAnglesPanel = new StackPanel
            {
                Margin = new Thickness(0, 12, 0, 12),
                Visibility = (_settings.CalculationMethodName == "Custom") ? Visibility.Visible : Visibility.Collapsed
            };
            var anglesBorder = new Border
            {
                Background = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkCardBg : Color.FromRgb(241, 245, 249)),
                Padding = new Thickness(16),
                CornerRadius = new CornerRadius(8),
                BorderBrush = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkBorder : UIThemeHelper.LightBorder),
                BorderThickness = new Thickness(1)
            };
            var anglesGrid = new Grid();
            anglesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            anglesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            anglesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });

            var fStack = new StackPanel();
            fStack.Children.Add(UIThemeHelper.CreateLabel(isAr ? "زاوية الفجر (°):" : "Fajr Angle (°):", isDark, false, 12));
            _fajrAngleBox = new System.Windows.Controls.TextBox { Text = _settings.CustomFajrAngle.ToString("0.0"), Width = 90, Height = 28, Margin = new Thickness(0, 4, 0, 0), HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
            UIThemeHelper.StyleTextBox(_fajrAngleBox, isDark);
            fStack.Children.Add(_fajrAngleBox);
            Grid.SetColumn(fStack, 0);
            anglesGrid.Children.Add(fStack);

            var mStack = new StackPanel();
            mStack.Children.Add(UIThemeHelper.CreateLabel(isAr ? "زاوية المغرب (°):" : "Maghrib Angle (°):", isDark, false, 12));
            _maghribAngleBox = new System.Windows.Controls.TextBox { Text = _settings.CustomMaghribAngle.ToString("0.0"), Width = 90, Height = 28, Margin = new Thickness(0, 4, 0, 0), HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
            UIThemeHelper.StyleTextBox(_maghribAngleBox, isDark);
            mStack.Children.Add(_maghribAngleBox);
            Grid.SetColumn(mStack, 1);
            anglesGrid.Children.Add(mStack);

            var iStack = new StackPanel();
            iStack.Children.Add(UIThemeHelper.CreateLabel(isAr ? "زاوية العشاء (°):" : "Isha Angle (°):", isDark, false, 12));
            _ishaAngleBox = new System.Windows.Controls.TextBox { Text = _settings.CustomIshaAngle.ToString("0.0"), Width = 90, Height = 28, Margin = new Thickness(0, 4, 0, 0), HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
            UIThemeHelper.StyleTextBox(_ishaAngleBox, isDark);
            iStack.Children.Add(_ishaAngleBox);
            Grid.SetColumn(iStack, 2);
            anglesGrid.Children.Add(iStack);

            anglesBorder.Child = anglesGrid;
            _customAnglesPanel.Children.Add(anglesBorder);
            panel.Children.Add(_customAnglesPanel);

            // Midnight Calculation Mode
            panel.Children.Add(new Border { Height = 14 });
            panel.Children.Add(UIThemeHelper.CreateLabel(isAr ? "طريقة حساب منتصف الليل الشرعي:" : "Midnight Calculation Method:", isDark, true, 13));
            _midnightCombo = new System.Windows.Controls.ComboBox { Width = 600, Height = 34, Margin = new Thickness(0, 6, 0, 0), HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
            UIThemeHelper.StyleComboBox(_midnightCombo, isDark);
            _midnightCombo.Items.Add(new ComboBoxItem
            {
                Content = isAr ? "الفقه الجعفري: منتصف ما بين الغروب والفجر (المعيار المعتمد)" :
                                 "Jafari: Midpoint between Sunset and Fajr (Standard Jafari)",
                Tag = "Jafari"
            });
            _midnightCombo.Items.Add(new ComboBoxItem
            {
                Content = isAr ? "الفلكي العام: منتصف ما بين الغروب والشروق" :
                                 "Standard: Midpoint between Sunset and Sunrise",
                Tag = "Standard"
            });
            _midnightCombo.SelectedIndex = (_settings.MidnightModeName == "Standard") ? 1 : 0;
            panel.Children.Add(_midnightCombo);

            // Imsak setting
            panel.Children.Add(new Border { Height = 14 });
            panel.Children.Add(UIThemeHelper.CreateLabel(isAr ? "وقت الإمساك (دقائق قبل الفجر):" : "Imsak Offset (Minutes before Fajr):", isDark, true, 13));
            var imsakPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
            _imsakSlider = new System.Windows.Controls.Slider
            {
                Minimum = 5,
                Maximum = 30,
                Value = _settings.ImsakMinutesBeforeFajr,
                Width = 200,
                TickFrequency = 1,
                IsSnapToTickEnabled = true,
                VerticalAlignment = VerticalAlignment.Center
            };
            _imsakLabel = new TextBlock
            {
                Text = _settings.ImsakMinutesBeforeFajr + (isAr ? " دقيقة قبل الفجر" : " min before Fajr"),
                Margin = new Thickness(12, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold,
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextPrimary : UIThemeHelper.LightTextPrimary)
            };
            _imsakSlider.ValueChanged += (s, e) =>
            {
                int iv = (int)Math.Round(_imsakSlider.Value);
                _imsakLabel.Text = iv + (isAr ? " دقيقة قبل الفجر" : " min before Fajr");
            };
            imsakPanel.Children.Add(_imsakSlider);
            imsakPanel.Children.Add(_imsakLabel);
            panel.Children.Add(imsakPanel);

            // Higher Latitudes
            panel.Children.Add(new Border { Height = 14 });
            panel.Children.Add(UIThemeHelper.CreateLabel(isAr ? "تعديل خطوط العرض العليا (للمناطق القطبية):" : "Higher Latitudes Adjustment:", isDark, true, 13));
            _highLatsCombo = new System.Windows.Controls.ComboBox { Width = 350, Height = 32, Margin = new Thickness(0, 6, 0, 0), HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
            UIThemeHelper.StyleComboBox(_highLatsCombo, isDark);
            _highLatsCombo.Items.Add(new ComboBoxItem { Content = isAr ? "الزاوية النسبية (Angle-Based - مستحسن)" : "Angle-Based (Recommended)", Tag = "AngleBased" });
            _highLatsCombo.Items.Add(new ComboBoxItem { Content = isAr ? "نصف الليل (Night Middle)" : "Night Middle", Tag = "NightMiddle" });
            _highLatsCombo.Items.Add(new ComboBoxItem { Content = isAr ? "سبع الليل (One Seventh)" : "One Seventh", Tag = "OneSeventh" });
            _highLatsCombo.Items.Add(new ComboBoxItem { Content = isAr ? "بدون تعديل (None)" : "None", Tag = "None" });
            int hlIdx = 0;
            if (_settings.HighLatsName == "NightMiddle") hlIdx = 1;
            else if (_settings.HighLatsName == "OneSeventh") hlIdx = 2;
            else if (_settings.HighLatsName == "None") hlIdx = 3;
            _highLatsCombo.SelectedIndex = hlIdx;
            panel.Children.Add(_highLatsCombo);

            // Time Format
            panel.Children.Add(new Border { Height = 14 });
            panel.Children.Add(UIThemeHelper.CreateLabel(isAr ? "صيغة عرض الوقت:" : "Time Format:", isDark, true, 13));
            _timeFormatCombo = new System.Windows.Controls.ComboBox { Width = 250, Height = 32, Margin = new Thickness(0, 6, 0, 0), HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
            UIThemeHelper.StyleComboBox(_timeFormatCombo, isDark);
            _timeFormatCombo.Items.Add(new ComboBoxItem { Content = isAr ? "نظام 12 ساعة (ص / م)" : "12-hour format (AM/PM)", Tag = false });
            _timeFormatCombo.Items.Add(new ComboBoxItem { Content = isAr ? "نظام 24 ساعة (عسكري)" : "24-hour format", Tag = true });
            _timeFormatCombo.SelectedIndex = _settings.TimeFormat24H ? 1 : 0;
            panel.Children.Add(_timeFormatCombo);

            scroll.Content = panel;
            return scroll;
        }

        private UIElement CreateLocationTabContent(bool isDark, bool isAr)
        {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var panel = new StackPanel { Margin = new Thickness(24) };

            // Automatic Location Detection Button Header
            var autoLocBorder = new Border
            {
                Background = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(20, 30, 48) : Color.FromRgb(238, 242, 255)),
                BorderBrush = UIThemeHelper.GetBrush(UIThemeHelper.AccentEmerald),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 16)
            };

            var autoGrid = new Grid();
            autoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            autoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var autoTextStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var autoTitle = UIThemeHelper.CreateLabel(isAr ? "تحديد الموقع الجغرافي تلقائياً:" : "Auto-Detect Location:", isDark, true, 14);
            _detectStatusLabel = new TextBlock
            {
                Text = isAr ? "اضغط على الزر لتحديد موقعك الحالي وخطوط الطول والعرض تلقائياً." : "Click to detect your current coordinates automatically.",
                FontSize = 12,
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextSecondary : UIThemeHelper.LightTextSecondary),
                Margin = new Thickness(0, 2, 0, 0)
            };
            autoTextStack.Children.Add(autoTitle);
            autoTextStack.Children.Add(_detectStatusLabel);
            Grid.SetColumn(autoTextStack, 0);
            autoGrid.Children.Add(autoTextStack);

            var detectBtn = new System.Windows.Controls.Button
            {
                Content = isAr ? "📍 تحديد موقعي تلقائياً" : "📍 Auto-Detect Location",
                Height = 36,
                Padding = new Thickness(16, 0, 16, 0),
                Background = UIThemeHelper.GetBrush(UIThemeHelper.AccentEmerald),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 13
            };
            detectBtn.Click += async (s, e) =>
            {
                detectBtn.IsEnabled = false;
                _detectStatusLabel.Text = isAr ? "جاري البحث وتحديد الموقع عبر الحساسات والشبكة..." : "Detecting location...";
                _detectStatusLabel.Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentGold);

                var result = await Task.Run(() => LocationDetector.DetectLocation());

                detectBtn.IsEnabled = true;
                if (result.Success)
                {
                    _locNameBox.Text = result.Name;
                    _latBox.Text = result.Latitude.ToString("0.0000");
                    _lonBox.Text = result.Longitude.ToString("0.0000");
                    _tzBox.Text = result.Timezone.ToString("0.0");
                    _detectStatusLabel.Text = (isAr ? "تم تحديد الموقع بنجاح: " : "Location detected: ") + result.Name;
                    _detectStatusLabel.Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentEmerald);
                }
                else
                {
                    _detectStatusLabel.Text = result.ErrorMessage;
                    _detectStatusLabel.Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentRed);
                }
            };
            Grid.SetColumn(detectBtn, 1);
            autoGrid.Children.Add(detectBtn);

            autoLocBorder.Child = autoGrid;
            panel.Children.Add(autoLocBorder);

            // Preset Selection
            panel.Children.Add(UIThemeHelper.CreateLabel(
                isAr ? "اختيار مدينة مسبقة الإعداد أو إدخال إحداثيات مخصصة:" : "Select City Preset or Custom Coordinates:",
                isDark, true, 14));

            _presetCombo = new System.Windows.Controls.ComboBox { Width = 400, Height = 34, Margin = new Thickness(0, 8, 0, 12), HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
            UIThemeHelper.StyleComboBox(_presetCombo, isDark);
            foreach (var preset in _settings.Presets)
            {
                _presetCombo.Items.Add(preset);
            }
            int selIdx = 0;
            for (int i = 0; i < _settings.Presets.Count; i++)
            {
                if (Math.Abs(_settings.Presets[i].Latitude - _settings.Location.Latitude) < 0.001 &&
                    Math.Abs(_settings.Presets[i].Longitude - _settings.Location.Longitude) < 0.001)
                {
                    selIdx = i;
                    break;
                }
            }
            _presetCombo.SelectedIndex = selIdx;
            _presetCombo.SelectionChanged += (s, e) =>
            {
                var p = _presetCombo.SelectedItem as LocationPreset;
                if (p != null)
                {
                    _locNameBox.Text = p.Name;
                    _latBox.Text = p.Latitude.ToString("0.0000");
                    _lonBox.Text = p.Longitude.ToString("0.0000");
                    _tzBox.Text = p.Timezone.ToString("0.0");
                    _elvBox.Text = p.Elevation.ToString("0");
                }
            };
            panel.Children.Add(_presetCombo);

            // Coordinates Grid
            var grid = new Grid { Margin = new Thickness(0, 8, 0, 12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });

            _locNameBox = new System.Windows.Controls.TextBox { Text = _settings.Location.Name, Height = 28 };
            UIThemeHelper.StyleTextBox(_locNameBox, isDark);
            AddGridRow(grid, 0, isAr ? "اسم الموقع:" : "Location Name:", _locNameBox, isDark);

            _latBox = new System.Windows.Controls.TextBox { Text = _settings.Location.Latitude.ToString("0.0000"), Height = 28 };
            UIThemeHelper.StyleTextBox(_latBox, isDark);
            AddGridRow(grid, 1, isAr ? "خط العرض (Latitude):" : "Latitude (°):", _latBox, isDark);

            _lonBox = new System.Windows.Controls.TextBox { Text = _settings.Location.Longitude.ToString("0.0000"), Height = 28 };
            UIThemeHelper.StyleTextBox(_lonBox, isDark);
            AddGridRow(grid, 2, isAr ? "خط الطول (Longitude):" : "Longitude (°):", _lonBox, isDark);

            _tzBox = new System.Windows.Controls.TextBox { Text = _settings.Location.Timezone.ToString("0.0"), Height = 28 };
            UIThemeHelper.StyleTextBox(_tzBox, isDark);
            AddGridRow(grid, 3, isAr ? "المنطقة الزمنية (UTC):" : "Time Zone (UTC):", _tzBox, isDark);

            _elvBox = new System.Windows.Controls.TextBox { Text = _settings.Location.Elevation.ToString("0"), Height = 28 };
            UIThemeHelper.StyleTextBox(_elvBox, isDark);
            AddGridRow(grid, 4, isAr ? "الارتفاع عن البحر (م):" : "Elevation (meters):", _elvBox, isDark);

            panel.Children.Add(grid);

            // Preset Action Buttons
            var btnStack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 20) };
            var savePresetBtn = new System.Windows.Controls.Button
            {
                Content = isAr ? "حفظ كمدينة جديدة في القائمة" : "Save as New Preset",
                Height = 32,
                Padding = new Thickness(14, 0, 14, 0),
                Background = UIThemeHelper.GetBrush(UIThemeHelper.AccentEmerald),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            savePresetBtn.Click += (s, e) =>
            {
                double lat, lon, tz, elv;
                if (double.TryParse(_latBox.Text, out lat) && double.TryParse(_lonBox.Text, out lon) && double.TryParse(_tzBox.Text, out tz))
                {
                    double.TryParse(_elvBox.Text, out elv);
                    var newPreset = new LocationPreset(_locNameBox.Text, lat, lon, tz, elv);
                    _settings.Presets.Add(newPreset);
                    _presetCombo.Items.Add(newPreset);
                    _presetCombo.SelectedItem = newPreset;
                    System.Windows.MessageBox.Show(isAr ? "تم حفظ المدينة بنجاح!" : "Preset saved successfully!", Title, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            };
            btnStack.Children.Add(savePresetBtn);
            panel.Children.Add(btnStack);

            // Hijri Date Adjustment Section
            panel.Children.Add(new Border
            {
                Height = 1,
                Background = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkBorder : UIThemeHelper.LightBorder),
                Margin = new Thickness(0, 8, 0, 16)
            });

            panel.Children.Add(UIThemeHelper.CreateLabel(
                isAr ? "ضبط التقويم الهجري ومطابقة الرؤية الشرعية للهلال:" : "Hijri Calendar & Moon Sighting Adjustment:",
                isDark, true, 14));

            var hijriPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            _hijriSlider = new System.Windows.Controls.Slider
            {
                Minimum = -2,
                Maximum = 2,
                Value = _settings.HijriOffset,
                Width = 180,
                TickFrequency = 1,
                IsSnapToTickEnabled = true,
                VerticalAlignment = VerticalAlignment.Center
            };
            _hijriLabel = new TextBlock
            {
                Text = (_settings.HijriOffset > 0 ? "+" : "") + _settings.HijriOffset + (isAr ? " يوم" : " days"),
                Width = 70,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold,
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextPrimary : UIThemeHelper.LightTextPrimary)
            };
            _hijriSlider.ValueChanged += (s, e) =>
            {
                int val = (int)Math.Round(_hijriSlider.Value);
                _hijriLabel.Text = (val > 0 ? "+" : "") + val + (isAr ? " يوم" : " days");
                UpdateHijriPreview(val, isAr);
            };

            hijriPanel.Children.Add(_hijriSlider);
            hijriPanel.Children.Add(_hijriLabel);
            panel.Children.Add(hijriPanel);

            _hijriPreviewLabel = new TextBlock
            {
                Text = "",
                FontSize = 13,
                Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentGold),
                Margin = new Thickness(0, 8, 0, 0),
                FontWeight = FontWeights.SemiBold
            };
            UpdateHijriPreview(_settings.HijriOffset, isAr);
            panel.Children.Add(_hijriPreviewLabel);

            scroll.Content = panel;
            return scroll;
        }

        private void UpdateHijriPreview(int offset, bool isAr)
        {
            if (_hijriPreviewLabel != null)
            {
                var hd = HijriCalendarHelper.GetHijriDate(DateTime.Today, offset);
                _hijriPreviewLabel.Text = (isAr ? "معاينة تاريخ اليوم الهجري بعد الضبط: " : "Preview of today's Hijri date: ") + hd.Format(isAr ? "ar" : "en");
            }
        }

        private void AddGridRow(Grid grid, int row, string label, UIElement control, bool isDark)
        {
            var tb = UIThemeHelper.CreateLabel(label, isDark, false, 13);
            Grid.SetRow(tb, row);
            Grid.SetColumn(tb, 0);
            grid.Children.Add(tb);

            Grid.SetRow(control, row);
            Grid.SetColumn(control, 1);
            grid.Children.Add(control);
        }

        private UIElement CreateAudioSystemTabContent(bool isDark, bool isAr)
        {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var panel = new StackPanel { Margin = new Thickness(24) };

            // Master Volume
            panel.Children.Add(UIThemeHelper.CreateLabel(isAr ? "مستوى الصوت العام:" : "Master Volume:", isDark, true, 14));

            var volPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            _volSlider = new System.Windows.Controls.Slider
            {
                Minimum = 0,
                Maximum = 100,
                Value = _settings.Volume,
                Width = 240,
                VerticalAlignment = VerticalAlignment.Center
            };
            _volLabel = new TextBlock
            {
                Text = _settings.Volume + "%",
                Width = 50,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold,
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextPrimary : UIThemeHelper.LightTextPrimary)
            };
            _volSlider.ValueChanged += (s, e) =>
            {
                int val = (int)Math.Round(_volSlider.Value);
                _volLabel.Text = val + "%";
            };

            _muteCheck = new System.Windows.Controls.CheckBox
            {
                Content = isAr ? "كتم كل الأصوات (Mute)" : "Mute all sounds",
                IsChecked = _settings.IsMuted,
                Margin = new Thickness(20, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            UIThemeHelper.StyleCheckBox(_muteCheck, isDark);

            volPanel.Children.Add(_volSlider);
            volPanel.Children.Add(_volLabel);
            volPanel.Children.Add(_muteCheck);
            panel.Children.Add(volPanel);

            // Adhan Library Section (Save multiple adhan sounds)
            panel.Children.Add(new Border
            {
                Height = 1,
                Background = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkBorder : UIThemeHelper.LightBorder),
                Margin = new Thickness(0, 16, 0, 16)
            });

            var libHeaderGrid = new Grid();
            libHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            libHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var libTitle = UIThemeHelper.CreateLabel(isAr ? "مكتبة أصوات الأذان المحفوظة:" : "Saved Adhan Sounds Library:", isDark, true, 14);
            Grid.SetColumn(libTitle, 0);
            libHeaderGrid.Children.Add(libTitle);

            var addSoundBtn = new System.Windows.Controls.Button
            {
                Content = isAr ? "+ إضافة أذان جديد من الجهاز" : "+ Add New Adhan Sound",
                Padding = new Thickness(12, 6, 12, 6),
                Background = UIThemeHelper.GetBrush(UIThemeHelper.AccentEmerald),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 12
            };
            addSoundBtn.Click += (s, e) =>
            {
                var dlg = new OpenFileDialog
                {
                    Filter = "Audio Files (*.mp3;*.wav)|*.mp3;*.wav|All Files (*.*)|*.*",
                    Title = isAr ? "اختر ملف صوت الأذان" : "Select Adhan Audio File"
                };
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string fName = Path.GetFileNameWithoutExtension(dlg.FileName);
                    string soundName = Microsoft.VisualBasic.Interaction.InputBox(
                        isAr ? "أدخل اسماً لهذا الأذان (مثلاً: أذان كربلاء):" : "Enter a display name for this Adhan:",
                        isAr ? "إضافة أذان جديد" : "Add Adhan",
                        fName);

                    if (!string.IsNullOrEmpty(soundName))
                    {
                        string id = Guid.NewGuid().ToString("N");
                        _settings.AdhanLibrary.Add(new AdhanSound(id, soundName, dlg.FileName, false));
                        RebuildAdhanLibraryList(isDark, isAr);
                        RefreshAllSoundCombos();
                    }
                }
            };
            Grid.SetColumn(addSoundBtn, 1);
            libHeaderGrid.Children.Add(addSoundBtn);
            panel.Children.Add(libHeaderGrid);

            _adhanLibraryListPanel = new StackPanel { Margin = new Thickness(0, 10, 0, 10) };
            RebuildAdhanLibraryList(isDark, isAr);
            panel.Children.Add(_adhanLibraryListPanel);

            // Windows Integration
            panel.Children.Add(new Border
            {
                Height = 1,
                Background = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkBorder : UIThemeHelper.LightBorder),
                Margin = new Thickness(0, 12, 0, 16)
            });

            _startupCheck = new System.Windows.Controls.CheckBox
            {
                Content = isAr ? "تشغيل التطبيق تلقائياً مع بدء إقلاع ويندوز (Launch on Windows Startup)" : "Launch automatically on Windows startup",
                IsChecked = _settings.AutoStartWithWindows,
                Margin = new Thickness(0, 0, 0, 8),
                FontWeight = FontWeights.SemiBold
            };
            UIThemeHelper.StyleCheckBox(_startupCheck, isDark);
            panel.Children.Add(_startupCheck);

            _closeToTrayCheck = new System.Windows.Controls.CheckBox
            {
                Content = isAr ? "التصغير إلى شريط المهام (Tray) عند الضغط على زر الإغلاق بدلاً من إنهائه" : "Minimize to system tray when closing instead of exiting",
                IsChecked = _settings.MinimizeToTrayOnClose,
                Margin = new Thickness(0, 0, 0, 16),
                FontWeight = FontWeights.SemiBold
            };
            UIThemeHelper.StyleCheckBox(_closeToTrayCheck, isDark);
            panel.Children.Add(_closeToTrayCheck);

            // Theme & Language
            var themeLangGrid = new Grid();
            themeLangGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            themeLangGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });

            var tStack = new StackPanel();
            tStack.Children.Add(UIThemeHelper.CreateLabel(isAr ? "المظهر:" : "Theme:", isDark, true, 13));
            _themeCombo = new System.Windows.Controls.ComboBox { Width = 160, Height = 30, Margin = new Thickness(0, 6, 0, 0), HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
            UIThemeHelper.StyleComboBox(_themeCombo, isDark);
            _themeCombo.Items.Add(new ComboBoxItem { Content = isAr ? "الوضع الليلي (Dark)" : "Dark Theme", Tag = "Dark" });
            _themeCombo.Items.Add(new ComboBoxItem { Content = isAr ? "الوضع الفاتح (Light)" : "Light Theme", Tag = "Light" });
            _themeCombo.SelectedIndex = (_settings.Theme == "Light") ? 1 : 0;
            tStack.Children.Add(_themeCombo);
            Grid.SetColumn(tStack, 0);
            themeLangGrid.Children.Add(tStack);

            var lStack = new StackPanel();
            lStack.Children.Add(UIThemeHelper.CreateLabel(isAr ? "اللغة:" : "Language:", isDark, true, 13));
            _langCombo = new System.Windows.Controls.ComboBox { Width = 160, Height = 30, Margin = new Thickness(0, 6, 0, 0), HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
            UIThemeHelper.StyleComboBox(_langCombo, isDark);
            _langCombo.Items.Add(new ComboBoxItem { Content = "العربية (Arabic)", Tag = "ar" });
            _langCombo.Items.Add(new ComboBoxItem { Content = "English", Tag = "en" });
            _langCombo.SelectedIndex = (_settings.Language == "en") ? 1 : 0;
            lStack.Children.Add(_langCombo);
            Grid.SetColumn(lStack, 1);
            themeLangGrid.Children.Add(lStack);

            panel.Children.Add(themeLangGrid);

            scroll.Content = panel;
            return scroll;
        }

        private void RebuildAdhanLibraryList(bool isDark, bool isAr)
        {
            if (_adhanLibraryListPanel == null) return;
            _adhanLibraryListPanel.Children.Clear();

            foreach (var sound in _settings.AdhanLibrary)
            {
                var sBorder = new Border
                {
                    Background = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkCardBg : Color.FromRgb(241, 245, 249)),
                    BorderBrush = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkBorder : UIThemeHelper.LightBorder),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 3, 0, 3)
                };

                var sGrid = new Grid();
                sGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                sGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                var nameBlock = new TextBlock
                {
                    Text = sound.Name,
                    FontWeight = FontWeights.Bold,
                    FontSize = 13,
                    Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextPrimary : UIThemeHelper.LightTextPrimary)
                };
                var pathBlock = new TextBlock
                {
                    Text = string.IsNullOrEmpty(sound.FilePath) ? (isAr ? "[ملف النظام المدمج]" : "[System Built-in]") : sound.FilePath,
                    FontSize = 11,
                    Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextMuted : UIThemeHelper.LightTextMuted)
                };
                infoStack.Children.Add(nameBlock);
                infoStack.Children.Add(pathBlock);
                Grid.SetColumn(infoStack, 0);
                sGrid.Children.Add(infoStack);

                var actStack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

                var playBtn = new System.Windows.Controls.Button
                {
                    Content = "▶ تجربة",
                    Width = 65,
                    Height = 28,
                    Margin = new Thickness(4, 0, 4, 0),
                    Background = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(51, 65, 85) : Color.FromRgb(226, 232, 240)),
                    Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentGold),
                    BorderThickness = new Thickness(0),
                    FontWeight = FontWeights.SemiBold,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    FontSize = 11
                };
                string fPath = sound.FilePath;
                playBtn.Click += (s, e) =>
                {
                    if (_audioNotifier.IsPlaying)
                    {
                        _audioNotifier.Stop();
                        playBtn.Content = "▶ تجربة";
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(fPath) && File.Exists(fPath))
                        {
                            _audioNotifier.PlayAudioFile(fPath, sound.Name);
                        }
                        else
                        {
                            _audioNotifier.PlayAdhan("Adhan");
                        }
                        playBtn.Content = "⏹ إيقاف";
                    }
                };
                actStack.Children.Add(playBtn);

                if (!sound.IsBuiltIn)
                {
                    var delBtn = new System.Windows.Controls.Button
                    {
                        Content = "🗑",
                        Width = 32,
                        Height = 28,
                        Background = UIThemeHelper.GetBrush(UIThemeHelper.AccentRed),
                        Foreground = Brushes.White,
                        BorderThickness = new Thickness(0),
                        Cursor = System.Windows.Input.Cursors.Hand,
                        ToolTip = isAr ? "حذف هذا الأذان" : "Delete"
                    };
                    string sid = sound.Id;
                    delBtn.Click += (s, e) =>
                    {
                        _settings.AdhanLibrary.RemoveAll(x => x.Id == sid);
                        RebuildAdhanLibraryList(isDark, isAr);
                        RefreshAllSoundCombos();
                    };
                    actStack.Children.Add(delBtn);
                }

                Grid.SetColumn(actStack, 1);
                sGrid.Children.Add(actStack);

                sBorder.Child = sGrid;
                _adhanLibraryListPanel.Children.Add(sBorder);
            }
        }

        private void RefreshAllSoundCombos()
        {
            foreach (var kvp in _soundSelectCombos)
            {
                var cb = kvp.Value;
                var selItem = cb.SelectedItem as ComboBoxItem;
                string currentId = selItem != null ? (selItem.Tag as string) : "default";
                PopulateSoundCombo(cb, currentId);
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            _audioNotifier.Stop();

            // 1. Matrix tab
            string[] events = new string[] { "Imsak", "Fajr", "Sunrise", "Dhuhr", "Asr", "Sunset", "Maghrib", "Isha", "Midnight" };
            foreach (var ev in events)
            {
                PrayerEventConfig cfg;
                if (!_settings.Prayers.TryGetValue(ev, out cfg))
                {
                    cfg = new PrayerEventConfig();
                    _settings.Prayers[ev] = cfg;
                }

                cfg.Visible = _visChecks[ev].IsChecked == true;
                cfg.OffsetMinutes = (int)Math.Round(_offsetSliders[ev].Value);

                var selNotif = _notifCombos[ev].SelectedItem as ComboBoxItem;
                cfg.NotificationMode = selNotif != null ? (selNotif.Tag as string) : "Adhan";

                var selRem = _reminderCombos[ev].SelectedItem as ComboBoxItem;
                cfg.ReminderMinutes = selRem != null ? (int)selRem.Tag : 0;

                if (_soundSelectCombos.ContainsKey(ev))
                {
                    var selSound = _soundSelectCombos[ev].SelectedItem as ComboBoxItem;
                    cfg.SoundId = selSound != null ? (selSound.Tag as string) : "default";
                }
            }

            // 2. Calculation tab
            var selMethod = _methodCombo.SelectedItem as ComboBoxItem;
            _settings.CalculationMethodName = selMethod != null ? (selMethod.Tag as string) : "Tubalani";

            double fa, ma, ia;
            if (double.TryParse(_fajrAngleBox.Text, out fa)) _settings.CustomFajrAngle = fa;
            if (double.TryParse(_maghribAngleBox.Text, out ma)) _settings.CustomMaghribAngle = ma;
            if (double.TryParse(_ishaAngleBox.Text, out ia)) _settings.CustomIshaAngle = ia;

            var selMid = _midnightCombo.SelectedItem as ComboBoxItem;
            _settings.MidnightModeName = selMid != null ? (selMid.Tag as string) : "Jafari";

            _settings.ImsakMinutesBeforeFajr = (int)Math.Round(_imsakSlider.Value);

            var selHl = _highLatsCombo.SelectedItem as ComboBoxItem;
            _settings.HighLatsName = selHl != null ? (selHl.Tag as string) : "AngleBased";

            var selTf = _timeFormatCombo.SelectedItem as ComboBoxItem;
            _settings.TimeFormat24H = selTf != null ? (bool)selTf.Tag : false;

            // 3. Location tab
            double lat, lon, tz, elv;
            if (double.TryParse(_latBox.Text, out lat) && double.TryParse(_lonBox.Text, out lon) && double.TryParse(_tzBox.Text, out tz))
            {
                double.TryParse(_elvBox.Text, out elv);
                _settings.Location.Name = _locNameBox.Text;
                _settings.Location.Latitude = lat;
                _settings.Location.Longitude = lon;
                _settings.Location.Timezone = tz;
                _settings.Location.Elevation = elv;
            }

            _settings.HijriOffset = (int)Math.Round(_hijriSlider.Value);

            // 4. Audio & System tab
            _settings.Volume = (int)Math.Round(_volSlider.Value);
            _settings.IsMuted = _muteCheck.IsChecked == true;

            _settings.AutoStartWithWindows = _startupCheck.IsChecked == true;
            _settings.MinimizeToTrayOnClose = _closeToTrayCheck.IsChecked == true;

            var selTh = _themeCombo.SelectedItem as ComboBoxItem;
            _settings.Theme = selTh != null ? (selTh.Tag as string) : "Dark";

            var selLa = _langCombo.SelectedItem as ComboBoxItem;
            _settings.Language = selLa != null ? (selLa.Tag as string) : "ar";

            _settings.Save();
            _audioNotifier.UpdateSettings(_settings);

            if (_onSettingsSaved != null)
            {
                _onSettingsSaved();
            }

            Close();
        }
    }
}

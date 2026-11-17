using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
        private Dictionary<string, System.Windows.Controls.ComboBox> _reminderSoundCombos = new Dictionary<string, System.Windows.Controls.ComboBox>(StringComparer.OrdinalIgnoreCase);
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

        // Audio & System Controls
        private System.Windows.Controls.Slider _volSlider;
        private TextBlock _volLabel;
        private System.Windows.Controls.CheckBox _muteCheck;
        private StackPanel _adhanLibraryListPanel;
        private System.Windows.Controls.CheckBox _startupCheck;
        private System.Windows.Controls.CheckBox _closeToTrayCheck;
        private System.Windows.Controls.ComboBox _themeCombo;
        private System.Windows.Controls.ComboBox _langCombo;
        private System.Windows.Controls.ComboBox _notifStyleCombo;
        private System.Windows.Controls.ComboBox _notifDurationCombo;
        private System.Windows.Controls.ComboBox _toastPositionCombo;
        private System.Windows.Controls.ComboBox _toastSizeCombo;

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
            Width = 1180;
            Height = 740;
            MinWidth = 1050;
            MinHeight = 650;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = isAr ? System.Windows.FlowDirection.RightToLeft : System.Windows.FlowDirection.LeftToRight;

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.CanResize;

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
                true);
            Grid.SetRow(titleBar, 0);
            mainGrid.Children.Add(titleBar);

            var tabControl = new System.Windows.Controls.TabControl
            {
                Margin = new Thickness(16, 10, 16, 6)
            };
            UIThemeHelper.ApplyTabControlTheme(tabControl, isDark);

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
                Content = content
            };
            UIThemeHelper.ApplyTabItemTheme(tab, isDark);
            return tab;
        }

        private UIElement CreateMatrixTabContent(bool isDark, bool isAr)
        {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
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
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(145) }); // Col 0: Event
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(65) });  // Col 1: Show
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(135) }); // Col 2: Offset
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(175) }); // Col 3: Alert Mode
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) }); // Col 4: Adhan Sound & Test
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(125) }); // Col 5: Reminder Time
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 200 }); // Col 6: Reminder Sound

            headerGrid.Children.Add(MakeHeaderLabel(isAr ? "الوقت" : "Event", 0));
            headerGrid.Children.Add(MakeHeaderLabel(isAr ? "عرض" : "Show", 1));
            headerGrid.Children.Add(MakeHeaderLabel(isAr ? "الضبط (دقيقة)" : "Offset (min)", 2));
            headerGrid.Children.Add(MakeHeaderLabel(isAr ? "نوع التنبيه" : "Alert Mode", 3));
            headerGrid.Children.Add(MakeHeaderLabel(isAr ? "صوت الأذان والتجربة (🔔)" : "Adhan Sound & Test (🔔)", 4));
            headerGrid.Children.Add(MakeHeaderLabel(isAr ? "وقت التذكير" : "Reminder Time", 5));
            headerGrid.Children.Add(MakeHeaderLabel(isAr ? "صوت التذكير (▶)" : "Reminder Sound (▶)", 6));

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
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(145) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(65) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(135) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(175) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(125) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 200 });

                // Event Name with High Contrast Pure White
                var nameBlock = new TextBlock
                {
                    Text = isAr ? string.Format("{0} ({1})", namesAr[ev], ev) : string.Format("{0} ({1})", ev, namesAr[ev]),
                    FontWeight = FontWeights.Bold,
                    FontSize = 12.5,
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
                    Width = 80,
                    IsSnapToTickEnabled = true,
                    TickFrequency = 1,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var valText = new TextBlock
                {
                    Text = (cfg.OffsetMinutes > 0 ? "+" : "") + cfg.OffsetMinutes + (isAr ? " د" : " min"),
                    Width = 48,
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
                    Width = 165,
                    Height = 30,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left
                };
                UIThemeHelper.StyleComboBox(notifCombo, isDark);
                notifCombo.Items.Add(new ComboBoxItem { Content = isAr ? "🔔 أذان كامل + إشعار" : "🔔 Full Adhan & Toast", Tag = "Adhan" });
                notifCombo.Items.Add(new ComboBoxItem { Content = isAr ? "🎵 نغمة تنبيه + إشعار" : "🎵 Chime Alert & Toast", Tag = "Chime" });
                notifCombo.Items.Add(new ComboBoxItem { Content = isAr ? "👁️ إشعار مرئي فقط (بدون صوت)" : "👁️ Visual Toast (Silent)", Tag = "Toast" });
                notifCombo.Items.Add(new ComboBoxItem { Content = isAr ? "⏹ معطل (إيقاف)" : "⏹ Disabled (Off)", Tag = "Off" });

                string mode = cfg.NotificationMode ?? "Adhan";
                int selNotifIdx = 0;
                for (int ni = 0; ni < notifCombo.Items.Count; ni++)
                {
                    var itm = notifCombo.Items[ni] as ComboBoxItem;
                    if (itm != null && string.Equals(itm.Tag as string, mode, StringComparison.OrdinalIgnoreCase))
                    {
                        selNotifIdx = ni;
                        break;
                    }
                }
                notifCombo.SelectedIndex = selNotifIdx;
                _notifCombos[ev] = notifCombo;
                Grid.SetColumn(notifCombo, 3);
                rowGrid.Children.Add(notifCombo);

                // Sound Selection Dropdown (Adhan Library Choice) + Play Button + Test Button
                var soundPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                var soundCombo = new System.Windows.Controls.ComboBox
                {
                    Width = 190,
                    Height = 30,
                    VerticalAlignment = VerticalAlignment.Center
                };
                UIThemeHelper.StyleComboBox(soundCombo, isDark);
                PopulateSoundCombo(soundCombo, cfg.SoundId);
                _soundSelectCombos[ev] = soundCombo;

                var playBtn = new System.Windows.Controls.Button
                {
                    Content = "▶",
                    Width = 28,
                    Height = 28,
                    Margin = new Thickness(4, 0, 0, 0),
                    Background = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(51, 65, 85) : Color.FromRgb(226, 232, 240)),
                    Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentGold),
                    BorderThickness = new Thickness(0),
                    FontWeight = FontWeights.Bold,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    ToolTip = isAr ? "تجربة صوت الأذان المختار" : "Test Play Selected Adhan"
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

                var testPrayerNotifBtn = new System.Windows.Controls.Button
                {
                    Content = "🔔",
                    Width = 28,
                    Height = 28,
                    Margin = new Thickness(4, 0, 0, 0),
                    Background = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(51, 65, 85) : Color.FromRgb(226, 232, 240)),
                    Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentEmerald),
                    BorderThickness = new Thickness(0),
                    FontWeight = FontWeights.Bold,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    ToolTip = isAr ? "تجربة تنبيه هذا الوقت بالكامل (إشعار وصوت)" : "Test full notification for this prayer"
                };
                string pNameAr = namesAr.ContainsKey(ev) ? namesAr[ev] : ev;
                testPrayerNotifBtn.Click += (s, e) =>
                {
                    var selMode = notifCombo.SelectedItem as ComboBoxItem;
                    string modeStr = selMode != null ? (selMode.Tag as string) : "Adhan";
                    var selSound = soundCombo.SelectedItem as ComboBoxItem;
                    string sId = selSound != null ? (selSound.Tag as string) : "default";

                    if (modeStr == "Off")
                    {
                        if (_audioNotifier.NotifyIcon != null)
                        {
                            _audioNotifier.NotifyIcon.ShowBalloonTip(4000,
                                isAr ? "تنبيه معطل" : "Alert Disabled",
                                isAr ? (string.Format("تنبيه {0}{1} مضبوط على (إيقاف) حالياً.", (PrayerCalculator.IsActualPrayer(evKey) ? "صلاة " : ""), pNameAr)) : ("Alert for " + evKey + " is set to (Off)."),
                                ToolTipIcon.Warning);
                        }
                    }
                    else
                    {
                        _audioNotifier.TriggerPrayerNotification(pNameAr, evKey, "12:00", modeStr, sId, cfg.CustomAudioPath);
                    }
                };

                soundPanel.Children.Add(soundCombo);
                soundPanel.Children.Add(playBtn);
                soundPanel.Children.Add(testPrayerNotifBtn);
                Grid.SetColumn(soundPanel, 4);
                rowGrid.Children.Add(soundPanel);

                // Reminder Minutes Dropdown
                var remCombo = new System.Windows.Controls.ComboBox
                {
                    Width = 118,
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
                for (int ri = 0; ri < remCombo.Items.Count; ri++)
                {
                    var itm = remCombo.Items[ri] as ComboBoxItem;
                    if (itm != null && (int)itm.Tag == cfg.ReminderMinutes)
                    {
                        remIdx = ri;
                        break;
                    }
                }
                remCombo.SelectedIndex = remIdx;
                _reminderCombos[ev] = remCombo;
                Grid.SetColumn(remCombo, 5);
                rowGrid.Children.Add(remCombo);

                // Independent Reminder Sound Setting & Preview (▶)
                var remSoundPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                var remSoundCombo = new System.Windows.Controls.ComboBox
                {
                    Width = 155,
                    Height = 30,
                    VerticalAlignment = VerticalAlignment.Center
                };
                UIThemeHelper.StyleComboBox(remSoundCombo, isDark);
                PopulateReminderSoundCombo(remSoundCombo, cfg.ReminderSoundId);
                _reminderSoundCombos[ev] = remSoundCombo;

                var playRemBtn = new System.Windows.Controls.Button
                {
                    Content = "▶",
                    Width = 28,
                    Height = 28,
                    Margin = new Thickness(4, 0, 0, 0),
                    Background = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(51, 65, 85) : Color.FromRgb(226, 232, 240)),
                    Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentGold),
                    BorderThickness = new Thickness(0),
                    FontWeight = FontWeights.Bold,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    ToolTip = isAr ? "تجربة صوت التذكير المختار" : "Test Play Selected Reminder Sound"
                };
                playRemBtn.Click += (s, e) =>
                {
                    if (_audioNotifier.IsPlaying)
                    {
                        _audioNotifier.Stop();
                        playRemBtn.Content = "▶";
                    }
                    else
                    {
                        var selItem = remSoundCombo.SelectedItem as ComboBoxItem;
                        string sId = selItem != null ? (selItem.Tag as string) : "chime";
                        _audioNotifier.PlayReminderSound(sId, cfg.CustomReminderAudioPath);
                        if (_audioNotifier.IsPlaying)
                        {
                            playRemBtn.Content = "⏹";
                        }
                    }
                };

                remSoundPanel.Children.Add(remSoundCombo);
                remSoundPanel.Children.Add(playRemBtn);
                Grid.SetColumn(remSoundPanel, 6);
                rowGrid.Children.Add(remSoundPanel);

                _audioNotifier.PlaybackStateChanged += (playing, track) =>
                {
                    if (!playing)
                    {
                        playBtn.Content = "▶";
                        playRemBtn.Content = "▶";
                    }
                };

                panel.Children.Add(rowGrid);
            }

            scroll.Content = panel;
            return scroll;
        }

        private void PopulateReminderSoundCombo(System.Windows.Controls.ComboBox combo, string selectedSoundId)
        {
            combo.Items.Clear();
            bool isAr = _settings.Language == "ar";

            combo.Items.Add(new ComboBoxItem { Content = isAr ? "نغمة هادئة (Chime)" : "Gentle Chime", Tag = "chime" });
            combo.Items.Add(new ComboBoxItem { Content = isAr ? "تنبيه خفيف (Beep)" : "Subtle Beep", Tag = "beep" });
            combo.Items.Add(new ComboBoxItem { Content = isAr ? "بدون صوت (مرئي)" : "Visual Only", Tag = "silent" });

            if (_settings.AdhanLibrary != null)
            {
                foreach (var sound in _settings.AdhanLibrary)
                {
                    if (string.Equals(sound.Id, "chime", StringComparison.OrdinalIgnoreCase)) continue;
                    combo.Items.Add(new ComboBoxItem { Content = sound.Name, Tag = sound.Id });
                }
            }

            int selIdx = 0;
            if (string.IsNullOrEmpty(selectedSoundId)) selectedSoundId = "chime";
            for (int i = 0; i < combo.Items.Count; i++)
            {
                var item = combo.Items[i] as ComboBoxItem;
                if (item != null && string.Equals(item.Tag as string, selectedSoundId, StringComparison.OrdinalIgnoreCase))
                {
                    selIdx = i;
                    break;
                }
            }
            combo.SelectedIndex = selIdx;
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
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
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
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
            var panel = new StackPanel { Margin = new Thickness(24) };

            // -----------------------------------------------------------------
            // Card 1: Smart City & Neighborhood Search (بدون تشغيل خدمات الموقع)
            // -----------------------------------------------------------------
            var searchCard = new Border
            {
                Background = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(20, 30, 48) : Color.FromRgb(238, 242, 255)),
                BorderBrush = UIThemeHelper.GetBrush(UIThemeHelper.AccentEmerald),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 12, 16, 14),
                Margin = new Thickness(0, 0, 0, 14)
            };

            var searchStack = new StackPanel();
            var searchHeader = UIThemeHelper.CreateLabel(
                isAr ? "🔍 البحث عن المدينة أو الحي (بدون الحاجة لخدمات الموقع أو GPS):" : "🔍 Search City or Neighborhood (No GPS or Location Services Required):",
                isDark, true, 14, UIThemeHelper.AccentEmerald);
            searchStack.Children.Add(searchHeader);

            var searchSubtext = new TextBlock
            {
                Text = isAr ?
                    "اكتب اسم مدينتك أو قريتك أو حيك وسيجلب البرنامج إحداثياتها الدقيقة فوراً:" :
                    "Type the name of your city, town, or neighborhood to fetch exact coordinates immediately:",
                FontSize = 12,
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextSecondary : UIThemeHelper.LightTextSecondary),
                Margin = new Thickness(0, 4, 0, 8),
                TextWrapping = TextWrapping.Wrap
            };
            searchStack.Children.Add(searchSubtext);

            var searchInputGrid = new Grid();
            searchInputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            searchInputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var searchBox = new System.Windows.Controls.TextBox
            {
                Height = 34,
                FontSize = 13,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(8, 0, 8, 0)
            };
            UIThemeHelper.StyleTextBox(searchBox, isDark);
            Grid.SetColumn(searchBox, 0);
            searchInputGrid.Children.Add(searchBox);

            var searchBtn = new System.Windows.Controls.Button
            {
                Content = isAr ? "🔍 بحث فوري" : "🔍 Search",
                Height = 34,
                Padding = new Thickness(16, 0, 16, 0),
                Margin = new Thickness(8, 0, 0, 0),
                Background = UIThemeHelper.GetBrush(UIThemeHelper.AccentEmerald),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 13
            };
            Grid.SetColumn(searchBtn, 1);
            searchInputGrid.Children.Add(searchBtn);
            searchStack.Children.Add(searchInputGrid);

            var searchResultsCombo = new System.Windows.Controls.ComboBox
            {
                Height = 32,
                Margin = new Thickness(0, 8, 0, 0),
                Visibility = Visibility.Collapsed
            };
            UIThemeHelper.StyleComboBox(searchResultsCombo, isDark);
            searchStack.Children.Add(searchResultsCombo);

            var searchStatusLabel = new TextBlock
            {
                Text = isAr ? "أمثلة: سيهات، تاروت، صفوى، الهفوف، سار، النجف، الكاظمية، أو أي منطقة بالعالم." : "Examples: Saihat, Tarout, Safwa, Hofuf, Saar, Najaf, Kadhimiya, London...",
                FontSize = 12,
                FontStyle = FontStyles.Italic,
                Foreground = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(148, 163, 184) : Color.FromRgb(100, 116, 139)),
                Margin = new Thickness(0, 6, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            searchStack.Children.Add(searchStatusLabel);

            // Search action
            Action doSearch = async () =>
            {
                string q = searchBox.Text.Trim();
                if (string.IsNullOrEmpty(q)) return;

                searchBtn.IsEnabled = false;
                searchStatusLabel.Text = isAr ? "جاري البحث عن الإحداثيات الدقيقة..." : "Searching for coordinates...";
                searchStatusLabel.Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentGold);

                var localMatches = new List<LocationPreset>();
                foreach (var p in _settings.Presets)
                {
                    if (p.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        localMatches.Add(p);
                    }
                }

                var webMatches = await Task.Run(() => LocationDetector.SearchLocationByName(q));

                searchBtn.IsEnabled = true;
                searchResultsCombo.Items.Clear();

                foreach (var lp in localMatches)
                {
                    searchResultsCombo.Items.Add(new ComboBoxItem
                    {
                        Content = "⭐ " + lp.Name + string.Format(" ({0:0.00}°, {1:0.00}°)", lp.Latitude, lp.Longitude),
                        Tag = new LocationDetectionResult { Name = lp.Name, Latitude = lp.Latitude, Longitude = lp.Longitude, Timezone = lp.Timezone }
                    });
                }

                foreach (var wm in webMatches)
                {
                    searchResultsCombo.Items.Add(new ComboBoxItem
                    {
                        Content = "🌐 " + wm.Name + string.Format(" ({0:0.00}°, {1:0.00}°)", wm.Latitude, wm.Longitude),
                        Tag = wm
                    });
                }

                if (searchResultsCombo.Items.Count > 0)
                {
                    searchResultsCombo.Visibility = Visibility.Visible;
                    searchResultsCombo.SelectedIndex = 0;
                    searchStatusLabel.Text = isAr ? string.Format("✓ تم العثور على {0} نتائج، تم اختيار الأولى تلقائياً ويمكنك التبديل:", searchResultsCombo.Items.Count) : string.Format("✓ Found {0} results. Top match selected:", searchResultsCombo.Items.Count);
                    searchStatusLabel.Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentEmerald);
                }
                else
                {
                    searchResultsCombo.Visibility = Visibility.Collapsed;
                    searchStatusLabel.Text = isAr ? "لم يتم العثور على نتائج. يمكنك تجربة كتابة اسم المدينة بلغة أخرى أو نسخ الإحداثيات." : "No results found. Try another spelling or paste coordinates.";
                    searchStatusLabel.Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentRed);
                }
            };

            searchBtn.Click += (s, e) => doSearch();
            searchBox.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter) doSearch();
            };

            searchResultsCombo.SelectionChanged += (s, e) =>
            {
                var sel = searchResultsCombo.SelectedItem as ComboBoxItem;
                if (sel != null && sel.Tag is LocationDetectionResult)
                {
                    var r = (LocationDetectionResult)sel.Tag;
                    _locNameBox.Text = r.Name;
                    _latBox.Text = r.Latitude.ToString("0.0000");
                    _lonBox.Text = r.Longitude.ToString("0.0000");
                    _tzBox.Text = r.Timezone.ToString("0.0");
                }
            };

            searchCard.Child = searchStack;
            panel.Children.Add(searchCard);

            // -----------------------------------------------------------------
            // Card 2: Smart Paste from Google Maps / Map Helper (دقة متناهية لمنزلك)
            // -----------------------------------------------------------------
            var pasteCard = new Border
            {
                Background = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkCardBg : Color.FromRgb(241, 245, 249)),
                BorderBrush = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkBorder : UIThemeHelper.LightBorder),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 14)
            };

            var pasteStack = new StackPanel();
            var pasteHeader = UIThemeHelper.CreateLabel(
                isAr ? "📋 لصق الإحداثيات المباشرة أو رابط خرائط جوجل (دقة بالمتر لمنزلك):" : "📋 Smart Paste Coordinates or Google Maps Link (Exact Meter Accuracy):",
                isDark, true, 13);
            pasteStack.Children.Add(pasteHeader);

            var pasteBtnRow = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 6) };

            var pasteBtn = new System.Windows.Controls.Button
            {
                Content = isAr ? "📋 لصق من الحافظة (Paste)" : "📋 Paste from Clipboard",
                Padding = new Thickness(14, 6, 14, 6),
                Background = UIThemeHelper.GetBrush(Color.FromRgb(2, 132, 199)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 12
            };

            var openMapsBtn = new System.Windows.Controls.Button
            {
                Content = isAr ? "🗺️ فتح خرائط جوجل لنسخ الإحداثيات" : "🗺️ Open Google Maps in Browser",
                Padding = new Thickness(14, 6, 14, 6),
                Margin = new Thickness(8, 0, 0, 0),
                Background = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(51, 65, 85) : Color.FromRgb(226, 232, 240)),
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextPrimary : UIThemeHelper.LightTextPrimary),
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 12
            };

            openMapsBtn.Click += (s, e) =>
            {
                try
                {
                    double curLat = _settings.Location != null ? _settings.Location.Latitude : 26.5577;
                    double curLon = _settings.Location != null ? _settings.Location.Longitude : 50.0081;
                    string url = string.Format(CultureInfo.InvariantCulture, "https://www.google.com/maps/@{0},{1},15z", curLat, curLon);
                    Process.Start(url);
                }
                catch { }
            };

            var pasteStatus = new TextBlock
            {
                Text = isAr ? "طريقة سهلة: اضغط بالزر الأيمن على منزلك في خرائط جوجل وانسخ الإحداثيات، ثم اضغط (لصق من الحافظة)." : "Tip: Right-click your house on Google Maps, copy coordinates, then click Paste here.",
                FontSize = 11.5,
                FontStyle = FontStyles.Italic,
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextSecondary : UIThemeHelper.LightTextSecondary),
                Margin = new Thickness(0, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };

            pasteBtn.Click += (s, e) =>
            {
                try
                {
                    string clip = System.Windows.Clipboard.GetText();
                    double lat, lon;
                    if (LocationDetector.TryParseCoordinates(clip, out lat, out lon))
                    {
                        _latBox.Text = lat.ToString("0.0000");
                        _lonBox.Text = lon.ToString("0.0000");
                        double tz = LocationDetector.EstimateTimezone(lon);
                        _tzBox.Text = tz.ToString("0.0");
                        _locNameBox.Text = isAr ? string.Format("موقع مخصص ({0:0.00}°, {1:0.00}°)", lat, lon) : string.Format("Custom Location ({0:0.00}°, {1:0.00}°)", lat, lon);
                        pasteStatus.Text = (isAr ? "✓ تم استخراج ولصق الإحداثيات بنجاح: " : "✓ Coordinates extracted successfully: ") + string.Format("{0:0.0000}°, {1:0.0000}°", lat, lon);
                        pasteStatus.Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentEmerald);
                    }
                    else
                    {
                        pasteStatus.Text = isAr ? "لم يتم العثور على إحداثيات صالحة في الحافظة. انسخ إحداثيات مثل: 26.5577, 50.0081" : "No valid coordinates found in clipboard. Copy numbers like: 26.5577, 50.0081";
                        pasteStatus.Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentRed);
                    }
                }
                catch (Exception ex)
                {
                    pasteStatus.Text = ex.Message;
                }
            };

            pasteBtnRow.Children.Add(pasteBtn);
            pasteBtnRow.Children.Add(openMapsBtn);
            pasteStack.Children.Add(pasteBtnRow);
            pasteStack.Children.Add(pasteStatus);
            pasteCard.Child = pasteStack;
            panel.Children.Add(pasteCard);

            // -----------------------------------------------------------------
            // Card 3: Preset Cities Dropdown (قائمة المدن المسبقة الإعداد)
            // -----------------------------------------------------------------
            panel.Children.Add(UIThemeHelper.CreateLabel(
                isAr ? "🏙️ اختيار مدينة أو بلدة من القائمة الجاهزة (أكثر من 50 مدينة):" : "🏙️ Select City from Ready Presets (50+ Cities & Towns):",
                isDark, true, 13));

            _presetCombo = new System.Windows.Controls.ComboBox { Width = 500, Height = 34, Margin = new Thickness(0, 8, 0, 14), HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
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
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
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

            // -------------------------------------------------------------
            // Dedicated Notification Types Test Section (اختبار أنواع التنبيهات)
            // -------------------------------------------------------------
            panel.Children.Add(new Border
            {
                Height = 1,
                Background = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkBorder : UIThemeHelper.LightBorder),
                Margin = new Thickness(0, 16, 0, 16)
            });

            var testCard = new Border
            {
                Background = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkCardBg : Color.FromRgb(241, 245, 249)),
                BorderBrush = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkBorder : UIThemeHelper.LightBorder),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var testCardStack = new StackPanel();

            var testTitleGrid = new Grid();
            testTitleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            testTitleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var testHeader = UIThemeHelper.CreateLabel(
                isAr ? "🔔 تجربة واختبار أنواع التنبيهات والإشعارات:" : "🔔 Test Notification & Alert Types:",
                isDark, true, 14, UIThemeHelper.AccentGold);
            Grid.SetColumn(testHeader, 0);
            testTitleGrid.Children.Add(testHeader);

            var stopAudioBtn = new System.Windows.Controls.Button
            {
                Content = isAr ? "⏹ إيقاف الصوت" : "⏹ Stop Sound",
                Padding = new Thickness(12, 4, 12, 4),
                Background = UIThemeHelper.GetBrush(UIThemeHelper.AccentRed),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 12
            };
            stopAudioBtn.Click += (s, e) => _audioNotifier.Stop();
            Grid.SetColumn(stopAudioBtn, 1);
            testTitleGrid.Children.Add(stopAudioBtn);
            testCardStack.Children.Add(testTitleGrid);

            var testSubtext = new TextBlock
            {
                Text = isAr ?
                    "اضغط على أي زر لتجربة نوع التنبيه فوراً والتأكد من ظهوره كإشعار ويندوز وسماع النغمات بوضوح:" :
                    "Click any button below to instantly trigger that notification type to verify Windows toast banners and audio output:",
                Margin = new Thickness(0, 6, 0, 12),
                FontSize = 12,
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextSecondary : UIThemeHelper.LightTextSecondary),
                TextWrapping = TextWrapping.Wrap
            };
            testCardStack.Children.Add(testSubtext);

            var testBtnWrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };

            var testStatusLabel = new TextBlock
            {
                Text = isAr ? "💡 ملاحظة: تظهر التنبيهات كإشعارات في زاوية الشاشة (Windows Banner / Balloon Tip)." : "💡 Note: Notifications appear as system toasts in the corner of your screen.",
                FontSize = 12,
                FontStyle = FontStyles.Italic,
                Foreground = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(148, 163, 184) : Color.FromRgb(100, 116, 139)),
                Margin = new Thickness(0, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };

            // 1. Visual Only
            Action syncNotifParams = () =>
            {
                if (_notifStyleCombo != null)
                {
                    var selStyle = _notifStyleCombo.SelectedItem as ComboBoxItem;
                    if (selStyle != null) _settings.NotificationStyle = selStyle.Tag as string;
                }
                if (_notifDurationCombo != null)
                {
                    var selDur = _notifDurationCombo.SelectedItem as ComboBoxItem;
                    if (selDur != null) _settings.NotificationDurationSeconds = (int)selDur.Tag;
                }
                if (_toastPositionCombo != null)
                {
                    var selPos = _toastPositionCombo.SelectedItem as ComboBoxItem;
                    if (selPos != null) _settings.ToastPosition = selPos.Tag as string;
                }
                if (_toastSizeCombo != null)
                {
                    var selSz = _toastSizeCombo.SelectedItem as ComboBoxItem;
                    if (selSz != null) _settings.ToastSize = selSz.Tag as string;
                }
            };

            var btnVisual = new System.Windows.Controls.Button
            {
                Content = isAr ? "👁️ إشعار مرئي فقط (بدون صوت)" : "👁️ Visual Toast (Silent)",
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 0, 8, 8),
                Background = UIThemeHelper.GetBrush(Color.FromRgb(2, 132, 199)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 12
            };
            btnVisual.Click += (s, e) =>
            {
                syncNotifParams();
                _audioNotifier.TestVisualNotification(isAr ? "الظهر" : "Dhuhr", "Dhuhr", isAr ? "12:00 م" : "12:00 PM");
                testStatusLabel.Text = isAr ? "✓ تم إرسال إشعار مرئي فقط بنجاح!" : "✓ Visual toast notification sent successfully!";
                testStatusLabel.Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentEmerald);
            };
            testBtnWrap.Children.Add(btnVisual);

            // 2. Chime
            var btnChime = new System.Windows.Controls.Button
            {
                Content = isAr ? "🎵 نغمة تنبيه + إشعار" : "🎵 Chime Alert & Toast",
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 0, 8, 8),
                Background = UIThemeHelper.GetBrush(Color.FromRgb(217, 119, 6)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 12
            };
            btnChime.Click += (s, e) =>
            {
                syncNotifParams();
                _audioNotifier.TestChimeNotification(isAr ? "الظهر" : "Dhuhr", "Dhuhr", isAr ? "12:00 م" : "12:00 PM");
                testStatusLabel.Text = isAr ? "✓ تم إرسال إشعار نغمة التنبيه وتشغيل الصوت بنجاح!" : "✓ Chime notification and tone sent successfully!";
                testStatusLabel.Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentEmerald);
            };
            testBtnWrap.Children.Add(btnChime);

            // 3. Adhan
            var btnAdhan = new System.Windows.Controls.Button
            {
                Content = isAr ? "📢 أذان كامل + إشعار" : "📢 Full Adhan & Toast",
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 0, 8, 8),
                Background = UIThemeHelper.GetBrush(UIThemeHelper.AccentEmerald),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 12
            };
            btnAdhan.Click += (s, e) =>
            {
                syncNotifParams();
                string testSoundId = _settings.Prayers.ContainsKey("Dhuhr") ? _settings.Prayers["Dhuhr"].SoundId : "default";
                _audioNotifier.TestAdhanNotification("Dhuhr", isAr ? "الظهر" : "Dhuhr", isAr ? "12:00 م" : "12:00 PM", testSoundId);
                testStatusLabel.Text = isAr ? "✓ جاري تشغيل الأذان وإرسال إشعار الصلاة! (يمكنك إيقافه بالزر الأحمر)" : "✓ Playing Adhan and notification banner! (You can stop it with the red button)";
                testStatusLabel.Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentEmerald);
            };
            testBtnWrap.Children.Add(btnAdhan);

            // 4. Pre-Prayer Reminder
            var btnReminder = new System.Windows.Controls.Button
            {
                Content = isAr ? "⏳ تذكير مسبق (10 دقائق)" : "⏳ Pre-Prayer Reminder (10m)",
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 0, 8, 8),
                Background = UIThemeHelper.GetBrush(Color.FromRgb(99, 102, 241)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 12
            };
            btnReminder.Click += (s, e) =>
            {
                syncNotifParams();
                _audioNotifier.TestReminderNotification(isAr ? "الظهر" : "Dhuhr", "Dhuhr", 10);
                testStatusLabel.Text = isAr ? "✓ تم إرسال إشعار التذكير المسبق (قبل 10 دقائق) بنجاح!" : "✓ Pre-prayer reminder notification sent successfully!";
                testStatusLabel.Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentEmerald);
            };
            testBtnWrap.Children.Add(btnReminder);

            testCardStack.Children.Add(testBtnWrap);
            testCardStack.Children.Add(testStatusLabel);

            // Notification Customization inside Test Card
            var notifOptionsGrid = new Grid { Margin = new Thickness(0, 12, 0, 4) };
            notifOptionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            notifOptionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            notifOptionsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            notifOptionsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Row 0, Col 0: Visual Style
            var nStyleStack = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
            nStyleStack.Children.Add(UIThemeHelper.CreateLabel(isAr ? "نوع وشكل الإشعار المرئي:" : "Notification Visual Style:", isDark, false, 12));
            _notifStyleCombo = new System.Windows.Controls.ComboBox { Height = 32, Margin = new Thickness(0, 4, 0, 0) };
            UIThemeHelper.StyleComboBox(_notifStyleCombo, isDark);
            _notifStyleCombo.Items.Add(new ComboBoxItem { Content = isAr ? "نافذة إشعار عائمة مخصصة (Custom Toast)" : "Custom Floating Toast Banner", Tag = "CustomToast" });
            _notifStyleCombo.Items.Add(new ComboBoxItem { Content = isAr ? "إشعار نظام ويندوز الافتراضي (Windows Native)" : "Windows Default Notification", Tag = "WindowsNative" });
            _notifStyleCombo.SelectedIndex = (_settings.NotificationStyle == "WindowsNative") ? 1 : 0;
            nStyleStack.Children.Add(_notifStyleCombo);
            Grid.SetRow(nStyleStack, 0);
            Grid.SetColumn(nStyleStack, 0);
            notifOptionsGrid.Children.Add(nStyleStack);

            // Row 0, Col 1: Duration
            var nDurStack = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
            nDurStack.Children.Add(UIThemeHelper.CreateLabel(isAr ? "مدة بقاء الإشعار على الشاشة:" : "Display Duration Before Dismissal:", isDark, false, 12));
            _notifDurationCombo = new System.Windows.Controls.ComboBox { Height = 32, Margin = new Thickness(0, 4, 0, 0) };
            UIThemeHelper.StyleComboBox(_notifDurationCombo, isDark);
            _notifDurationCombo.Items.Add(new ComboBoxItem { Content = isAr ? "5 ثوانٍ" : "5 Seconds", Tag = 5 });
            _notifDurationCombo.Items.Add(new ComboBoxItem { Content = isAr ? "10 ثوانٍ (الافتراضي)" : "10 Seconds (Default)", Tag = 10 });
            _notifDurationCombo.Items.Add(new ComboBoxItem { Content = isAr ? "15 ثانية" : "15 Seconds", Tag = 15 });
            _notifDurationCombo.Items.Add(new ComboBoxItem { Content = isAr ? "20 ثانية" : "20 Seconds", Tag = 20 });
            _notifDurationCombo.Items.Add(new ComboBoxItem { Content = isAr ? "30 ثانية" : "30 Seconds", Tag = 30 });
            _notifDurationCombo.Items.Add(new ComboBoxItem { Content = isAr ? "60 ثانية (دقيقة كاملة)" : "60 Seconds (1 Minute)", Tag = 60 });
            _notifDurationCombo.Items.Add(new ComboBoxItem { Content = isAr ? "دائم (حتى الإغلاق بالضغط عليه)" : "Persistent (Until Clicked)", Tag = 0 });

            int durIdx = 1;
            int curDur = _settings.NotificationDurationSeconds;
            for (int di = 0; di < _notifDurationCombo.Items.Count; di++)
            {
                var ci = _notifDurationCombo.Items[di] as ComboBoxItem;
                if (ci != null && (int)ci.Tag == curDur)
                {
                    durIdx = di;
                    break;
                }
            }
            _notifDurationCombo.SelectedIndex = durIdx;
            nDurStack.Children.Add(_notifDurationCombo);
            Grid.SetRow(nDurStack, 0);
            Grid.SetColumn(nDurStack, 1);
            notifOptionsGrid.Children.Add(nDurStack);

            // Row 1, Col 0: Toast Position
            var nPosStack = new StackPanel { Margin = new Thickness(0, 10, 10, 0) };
            nPosStack.Children.Add(UIThemeHelper.CreateLabel(isAr ? "موضع ظهور الإشعار العائم على الشاشة:" : "Floating Toast Screen Position:", isDark, false, 12));
            _toastPositionCombo = new System.Windows.Controls.ComboBox { Height = 32, Margin = new Thickness(0, 4, 0, 0) };
            UIThemeHelper.StyleComboBox(_toastPositionCombo, isDark);
            _toastPositionCombo.Items.Add(new ComboBoxItem { Content = isAr ? "أسفل اليمين (الافتراضي)" : "Bottom-Right (Default)", Tag = "BottomRight" });
            _toastPositionCombo.Items.Add(new ComboBoxItem { Content = isAr ? "أسفل اليسار" : "Bottom-Left", Tag = "BottomLeft" });
            _toastPositionCombo.Items.Add(new ComboBoxItem { Content = isAr ? "أعلى اليمين" : "Top-Right", Tag = "TopRight" });
            _toastPositionCombo.Items.Add(new ComboBoxItem { Content = isAr ? "أعلى اليسار" : "Top-Left", Tag = "TopLeft" });
            _toastPositionCombo.Items.Add(new ComboBoxItem { Content = isAr ? "أعلى الوسط (شريط علوي)" : "Top-Center (Top Banner)", Tag = "TopCenter" });
            _toastPositionCombo.Items.Add(new ComboBoxItem { Content = isAr ? "وسط الشاشة (تنبيه مركزي)" : "Screen Center (Center)", Tag = "Center" });
            _toastPositionCombo.Items.Add(new ComboBoxItem { Content = isAr ? "🎯 موضع مخصص (تم سحبه وحفظه بالماوس)" : "🎯 Custom Position (Saved via Drag & Drop)", Tag = "Custom" });

            string curPos = _settings.ToastPosition ?? "BottomRight";
            int posIdx = 0;
            for (int pi = 0; pi < _toastPositionCombo.Items.Count; pi++)
            {
                var ci = _toastPositionCombo.Items[pi] as ComboBoxItem;
                if (ci != null && string.Equals(ci.Tag as string, curPos, StringComparison.OrdinalIgnoreCase))
                {
                    posIdx = pi;
                    break;
                }
            }
            _toastPositionCombo.SelectedIndex = posIdx;
            nPosStack.Children.Add(_toastPositionCombo);

            var setCustomPosBtn = new System.Windows.Controls.Button
            {
                Content = isAr ? "📍 تحديد موضع مخصص بالسحب (Drag & Drop)..." : "📍 Set Custom Position via Drag & Drop...",
                Height = 30,
                Margin = new Thickness(0, 6, 0, 0),
                Background = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(30, 41, 59) : Color.FromRgb(241, 245, 249)),
                Foreground = UIThemeHelper.GetBrush(Color.FromRgb(16, 185, 129)),
                BorderBrush = UIThemeHelper.GetBrush(Color.FromRgb(16, 185, 129)),
                BorderThickness = new Thickness(1),
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 11.5
            };
            setCustomPosBtn.Click += (s, e) =>
            {
                syncNotifParams();
                ToastNotificationWindow.ShowCalibrationToast(isDark, isAr, _settings.ToastSize, _settings.CustomToastLeft, _settings.CustomToastTop, (newLeft, newTop) =>
                {
                    _settings.CustomToastLeft = newLeft;
                    _settings.CustomToastTop = newTop;
                    _settings.ToastPosition = "Custom";
                    _settings.Save();

                    for (int pi = 0; pi < _toastPositionCombo.Items.Count; pi++)
                    {
                        var ci = _toastPositionCombo.Items[pi] as ComboBoxItem;
                        if (ci != null && (string)ci.Tag == "Custom")
                        {
                            _toastPositionCombo.SelectedIndex = pi;
                            break;
                        }
                    }

                    testStatusLabel.Text = isAr
                        ? string.Format("✓ تم تثبيت وحفظ الموضع المخصص بنجاح! (X: {0}, Y: {1})", (int)newLeft, (int)newTop)
                        : string.Format("✓ Custom position saved successfully! (X: {0}, Y: {1})", (int)newLeft, (int)newTop);
                    testStatusLabel.Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentEmerald);
                });
            };
            nPosStack.Children.Add(setCustomPosBtn);

            Grid.SetRow(nPosStack, 1);
            Grid.SetColumn(nPosStack, 0);
            notifOptionsGrid.Children.Add(nPosStack);

            // Row 1, Col 1: Toast Size
            var nSizeStack = new StackPanel { Margin = new Thickness(10, 10, 0, 0) };
            nSizeStack.Children.Add(UIThemeHelper.CreateLabel(isAr ? "حجم الإشعار العائم:" : "Floating Toast Size:", isDark, false, 12));
            _toastSizeCombo = new System.Windows.Controls.ComboBox { Height = 32, Margin = new Thickness(0, 4, 0, 0) };
            UIThemeHelper.StyleComboBox(_toastSizeCombo, isDark);
            _toastSizeCombo.Items.Add(new ComboBoxItem { Content = isAr ? "صغير ومدمج (Small - 310px)" : "Small & Compact (310px)", Tag = "Small" });
            _toastSizeCombo.Items.Add(new ComboBoxItem { Content = isAr ? "متوسط (Medium - 380px - الافتراضي)" : "Medium (380px - Default)", Tag = "Medium" });
            _toastSizeCombo.Items.Add(new ComboBoxItem { Content = isAr ? "كبير وواضح (Large - 460px)" : "Large & Clear (460px)", Tag = "Large" });
            _toastSizeCombo.Items.Add(new ComboBoxItem { Content = isAr ? "عريض وبارز (Wide - 540px)" : "Wide & Prominent (540px)", Tag = "Wide" });

            string curSz = _settings.ToastSize ?? "Medium";
            int szIdx = 1;
            for (int si = 0; si < _toastSizeCombo.Items.Count; si++)
            {
                var ci = _toastSizeCombo.Items[si] as ComboBoxItem;
                if (ci != null && string.Equals(ci.Tag as string, curSz, StringComparison.OrdinalIgnoreCase))
                {
                    szIdx = si;
                    break;
                }
            }
            _toastSizeCombo.SelectedIndex = szIdx;
            nSizeStack.Children.Add(_toastSizeCombo);
            Grid.SetRow(nSizeStack, 1);
            Grid.SetColumn(nSizeStack, 1);
            notifOptionsGrid.Children.Add(nSizeStack);

            testCardStack.Children.Add(notifOptionsGrid);

            testCard.Child = testCardStack;
            panel.Children.Add(testCard);

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

            // -------------------------------------------------------------
            // Settings File Location Section (مسار ملف الإعدادات)
            // -------------------------------------------------------------
            panel.Children.Add(new Border
            {
                Height = 1,
                Background = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkBorder : UIThemeHelper.LightBorder),
                Margin = new Thickness(0, 16, 0, 16)
            });

            var settingsLocTitle = UIThemeHelper.CreateLabel(
                isAr ? "📁 موقع ملف الإعدادات المحفوظة (Settings Location):" : "📁 Settings File Location:",
                isDark, true, 14);
            panel.Children.Add(settingsLocTitle);

            var pathBox = new System.Windows.Controls.TextBox
            {
                Text = AppSettings.GetConfigFilePath(),
                IsReadOnly = true,
                Margin = new Thickness(0, 8, 0, 8),
                Padding = new Thickness(8, 6, 8, 6),
                FontSize = 12
            };
            UIThemeHelper.StyleTextBox(pathBox, isDark);
            panel.Children.Add(pathBox);

            var pathButtonsPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };

            var openFolderBtn = new System.Windows.Controls.Button
            {
                Content = isAr ? "📁 فتح مجلد الإعدادات في متصفح الملفات" : "📁 Open Settings Folder",
                Padding = new Thickness(14, 6, 14, 6),
                Background = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(51, 65, 85) : Color.FromRgb(226, 232, 240)),
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextPrimary : UIThemeHelper.LightTextPrimary),
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 12
            };
            openFolderBtn.Click += (s, e) =>
            {
                try
                {
                    string dir = AppSettings.GetConfigDirectory();
                    Process.Start("explorer.exe", dir);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                }
            };
            pathButtonsPanel.Children.Add(openFolderBtn);

            var openFileBtn = new System.Windows.Controls.Button
            {
                Content = isAr ? "📄 فتح ملف settings.json" : "📄 Open settings.json File",
                Padding = new Thickness(14, 6, 14, 6),
                Margin = new Thickness(8, 0, 0, 0),
                Background = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(51, 65, 85) : Color.FromRgb(226, 232, 240)),
                Foreground = UIThemeHelper.GetBrush(isDark ? UIThemeHelper.DarkTextPrimary : UIThemeHelper.LightTextPrimary),
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 12
            };
            openFileBtn.Click += (s, e) =>
            {
                try
                {
                    string f = AppSettings.GetConfigFilePath();
                    if (File.Exists(f))
                    {
                        Process.Start(f);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                }
            };
            pathButtonsPanel.Children.Add(openFileBtn);

            panel.Children.Add(pathButtonsPanel);

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

            foreach (var kvp in _reminderSoundCombos)
            {
                var cb = kvp.Value;
                var selItem = cb.SelectedItem as ComboBoxItem;
                string currentId = selItem != null ? (selItem.Tag as string) : "chime";
                PopulateReminderSoundCombo(cb, currentId);
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

                if (_reminderSoundCombos.ContainsKey(ev))
                {
                    var selRemSound = _reminderSoundCombos[ev].SelectedItem as ComboBoxItem;
                    cfg.ReminderSoundId = selRemSound != null ? (selRemSound.Tag as string) : "chime";
                }

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

            if (_notifStyleCombo != null)
            {
                var selStyle = _notifStyleCombo.SelectedItem as ComboBoxItem;
                _settings.NotificationStyle = selStyle != null ? (selStyle.Tag as string) : "CustomToast";
            }

            if (_notifDurationCombo != null)
            {
                var selDur = _notifDurationCombo.SelectedItem as ComboBoxItem;
                _settings.NotificationDurationSeconds = selDur != null ? (int)selDur.Tag : 10;
            }

            if (_toastPositionCombo != null)
            {
                var selPos = _toastPositionCombo.SelectedItem as ComboBoxItem;
                _settings.ToastPosition = selPos != null ? (selPos.Tag as string) : "BottomRight";
            }

            if (_toastSizeCombo != null)
            {
                var selSz = _toastSizeCombo.SelectedItem as ComboBoxItem;
                _settings.ToastSize = selSz != null ? (selSz.Tag as string) : "Medium";
            }

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

using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace ShiaPrayerTimes
{
    public static class UIThemeHelper
    {
        // Dark Theme Palette
        public static readonly Color DarkBg = Color.FromRgb(15, 23, 42);           // #0F172A
        public static readonly Color DarkHeaderBg = Color.FromRgb(15, 23, 42);     // #0F172A
        public static readonly Color DarkCardBg = Color.FromRgb(30, 41, 59);       // #1E293B
        public static readonly Color DarkInputBg = Color.FromRgb(24, 33, 47);      // #18212F
        public static readonly Color DarkBorder = Color.FromRgb(71, 85, 105);      // #475569
        public static readonly Color DarkTextPrimary = Color.FromRgb(255, 255, 255); // #FFFFFF
        public static readonly Color DarkTextSecondary = Color.FromRgb(203, 213, 225); // #CBD5E1
        public static readonly Color DarkTextMuted = Color.FromRgb(148, 163, 184); // #94A3B8

        // Light Theme Palette
        public static readonly Color LightBg = Color.FromRgb(248, 250, 252);       // #F8FAFC
        public static readonly Color LightHeaderBg = Color.FromRgb(255, 255, 255); // White
        public static readonly Color LightCardBg = Color.FromRgb(255, 255, 255);   // White
        public static readonly Color LightInputBg = Color.FromRgb(241, 245, 249);  // #F1F5F9
        public static readonly Color LightBorder = Color.FromRgb(203, 213, 225);   // #CBD5E1
        public static readonly Color LightTextPrimary = Color.FromRgb(15, 23, 42);  // #0F172A
        public static readonly Color LightTextSecondary = Color.FromRgb(51, 65, 85); // #334155
        public static readonly Color LightTextMuted = Color.FromRgb(100, 116, 139); // #64748B

        // Accents
        public static readonly Color AccentGold = Color.FromRgb(245, 158, 11);     // #F59E0B
        public static readonly Color AccentEmerald = Color.FromRgb(16, 185, 129);  // #10B981
        public static readonly Color AccentRed = Color.FromRgb(239, 68, 68);       // #EF4444

        private static ControlTemplate _darkComboTemplate = null;
        private static ControlTemplate _lightComboTemplate = null;

        private static Style _darkTabControlStyle = null;
        private static Style _lightTabControlStyle = null;
        private static Style _darkTabItemStyle = null;
        private static Style _lightTabItemStyle = null;

        public static SolidColorBrush GetBrush(Color c)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        public static Border CreateIntegratedTitleBar(
            Window window,
            string title,
            string subtitle,
            bool isDark,
            bool isAr,
            UIElement middleContent = null,
            bool showMinMax = true)
        {
            var titleBar = new Border
            {
                Height = 48,
                Background = GetBrush(isDark ? DarkHeaderBg : LightHeaderBg),
                BorderBrush = GetBrush(isDark ? DarkBorder : LightBorder),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(14, 0, 8, 0)
            };

            // Window dragging
            titleBar.MouseDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    if (e.ClickCount == 2 && showMinMax)
                    {
                        window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                    }
                    else
                    {
                        window.DragMove();
                    }
                }
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Left / Right (Icon & Title)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Middle
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Controls

            // App Icon & Title
            var titleStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var iconText = new TextBlock
            {
                Text = "🌙",
                FontSize = 18,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            titleStack.Children.Add(iconText);

            var titleBlock = new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = GetBrush(AccentGold),
                VerticalAlignment = VerticalAlignment.Center
            };
            titleStack.Children.Add(titleBlock);

            if (!string.IsNullOrEmpty(subtitle))
            {
                var subBlock = new TextBlock
                {
                    Text = " - " + subtitle,
                    FontSize = 12,
                    Foreground = GetBrush(isDark ? DarkTextMuted : LightTextMuted),
                    VerticalAlignment = VerticalAlignment.Center
                };
                titleStack.Children.Add(subBlock);
            }

            Grid.SetColumn(titleStack, 0);
            grid.Children.Add(titleStack);

            // Middle Content
            if (middleContent != null)
            {
                Grid.SetColumn(middleContent, 1);
                grid.Children.Add(middleContent);
            }

            // Window Control Buttons (Minimize, Maximize, Close)
            var winBtnStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            Color btnHoverBg = isDark ? Color.FromRgb(51, 65, 85) : Color.FromRgb(226, 232, 240);
            Color textCol = isDark ? DarkTextPrimary : LightTextPrimary;

            if (showMinMax)
            {
                var minBtn = CreateTitleBarButton("—", "تصغير / Minimize", textCol, btnHoverBg, 40, 32);
                minBtn.Click += (s, e) => window.WindowState = WindowState.Minimized;
                winBtnStack.Children.Add(minBtn);

                var maxBtn = CreateTitleBarButton(window.WindowState == WindowState.Maximized ? "🗗" : "🗖", "تكبير / Maximize", textCol, btnHoverBg, 40, 32);
                maxBtn.Click += (s, e) =>
                {
                    if (window.WindowState == WindowState.Maximized)
                    {
                        window.WindowState = WindowState.Normal;
                        maxBtn.Content = "🗖";
                    }
                    else
                    {
                        window.WindowState = WindowState.Maximized;
                        maxBtn.Content = "🗗";
                    }
                };
                window.StateChanged += (s, e) =>
                {
                    maxBtn.Content = window.WindowState == WindowState.Maximized ? "🗗" : "🗖";
                };
                winBtnStack.Children.Add(maxBtn);
            }

            // Close Button
            var closeBtn = CreateTitleBarButton("✕", "إغلاق / Close", textCol, AccentRed, 44, 32, true);
            closeBtn.Click += (s, e) => window.Close();
            winBtnStack.Children.Add(closeBtn);

            Grid.SetColumn(winBtnStack, 2);
            grid.Children.Add(winBtnStack);

            titleBar.Child = grid;
            return titleBar;
        }

        private static Button CreateTitleBarButton(string symbol, string tooltip, Color textCol, Color hoverBg, double w, double h, bool isClose = false)
        {
            var btn = new Button
            {
                Content = symbol,
                Width = w,
                Height = h,
                Background = Brushes.Transparent,
                Foreground = GetBrush(textCol),
                BorderThickness = new Thickness(0),
                FontSize = 13,
                Cursor = Cursors.Hand,
                ToolTip = tooltip,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };

            btn.MouseEnter += (s, e) =>
            {
                btn.Background = GetBrush(hoverBg);
                if (isClose) btn.Foreground = Brushes.White;
            };
            btn.MouseLeave += (s, e) =>
            {
                btn.Background = Brushes.Transparent;
                btn.Foreground = GetBrush(textCol);
            };

            return btn;
        }

        public static void StyleComboBox(ComboBox combo, bool isDark)
        {
            EnsureTemplates();
            combo.Template = isDark ? _darkComboTemplate : _lightComboTemplate;
            combo.Background = GetBrush(isDark ? DarkInputBg : LightInputBg);
            combo.Foreground = GetBrush(isDark ? DarkTextPrimary : LightTextPrimary);
            combo.BorderBrush = GetBrush(isDark ? DarkBorder : LightBorder);
            combo.BorderThickness = new Thickness(1);
            combo.FontSize = 13;
        }

        private static void EnsureTemplates()
        {
            if (_darkComboTemplate == null)
            {
                string darkXaml = @"
                <ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                                 xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                                 TargetType='{x:Type ComboBox}'>
                    <Grid>
                        <ToggleButton Name='ToggleButton'
                                      Focusable='false'
                                      IsChecked='{Binding Path=IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}'
                                      ClickMode='Press'>
                            <ToggleButton.Template>
                                <ControlTemplate TargetType='{x:Type ToggleButton}'>
                                    <Border Name='Border' Background='#1E293B' BorderBrush='#475569' BorderThickness='1' CornerRadius='4'>
                                        <Grid>
                                            <Grid.ColumnDefinitions>
                                                <ColumnDefinition Width='*'/>
                                                <ColumnDefinition Width='24'/>
                                            </Grid.ColumnDefinitions>
                                            <Path Grid.Column='1' HorizontalAlignment='Center' VerticalAlignment='Center'
                                                  Data='M 0 0 L 4 4 L 8 0 Z' Fill='#F59E0B'/>
                                        </Grid>
                                    </Border>
                                </ControlTemplate>
                            </ToggleButton.Template>
                        </ToggleButton>
                        <ContentPresenter Name='ContentSite'
                                          IsHitTestVisible='False'
                                          Content='{TemplateBinding SelectionBoxItem}'
                                          ContentTemplate='{TemplateBinding SelectionBoxItemTemplate}'
                                          ContentTemplateSelector='{TemplateBinding ItemTemplateSelector}'
                                          Margin='10,3,26,3'
                                          VerticalAlignment='Center'
                                          HorizontalAlignment='{TemplateBinding HorizontalContentAlignment}'
                                          TextBlock.Foreground='#FFFFFF'
                                          TextBlock.FontWeight='SemiBold'
                                          TextBlock.FontSize='12'/>
                        <Popup Name='Popup'
                               Placement='Bottom'
                               IsOpen='{TemplateBinding IsDropDownOpen}'
                               AllowsTransparency='True'
                               Focusable='False'
                               PopupAnimation='Slide'>
                            <Grid Name='DropDown'
                                  SnapsToDevicePixels='True'
                                  MinWidth='{TemplateBinding ActualWidth}'
                                  MaxHeight='{TemplateBinding MaxDropDownHeight}'>
                                <Border Name='DropDownBorder' Background='#1E293B' BorderBrush='#475569' BorderThickness='1' CornerRadius='4'>
                                    <ScrollViewer Margin='2' SnapsToDevicePixels='True'>
                                        <StackPanel IsItemsHost='True' KeyboardNavigation.DirectionalNavigation='Contained'/>
                                    </ScrollViewer>
                                </Border>
                            </Grid>
                        </Popup>
                    </Grid>
                </ControlTemplate>";

                _darkComboTemplate = (ControlTemplate)XamlReader.Parse(darkXaml);
            }

            if (_lightComboTemplate == null)
            {
                string lightXaml = @"
                <ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                                 xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                                 TargetType='{x:Type ComboBox}'>
                    <Grid>
                        <ToggleButton Name='ToggleButton'
                                      Focusable='false'
                                      IsChecked='{Binding Path=IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}'
                                      ClickMode='Press'>
                            <ToggleButton.Template>
                                <ControlTemplate TargetType='{x:Type ToggleButton}'>
                                    <Border Name='Border' Background='#F1F5F9' BorderBrush='#CBD5E1' BorderThickness='1' CornerRadius='4'>
                                        <Grid>
                                            <Grid.ColumnDefinitions>
                                                <ColumnDefinition Width='*'/>
                                                <ColumnDefinition Width='24'/>
                                            </Grid.ColumnDefinitions>
                                            <Path Grid.Column='1' HorizontalAlignment='Center' VerticalAlignment='Center'
                                                  Data='M 0 0 L 4 4 L 8 0 Z' Fill='#D97706'/>
                                        </Grid>
                                    </Border>
                                </ControlTemplate>
                            </ToggleButton.Template>
                        </ToggleButton>
                        <ContentPresenter Name='ContentSite'
                                          IsHitTestVisible='False'
                                          Content='{TemplateBinding SelectionBoxItem}'
                                          ContentTemplate='{TemplateBinding SelectionBoxItemTemplate}'
                                          ContentTemplateSelector='{TemplateBinding ItemTemplateSelector}'
                                          Margin='10,3,26,3'
                                          VerticalAlignment='Center'
                                          HorizontalAlignment='{TemplateBinding HorizontalContentAlignment}'
                                          TextBlock.Foreground='#0F172A'
                                          TextBlock.FontWeight='SemiBold'
                                          TextBlock.FontSize='12'/>
                        <Popup Name='Popup'
                               Placement='Bottom'
                               IsOpen='{TemplateBinding IsDropDownOpen}'
                               AllowsTransparency='True'
                               Focusable='False'
                               PopupAnimation='Slide'>
                            <Grid Name='DropDown'
                                  SnapsToDevicePixels='True'
                                  MinWidth='{TemplateBinding ActualWidth}'
                                  MaxHeight='{TemplateBinding MaxDropDownHeight}'>
                                <Border Name='DropDownBorder' Background='#FFFFFF' BorderBrush='#CBD5E1' BorderThickness='1' CornerRadius='4'>
                                    <ScrollViewer Margin='2' SnapsToDevicePixels='True'>
                                        <StackPanel IsItemsHost='True' KeyboardNavigation.DirectionalNavigation='Contained'/>
                                    </ScrollViewer>
                                </Border>
                            </Grid>
                        </Popup>
                    </Grid>
                </ControlTemplate>";

                _lightComboTemplate = (ControlTemplate)XamlReader.Parse(lightXaml);
            }
        }

        public static void ApplyTabControlTheme(TabControl tabControl, bool isDark)
        {
            EnsureTabStyles();
            tabControl.Style = isDark ? _darkTabControlStyle : _lightTabControlStyle;
            tabControl.ItemContainerStyle = isDark ? _darkTabItemStyle : _lightTabItemStyle;
        }

        public static void ApplyTabItemTheme(TabItem tabItem, bool isDark)
        {
            EnsureTabStyles();
            tabItem.Style = isDark ? _darkTabItemStyle : _lightTabItemStyle;
        }

        private static void EnsureTabStyles()
        {
            if (_darkTabItemStyle == null)
            {
                string xaml = @"
                <ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>

                    <!-- Dark Tab Control Style -->
                    <Style x:Key='DarkTabControl' TargetType='{x:Type TabControl}'>
                        <Setter Property='Background' Value='Transparent'/>
                        <Setter Property='BorderThickness' Value='0'/>
                        <Setter Property='Template'>
                            <Setter.Value>
                                <ControlTemplate TargetType='{x:Type TabControl}'>
                                    <Grid>
                                        <Grid.RowDefinitions>
                                            <RowDefinition Height='Auto'/>
                                            <RowDefinition Height='*'/>
                                        </Grid.RowDefinitions>
                                        <Border Grid.Row='0' BorderBrush='#334155' BorderThickness='0,0,0,1' Margin='0,0,0,8'>
                                            <TabPanel IsItemsHost='True' Margin='0,0,0,-1'/>
                                        </Border>
                                        <Border Grid.Row='1' Background='Transparent' BorderThickness='0'>
                                            <ContentPresenter ContentSource='SelectedContent'/>
                                        </Border>
                                    </Grid>
                                </ControlTemplate>
                            </Setter.Value>
                        </Setter>
                    </Style>

                    <!-- Dark Tab Item Style -->
                    <Style x:Key='DarkTabItem' TargetType='{x:Type TabItem}'>
                        <Setter Property='FocusVisualStyle' Value='{x:Null}'/>
                        <Setter Property='FontSize' Value='13'/>
                        <Setter Property='FontWeight' Value='SemiBold'/>
                        <Setter Property='Cursor' Value='Hand'/>
                        <Setter Property='Template'>
                            <Setter.Value>
                                <ControlTemplate TargetType='{x:Type TabItem}'>
                                    <Border Name='TabBorder'
                                            Background='#1E293B'
                                            BorderBrush='#334155'
                                            BorderThickness='1,1,1,0'
                                            CornerRadius='6,6,0,0'
                                            Margin='3,0,3,0'
                                            Padding='16,10,16,10'>
                                        <ContentPresenter Name='ContentSite'
                                                          VerticalAlignment='Center'
                                                          HorizontalAlignment='Center'
                                                          ContentSource='Header'
                                                          RecognizesAccessKey='True'/>
                                    </Border>
                                    <ControlTemplate.Triggers>
                                        <Trigger Property='IsMouseOver' Value='True'>
                                            <Setter TargetName='TabBorder' Property='Background' Value='#334155'/>
                                            <Setter TargetName='TabBorder' Property='BorderBrush' Value='#475569'/>
                                            <Setter TargetName='TabBorder' Property='TextElement.Foreground' Value='#FFFFFF'/>
                                        </Trigger>
                                        <Trigger Property='IsSelected' Value='True'>
                                            <Setter TargetName='TabBorder' Property='Background' Value='#10B981'/>
                                            <Setter TargetName='TabBorder' Property='BorderBrush' Value='#10B981'/>
                                            <Setter TargetName='TabBorder' Property='TextElement.Foreground' Value='#FFFFFF'/>
                                            <Setter TargetName='TabBorder' Property='TextElement.FontWeight' Value='Bold'/>
                                        </Trigger>
                                        <Trigger Property='IsSelected' Value='False'>
                                            <Setter TargetName='TabBorder' Property='Background' Value='#1E293B'/>
                                            <Setter TargetName='TabBorder' Property='BorderBrush' Value='#334155'/>
                                            <Setter TargetName='TabBorder' Property='TextElement.Foreground' Value='#CBD5E1'/>
                                        </Trigger>
                                    </ControlTemplate.Triggers>
                                </ControlTemplate>
                            </Setter.Value>
                        </Setter>
                    </Style>

                    <!-- Light Tab Control Style -->
                    <Style x:Key='LightTabControl' TargetType='{x:Type TabControl}'>
                        <Setter Property='Background' Value='Transparent'/>
                        <Setter Property='BorderThickness' Value='0'/>
                        <Setter Property='Template'>
                            <Setter.Value>
                                <ControlTemplate TargetType='{x:Type TabControl}'>
                                    <Grid>
                                        <Grid.RowDefinitions>
                                            <RowDefinition Height='Auto'/>
                                            <RowDefinition Height='*'/>
                                        </Grid.RowDefinitions>
                                        <Border Grid.Row='0' BorderBrush='#CBD5E1' BorderThickness='0,0,0,1' Margin='0,0,0,8'>
                                            <TabPanel IsItemsHost='True' Margin='0,0,0,-1'/>
                                        </Border>
                                        <Border Grid.Row='1' Background='Transparent' BorderThickness='0'>
                                            <ContentPresenter ContentSource='SelectedContent'/>
                                        </Border>
                                    </Grid>
                                </ControlTemplate>
                            </Setter.Value>
                        </Setter>
                    </Style>

                    <!-- Light Tab Item Style -->
                    <Style x:Key='LightTabItem' TargetType='{x:Type TabItem}'>
                        <Setter Property='FocusVisualStyle' Value='{x:Null}'/>
                        <Setter Property='FontSize' Value='13'/>
                        <Setter Property='FontWeight' Value='SemiBold'/>
                        <Setter Property='Cursor' Value='Hand'/>
                        <Setter Property='Template'>
                            <Setter.Value>
                                <ControlTemplate TargetType='{x:Type TabItem}'>
                                    <Border Name='TabBorder'
                                            Background='#F1F5F9'
                                            BorderBrush='#CBD5E1'
                                            BorderThickness='1,1,1,0'
                                            CornerRadius='6,6,0,0'
                                            Margin='3,0,3,0'
                                            Padding='16,10,16,10'>
                                        <ContentPresenter Name='ContentSite'
                                                          VerticalAlignment='Center'
                                                          HorizontalAlignment='Center'
                                                          ContentSource='Header'
                                                          RecognizesAccessKey='True'/>
                                    </Border>
                                    <ControlTemplate.Triggers>
                                        <Trigger Property='IsMouseOver' Value='True'>
                                            <Setter TargetName='TabBorder' Property='Background' Value='#E2E8F0'/>
                                            <Setter TargetName='TabBorder' Property='BorderBrush' Value='#94A3B8'/>
                                            <Setter TargetName='TabBorder' Property='TextElement.Foreground' Value='#0F172A'/>
                                        </Trigger>
                                        <Trigger Property='IsSelected' Value='True'>
                                            <Setter TargetName='TabBorder' Property='Background' Value='#059669'/>
                                            <Setter TargetName='TabBorder' Property='BorderBrush' Value='#059669'/>
                                            <Setter TargetName='TabBorder' Property='TextElement.Foreground' Value='#FFFFFF'/>
                                            <Setter TargetName='TabBorder' Property='TextElement.FontWeight' Value='Bold'/>
                                        </Trigger>
                                        <Trigger Property='IsSelected' Value='False'>
                                            <Setter TargetName='TabBorder' Property='Background' Value='#F1F5F9'/>
                                            <Setter TargetName='TabBorder' Property='BorderBrush' Value='#CBD5E1'/>
                                            <Setter TargetName='TabBorder' Property='TextElement.Foreground' Value='#334155'/>
                                        </Trigger>
                                    </ControlTemplate.Triggers>
                                </ControlTemplate>
                            </Setter.Value>
                        </Setter>
                    </Style>
                </ResourceDictionary>";

                var dict = (ResourceDictionary)XamlReader.Parse(xaml);
                _darkTabControlStyle = (Style)dict["DarkTabControl"];
                _darkTabItemStyle = (Style)dict["DarkTabItem"];
                _lightTabControlStyle = (Style)dict["LightTabControl"];
                _lightTabItemStyle = (Style)dict["LightTabItem"];
            }
        }

        public static void StyleTextBox(TextBox box, bool isDark)
        {
            box.Background = GetBrush(isDark ? DarkCardBg : LightInputBg);
            box.Foreground = GetBrush(isDark ? DarkTextPrimary : LightTextPrimary);
            box.BorderBrush = GetBrush(isDark ? DarkBorder : LightBorder);
            box.BorderThickness = new Thickness(1);
            box.Padding = new Thickness(6, 4, 6, 4);
            box.CaretBrush = GetBrush(isDark ? AccentGold : DarkTextPrimary);
            box.FontSize = 13;
        }

        public static void StyleCheckBox(CheckBox check, bool isDark)
        {
            check.Foreground = GetBrush(isDark ? DarkTextPrimary : LightTextPrimary);
            check.FontSize = 13;
            check.FontWeight = FontWeights.SemiBold;
        }

        public static TextBlock CreateLabel(string text, bool isDark, bool isBold = false, double fontSize = 13, Color? customColor = null)
        {
            Color c = customColor ?? (isDark ? DarkTextPrimary : LightTextPrimary);
            return new TextBlock
            {
                Text = text,
                Foreground = GetBrush(c),
                FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                FontSize = fontSize,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
    }
}

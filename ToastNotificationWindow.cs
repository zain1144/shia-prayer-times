using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ShiaPrayerTimes
{
    public class ToastNotificationWindow : Window
    {
        private static ToastNotificationWindow _activeToast = null;

        private DispatcherTimer _timer;
        private DateTime _startTime;
        private TimeSpan _duration;
        private TimeSpan _elapsedBeforePause;
        private bool _isPaused = false;
        private Rectangle _progressBar;
        private double _maxBarWidth = 356;
        private bool _isClosing = false;
        private Action _onClickAction;

        public static void ShowToast(string title, string message, int durationSeconds, bool isDark, bool isAr, string iconSymbol = "🕌", Action onClick = null)
        {
            try
            {
                if (Application.Current == null) return;

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    if (_activeToast != null)
                    {
                        try { _activeToast.Close(); } catch { }
                        _activeToast = null;
                    }

                    var toast = new ToastNotificationWindow(title, message, durationSeconds, isDark, isAr, iconSymbol, onClick);
                    _activeToast = toast;
                    toast.Show();
                }));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Toast show error: " + ex.Message);
            }
        }

        public ToastNotificationWindow(string title, string message, int durationSeconds, bool isDark, bool isAr, string iconSymbol, Action onClick)
        {
            _onClickAction = onClick;
            _duration = TimeSpan.FromSeconds(Math.Max(0, durationSeconds));

            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            ShowActivated = false;
            Focusable = false;
            Width = 380;
            SizeToContent = SizeToContent.Height;
            FlowDirection = isAr ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            Opacity = 1;

            // Outer Container with Drop Shadow
            var outerBorder = new Border
            {
                Margin = new Thickness(12),
                CornerRadius = new CornerRadius(12),
                Background = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(15, 23, 42) : Color.FromRgb(255, 255, 255)),
                BorderBrush = UIThemeHelper.GetBrush(Color.FromRgb(16, 185, 129)), // Emerald accent
                BorderThickness = new Thickness(1.5),
                ClipToBounds = true,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 14,
                    ShadowDepth = 3,
                    Opacity = 0.45,
                    Color = Colors.Black
                }
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Progress bar

            // Content Grid
            var contentGrid = new Grid { Margin = new Thickness(12, 10, 12, 10), Cursor = Cursors.Hand };
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Icon
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Texts
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Close Button

            contentGrid.MouseLeftButtonUp += (s, e) =>
            {
                if (_onClickAction != null) _onClickAction();
                Dismiss();
            };

            // 1. Icon Badge
            var iconBorder = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(10),
                Background = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(6, 78, 59) : Color.FromRgb(209, 250, 229)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            var iconBlock = new TextBlock
            {
                Text = string.IsNullOrEmpty(iconSymbol) ? "🕌" : iconSymbol,
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBorder.Child = iconBlock;
            Grid.SetColumn(iconBorder, 0);
            contentGrid.Children.Add(iconBorder);

            // 2. Texts Stack
            var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var titleBlock = new TextBlock
            {
                Text = title,
                FontSize = 13.5,
                FontWeight = FontWeights.Bold,
                Foreground = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(248, 250, 252) : Color.FromRgb(15, 23, 42)),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            var bodyBlock = new TextBlock
            {
                Text = message,
                FontSize = 12,
                Margin = new Thickness(0, 3, 0, 0),
                Foreground = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(203, 213, 225) : Color.FromRgb(71, 85, 105)),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 18
            };
            textStack.Children.Add(titleBlock);
            textStack.Children.Add(bodyBlock);
            Grid.SetColumn(textStack, 1);
            contentGrid.Children.Add(textStack);

            // 3. Close Button
            var closeBtn = new Border
            {
                Width = 24,
                Height = 24,
                CornerRadius = new CornerRadius(12),
                Background = Brushes.Transparent,
                VerticalAlignment = VerticalAlignment.Top,
                Cursor = Cursors.Hand,
                ToolTip = isAr ? "إغلاق" : "Dismiss"
            };
            var closeTxt = new TextBlock
            {
                Text = "✕",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(148, 163, 184) : Color.FromRgb(100, 116, 139))
            };
            closeBtn.Child = closeTxt;
            closeBtn.MouseEnter += (s, e) =>
            {
                closeBtn.Background = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(51, 65, 85) : Color.FromRgb(226, 232, 240));
                closeTxt.Foreground = UIThemeHelper.GetBrush(UIThemeHelper.AccentRed);
            };
            closeBtn.MouseLeave += (s, e) =>
            {
                closeBtn.Background = Brushes.Transparent;
                closeTxt.Foreground = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(148, 163, 184) : Color.FromRgb(100, 116, 139));
            };
            closeBtn.MouseLeftButtonUp += (s, e) =>
            {
                e.Handled = true;
                Dismiss();
            };
            Grid.SetColumn(closeBtn, 2);
            contentGrid.Children.Add(closeBtn);

            Grid.SetRow(contentGrid, 0);
            mainGrid.Children.Add(contentGrid);

            // 4. Progress Bar
            if (durationSeconds > 0)
            {
                var barTrack = new Grid
                {
                    Height = 3.5,
                    Background = UIThemeHelper.GetBrush(isDark ? Color.FromRgb(30, 41, 59) : Color.FromRgb(226, 232, 240))
                };
                _progressBar = new Rectangle
                {
                    Height = 3.5,
                    Width = _maxBarWidth,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Fill = UIThemeHelper.GetBrush(Color.FromRgb(16, 185, 129))
                };
                barTrack.Children.Add(_progressBar);
                Grid.SetRow(barTrack, 1);
                mainGrid.Children.Add(barTrack);
            }

            outerBorder.Child = mainGrid;
            Content = outerBorder;

            // Pause on hover
            MouseEnter += (s, e) =>
            {
                if (durationSeconds > 0 && !_isPaused && _timer != null)
                {
                    _isPaused = true;
                    _elapsedBeforePause += DateTime.UtcNow - _startTime;
                    _timer.Stop();
                }
            };
            MouseLeave += (s, e) =>
            {
                if (durationSeconds > 0 && _isPaused && _timer != null && !_isClosing)
                {
                    _isPaused = false;
                    _startTime = DateTime.UtcNow;
                    _timer.Start();
                }
            };

            Loaded += (s, e) =>
            {
                PositionWindow();
                AnimateFadeIn();

                if (durationSeconds > 0)
                {
                    _startTime = DateTime.UtcNow;
                    _elapsedBeforePause = TimeSpan.Zero;
                    _timer = new DispatcherTimer(DispatcherPriority.Normal)
                    {
                        Interval = TimeSpan.FromMilliseconds(40)
                    };
                    _timer.Tick += Timer_Tick;
                    _timer.Start();
                }
            };

            Closed += (s, e) =>
            {
                if (_timer != null)
                {
                    _timer.Stop();
                    _timer = null;
                }
                if (_activeToast == this)
                {
                    _activeToast = null;
                }
            };
        }

        private void PositionWindow()
        {
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Right - ActualWidth - 8;
            Top = workArea.Bottom - ActualHeight - 8;
        }

        private void AnimateFadeIn()
        {
            var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220));
            BeginAnimation(OpacityProperty, anim);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_isClosing) return;

            var elapsed = _elapsedBeforePause + (DateTime.UtcNow - _startTime);
            double fraction = elapsed.TotalMilliseconds / _duration.TotalMilliseconds;

            if (fraction >= 1.0)
            {
                if (_timer != null) _timer.Stop();
                Dismiss();
            }
            else
            {
                if (_progressBar != null)
                {
                    double remainingFraction = Math.Max(0.0, 1.0 - fraction);
                    _progressBar.Width = _maxBarWidth * remainingFraction;
                }
            }
        }

        public void Dismiss()
        {
            if (_isClosing) return;
            _isClosing = true;

            if (_timer != null)
            {
                _timer.Stop();
            }

            var anim = new DoubleAnimation(Opacity, 0, TimeSpan.FromMilliseconds(180));
            anim.Completed += (s, e) =>
            {
                try { Close(); } catch { }
            };
            BeginAnimation(OpacityProperty, anim);
        }
    }
}

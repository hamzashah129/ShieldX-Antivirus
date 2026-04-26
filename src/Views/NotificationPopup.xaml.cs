using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ShieldX.Services;

namespace ShieldX.Views
{
    public enum NotificationType
    {
        Success,  // Green — no threats, clean
        Warning,  // Orange — suspicious
        Danger,   // Red — threat blocked
        Info,     // Blue — information
        Usb       // Teal — USB event
    }

    public partial class NotificationPopup : Window
    {
        private DispatcherTimer _timer;
        private DispatcherTimer _progressTimer;
        private double _progressValue = 100;
        private int _durationSeconds;
        private Action? _onClick;
        private NotificationType _currentType;
        private string _currentBadge;

        public NotificationPopup()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            
            // No longer subscribing to theme changes - always dark theme
        }

        public void Show(
            string title,
            string message,
            NotificationType type = NotificationType.Info,
            int durationSeconds = 5,
            Action? onClick = null,
            string badge = "")
        {
            _durationSeconds = durationSeconds;
            _onClick = onClick;
            _currentType = type;
            _currentBadge = badge;

            // Set text
            TitleText.Text   = title;
            MessageText.Text = message;

            // Apply colors
            ApplyColors(type, badge);

            // Position bottom-right of screen
            var screen = SystemParameters.WorkArea;
            Left = screen.Right - Width - 20;
            Top  = screen.Bottom - Height - 20;

            Show();

            // Slide in animation
            var slideIn = (Storyboard)FindResource("SlideIn");
            slideIn.Begin();

            // Click handler
            MouseLeftButtonDown += (s, e) =>
            {
                _onClick?.Invoke();
                DismissWithAnimation();
            };
        }

        private void ApplyColors(NotificationType type, string badge)
        {
            // Always use dark theme colors for notifications (professional look)
            BackgroundBorder.Background = new SolidColorBrush(Color.FromRgb(22, 27, 39)); // #161B27
            BackgroundBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(45, 55, 72)); // #2D3748
            
            ShieldXText.Foreground = new SolidColorBrush(Color.FromRgb(113, 128, 150)); // #718096
            TitleText.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255)); // #FFFFFF
            MessageText.Foreground = new SolidColorBrush(Color.FromRgb(113, 128, 150)); // #718096
            CloseBtn.Foreground = new SolidColorBrush(Color.FromRgb(74, 85, 104)); // #4A5568

            // Set colors based on type (always dark theme)
            switch (type)
            {
                case NotificationType.Success:
                    IconText.Text        = "✅";
                    BadgeText.Text       = string.IsNullOrEmpty(badge)
                        ? "CLEAN" : badge;
                    BadgeText.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                    BadgeBorder.Background = new SolidColorBrush(Color.FromRgb(13, 40, 24));
                    GradTop.Color = Color.FromRgb(0, 229, 204);
                    GradBottom.Color = Color.FromRgb(16, 185, 129);
                    IconGrad1.Color = Color.FromRgb(0, 46, 41);
                    IconGrad2.Color = Color.FromRgb(0, 30, 25);
                    break;

                case NotificationType.Warning:
                    IconText.Text        = "⚠️";
                    BadgeText.Text       = string.IsNullOrEmpty(badge)
                        ? "WARNING" : badge;
                    BadgeText.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                    BadgeBorder.Background = new SolidColorBrush(Color.FromRgb(26, 21, 13));
                    GradTop.Color = Color.FromRgb(245, 158, 11);
                    GradBottom.Color = Color.FromRgb(180, 100, 0);
                    IconGrad1.Color = Color.FromRgb(40, 30, 0);
                    IconGrad2.Color = Color.FromRgb(26, 20, 0);
                    break;

                case NotificationType.Danger:
                    IconText.Text        = "🚨";
                    BadgeText.Text       = string.IsNullOrEmpty(badge)
                        ? "THREAT BLOCKED" : badge;
                    BadgeText.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                    BadgeBorder.Background = new SolidColorBrush(Color.FromRgb(26, 13, 13));
                    GradTop.Color = Color.FromRgb(239, 68, 68);
                    GradBottom.Color = Color.FromRgb(185, 28, 28);
                    IconGrad1.Color = Color.FromRgb(46, 0, 0);
                    IconGrad2.Color = Color.FromRgb(30, 0, 0);
                    break;

                case NotificationType.Usb:
                    IconText.Text        = "🔌";
                    BadgeText.Text       = string.IsNullOrEmpty(badge)
                        ? "USB DETECTED" : badge;
                    BadgeText.Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 204));
                    BadgeBorder.Background = new SolidColorBrush(Color.FromRgb(0, 46, 41));
                    GradTop.Color = Color.FromRgb(0, 229, 204);
                    GradBottom.Color = Color.FromRgb(124, 58, 237);
                    IconGrad1.Color = Color.FromRgb(0, 30, 30);
                    IconGrad2.Color = Color.FromRgb(15, 0, 30);
                    break;

                default: // Info
                    IconText.Text        = "ℹ️";
                    BadgeText.Text       = string.IsNullOrEmpty(badge)
                        ? "INFO" : badge;
                    BadgeText.Foreground = new SolidColorBrush(Color.FromRgb(99, 179, 237));
                    BadgeBorder.Background = new SolidColorBrush(Color.FromRgb(13, 26, 46));
                    GradTop.Color = Color.FromRgb(99, 179, 237);
                    GradBottom.Color = Color.FromRgb(66, 153, 225);
                    IconGrad1.Color = Color.FromRgb(0, 15, 30);
                    IconGrad2.Color = Color.FromRgb(5, 20, 40);
                    break;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            StartTimers();
        }

        private void StartTimers()
        {
            // Progress bar countdown
            double totalMs = _durationSeconds * 1000.0;
            double interval = 50; // update every 50ms
            double decrement = 100.0 / (totalMs / interval);

            _progressTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(interval)
            };
            _progressTimer.Tick += (s, e) =>
            {
                _progressValue -= decrement;
                TimerBar.Value = Math.Max(0, _progressValue);
                if (_progressValue <= 0)
                    _progressTimer.Stop();
            };
            _progressTimer.Start();

            // Auto dismiss
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(_durationSeconds)
            };
            _timer.Tick += (s, e) =>
            {
                _timer.Stop();
                DismissWithAnimation();
            };
            _timer.Start();
        }

        private void DismissWithAnimation()
        {
            _timer?.Stop();
            _progressTimer?.Stop();

            var slideOut = (Storyboard)FindResource("SlideOut");
            slideOut.Completed += (s, e) => Close();
            slideOut.Begin();
        }

        private void CloseBtn_Click(object sender,
            RoutedEventArgs e)
        {
            DismissWithAnimation();
        }
    }
}

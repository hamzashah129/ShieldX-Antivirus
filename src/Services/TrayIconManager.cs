using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using System.Reflection;

namespace ShieldX.Services
{
    /// <summary>
    /// Manages system tray icon and context menu for ShieldX
    /// </summary>
    public class TrayIconManager : IDisposable
    {
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _contextMenu;
        private WindowState _lastWindowState = WindowState.Normal;
        private bool _isMinimizedToTray;

        public event EventHandler<EventArgs> OpenRequested;
        public event EventHandler<EventArgs> QuickScanRequested;
        public event EventHandler<EventArgs> PauseProtectionRequested;
        public event EventHandler<EventArgs> ExitRequested;

        public void Initialize(Window mainWindow)
        {
            try
            {
                // Create notify icon
                _trayIcon = new NotifyIcon
                {
                    Icon = GetAppIcon(),
                    Visible = true,
                    Text = "ShieldX Professional Antivirus v3.1.1"
                };

                // Handle double-click to open window
                _trayIcon.DoubleClick += (s, e) => OnOpenRequested();

                // Create context menu
                _contextMenu = new ContextMenuStrip();
                _contextMenu.Items.Add("Open ShieldX", null, (s, e) => OnOpenRequested());
                _contextMenu.Items.Add(new ToolStripSeparator());
                _contextMenu.Items.Add("Quick Scan", null, (s, e) => OnQuickScanRequested());
                _contextMenu.Items.Add("Pause Protection", null, (s, e) => OnPauseProtectionRequested());
                _contextMenu.Items.Add(new ToolStripSeparator());
                _contextMenu.Items.Add("Exit", null, (s, e) => OnExitRequested());

                _trayIcon.ContextMenuStrip = _contextMenu;

                // Hook into window state changes
                mainWindow.StateChanged += MainWindow_StateChanged;
                mainWindow.Closing += MainWindow_Closing;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to initialize system tray icon");
            }
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            var window = sender as Window;
            if (window == null) return;

            if (window.WindowState == WindowState.Minimized)
            {
                _lastWindowState = WindowState.Normal;
                window.Hide();
                _isMinimizedToTray = true;
                // Show modern toast notification instead of balloon tip
                ToastNotificationService.ShowBackgroundMessage("Protection is running in the background.");
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var window = sender as Window;
            if (window != null && window.WindowState == WindowState.Minimized)
            {
                e.Cancel = true;
            }
        }

        public void ShowBalloon(string title, string message, ToolTipIcon icon = ToolTipIcon.Info, int timeout = 3000)
        {
            // Use modern Toast notifications instead of old balloon tips
            var severity = icon switch
            {
                ToolTipIcon.Warning => ToastNotificationService.NotificationSeverity.Warning,
                ToolTipIcon.Error => ToastNotificationService.NotificationSeverity.Critical,
                ToolTipIcon.Info => ToastNotificationService.NotificationSeverity.Info,
                _ => ToastNotificationService.NotificationSeverity.Info
            };

            ToastNotificationService.ShowNotification(title, message, null, severity, timeout / 1000);
        }

        public void ShowThreatNotification(string fileName, string threatName, string severity)
        {
            // Use modern Toast notification with severity-based styling
            ToastNotificationService.ShowThreatNotification(fileName, threatName, severity);
        }

        public void ShowScanCompleted(int threatsFound)
        {
            // Use modern Toast notification instead of balloon tip
            var title = threatsFound == 0 ? "✅ Scan Complete" : "⚠️ Scan Complete";
            var message = threatsFound == 0
                ? "No threats found."
                : $"{threatsFound} threat(s) found.";
            var severity = threatsFound == 0 
                ? ToastNotificationService.NotificationSeverity.Success 
                : ToastNotificationService.NotificationSeverity.Warning;

            ToastNotificationService.ShowNotification(title, message, null, severity, 6);
        }

        public void Restore(Window window)
        {
            if (_isMinimizedToTray && window != null)
            {
                window.Show();
                window.WindowState = _lastWindowState == WindowState.Minimized ? WindowState.Normal : _lastWindowState;
                window.Activate();
                _isMinimizedToTray = false;
            }
        }

        private Icon GetAppIcon()
        {
            try
            {
                // Try to get embedded icon from resources
                var assembly = Assembly.GetExecutingAssembly();
                var iconName = "ShieldX.assets.shieldx.ico";
                using var stream = assembly.GetManifestResourceStream(iconName);
                if (stream != null)
                {
                    return new Icon(stream);
                }
            }
            catch
            {
                // Fall back to system icon
            }

            // Return professional blue shield icon
            return CreateCustomIcon();
        }

        private static Icon CreateCustomIcon()
        {
            try
            {
                // Create a simple professional shield icon
                using (var bitmap = new Bitmap(16, 16))
                {
                    using (var g = Graphics.FromImage(bitmap))
                    {
                        g.Clear(Color.Transparent);
                        
                        // Draw simple shield with professional blue
                        var brush = new SolidBrush(Color.FromArgb(0, 122, 204));
                        var points = new PointF[] {
                            new PointF(8, 1), new PointF(13, 4), new PointF(14, 9),
                            new PointF(8, 15), new PointF(2, 9), new PointF(3, 4)
                        };
                        g.FillPolygon(brush, points);
                        brush.Dispose();
                    }
                    var handle = bitmap.GetHicon();
                    return Icon.FromHandle(handle);
                }
            }
            catch
            {
                return SystemIcons.Shield;
            }
        }

        private void OnOpenRequested() => OpenRequested?.Invoke(this, EventArgs.Empty);
        private void OnQuickScanRequested() => QuickScanRequested?.Invoke(this, EventArgs.Empty);
        private void OnPauseProtectionRequested() => PauseProtectionRequested?.Invoke(this, EventArgs.Empty);
        private void OnExitRequested() => ExitRequested?.Invoke(this, EventArgs.Empty);

        public void Dispose()
        {
            _contextMenu?.Dispose();
            _trayIcon?.Dispose();
        }
    }
}

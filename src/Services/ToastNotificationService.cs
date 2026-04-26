using System;
using System.Collections.Generic;
using System.IO;
using ShieldX.Views;
using System.Windows;

namespace ShieldX.Services
{
    /// <summary>
    /// Provides modern Windows 10/11 style Toast Notifications using WPF
    /// Shows beautiful, professional notifications with animations
    /// </summary>
    public static class ToastNotificationService
    {
        private const string AppId = "ShieldX.Antivirus.App";
        private static readonly Dictionary<string, int> _recentNotifications = new();
        private static readonly object _lock = new();

        /// <summary>
        /// Shows a modern notification popup
        /// </summary>
        public static void ShowNotification(
            string title,
            string message,
            string imagePath = null,
            NotificationSeverity severity = NotificationSeverity.Info,
            int durationSeconds = 5)
        {
            try
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    var notificationType = SeverityToNotificationType(severity);
                    var popup = new NotificationPopup();
                    popup.Show(title, message, notificationType, durationSeconds, null, GetBadgeText(notificationType));
                });
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Notification failed");
            }
        }

        /// <summary>
        /// Shows a threat detection notification with custom styling
        /// </summary>
        public static void ShowThreatNotification(
            string fileName,
            string threatName,
            string severity,
            string iconPath = null)
        {
            try
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    var notificationType = NotificationType.Danger;
                    var title = "🛡️ Threat Blocked!";
                    var message = $"{Path.GetFileName(fileName)}\n{threatName}";

                    var popup = new NotificationPopup();
                    popup.Show(title, message, notificationType, 8, null, "THREAT BLOCKED");
                });
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Threat notification failed");
            }
        }

        /// <summary>
        /// Shows scan completion notification
        /// </summary>
        public static void ShowScanCompleted(
            int threatsFound,
            int filesScanned,
            long timeTakenMs = 0)
        {
            try
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    var notificationType = threatsFound == 0 ? NotificationType.Success : NotificationType.Warning;
                    var title = threatsFound == 0 ? "✅ Scan Complete" : "⚠️ Scan Complete";
                    var message = threatsFound == 0
                        ? $"Scanned {filesScanned} files. No threats found."
                        : $"Scanned {filesScanned} files. {threatsFound} threat(s) found.";

                    if (timeTakenMs > 0)
                    {
                        var seconds = timeTakenMs / 1000;
                        message += $"\nCompleted in {seconds}s";
                    }

                    var popup = new NotificationPopup();
                    popup.Show(title, message, notificationType, 6, null, threatsFound == 0 ? "CLEAN" : "THREATS FOUND");
                });
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Scan completed notification failed");
            }
        }

        /// <summary>
        /// Shows protection status notification
        /// </summary>
        public static void ShowProtectionStatus(
            bool isEnabled,
            string details = "")
        {
            try
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    var notificationType = isEnabled ? NotificationType.Success : NotificationType.Warning;
                    var title = isEnabled ? "🟢 Protection Active" : "🔴 Protection Disabled";
                    var message = isEnabled
                        ? $"Real-time protection is running.\n{details}".Trim()
                        : $"Real-time protection is disabled.\n{details}".Trim();

                    var popup = new NotificationPopup();
                    popup.Show(title, message, notificationType, 5, null, isEnabled ? "ACTIVE" : "DISABLED");
                });
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Protection status notification failed");
            }
        }

        /// <summary>
        /// Shows update available notification
        /// </summary>
        public static void ShowUpdateAvailable(
            string version,
            string releaseNotes = "")
        {
            try
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    var title = "📦 Update Available";
                    var message = $"ShieldX v{version} is ready to install.";
                    if (!string.IsNullOrEmpty(releaseNotes))
                        message += $"\n{releaseNotes}";

                    var popup = new NotificationPopup();
                    popup.Show(title, message, NotificationType.Info, 10, null, "UPDATE");
                });
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Update notification failed");
            }
        }

        /// <summary>
        /// Shows USB detection notification
        /// </summary>
        public static void ShowUsbDetected(
            string usbName,
            bool isTrusted = false)
        {
            try
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    var notificationType = isTrusted ? NotificationType.Success : NotificationType.Warning;
                    var title = isTrusted ? "🔌 USB Detected (Trusted)" : "🔌 USB Detected";
                    var message = isTrusted
                        ? $"{usbName}\nThis is a trusted device."
                        : $"{usbName}\nScanning for threats...";

                    var popup = new NotificationPopup();
                    popup.Show(title, message, notificationType, 6, null, isTrusted ? "TRUSTED" : "SCANNING");
                });
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "USB notification failed");
            }
        }

        /// <summary>
        /// Shows background operation notification
        /// </summary>
        public static void ShowBackgroundMessage(
            string message)
        {
            try
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    var title = "ShieldX";
                    var popup = new NotificationPopup();
                    popup.Show(title, message, NotificationType.Info, 4, null, "INFO");
                });
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Background message notification failed");
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Helper Methods
        // ─────────────────────────────────────────────────────────────

        private static NotificationType SeverityToNotificationType(NotificationSeverity severity)
        {
            return severity switch
            {
                NotificationSeverity.Critical => NotificationType.Danger,
                NotificationSeverity.High => NotificationType.Warning,
                NotificationSeverity.Warning => NotificationType.Warning,
                NotificationSeverity.Success => NotificationType.Success,
                _ => NotificationType.Info
            };
        }

        private static string GetBadgeText(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => "SUCCESS",
                NotificationType.Warning => "WARNING",
                NotificationType.Danger => "DANGER",
                NotificationType.Usb => "USB",
                _ => "INFO"
            };
        }

        public enum NotificationSeverity
        {
            Info,
            Success,
            Warning,
            High,
            Critical
        }
    }
}

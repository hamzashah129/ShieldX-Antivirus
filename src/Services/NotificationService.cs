using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using ShieldX.Views;

namespace ShieldX.Services
{
    public static class NotificationService
    {
        private static readonly List<NotificationPopup>
            _active = new();
        private static readonly object _lock = new();

        public static void Show(
            string title,
            string message,
            NotificationType type = NotificationType.Info,
            int durationSeconds = 5,
            Action? onClick = null,
            string badge = "")
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                try
                {
                    // Stack notifications
                    lock (_lock)
                    {
                        // Move existing up
                        foreach (var existing in _active)
                        {
                            existing.Top -= 115;
                        }
                    }

                    var popup = new NotificationPopup();
                    popup.Closed += (s, e) =>
                    {
                        lock (_lock)
                            _active.Remove(popup);
                    };

                    lock (_lock)
                        _active.Add(popup);

                    popup.Show(title, message, type,
                        durationSeconds, onClick, badge);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[Notification] Show failed: {ex.Message}");
                }
            });
        }

        // Convenience methods
        public static void ShowSuccess(string title, string msg,
            int duration = 4) =>
            Show(title, msg, NotificationType.Success, duration);

        public static void ShowDanger(string title, string msg,
            int duration = 8) =>
            Show(title, msg, NotificationType.Danger, duration);

        public static void ShowWarning(string title, string msg,
            int duration = 6) =>
            Show(title, msg, NotificationType.Warning, duration);

        public static void ShowUsb(string title, string msg,
            int duration = 5, Action? onClick = null) =>
            Show(title, msg, NotificationType.Usb, duration,
                onClick);

        public static void ShowInfo(string title, string msg,
            int duration = 4) =>
            Show(title, msg, NotificationType.Info, duration);
    }
}

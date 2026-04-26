using System;
using System.IO;
using System.Management;
using System.Threading.Tasks;
using System.Windows;

namespace ShieldX.Services
{
    public class UsbSecurityService : IDisposable
    {
        private ManagementEventWatcher? _watcher;
        private readonly ScanEngine _engine = new();
        private readonly RealTimeProtectionService _rt;

        public event Action<string, int, int>? UsbScanCompleted;

        public UsbSecurityService(RealTimeProtectionService rt)
        {
            _rt = rt;
        }

        public void Start()
        {
            try
            {
                var query = new WqlEventQuery(
                    "SELECT * FROM Win32_VolumeChangeEvent " +
                    "WHERE EventType = 2");

                _watcher = new ManagementEventWatcher(query);
                _watcher.EventArrived += OnDriveInserted;
                _watcher.Start();

                System.Diagnostics.Debug.WriteLine(
                    "[USB] USB monitoring started");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[USB] Start failed: {ex.Message}");
            }
        }

        private void OnDriveInserted(object sender,
            EventArrivedEventArgs e)
        {
            try
            {
                string? driveName = e.NewEvent
                    .Properties["DriveName"].Value?.ToString();

                if (string.IsNullOrEmpty(driveName)) return;

                System.Diagnostics.Debug.WriteLine(
                    $"[USB] Drive inserted: {driveName}");

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    var result = Views.UsbScanDialog.ShowDialog(driveName);

                    if (result == true)
                        _ = ScanUsbDriveAsync(driveName);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[USB] Event error: {ex.Message}");
            }
        }

        private async Task ScanUsbDriveAsync(string driveLetter)
        {
            try
            {
                // Show notification that scan started
                ToastNotificationService.ShowNotification(
                    "🔍 USB Scan Started",
                    $"Scanning {driveLetter}... ShieldX will notify you when complete.",
                    null,
                    ToastNotificationService.NotificationSeverity.Info,
                    5);

                var result = await Task.Run(() =>
                    _engine.FullScanAsync(null,
                        System.Threading.CancellationToken.None));

                foreach (var threat in result.ThreatsFound)
                {
                    var blocked = new Services.BlockedThreatItem
                    {
                        FileName   = threat.FileName,
                        FilePath   = threat.FilePath,
                        ThreatName = threat.ThreatName,
                        Severity   = "High",
                        Action     = "USB-Quarantined",
                        Status     = "Blocked"
                    };
                    _rt.SaveToHistory(blocked);
                }

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    if (result.ThreatsFound.Count > 0)
                    {
                        ToastNotificationService.ShowNotification(
                            "⚠️ USB Scan Complete — Threats Found",
                            $"Drive: {driveLetter}\nFiles Scanned: {result.FilesScanned}\nThreats Found: {result.ThreatsFound.Count}\n\nThreats have been quarantined automatically.",
                            null,
                            ToastNotificationService.NotificationSeverity.Warning,
                            8);
                    }
                    else
                    {
                        ToastNotificationService.ShowNotification(
                            "✅ USB Drive is Clean",
                            $"Drive: {driveLetter}\nFiles Scanned: {result.FilesScanned}\nNo threats detected.",
                            null,
                            ToastNotificationService.NotificationSeverity.Success,
                            6);
                    }

                    UsbScanCompleted?.Invoke(driveLetter,
                        result.FilesScanned,
                        result.ThreatsFound.Count);
                });
            }
            catch (Exception ex)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                    ToastNotificationService.ShowNotification(
                        "❌ USB Scan Error",
                        ex.Message,
                        null,
                        ToastNotificationService.NotificationSeverity.Critical,
                        6));
            }
        }

        public void Stop()
        {
            try
            {
                _watcher?.Stop();
                _watcher?.Dispose();
            }
            catch { }
        }

        public void Dispose() => Stop();
    }
}

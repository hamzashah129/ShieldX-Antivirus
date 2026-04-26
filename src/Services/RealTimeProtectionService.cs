using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ShieldX.Services
{
    public class BlockedThreatItem
    {
        public string Id          { get; set; } = Guid.NewGuid().ToString();
        public DateTime BlockedAt { get; set; } = DateTime.Now;
        public string FileName    { get; set; } = "";
        public string FilePath    { get; set; } = "";
        public string ThreatName  { get; set; } = "";
        public string Severity    { get; set; } = "High";
        public string Action      { get; set; } = "Quarantined";
        public string Status      { get; set; } = "Blocked";
        public string TimeText    => BlockedAt.ToString("MM/dd/yyyy hh:mm:ss tt");
    }

    public class RealTimeProtectionService : IDisposable
    {
        private readonly List<FileSystemWatcher> _watchers = new();
        private readonly ScanEngine _engine = new();
        private readonly HashSet<string> _processing = new();
        private readonly object _lock = new();
        private bool _isRunning;
        public bool _isPaused { get; set; }

        private static readonly string HistoryPath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "ShieldX", "blocked_threats.json");

        public event Action<BlockedThreatItem>? ThreatBlocked;
        public event Action<string, string, string>? ThreatDetected;  // For compatibility with old code
        public event Action<string>? StatusChanged;

        private static readonly string[] Exclusions =
        {
            System.Diagnostics.Process.GetCurrentProcess()
                .MainModule?.FileName ?? "",
            Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData), "ShieldX"),
        };

        private static readonly string[] WatchPaths =
        {
            Environment.GetFolderPath(
                Environment.SpecialFolder.Desktop),
            Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile), "Downloads"),
            Environment.GetFolderPath(
                Environment.SpecialFolder.Startup),
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonStartup),
            Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData), "Temp"),
        };

        private static readonly HashSet<string> WatchedExts = new(
            StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".dll", ".bat", ".cmd", ".vbs",
            ".ps1", ".scr", ".pif", ".com", ".msi",
            ".hta", ".js", ".jse", ".wsf", ".lnk"
        };

        public bool IsRunning => _isRunning;

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;

            foreach (var path in WatchPaths)
            {
                if (!Directory.Exists(path)) continue;
                try
                {
                    var w = new FileSystemWatcher(path)
                    {
                        Filter = "*.*",
                        IncludeSubdirectories = true,
                        NotifyFilter = NotifyFilters.FileName
                                     | NotifyFilters.LastWrite
                                     | NotifyFilters.CreationTime,
                        EnableRaisingEvents = true
                    };
                    w.Created += (s, e) => OnFile(e.FullPath);
                    w.Changed += (s, e) => OnFile(e.FullPath);
                    w.Renamed += (s, e) => OnFile(e.FullPath);
                    _watchers.Add(w);
                    Debug.WriteLine($"[RT] Watching: {path}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[RT] Watch failed {path}: {ex.Message}");
                }
            }

            StatusChanged?.Invoke("Real-time protection active ✓");
            Debug.WriteLine("[RT] Protection started");
        }

        public void Stop()
        {
            _isRunning = false;
            foreach (var w in _watchers)
            {
                w.EnableRaisingEvents = false;
                w.Dispose();
            }
            _watchers.Clear();
            StatusChanged?.Invoke("Real-time protection stopped");
        }

        private void OnFile(string path)
        {
            string ext = Path.GetExtension(path);
            if (!WatchedExts.Contains(ext)) return;

            lock (_lock)
            {
                if (_processing.Contains(path)) return;
                _processing.Add(path);
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(800);

                    if (!File.Exists(path)) return;
                    if (IsExcluded(path)) return;

                    Debug.WriteLine($"[RT] Scanning: {path}");

                    // Perform signature-based scan
                    var result = await _engine.ScanFileAsync(
                        path, null, CancellationToken.None);

                    // Perform ML-based analysis
                    var mlAnalysis = await PerformMLAnalysisAsync(path);

                    // Check if the file is flagged as a threat (signature or ML)
                    bool isThreat = (result != null && result.Type == "Threat") ||
                                   (mlAnalysis != null && mlAnalysis.Confidence > 0.7);

                    if (isThreat)
                    {
                        string threatName = GetThreatName(path);
                        
                        // Enhance threat name with ML prediction if available
                        if (mlAnalysis != null && mlAnalysis.Confidence > 0.7)
                        {
                            threatName = $"{mlAnalysis.PredictedThreatType} (ML detected)";
                        }

                        Debug.WriteLine(
                            $"[RT] THREAT FOUND: {path} = {threatName}");

                        bool quarantined = await QuarantineAsync(path);

                        var item = new BlockedThreatItem
                        {
                            FileName   = Path.GetFileName(path),
                            FilePath   = path,
                            ThreatName = threatName,
                            Severity   = mlAnalysis?.Confidence > 0.9 ? "Critical" : "High",
                            Action     = quarantined
                                ? "Quarantined" : "Detected",
                            Status     = quarantined
                                ? "Blocked" : "Alert"
                        };

                        SaveToHistory(item);
                        
                        // Log the threat to LogService
                        try
                        {
                            string scanMethod = mlAnalysis?.Confidence > 0.7 ? "ML Analysis" : "Signature";
                            Serilog.Log.Warning(
                                $"Threat {(quarantined ? "blocked" : "detected")} by {scanMethod}: {threatName} in {path}");
                            LogService.Instance.AddWarning(
                                $"Threat {(quarantined ? "blocked" : "detected")}: {threatName}",
                                $"File: {path}");
                        }
                        catch { }

                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            ThreatBlocked?.Invoke(item);
                            ThreatDetected?.Invoke(item.FileName, item.ThreatName, item.FilePath);
                        });

                        ShowNotification(item);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[RT] Error: {ex.Message}");
                }
                finally
                {
                    await Task.Delay(30000);
                    lock (_lock) _processing.Remove(path);
                }
            });
        }

        private async Task<bool> QuarantineAsync(string filePath)
        {
            string quarDir = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "ShieldX", "Quarantine");

            try
            {
                Directory.CreateDirectory(quarDir);
                string dest = Path.Combine(quarDir,
                    $"{Guid.NewGuid()}_{Path.GetFileName(filePath)}.quar");

                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        if (IsFileLocked(filePath))
                        {
                            await Task.Delay(1000);
                            continue;
                        }
                        File.Move(filePath, dest,
                            overwrite: false);
                        Debug.WriteLine(
                            $"[RT] Quarantined to: {dest}");
                        return true;
                    }
                    catch (IOException)
                    {
                        await Task.Delay(1000);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"[RT] Quarantine failed: {ex.Message}");
            }
            return false;
        }

        private static void ShowNotification(BlockedThreatItem item)
        {
            Task.Run(() =>
            {
                try
                {
                    // Use modern Toast notifications instead of old balloon tips
                    ToastNotificationService.ShowThreatNotification(
                        item.FileName,
                        item.ThreatName,
                        "High", // Default to high severity
                        null);
                }
                catch (Exception ex)
                {
                    Serilog.Log.Warning(ex, "Toast notification failed, using fallback notification");
                    try
                    {
                        Application.Current?.Dispatcher.Invoke(() =>
                            MessageBox.Show(
                                $"THREAT BLOCKED!\n\n" +
                                $"File: {item.FileName}\n" +
                                $"Threat: {item.ThreatName}\n" +
                                $"Action: {item.Action}",
                                "ShieldX Security Alert",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning));
                    }
                    catch { }
                }
            });
        }

        public void SaveToHistory(BlockedThreatItem item)
        {
            try
            {
                Directory.CreateDirectory(
                    Path.GetDirectoryName(HistoryPath)!);

                var list = LoadHistory();
                list.Insert(0, item);
                if (list.Count > 500)
                    list = list.Take(500).ToList();

                File.WriteAllText(HistoryPath,
                    System.Text.Json.JsonSerializer.Serialize(
                        list, new System.Text.Json.JsonSerializerOptions
                        {
                            WriteIndented = true
                        }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RT] Save history failed: {ex.Message}");
            }
        }

        public List<BlockedThreatItem> LoadHistory()
        {
            try
            {
                if (!File.Exists(HistoryPath))
                    return new List<BlockedThreatItem>();

                var json = File.ReadAllText(HistoryPath);
                return System.Text.Json.JsonSerializer
                    .Deserialize<List<BlockedThreatItem>>(json)
                    ?? new List<BlockedThreatItem>();
            }
            catch { return new List<BlockedThreatItem>(); }
        }

        private static string GetThreatName(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path)
                             .ToLower();
            string[] patterns = {
                "keylogger","trojan","malware","ransomware",
                "xworm","darkcomet","spyware","rootkit",
                "backdoor","mimikatz","stealer","injector",
                "crypter","payload","shellcode","rat",
                "cryptolocker","wannacry","netbus"
            };
            foreach (var p in patterns)
                if (name.Contains(p))
                    return $"Suspicious.{char.ToUpper(p[0])}{p.Substring(1)}";
            return "Suspicious.UnknownThreat";
        }

        private static bool IsFileLocked(string path)
        {
            try
            {
                using var s = File.Open(path, FileMode.Open,
                    FileAccess.ReadWrite, FileShare.None);
                return false;
            }
            catch { return true; }
        }

        private static bool IsExcluded(string path) =>
            Exclusions.Any(e =>
                !string.IsNullOrEmpty(e) &&
                path.StartsWith(e,
                    StringComparison.OrdinalIgnoreCase));

        private async Task<MLThreatAnalysisService.MLThreatAnalysis> PerformMLAnalysisAsync(string filePath)
        {
            try
            {
                var mlService = BackgroundServiceManager.GetMLAnalysisService();
                if (mlService == null || !mlService.IsReady)
                    return null;

                return await mlService.AnalyzeFileAsync(filePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RT] ML analysis error: {ex.Message}");
                return null;
            }
        }

        public void Dispose() => Stop();
    }
}
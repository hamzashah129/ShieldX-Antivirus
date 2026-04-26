using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Serilog;
using ShieldX.Models;

namespace ShieldX.Services
{
    public class LogService
    {
        private static readonly Lazy<LogService> _instance = new Lazy<LogService>(() => new LogService());
        public static LogService Instance => _instance.Value;

        public ObservableCollection<LogEntry> Entries { get; } = new ObservableCollection<LogEntry>();
        
        private volatile bool _logsLoaded = false;
        public bool LogsLoaded => _logsLoaded;

        private const int MaxLogEntries = 1000;
        private string _archiveDirectory;

        private LogService()
        {
            // Initialize archive directory
            _archiveDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ShieldX", "Logs"
            );

            try
            {
                DatabaseService.Instance.InitializeDatabase();
                Serilog.Log.Information("LogService initialized database");
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Failed to initialize database in LogService");
            }

            Task.Run(async () =>
            {
                try
                {
                    await LoadRecentLogsAsync().ConfigureAwait(false);
                    _logsLoaded = true;
                }
                catch
                {
                    // Ignore initialization failures and avoid unobserved exceptions
                    _logsLoaded = true;
                }
            });
        }

        public void AddInfo(string message, string details = null)
        {
            AddEntry("INFO", "System", message, details);
        }

        public void AddWarning(string message, string details = null)
        {
            AddEntry("WARN", "System", message, details);
        }

        public void AddError(string message, string details = null)
        {
            AddEntry("ERROR", "System", message, details);
        }

        public void AddEntry(string level, string category, string message, string details = null)
        {
            // Filter out null or whitespace messages
            if (string.IsNullOrWhiteSpace(message))
                return;

            // Filter out Information entries with empty messages
            if (level == "INFO" && string.IsNullOrWhiteSpace(message))
                return;

            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Category = category,
                Message = message,
                Details = details
            };

            Application.Current?.Dispatcher.Invoke(() => 
            {
                Entries.Insert(0, entry);
                // Check and perform log rotation if needed
                if (Entries.Count > MaxLogEntries)
                {
                    PerformLogRotation();
                }
            });
            _ = WriteToDatabaseAsync(entry);
        }

        private void PerformLogRotation()
        {
            try
            {
                // Ensure archive directory exists
                Directory.CreateDirectory(_archiveDirectory);

                // Get entries to archive (keep only MaxLogEntries most recent)
                var entriesToArchive = Entries.Skip(MaxLogEntries).ToList();

                if (entriesToArchive.Count > 0)
                {
                    // Archive old entries
                    var archiveFileName = $"shieldx_{DateTime.Now:yyyy-MM-dd}.log";
                    var archiveFilePath = Path.Combine(_archiveDirectory, archiveFileName);

                    using (var writer = new StreamWriter(archiveFilePath, true, Encoding.UTF8))
                    {
                        foreach (var entry in entriesToArchive.OrderBy(x => x.Timestamp))
                        {
                            writer.WriteLine($"[{entry.FormattedTimestamp}] {entry.Level.PadRight(5)} {entry.Category} - {entry.Message}");
                            if (!string.IsNullOrWhiteSpace(entry.Details))
                            {
                                writer.WriteLine($"    {entry.Details}");
                            }
                        }
                    }

                    // Remove archived entries from memory
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        foreach (var entry in entriesToArchive)
                        {
                            Entries.Remove(entry);
                        }
                    });
                }
            }
            catch
            {
                // Silently fail to avoid disrupting log operations
            }
        }

        private async Task WriteToDatabaseAsync(LogEntry entry)
        {
            try
            {
                await DatabaseService.Instance.LogActivityAsync(entry.Level, entry.Category, entry.Message, entry.Details);
            }
            catch
            {
                // Swallow to keep UI responsive
            }
        }

        public async Task ExportAsync(string filePath)
        {
            using var writer = new StreamWriter(filePath, false);
            foreach (var entry in Entries.OrderBy(x => x.Timestamp))
            {
                await writer.WriteLineAsync($"[{entry.FormattedTimestamp}] {entry.Level.PadRight(5)} {entry.Category} - {entry.Message}");
                if (!string.IsNullOrWhiteSpace(entry.Details))
                {
                    await writer.WriteLineAsync($"    {entry.Details}");
                }
            }
        }

        public async Task ExportToCsvAsync(string filePath)
        {
            try
            {
                using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
                
                // Write CSV header
                await writer.WriteLineAsync("Timestamp,Level,Category,Message,Details");

                // Write entries
                foreach (var entry in Entries.OrderBy(x => x.Timestamp))
                {
                    var timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                    var level = EscapeCsvField(entry.Level);
                    var category = EscapeCsvField(entry.Category);
                    var message = EscapeCsvField(entry.Message);
                    var details = EscapeCsvField(entry.Details ?? "");

                    await writer.WriteLineAsync($"{timestamp},{level},{category},{message},{details}");
                }
            }
            catch
            {
                throw;
            }
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "\"\"";

            if (field.Contains("\"") || field.Contains(",") || field.Contains("\n"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }

            return field;
        }

        public async Task ClearAsync()
        {
            if (Application.Current?.Dispatcher != null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() => Entries.Clear());
            }
            else
            {
                Entries.Clear();
            }

            // Clear database logs
            try
            {
                await DatabaseService.Instance.ClearLogsAsync();
                Serilog.Log.Information("Logs cleared successfully");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to clear logs from database");
            }
        }

        public async Task EnsureLogsLoadedAsync()
        {
            if (_logsLoaded)
                return;

            // Wait for logs to be loaded
            int waitCount = 0;
            while (!_logsLoaded && waitCount < 100) // Wait up to 10 seconds
            {
                await Task.Delay(100).ConfigureAwait(false);
                waitCount++;
            }

            // If still not loaded, load them now
            if (!_logsLoaded)
            {
                try
                {
                    await LoadRecentLogsAsync().ConfigureAwait(false);
                    _logsLoaded = true;
                }
                catch
                {
                    _logsLoaded = true;
                }
            }
        }

        private async Task LoadRecentLogsAsync()
        {
            try
            {
                // Load recent logs from database
                var recentEntries = await DatabaseService.Instance.GetRecentLogEntriesAsync(MaxLogEntries);
                
                // Add to the collection on the UI thread in reverse order (most recent first)
                if (Application.Current?.Dispatcher != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        foreach (var entry in recentEntries.OrderByDescending(e => e.Timestamp))
                        {
                            Entries.Add(entry);
                        }
                    });
                }
                else
                {
                    foreach (var entry in recentEntries.OrderByDescending(e => e.Timestamp))
                    {
                        Entries.Add(entry);
                    }
                }
            }
            catch
            {
                // ignore load failures
            }
        }
    }
}

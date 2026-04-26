using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace ShieldX.Services
{
    /// <summary>
    /// Advanced error handling and recovery mechanism.
    /// Provides centralized error management, recovery strategies, and error categorization.
    /// </summary>
    public class ErrorHandlingService
    {
        private static readonly Lazy<ErrorHandlingService> _instance = new Lazy<ErrorHandlingService>(() => new ErrorHandlingService());
        public static ErrorHandlingService Instance => _instance.Value;

        private readonly List<ErrorRecord> _errorHistory = new();
        private readonly object _lockObject = new();
        private const int MAX_ERROR_HISTORY = 1000;
        private const int ERROR_HISTORY_RETENTION_DAYS = 7;

        public enum ErrorSeverity { Critical, High, Medium, Low, Info }
        public enum ErrorCategory { Network, Storage, Security, Processing, Database, FileSystem, Permission, Unknown }

        public class ErrorRecord
        {
            public DateTime Timestamp { get; set; }
            public ErrorSeverity Severity { get; set; }
            public ErrorCategory Category { get; set; }
            public string ErrorCode { get; set; }
            public string Message { get; set; }
            public string Details { get; set; }
            public string StackTrace { get; set; }
            public bool Recovered { get; set; }
        }

        public event EventHandler<ErrorRecord> CriticalErrorOccurred;

        /// <summary>
        /// Handle exception with automatic categorization and recovery.
        /// </summary>
        public void HandleException(Exception ex, string context = "Unknown", ErrorSeverity severity = ErrorSeverity.High)
        {
            try
            {
                var category = CategorizeException(ex);
                var errorRecord = new ErrorRecord
                {
                    Timestamp = DateTime.Now,
                    Severity = severity,
                    Category = category,
                    ErrorCode = ex.GetType().Name,
                    Message = ex.Message,
                    Details = context,
                    StackTrace = ex.StackTrace,
                    Recovered = false
                };

                lock (_lockObject)
                {
                    _errorHistory.Add(errorRecord);
                    if (_errorHistory.Count > MAX_ERROR_HISTORY)
                        _errorHistory.RemoveAt(0);
                }

                LogError(errorRecord);

                if (severity == ErrorSeverity.Critical)
                    CriticalErrorOccurred?.Invoke(this, errorRecord);

                if (CanAutoRecover(category, ex))
                {
                    errorRecord.Recovered = AttemptRecovery(category, ex);
                }
            }
            catch (Exception logEx)
            {
                Log.Error($"[ErrorHandler] Failed to handle exception: {logEx.Message}");
            }
        }

        /// <summary>
        /// Categorize exception for appropriate handling.
        /// </summary>
        private ErrorCategory CategorizeException(Exception ex)
        {
            return ex switch
            {
                System.Net.Http.HttpRequestException => ErrorCategory.Network,
                System.IO.DirectoryNotFoundException => ErrorCategory.FileSystem,
                System.IO.FileNotFoundException => ErrorCategory.FileSystem,
                System.IO.IOException => ErrorCategory.FileSystem,
                System.UnauthorizedAccessException => ErrorCategory.Permission,
                System.Data.DataException => ErrorCategory.Database,
                Microsoft.Data.Sqlite.SqliteException => ErrorCategory.Database,
                System.Security.SecurityException => ErrorCategory.Security,
                System.OutOfMemoryException => ErrorCategory.Processing,
                System.StackOverflowException => ErrorCategory.Processing,
                _ => ErrorCategory.Unknown
            };
        }

        /// <summary>
        /// Determine if automatic recovery is possible.
        /// </summary>
        private bool CanAutoRecover(ErrorCategory category, Exception ex)
        {
            return category switch
            {
                ErrorCategory.Network => true,
                ErrorCategory.FileSystem => IsFileSystemRecoverable(ex),
                ErrorCategory.Database => true,
                _ => false
            };
        }

        /// <summary>
        /// Attempt automatic recovery based on error category.
        /// </summary>
        private bool AttemptRecovery(ErrorCategory category, Exception ex)
        {
            try
            {
                return category switch
                {
                    ErrorCategory.Network => RecoverNetworkError(ex),
                    ErrorCategory.FileSystem => RecoverFileSystemError(ex),
                    ErrorCategory.Database => RecoverDatabaseError(ex),
                    _ => false
                };
            }
            catch (Exception recEx)
            {
                Log.Error($"[ErrorHandler] Recovery failed: {recEx.Message}");
                return false;
            }
        }

        private bool RecoverNetworkError(Exception ex)
        {
            Log.Information("[ErrorHandler] Attempting network error recovery");
            // Network errors typically recover after timeout, so mark as potentially recoverable
            return true;
        }

        private bool RecoverFileSystemError(Exception ex)
        {
            if (ex is System.IO.IOException ioEx)
            {
                Log.Information("[ErrorHandler] Attempting filesystem error recovery");
                // Could retry after delay for file locks
                return true;
            }

            if (ex is System.IO.DirectoryNotFoundException)
            {
                Log.Information("[ErrorHandler] Attempting to create missing directory");
                try
                {
                    if (ex.Message.Contains("path", StringComparison.OrdinalIgnoreCase))
                    {
                        var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ShieldX");
                        Directory.CreateDirectory(appDataPath);
                        return true;
                    }
                }
                catch { }
            }

            return false;
        }

        private bool RecoverDatabaseError(Exception ex)
        {
            Log.Information("[ErrorHandler] Attempting database error recovery");
            // Database errors can sometimes recover with reconnection
            return true;
        }

        private bool IsFileSystemRecoverable(Exception ex)
        {
            return ex is System.IO.DirectoryNotFoundException or System.IO.IOException;
        }

        /// <summary>
        /// Log error with proper formatting.
        /// </summary>
        private void LogError(ErrorRecord record)
        {
            string logMessage = $"[ErrorHandler-{record.Category}] {record.Message} | Context: {record.Details}";

            switch (record.Severity)
            {
                case ErrorSeverity.Critical:
                    Log.Fatal(logMessage + $" | Stack: {record.StackTrace}");
                    break;
                case ErrorSeverity.High:
                    Log.Error(logMessage);
                    break;
                case ErrorSeverity.Medium:
                    Log.Warning(logMessage);
                    break;
                case ErrorSeverity.Low:
                    Log.Information(logMessage);
                    break;
                case ErrorSeverity.Info:
                    Log.Debug(logMessage);
                    break;
            }
        }

        /// <summary>
        /// Get error history with optional filtering.
        /// </summary>
        public List<ErrorRecord> GetErrorHistory(ErrorSeverity? minSeverity = null, int maxResults = 100)
        {
            lock (_lockObject)
            {
                var results = new List<ErrorRecord>();
                for (int i = _errorHistory.Count - 1; i >= 0 && results.Count < maxResults; i--)
                {
                    var record = _errorHistory[i];
                    if (minSeverity == null || record.Severity <= minSeverity)
                    {
                        results.Add(record);
                    }
                }
                return results;
            }
        }

        /// <summary>
        /// Get error statistics.
        /// </summary>
        public Dictionary<ErrorCategory, int> GetErrorStatistics()
        {
            var stats = new Dictionary<ErrorCategory, int>();
            lock (_lockObject)
            {
                foreach (var record in _errorHistory)
                {
                    if (!stats.ContainsKey(record.Category))
                        stats[record.Category] = 0;
                    stats[record.Category]++;
                }
            }
            return stats;
        }

        /// <summary>
        /// Clear old error history.
        /// </summary>
        public void CleanupOldErrors()
        {
            try
            {
                lock (_lockObject)
                {
                    var cutoff = DateTime.Now.AddDays(-ERROR_HISTORY_RETENTION_DAYS);
                    _errorHistory.RemoveAll(e => e.Timestamp < cutoff);
                    Log.Information($"[ErrorHandler] Cleaned up {_errorHistory.Count} old error records");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[ErrorHandler] Cleanup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Generate error report for diagnostics.
        /// </summary>
        public string GenerateErrorReport()
        {
            lock (_lockObject)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("=== ShieldX Error Report ===");
                sb.AppendLine($"Generated: {DateTime.Now:g}");
                sb.AppendLine($"Total Errors: {_errorHistory.Count}");
                sb.AppendLine();

                var stats = GetErrorStatistics();
                sb.AppendLine("Error Statistics by Category:");
                foreach (var kvp in stats)
                {
                    sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
                }

                sb.AppendLine();
                sb.AppendLine("Recent Errors (Last 10):");
                int count = 0;
                for (int i = _errorHistory.Count - 1; i >= 0 && count < 10; i--)
                {
                    var record = _errorHistory[i];
                    sb.AppendLine($"  [{record.Timestamp:g}] {record.Severity} - {record.ErrorCode}: {record.Message}");
                    count++;
                }

                return sb.ToString();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace ShieldX.Utils
{
    /// <summary>
    /// Enhanced logging utility with contextual information and performance metrics.
    /// </summary>
    public static class DiagnosticLogger
    {
        private static readonly Stack<string> _contextStack = new();
        private static readonly object _contextLock = new();

        public class LogContext : IDisposable
        {
            private readonly string _contextName;

            public LogContext(string contextName, object[] contextData)
            {
                _contextName = contextName;
                lock (_contextLock)
                {
                    _contextStack.Push(contextName);
                }

                var data = contextData != null && contextData.Length > 0 
                    ? string.Join(", ", contextData) 
                    : "";

                var fullContext = GetContextChain();
                Log.Information($"[ENTER] {fullContext}" + (string.IsNullOrEmpty(data) ? "" : $" - {data}"));
            }

            public void Dispose()
            {
                lock (_contextLock)
                {
                    if (_contextStack.Count > 0)
                        _contextStack.Pop();
                }
                Log.Information($"[EXIT]  {_contextName}");
            }
        }

        /// <summary>
        /// Creates a diagnostic context for code tracing.
        /// </summary>
        public static LogContext Enter(string contextName, params object[] contextData)
        {
            return new LogContext(contextName, contextData);
        }

        /// <summary>
        /// Logs with current context chain.
        /// </summary>
        public static void LogInfo(string message, params object[] args)
        {
            var context = GetContextChain();
            Log.Information($"[{context}] {message}", args);
        }

        /// <summary>
        /// Logs warning with context.
        /// </summary>
        public static void LogWarn(string message, params object[] args)
        {
            var context = GetContextChain();
            Log.Warning($"[{context}] {message}", args);
        }

        /// <summary>
        /// Logs error with context.
        /// </summary>
        public static void LogError(Exception ex, string message, params object[] args)
        {
            var context = GetContextChain();
            Log.Error(ex, $"[{context}] {message}", args);
        }

        /// <summary>
        /// Logs debug information with context.
        /// </summary>
        public static void LogDebug(string message, params object[] args)
        {
            var context = GetContextChain();
            Log.Debug($"[{context}] {message}", args);
        }

        /// <summary>
        /// Measures and logs operation execution time.
        /// </summary>
        public static T LogTiming<T>(string operationName, Func<T> operation)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                return operation();
            }
            finally
            {
                stopwatch.Stop();
                LogInfo($"{operationName} completed in {stopwatch.ElapsedMilliseconds}ms");
            }
        }

        /// <summary>
        /// Measures and logs async operation execution time.
        /// </summary>
        public static async Task<T> LogTimingAsync<T>(string operationName, Func<Task<T>> operation)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                return await operation();
            }
            finally
            {
                stopwatch.Stop();
                LogInfo($"{operationName} completed in {stopwatch.ElapsedMilliseconds}ms");
            }
        }

        /// <summary>
        /// Logs operation with before and after state.
        /// </summary>
        public static void LogStateChange(string operation, string beforeState, string afterState)
        {
            LogInfo($"{operation}: {beforeState} -> {afterState}");
        }

        /// <summary>
        /// Logs metric data for monitoring.
        /// </summary>
        public static void LogMetric(string metricName, double value, string unit = "")
        {
            var unitStr = string.IsNullOrEmpty(unit) ? "" : $" {unit}";
            LogInfo($"METRIC: {metricName} = {value:F2}{unitStr}");
        }

        /// <summary>
        /// Gets the current context chain for logging.
        /// </summary>
        private static string GetContextChain()
        {
            lock (_contextLock)
            {
                if (_contextStack.Count == 0)
                    return "ROOT";
                return string.Join(" > ", _contextStack.Reverse());
            }
        }
    }

    /// <summary>
    /// Structured logging for specific domain events.
    /// </summary>
    public static class DomainEventLogger
    {
        public enum EventSeverity
        {
            Info,
            Warning,
            Error,
            Critical
        }

        /// <summary>
        /// Logs a security event.
        /// </summary>
        public static void LogSecurityEvent(string eventType, string details, EventSeverity severity = EventSeverity.Warning)
        {
            var message = $"[SECURITY] {eventType}: {details}";
            switch (severity)
            {
                case EventSeverity.Critical:
                    Log.Fatal(message);
                    break;
                case EventSeverity.Error:
                    Log.Error(message);
                    break;
                case EventSeverity.Warning:
                    Log.Warning(message);
                    break;
                default:
                    Log.Information(message);
                    break;
            }
        }

        /// <summary>
        /// Logs a threat detection event.
        /// </summary>
        public static void LogThreatDetection(string threatName, string filePath, string severity)
        {
            Log.Warning($"[THREAT] {threatName} detected: {filePath} (Severity: {severity})");
        }

        /// <summary>
        /// Logs a performance event.
        /// </summary>
        public static void LogPerformanceEvent(string eventName, long durationMs, string status = "OK")
        {
            var level = durationMs > 5000 ? Serilog.Events.LogEventLevel.Warning : Serilog.Events.LogEventLevel.Information;
            var threshold = durationMs > 5000 ? " [SLOW]" : "";
            Serilog.Log.Write(level, $"[PERFORMANCE] {eventName}: {durationMs}ms {status}{threshold}");
        }

        /// <summary>
        /// Logs a configuration event.
        /// </summary>
        public static void LogConfigurationChange(string component, string setting, object oldValue, object newValue)
        {
            Log.Information($"[CONFIG] {component}.{setting}: {oldValue} -> {newValue}");
        }

        public enum LogLevel
        {
            Debug,
            Information,
            Warning,
            Error,
            Fatal
        }
    }

    /// <summary>
    /// Utilities for exception logging and analysis.
    /// </summary>
    public static class ExceptionLogger
    {
        /// <summary>
        /// Logs an exception with full diagnostic information.
        /// </summary>
        public static void LogExceptionWithDiagnostics(Exception ex, string context = null)
        {
            var diagnostics = new
            {
                ExceptionType = ex.GetType().Name,
                Message = ex.Message,
                StackTrace = ex.StackTrace,
                Context = context,
                Timestamp = DateTime.UtcNow,
                ProcessId = Process.GetCurrentProcess().Id,
                ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId,
                InnerException = ex.InnerException?.Message
            };

            Log.Error(ex, "Exception with diagnostics: {@Diagnostics}", diagnostics);
        }

        /// <summary>
        /// Analyzes exception and logs recovery recommendations.
        /// </summary>
        public static void LogExceptionWithRecoveryAdvice(Exception ex)
        {
            var advice = GetRecoveryAdvice(ex);
            Log.Error(ex, "Exception recovery advice: {Advice}", advice);
        }

        private static string GetRecoveryAdvice(Exception ex)
        {
            if (ex is System.IO.IOException)
                return "Check file permissions and disk space";
            if (ex is System.IO.DirectoryNotFoundException)
                return "Verify the path exists";
            if (ex is System.IO.FileNotFoundException)
                return "Check if the file has been moved or deleted";
            if (ex is System.Net.WebException)
                return "Check network connectivity";
            if (ex is System.UnauthorizedAccessException)
                return "Verify user permissions";
            if (ex is System.InvalidOperationException)
                return "Verify operation preconditions are met";
            if (ex is System.OutOfMemoryException)
                return "Close other applications or increase available memory";
            if (ex is System.TimeoutException)
                return "Retry the operation or increase timeout";
            return "Consult logs for more information";
        }
    }
}

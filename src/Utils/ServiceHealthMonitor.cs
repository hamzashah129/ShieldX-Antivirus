using System;
using System.Collections.Generic;
using System.Diagnostics;
using Serilog;

namespace ShieldX.Utils
{
    /// <summary>
    /// Tracks health status of services and components.
    /// </summary>
    public class ServiceHealthMonitor
    {
        public enum HealthStatus
        {
            Healthy,
            Warning,
            Critical,
            Unknown
        }

        public class HealthReport
        {
            public string ComponentName { get; set; }
            public HealthStatus Status { get; set; }
            public string Message { get; set; }
            public DateTime LastChecked { get; set; }
            public TimeSpan ResponseTime { get; set; }
        }

        private readonly Dictionary<string, HealthReport> _reports = new();
        private readonly object _lockObject = new object();

        /// <summary>
        /// Registers a health check with a verification function.
        /// </summary>
        public void RegisterHealthCheck(string componentName, Func<HealthReport> healthCheckFunc)
        {
            lock (_lockObject)
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    var report = healthCheckFunc();
                    report.ResponseTime = stopwatch.Elapsed;
                    report.LastChecked = DateTime.UtcNow;
                    _reports[componentName] = report;

                    if (report.Status != HealthStatus.Healthy)
                    {
                        Log.Warning($"Health check for '{componentName}': {report.Status} - {report.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Health check for '{componentName}' failed");
                    _reports[componentName] = new HealthReport
                    {
                        ComponentName = componentName,
                        Status = HealthStatus.Critical,
                        Message = $"Health check error: {ex.Message}",
                        LastChecked = DateTime.UtcNow,
                        ResponseTime = stopwatch.Elapsed
                    };
                }
            }
        }

        /// <summary>
        /// Gets the health report for a specific component.
        /// </summary>
        public HealthReport GetHealth(string componentName)
        {
            lock (_lockObject)
            {
                return _reports.TryGetValue(componentName, out var report) 
                    ? report 
                    : new HealthReport { ComponentName = componentName, Status = HealthStatus.Unknown };
            }
        }

        /// <summary>
        /// Gets overall system health status.
        /// </summary>
        public HealthStatus GetOverallHealth()
        {
            lock (_lockObject)
            {
                var criticalCount = 0;
                var warningCount = 0;

                foreach (var report in _reports.Values)
                {
                    if (report.Status == HealthStatus.Critical)
                        criticalCount++;
                    else if (report.Status == HealthStatus.Warning)
                        warningCount++;
                }

                if (criticalCount > 0)
                    return HealthStatus.Critical;
                if (warningCount > 2)
                    return HealthStatus.Warning;
                return HealthStatus.Healthy;
            }
        }

        /// <summary>
        /// Gets all health reports.
        /// </summary>
        public IReadOnlyDictionary<string, HealthReport> GetAllReports()
        {
            lock (_lockObject)
            {
                return new Dictionary<string, HealthReport>(_reports);
            }
        }
    }
}

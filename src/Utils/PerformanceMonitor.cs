using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Serilog;

namespace ShieldX.Utils
{
    /// <summary>
    /// Utility for monitoring operation performance and resource usage.
    /// </summary>
    public class PerformanceMonitor
    {
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;

        public PerformanceMonitor(string operationName)
        {
            _operationName = operationName;
            _stopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// Logs the operation performance and returns elapsed milliseconds.
        /// </summary>
        public long Stop(bool logIfSlow = true, int thresholdMs = 5000)
        {
            _stopwatch.Stop();
            var elapsed = _stopwatch.ElapsedMilliseconds;

            if (logIfSlow && elapsed > thresholdMs)
            {
                Log.Warning($"Operation '{_operationName}' took {elapsed}ms (exceeded threshold of {thresholdMs}ms)");
            }
            else
            {
                Log.Debug($"Operation '{_operationName}' completed in {elapsed}ms");
            }

            return elapsed;
        }

        public static async Task<T> MeasureAsync<T>(
            Func<Task<T>> operation,
            string operationName,
            int slowThresholdMs = 5000)
        {
            var monitor = new PerformanceMonitor(operationName);
            try
            {
                return await operation();
            }
            finally
            {
                monitor.Stop(logIfSlow: true, thresholdMs: slowThresholdMs);
            }
        }

        public static T Measure<T>(
            Func<T> operation,
            string operationName,
            int slowThresholdMs = 5000)
        {
            var monitor = new PerformanceMonitor(operationName);
            try
            {
                return operation();
            }
            finally
            {
                monitor.Stop(logIfSlow: true, thresholdMs: slowThresholdMs);
            }
        }
    }
}

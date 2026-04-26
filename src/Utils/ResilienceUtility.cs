using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Serilog;

namespace ShieldX.Utils
{
    /// <summary>
    /// Utility class for resilient operation execution with retry logic.
    /// </summary>
    public static class ResilienceUtility
    {
        /// <summary>
        /// Executes an operation with exponential backoff retry logic.
        /// </summary>
        public static async Task<T> ExecuteWithRetryAsync<T>(
            Func<Task<T>> operation,
            string operationName,
            int maxRetries = 3,
            int initialDelayMs = 100,
            Action<int, Exception> onRetry = null)
        {
            int attempt = 0;
            int delayMs = initialDelayMs;

            while (true)
            {
                try
                {
                    attempt++;
                    Log.Debug($"Executing {operationName} (attempt {attempt}/{maxRetries})");
                    return await operation();
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    onRetry?.Invoke(attempt, ex);
                    Log.Warning(ex, $"{operationName} failed on attempt {attempt}. Retrying in {delayMs}ms...");
                    await Task.Delay(delayMs);
                    delayMs = (int)Math.Min(delayMs * 1.5, 10000); // Exponential backoff, max 10s
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{operationName} failed after {maxRetries} attempts");
                    throw;
                }
            }
        }

        /// <summary>
        /// Executes a synchronous operation with retry logic.
        /// </summary>
        public static T ExecuteWithRetry<T>(
            Func<T> operation,
            string operationName,
            int maxRetries = 3,
            int initialDelayMs = 100,
            Action<int, Exception> onRetry = null)
        {
            int attempt = 0;
            int delayMs = initialDelayMs;

            while (true)
            {
                try
                {
                    attempt++;
                    Log.Debug($"Executing {operationName} (attempt {attempt}/{maxRetries})");
                    return operation();
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    onRetry?.Invoke(attempt, ex);
                    Log.Warning(ex, $"{operationName} failed on attempt {attempt}. Retrying in {delayMs}ms...");
                    Task.Delay(delayMs).Wait();
                    delayMs = (int)Math.Min(delayMs * 1.5, 10000);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{operationName} failed after {maxRetries} attempts");
                    throw;
                }
            }
        }

        /// <summary>
        /// Executes an operation with a timeout.
        /// </summary>
        public static async Task<T> ExecuteWithTimeoutAsync<T>(
            Func<Task<T>> operation,
            string operationName,
            int timeoutMs = 30000)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var task = operation();
                var completedTask = await Task.WhenAny(task, Task.Delay(timeoutMs));

                if (completedTask != task)
                {
                    Log.Error($"{operationName} exceeded timeout of {timeoutMs}ms");
                    throw new TimeoutException($"Operation '{operationName}' exceeded timeout of {timeoutMs}ms");
                }

                return await task;
            }
            finally
            {
                stopwatch.Stop();
                Log.Debug($"{operationName} completed in {stopwatch.ElapsedMilliseconds}ms");
            }
        }

        /// <summary>
        /// Executes a void operation with retry logic.
        /// </summary>
        public static async Task ExecuteWithRetryAsync(
            Func<Task> operation,
            string operationName,
            int maxRetries = 3,
            int initialDelayMs = 100,
            Action<int, Exception> onRetry = null)
        {
            int attempt = 0;
            int delayMs = initialDelayMs;

            while (true)
            {
                try
                {
                    attempt++;
                    Log.Debug($"Executing {operationName} (attempt {attempt}/{maxRetries})");
                    await operation();
                    return;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    onRetry?.Invoke(attempt, ex);
                    Log.Warning(ex, $"{operationName} failed on attempt {attempt}. Retrying in {delayMs}ms...");
                    await Task.Delay(delayMs);
                    delayMs = (int)Math.Min(delayMs * 1.5, 10000);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{operationName} failed after {maxRetries} attempts");
                    throw;
                }
            }
        }
    }
}

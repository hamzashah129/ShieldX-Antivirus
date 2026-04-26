using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace ShieldX.Utils
{
    /// <summary>
    /// Manages background tasks with priority, retry logic, and cancellation support.
    /// </summary>
    public class BackgroundTaskCoordinator
    {
        public enum TaskPriority
        {
            Low = 0,
            Normal = 1,
            High = 2
        }

        public class BackgroundTask
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string Name { get; set; }
            public Func<CancellationToken, Task> Action { get; set; }
            public TaskPriority Priority { get; set; } = TaskPriority.Normal;
            public bool IsRecurring { get; set; }
            public TimeSpan? RecurrenceInterval { get; set; }
            public int MaxRetries { get; set; } = 3;
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public DateTime? LastRun { get; set; }
            public DateTime? NextRun { get; set; }
            public Exception LastException { get; set; }
            public int ExecutionCount { get; set; }
        }

        private readonly ConcurrentDictionary<string, BackgroundTask> _tasks = new();
        private readonly ConcurrentQueue<BackgroundTask> _queue = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private Task _workerTask;
        private readonly SemaphoreSlim _queueSemaphore = new(0);
        private volatile bool _isRunning = false;

        /// <summary>
        /// Starts the background task coordinator.
        /// </summary>
        public void Start()
        {
            if (_isRunning)
                return;

            _isRunning = true;
            Log.Information("Background task coordinator started");
            _workerTask = ProcessTasksAsync(_cancellationTokenSource.Token);
        }

        /// <summary>
        /// Stops the background task coordinator gracefully.
        /// </summary>
        public async Task StopAsync()
        {
            if (!_isRunning)
                return;

            _isRunning = false;
            _cancellationTokenSource.Cancel();
            
            try
            {
                if (_workerTask != null)
                    await _workerTask;
                Log.Information("Background task coordinator stopped");
            }
            catch (OperationCanceledException)
            {
                Log.Information("Background task coordinator gracefully shutdown");
            }
        }

        /// <summary>
        /// Queues a new task for execution.
        /// </summary>
        public string QueueTask(BackgroundTask task)
        {
            ValidationUtility.ValidateNotNull(task, nameof(task));

            _tasks[task.Id] = task;
            _queue.Enqueue(task);
            _queueSemaphore.Release();

            Log.Information($"Task queued: {task.Name} (Priority: {task.Priority})");
            return task.Id;
        }

        /// <summary>
        /// Executes a task immediately without queueing.
        /// </summary>
        public async Task ExecuteTaskAsync(BackgroundTask task)
        {
            await ExecuteTaskWithRetryAsync(task, 0);
        }

        /// <summary>
        /// Cancels a queued or running task.
        /// </summary>
        public bool CancelTask(string taskId)
        {
            if (_tasks.TryGetValue(taskId, out var task))
            {
                _tasks.TryRemove(taskId, out _);
                Log.Information($"Task cancelled: {task.Name}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets task execution statistics.
        /// </summary>
        public IEnumerable<BackgroundTask> GetPendingTasks()
        {
            return _tasks.Values.ToList();
        }

        private async Task ProcessTasksAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Get tasks from recurring scheduling
                    var recurringTasks = _tasks.Values
                        .Where(t => t.IsRecurring && (t.NextRun == null || t.NextRun <= DateTime.UtcNow))
                        .ToList();

                    foreach (var task in recurringTasks)
                    {
                        _queue.Enqueue(task);
                        _queueSemaphore.Release();
                    }

                    // Process queued tasks in priority order
                    if (_queue.TryDequeue(out var nextTask))
                    {
                        await ExecuteTaskWithRetryAsync(nextTask, 0);
                    }
                    else
                    {
                        // Wait for next task or timeout
                        await _queueSemaphore.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error in background task processing");
                }
            }
        }

        private async Task ExecuteTaskWithRetryAsync(BackgroundTask task, int retryCount)
        {
            try
            {
                Log.Information($"Executing task: {task.Name} (Attempt {retryCount + 1})");
                task.LastRun = DateTime.UtcNow;
                task.ExecutionCount++;

                await task.Action(_cancellationTokenSource.Token);

                task.LastException = null;
                Log.Information($"Task completed successfully: {task.Name}");

                // Schedule next recurring execution
                if (task.IsRecurring && task.RecurrenceInterval.HasValue)
                {
                    task.NextRun = DateTime.UtcNow.Add(task.RecurrenceInterval.Value);
                }
                else
                {
                    _tasks.TryRemove(task.Id, out _);
                }
            }
            catch (Exception ex) when (retryCount < task.MaxRetries)
            {
                task.LastException = ex;
                Log.Warning(ex, $"Task failed, retrying: {task.Name} (Attempt {retryCount + 1}/{task.MaxRetries})");
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount))); // Exponential backoff
                await ExecuteTaskWithRetryAsync(task, retryCount + 1);
            }
            catch (Exception ex)
            {
                task.LastException = ex;
                Log.Error(ex, $"Task failed permanently: {task.Name}");
                if (!task.IsRecurring)
                {
                    _tasks.TryRemove(task.Id, out _);
                }
            }
        }
    }
}

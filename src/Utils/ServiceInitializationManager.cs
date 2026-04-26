using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace ShieldX.Utils
{
    /// <summary>
    /// Manages asynchronous service initialization with progress reporting and error handling.
    /// </summary>
    public class ServiceInitializationManager
    {
        public class InitializationStep
        {
            public string Name { get; set; }
            public Func<Task> Initializer { get; set; }
            public bool Required { get; set; } = true;
            public string FailureMessage { get; set; }
        }

        public class InitializationProgress
        {
            public int Total { get; set; }
            public int Completed { get; set; }
            public string CurrentStep { get; set; }
            public bool IsComplete { get; set; }
            public double PercentComplete => Total > 0 ? (Completed * 100.0) / Total : 0;
            public List<InitializationError> Errors { get; } = new();
        }

        public class InitializationError
        {
            public string StepName { get; set; }
            public Exception Exception { get; set; }
            public bool WasRequired { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private readonly List<InitializationStep> _steps = new();
        private readonly InitializationProgress _progress = new();

        public event Action<InitializationProgress> ProgressChanged;

        public void AddStep(string name, Func<Task> initializer, bool required = true, string failureMessage = null)
        {
            _steps.Add(new InitializationStep
            {
                Name = name,
                Initializer = initializer,
                Required = required,
                FailureMessage = failureMessage ?? $"Failed to initialize {name}"
            });
        }

        public async Task<bool> InitializeAsync()
        {
            _progress.Total = _steps.Count;
            _progress.Completed = 0;
            _progress.Errors.Clear();

            foreach (var step in _steps)
            {
                _progress.CurrentStep = step.Name;
                ProgressChanged?.Invoke(_progress);

                try
                {
                    Log.Information($"Initializing step: {step.Name}");
                    await ResilienceUtility.ExecuteWithTimeoutAsync<bool>(
                        async () => { await step.Initializer(); return true; },
                        $"Initialization step: {step.Name}",
                        timeoutMs: 30000);

                    Log.Information($"Successfully initialized: {step.Name}");
                    _progress.Completed++;
                    ProgressChanged?.Invoke(_progress);
                }
                catch (Exception ex)
                {
                    var error = new InitializationError
                    {
                        StepName = step.Name,
                        Exception = ex,
                        WasRequired = step.Required,
                        Timestamp = DateTime.UtcNow
                    };

                    _progress.Errors.Add(error);

                    if (step.Required)
                    {
                        Log.Fatal(ex, $"Required initialization step failed: {step.Name}");
                        _progress.IsComplete = false;
                        return false;
                    }
                    else
                    {
                        Log.Warning(ex, $"Optional initialization step failed: {step.Name}. Continuing with other steps.");
                        _progress.Completed++;
                        ProgressChanged?.Invoke(_progress);
                    }
                }
            }

            _progress.IsComplete = true;
            _progress.CurrentStep = "Initialization Complete";
            ProgressChanged?.Invoke(_progress);

            return _progress.Errors.Count == 0 || _progress.Errors.All(e => !e.WasRequired);
        }

        public InitializationProgress GetProgress() => _progress;
    }
}

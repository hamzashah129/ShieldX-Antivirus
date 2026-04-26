using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ShieldX.Services
{
    public interface IUpdateScheduler
    {
        void Start();
        void Stop();
        bool IsRunning { get; }
        event EventHandler<UpdateCheckEventArgs>? UpdateCheckCompleted;
    }

    public class UpdateCheckEventArgs : EventArgs
    {
        public bool UpdateAvailable { get; set; }
        public string? LatestVersion { get; set; }
        public Exception? Error { get; set; }
    }

    public class UpdateScheduler : IUpdateScheduler
    {
        private readonly UpdateService _updateService;
        private readonly AppConfig _config;
        private readonly Timer? _timer;
        private bool _isRunning;
        private CancellationTokenSource? _cts;

        public bool IsRunning => _isRunning;
        public event EventHandler<UpdateCheckEventArgs>? UpdateCheckCompleted;

        public UpdateScheduler()
        {
            _updateService = new UpdateService();
            _config = ConfigurationService.Current;
            _timer = new Timer(OnCheckInterval, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Start()
        {
            if (_isRunning) return;

            _isRunning = true;
            _cts = new CancellationTokenSource();

            int intervalMs = _config.Updates.CheckIntervalHours * 60 * 60 * 1000;
            if (intervalMs < 3600000) // Minimum 1 hour
                intervalMs = 3600000;

            // Start first check after a small delay
            _timer?.Change(TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(intervalMs));
            Debug.WriteLine($"[UpdateScheduler] Started with interval: {_config.Updates.CheckIntervalHours} hours");
        }

        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _cts?.Cancel();
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            Debug.WriteLine("[UpdateScheduler] Stopped");
        }

        private async void OnCheckInterval(object? state)
        {
            if (!_isRunning || _cts?.Token.IsCancellationRequested == true)
                return;

            try
            {
                Debug.WriteLine("[UpdateScheduler] Performing scheduled update check");
                var result = await _updateService.CheckForUpdateAsync();

                var args = new UpdateCheckEventArgs
                {
                    UpdateAvailable = result?.IsUpdateAvailable ?? false,
                    LatestVersion = result?.LatestVersion
                };

                UpdateCheckCompleted?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateScheduler] Check failed: {ex.Message}");
                UpdateCheckCompleted?.Invoke(this, new UpdateCheckEventArgs { Error = ex });
            }
        }

        public void Dispose()
        {
            Stop();
            _timer?.Dispose();
            _cts?.Dispose();
        }
    }
}

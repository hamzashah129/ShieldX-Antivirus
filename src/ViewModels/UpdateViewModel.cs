using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ShieldX.Services;

namespace ShieldX.ViewModels
{
    public class ReleaseInfo
    {
        public string Version { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "";
    }

    public class UpdateViewModel : INotifyPropertyChanged
    {
        private readonly UpdateService _service = new();
        private readonly UpdateScheduler _scheduler = new();
        private CancellationTokenSource? _cts;

        // Error handling
        private bool _hasError;
        public bool HasError
        {
            get => _hasError;
            set { _hasError = value; OnPropertyChanged(); }
        }

        private string _errorMessage = "";
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        // Release history
        private ObservableCollection<ReleaseInfo> _releaseHistory = new();
        public ObservableCollection<ReleaseInfo> ReleaseHistory
        {
            get => _releaseHistory;
            set { _releaseHistory = value; OnPropertyChanged(); }
        }

        // State
        private bool _isChecking;
        public bool IsChecking
        {
            get => _isChecking;
            set { _isChecking = value; OnPropertyChanged(); }
        }

        private bool _isDownloading;
        public bool IsDownloading
        {
            get => _isDownloading;
            set
            {
                _isDownloading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotDownloading));
            }
        }
        public bool IsNotDownloading => !IsDownloading;

        private bool _updateAvailable;
        public bool UpdateAvailable
        {
            get => _updateAvailable;
            set
            {
                _updateAvailable = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsUpToDate));
            }
        }
        public bool IsUpToDate =>
            !UpdateAvailable && !IsChecking && _hasChecked;

        private bool _hasChecked;

        // Version info
        public string CurrentVersion { get; } = "3.1.1";

        private string _latestVersion = "";
        public string LatestVersion
        {
            get => _latestVersion;
            set { _latestVersion = value; OnPropertyChanged(); }
        }

        private string _releaseNotes = "";
        public string ReleaseNotes
        {
            get => _releaseNotes;
            set
            {
                _releaseNotes = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasReleaseNotes));
            }
        }
        public bool HasReleaseNotes =>
            !string.IsNullOrEmpty(ReleaseNotes);

        private DateTime _releaseDate;
        public DateTime ReleaseDate
        {
            get => _releaseDate;
            set { _releaseDate = value; OnPropertyChanged(); }
        }

        // Download progress
        private int _downloadProgress;
        public int DownloadProgress
        {
            get => _downloadProgress;
            set { _downloadProgress = value; OnPropertyChanged(); }
        }

        private string _downloadStatus = "";
        public string DownloadStatus
        {
            get => _downloadStatus;
            set { _downloadStatus = value; OnPropertyChanged(); }
        }

        // Settings
        private bool _autoCheckEnabled = true;
        public bool AutoCheckEnabled
        {
            get => _autoCheckEnabled;
            set
            {
                _autoCheckEnabled = value;
                OnPropertyChanged();

                // Control scheduler
                if (value && !_scheduler.IsRunning)
                {
                    _scheduler.Start();
                    _scheduler.UpdateCheckCompleted += OnScheduledCheckCompleted;
                }
                else if (!value && _scheduler.IsRunning)
                {
                    _scheduler.Stop();
                    _scheduler.UpdateCheckCompleted -= OnScheduledCheckCompleted;
                }
            }
        }

        private bool _notifyBeforeInstall = true;
        public bool NotifyBeforeInstall
        {
            get => _notifyBeforeInstall;
            set { _notifyBeforeInstall = value; OnPropertyChanged(); }
        }

        private UpdateInfo? _updateInfo;

        private void OnScheduledCheckCompleted(object? sender, UpdateCheckEventArgs e)
        {
            if (e.Error != null)
            {
                Debug.WriteLine($"[ViewModel] Scheduled check failed: {e.Error.Message}");
                return;
            }

            if (e.UpdateAvailable && !UpdateAvailable)
            {
                // Update became available since last check
                Debug.WriteLine($"[ViewModel] Update available: {e.LatestVersion}");
                _ = CheckForUpdate(); // Refresh the data
            }
        }

        // Commands
        public ICommand CheckUpdateCommand       { get; }
        public ICommand DownloadInstallCommand   { get; }
        public ICommand ViewReleaseCommand       { get; }
        public ICommand CancelDownloadCommand    { get; }

        public UpdateViewModel()
        {
            CheckUpdateCommand =
                new AsyncCmd(CheckForUpdate);
            DownloadInstallCommand =
                new AsyncCmd(DownloadAndInstall);
            ViewReleaseCommand =
                new RelayCmd(_ => ViewRelease());
            CancelDownloadCommand =
                new RelayCmd(_ => CancelDownload());

            // Initialize release history
            InitializeReleaseHistory();

            // Setup scheduler
            _scheduler.UpdateCheckCompleted += OnScheduledCheckCompleted;

            if (AutoCheckEnabled)
            {
                _ = CheckForUpdate();
                _scheduler.Start();
            }
        }

        private void InitializeReleaseHistory()
        {
            // Load release history asynchronously
            _ = LoadReleaseHistoryAsync();
        }

        private async Task LoadReleaseHistoryAsync()
        {
            try
            {
                var releases = await _service.GetReleaseHistoryAsync(10);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ReleaseHistory.Clear();

                    foreach (var release in releases)
                    {
                        var status = release.LatestVersion == CurrentVersion ? "Installed" : "";
                        var description = release.ReleaseNotes;

                        // Limit description to first 3 lines
                        if (!string.IsNullOrEmpty(description))
                        {
                            var lines = description.Split('\n');
                            description = string.Join("\n", lines.Take(3));
                            if (lines.Length > 3)
                                description += "\n...";
                        }

                        ReleaseHistory.Add(new ReleaseInfo
                        {
                            Version = $"v{release.LatestVersion}",
                            Name = release.ReleaseName,
                            Description = description,
                            Status = status
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ViewModel] Failed to load release history: {ex.Message}");
                // Fallback to default history
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (ReleaseHistory.Count == 0)
                    {
                        ReleaseHistory.Add(new ReleaseInfo
                        {
                            Version = "v3.1.1",
                            Name = "ShieldX Professional — Latest",
                            Description = "• Real-time file system protection\n• Parallel multi-threaded scanning\n• Password Vault (AES-256)",
                            Status = "Installed"
                        });
                    }
                });
            }
        }

        private async Task CheckForUpdate()
        {
            IsChecking = true;
            HasError = false;
            ErrorMessage = "";
            _hasChecked = false;
            OnPropertyChanged(nameof(IsUpToDate));

            try
            {
                _updateInfo = await _service.CheckForUpdateAsync();

                // Only if there's actual new version data
                if (_updateInfo != null && _updateInfo.LatestVersion != CurrentVersion)
                {
                    UpdateAvailable = _updateInfo.IsUpdateAvailable;
                    LatestVersion = _updateInfo.LatestVersion;
                    ReleaseNotes = _updateInfo.ReleaseNotes;
                    ReleaseDate = _updateInfo.ReleaseDate;
                }
                else
                {
                    UpdateAvailable = false;  // NEVER true when same version
                }

                _hasChecked = true;
                OnPropertyChanged(nameof(IsUpToDate));
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Failed to check for updates: {ex.Message}";
                Debug.WriteLine($"[Update] Error: {ex.Message}");
            }
            finally
            {
                IsChecking = false;
                IsDownloading = false;
                _hasChecked = true;
                OnPropertyChanged(nameof(IsChecking));
                OnPropertyChanged(nameof(IsUpToDate));
                OnPropertyChanged(nameof(UpdateAvailable));
            }
        }

        private async Task DownloadAndInstall()
        {
            if (_updateInfo == null) return;

            if (NotifyBeforeInstall)
            {
                var r = MessageBox.Show(
                    $"Download and install ShieldX Professional v{_updateInfo.LatestVersion}?\n\n" +
                    "The application will restart after installation.",
                    "ShieldX Professional — Install Update",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (r != MessageBoxResult.Yes) return;
            }

            IsDownloading = true;
            DownloadProgress = 0;
            DownloadStatus = "Starting download...";
            HasError = false;
            ErrorMessage = "";
            _cts = new CancellationTokenSource();

            try
            {
                var progress = new Progress<int>(pct =>
                {
                    DownloadProgress = pct;
                    DownloadStatus = pct < 100
                        ? $"Downloading ({pct}%)..."
                        : "Installing...";
                });

                bool success = await _service
                    .DownloadAndInstallAsync(_updateInfo, progress);

                if (!success)
                {
                    DownloadStatus = "Opening browser for download...";
                    HasError = true;
                    ErrorMessage = "Download failed. Opening browser for manual download.";
                }
            }
            catch (OperationCanceledException)
            {
                DownloadStatus = "Download cancelled";
                HasError = true;
                ErrorMessage = "Download was cancelled by user";
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Download failed: {ex.Message}";
                Debug.WriteLine($"[ViewModel] Download error: {ex.Message}");
            }
            finally
            {
                IsDownloading = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void ViewRelease()
        {
            string url = _updateInfo?.DownloadUrl
                ?? "https://github.com/ShieldXAntivirus/ShieldX/releases";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void CancelDownload()
        {
            _cts?.Cancel();
            IsDownloading = false;
            DownloadStatus = "Download cancelled";
        }

        private sealed class RelayCmd : ICommand
        {
            private readonly Action<object?> _e;
            public RelayCmd(Action<object?> e) => _e = e;
            public bool CanExecute(object? p) => true;
            public void Execute(object? p) => _e(p);
            public event EventHandler? CanExecuteChanged;
        }

        private sealed class AsyncCmd : ICommand
        {
            private readonly Func<Task> _e;
            private bool _busy;
            public AsyncCmd(Func<Task> e) => _e = e;
            public bool CanExecute(object? p) => !_busy;
            public async void Execute(object? p)
            {
                if (_busy) return;
                _busy = true;
                try { await _e(); }
                catch { }
                finally { _busy = false; }
            }
            public event EventHandler? CanExecuteChanged;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(
            [CallerMemberName] string? n = null) =>
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(n));
    }
}

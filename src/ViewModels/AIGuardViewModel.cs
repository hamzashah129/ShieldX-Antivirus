using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ShieldX.Models;
using ShieldX.Services;

namespace ShieldX.ViewModels
{
    public class AIDetectionItem : INotifyPropertyChanged
    {
        private string _timeText = "";
        private string _processName = "";
        private string _threatClass = "";
        private float _threatScore = 0;
        private string _action = "";

        public string TimeText
        {
            get => _timeText;
            set { _timeText = value; OnPropertyChanged(); }
        }

        public string ProcessName
        {
            get => _processName;
            set { _processName = value; OnPropertyChanged(); }
        }

        public string ThreatClass
        {
            get => _threatClass;
            set { _threatClass = value; OnPropertyChanged(); }
        }

        public float ThreatScore
        {
            get => _threatScore;
            set { _threatScore = value; OnPropertyChanged(); }
        }

        public string Action
        {
            get => _action;
            set { _action = value; OnPropertyChanged(); }
        }

        public string ScorePercentage => $"{(ThreatScore * 100):F0}%";

        public Brush ScoreColor => ThreatScore > 0.85f ? new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B)) :
                                    ThreatScore > 0.65f ? new SolidColorBrush(Color.FromRgb(0xFF, 0xA5, 0x00)) :
                                    ThreatScore > 0.45f ? new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00)) :
                                    new SolidColorBrush(Colors.White);

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class AIGuardViewModel : INotifyPropertyChanged
    {
        private bool _isAIGuardActive = false;
        private ObservableCollection<AIDetectionItem> _recentDetections = new();
        private int _processesScannedToday = 0;
        private int _threatsBlockedToday = 0;
        private int _suspiciousFlagged = 0;
        private string _statusText = "AI Guard Ready";
        private int _trainingSamples = 0;
        private RelayCommand _clearDetectionsCommand;
        private RelayCommand _exportReportCommand;

        public bool IsAIGuardActive
        {
            get => _isAIGuardActive;
            set
            {
                if (_isAIGuardActive != value)
                {
                    _isAIGuardActive = value;
                    OnPropertyChanged();

                    if (value)
                        AIGuardService.Instance.Start();
                    else
                        AIGuardService.Instance.Stop();
                }
            }
        }

        public ObservableCollection<AIDetectionItem> RecentDetections
        {
            get => _recentDetections;
            set { _recentDetections = value; OnPropertyChanged(); }
        }

        public int ProcessesScannedToday
        {
            get => _processesScannedToday;
            set { _processesScannedToday = value; OnPropertyChanged(); }
        }

        public int ThreatsBlockedToday
        {
            get => _threatsBlockedToday;
            set { _threatsBlockedToday = value; OnPropertyChanged(); }
        }

        public int SuspiciousFlagged
        {
            get => _suspiciousFlagged;
            set { _suspiciousFlagged = value; OnPropertyChanged(); }
        }

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public int TrainingSamples
        {
            get => _trainingSamples;
            set { _trainingSamples = value; OnPropertyChanged(); }
        }

        public string ModelVersion => "AI Guard v1.0 — Heuristic Engine";

        public ICommand ClearDetectionsCommand
        {
            get
            {
                return _clearDetectionsCommand ??= new RelayCommand(() =>
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        RecentDetections.Clear();
                        LogService.Instance.AddInfo("Cleared recent detections", "AIGuard");
                    });
                });
            }
        }

        public ICommand ExportReportCommand
        {
            get
            {
                return _exportReportCommand ??= new RelayCommand(() =>
                {
                    try
                    {
                        string logsPath = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "ShieldX", "Logs");
                        Directory.CreateDirectory(logsPath);

                        string reportPath = Path.Combine(logsPath,
                            $"AIGuard_Report_{DateTime.Now:yyyyMMdd_HHmmss}.json");

                        // Serialize detections to JSON
                        var json = System.Text.Json.JsonSerializer.Serialize(
                            RecentDetections,
                            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                        System.IO.File.WriteAllText(reportPath, json);

                        LogService.Instance.AddInfo($"Exported AI Guard report to: {reportPath}", "AIGuard");
                        ToastNotificationService.ShowNotification("Report Exported",
                            $"AI Guard report saved to Logs folder", severity: ToastNotificationService.NotificationSeverity.Success);
                    }
                    catch (Exception ex)
                    {
                        LogService.Instance.AddError($"Error exporting report: {ex.Message}", "AIGuard");
                        ToastNotificationService.ShowNotification("Export Failed",
                            "Failed to export AI Guard report", severity: ToastNotificationService.NotificationSeverity.Critical);
                    }
                });
            }
        }

        public AIGuardViewModel()
        {
            // Start AI Guard service automatically
            IsAIGuardActive = true;

            // Subscribe to AI Guard events
            AIGuardService.Instance.ThreatBlocked += OnThreatBlocked;
            AIGuardService.Instance.ThreatSuspended += OnThreatSuspended;
            AIGuardService.Instance.ThreatFlagged += OnThreatFlagged;
            AIGuardService.Instance.StatusChanged += OnStatusChanged;

            TrainingSamples = AIGuardTrainingService.Instance.GetTrainingSampleCount();

            // Update UI periodically
            var timer = new System.Threading.Timer(_ =>
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    ProcessesScannedToday = AIGuardService.Instance.ProcessesScannedToday;
                    ThreatsBlockedToday = AIGuardService.Instance.ThreatsBlockedToday;
                    SuspiciousFlagged = AIGuardService.Instance.SuspiciousFlaggedToday;
                });
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
        }

        private void OnThreatBlocked(AIGuardResult result)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var item = new AIDetectionItem
                {
                    TimeText = result.DetectedAt.ToString("HH:mm:ss"),
                    ProcessName = result.Process.Name,
                    ThreatClass = result.ThreatClass,
                    ThreatScore = result.ThreatScore,
                    Action = "Blocked"
                };

                RecentDetections.Insert(0, item);
                if (RecentDetections.Count > 50)
                    RecentDetections.RemoveAt(RecentDetections.Count - 1);

                ThreatsBlockedToday++;
            });
        }

        private void OnThreatSuspended(AIGuardResult result)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var item = new AIDetectionItem
                {
                    TimeText = result.DetectedAt.ToString("HH:mm:ss"),
                    ProcessName = result.Process.Name,
                    ThreatClass = result.ThreatClass,
                    ThreatScore = result.ThreatScore,
                    Action = "Suspended"
                };

                RecentDetections.Insert(0, item);
                if (RecentDetections.Count > 50)
                    RecentDetections.RemoveAt(RecentDetections.Count - 1);

                SuspiciousFlagged++;
            });
        }

        private void OnThreatFlagged(AIGuardResult result)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var item = new AIDetectionItem
                {
                    TimeText = result.DetectedAt.ToString("HH:mm:ss"),
                    ProcessName = result.Process.Name,
                    ThreatClass = result.ThreatClass,
                    ThreatScore = result.ThreatScore,
                    Action = "Flagged"
                };

                RecentDetections.Insert(0, item);
                if (RecentDetections.Count > 50)
                    RecentDetections.RemoveAt(RecentDetections.Count - 1);

                SuspiciousFlagged++;
            });
        }

        private void OnStatusChanged(string message)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                StatusText = message;
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute?.Invoke();
    }
}

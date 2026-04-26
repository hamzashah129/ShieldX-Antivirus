using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ShieldX.Services;

namespace ShieldX.Models
{
    public class AppState : INotifyPropertyChanged
    {
        private static readonly Lazy<AppState> _instance = new Lazy<AppState>(() => new AppState());
        public static AppState Instance => _instance.Value;

        private int _totalThreatsFound;
        private int _totalFilesScanned;
        private int _totalScansRun;
        private int _quarantinedCount;
        private DateTime _lastScanTime;
        private string _systemHealth;
        private int _securityScore;
        private int _blockedConnectionsCount;
        private int _scanProgress;
        private string _currentScanStatus;

        public int TotalThreatsFound
        {
            get => _totalThreatsFound;
            set { _totalThreatsFound = value; OnPropertyChanged(); }
        }

        public int TotalFilesScanned
        {
            get => _totalFilesScanned;
            set { _totalFilesScanned = value; OnPropertyChanged(); }
        }

        public int TotalScansRun
        {
            get => _totalScansRun;
            set { _totalScansRun = value; OnPropertyChanged(); }
        }

        public int QuarantinedCount
        {
            get => _quarantinedCount;
            set { _quarantinedCount = value; OnPropertyChanged(); }
        }

        public DateTime LastScanTime
        {
            get => _lastScanTime;
            set
            {
                _lastScanTime = value;
                OnPropertyChanged();
                SecurityScoreEngine.Instance.RecalculateScore();
            }
        }

        public string SystemHealth
        {
            get => _systemHealth;
            set { _systemHealth = value; OnPropertyChanged(); }
        }

        public int SecurityScore
        {
            get => _securityScore;
            set { _securityScore = value; OnPropertyChanged(); }
        }

        public int BlockedConnectionsCount
        {
            get => _blockedConnectionsCount;
            set { _blockedConnectionsCount = value; OnPropertyChanged(); }
        }

        public int ScanProgress
        {
            get => _scanProgress;
            set { _scanProgress = value; OnPropertyChanged(); }
        }

        public string CurrentScanStatus
        {
            get => _currentScanStatus;
            set { _currentScanStatus = value; OnPropertyChanged(); }
        }

        private AppState()
        {
            SystemHealth = "Unknown";
            SecurityScore = 0;
            CurrentScanStatus = "Ready";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
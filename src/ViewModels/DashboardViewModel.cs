using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ShieldX.Models;
using ShieldX.Services;

namespace ShieldX.ViewModels
{
    /// <summary>
    /// Represents a protection module with enable/disable capability.
    /// </summary>
    public class ProtectionModule : INotifyPropertyChanged
    {
        private bool _isEnabled = true;

        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusColor));
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        public string StatusColor =>
            IsEnabled ? "#10B981" : "#4A5568";

        public string StatusText =>
            IsEnabled ? "Active" : "Inactive";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// ViewModel for the Dashboard page.
    /// Manages alerts, modules, security score, and protection status.
    /// </summary>
    public class DashboardViewModel : INotifyPropertyChanged
    {
        // Constants for score calculation and display limits
        private const int SECURITY_SCORE_MAX = 100;
        private const int SECURITY_SCORE_MIN = 0;
        private const int POINTS_PER_DISABLED_MODULE = 10;
        private const int POINTS_PER_THREAT = 5;

        /// <summary>
        /// Relay command implementation for binding actions to UI elements.
        /// </summary>
        private sealed class RelayCommand : ICommand
        {
            private readonly Action<object> _execute;
            private readonly Predicate<object> _canExecute;

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;
            public void Execute(object parameter) => _execute(parameter);
        }
        private const int MAX_THREAT_PENALTY = 30;
        private const int STALE_SCAN_PENALTY = 15;
        private const int ACTIVE_THREATS_PENALTY = 20;
        private const int STALE_SCAN_DAYS = 7;
        private const int MAX_ALERTS_DISPLAYED = 10;
        private const int SCORE_RECALC_SECONDS = 30;

        private ObservableCollection<AlertItem> _recentAlerts = new();
        private int _threatBlockedToday = 0;
        private int _dynamicSecurityScore = 100;
        private DateTime? _lastScanDate;
        private int _lastScanThreats = 0;

        public ObservableCollection<ProtectionModule> ProtectionModules { get; }

        public DashboardViewModel()
        {
            ProtectionModules = new ObservableCollection<ProtectionModule>
            {
                new()
                {
                    Name        = "RealTimeProtection",
                    Description = "Real-time file scanning and protection",
                    Icon        = "Shield",
                    IsEnabled   = true
                },
                new()
                {
                    Name        = "WebShield",
                    Description = "Web browsing protection and URL filtering",
                    Icon        = "Web",
                    IsEnabled   = true
                },
                new()
                {
                    Name        = "RansomwareShield",
                    Description = "Advanced ransomware detection",
                    Icon        = "Lock",
                    IsEnabled   = true
                },
                new()
                {
                    Name        = "FirewallMonitor",
                    Description = "Network firewall monitoring",
                    Icon        = "Firewall",
                    IsEnabled   = true
                },
                new()
                {
                    Name        = "ExploitGuard",
                    Description = "Exploit mitigation and protection",
                    Icon        = "ShieldAlert",
                    IsEnabled   = true
                },
                new()
                {
                    Name        = "EmailProtection",
                    Description = "Email attachment scanning",
                    Icon        = "Mail",
                    IsEnabled   = true
                },
                new()
                {
                    Name        = "DNSFilter",
                    Description = "DNS-based domain blocking",
                    Icon        = "Globe",
                    IsEnabled   = true
                },
                new()
                {
                    Name        = "BehaviorMonitor",
                    Description = "Behavioral anomaly detection",
                    Icon        = "Activity",
                    IsEnabled   = true
                },
                new()
                {
                    Name        = "VulnerabilityScanner",
                    Description = "CVE vulnerability monitoring",
                    Icon        = "AlertTriangle",
                    IsEnabled   = true
                },
            };

            AppState.Instance.PropertyChanged += OnAppStateChanged;
            App.RealTimeProtection.ThreatDetected += OnThreatDetected;

            // Recalculate security score periodically
            var timer = new System.Threading.Timer(_ => RecalculateSecurityScore(),
                null, TimeSpan.FromSeconds(SCORE_RECALC_SECONDS), TimeSpan.FromSeconds(SCORE_RECALC_SECONDS));

            UpdateStats();
        }

        /// <summary>
        /// Collection of recent threat alerts for display.
        /// </summary>
        public ObservableCollection<AlertItem> RecentAlerts
        {
            get => _recentAlerts;
            set
            {
                if (_recentAlerts != value)
                {
                    _recentAlerts = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Number of threats blocked today.
        /// </summary>
        public int ThreatBlockedToday
        {
            get => _threatBlockedToday;
            set
            {
                if (_threatBlockedToday != value)
                {
                    _threatBlockedToday = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasNoAlerts));
                }
            }
        }

        /// <summary>
        /// Dynamic security score based on system state.
        /// </summary>
        public int DynamicSecurityScore
        {
            get => _dynamicSecurityScore;
            set
            {
                if (_dynamicSecurityScore != value)
                {
                    _dynamicSecurityScore = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Indicates if there are no alerts to display.
        /// </summary>
        public bool HasNoAlerts => RecentAlerts.Count == 0;

        public System.Collections.ObjectModel.ObservableCollection<SecurityModule> Modules => ModuleManager.Instance.Modules;

        private void OnAppStateChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateStats();
            RecalculateSecurityScore();
        }

        /// <summary>
        /// Handles threat detection events from real-time protection service.
        /// </summary>
        private void OnThreatDetected(string fileName, string threat, string path)
        {
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        // Create alert item
                        var alert = new AlertItem
                        {
                            Time = DateTime.Now.ToString("hh:mm:ss tt"),
                            FileName = fileName,
                            Threat = threat,
                            Path = path,
                            Action = "Auto-Quarantined"
                        };

                        // Insert at beginning of list
                        RecentAlerts.Insert(0, alert);

                        // Keep only last N alerts on dashboard
                        while (RecentAlerts.Count > MAX_ALERTS_DISPLAYED)
                        {
                            RecentAlerts.RemoveAt(RecentAlerts.Count - 1);
                        }

                        // Increment threat counter
                        ThreatBlockedToday++;
                        _lastScanThreats = _lastScanThreats + 1;

                        OnPropertyChanged(nameof(HasNoAlerts));
                        RecalculateSecurityScore();
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Error(ex, "Error handling threat detection");
                    }
                });
            }
        }

        /// <summary>
        /// Removes an alert from the recent alerts list.
        /// </summary>
        public void RemoveAlert(AlertItem alert)
        {
            if (alert != null && RecentAlerts.Contains(alert))
            {
                RecentAlerts.Remove(alert);
                OnPropertyChanged(nameof(HasNoAlerts));
            }
        }

        /// <summary>
        /// Toggles a protection module on or off.
        /// </summary>
        public void ToggleModule(ProtectionModule module)
        {
            if (module != null)
            {
                module.IsEnabled = !module.IsEnabled;
                RecalculateSecurityScore();
            }
        }

        private void UpdateStats()
        {
            OnPropertyChanged(nameof(Modules));
            RecalculateSecurityScore();
        }

        /// <summary>
        /// Calculates security score based on real system state.
        /// Factors: enabled modules (10 pts each), threats found (5 pts each, max 30), 
        /// scan age (15 pts if stale), active threats today (20 pts).
        /// Scale: 0-100, dynamic based on protection state.
        /// </summary>
        private void RecalculateSecurityScore()
        {
            int score = SECURITY_SCORE_MAX;

            try
            {
                // Factor 1: Deduct for each disabled protection module (-10 per module)
                if (ModuleManager.Instance.Modules != null)
                {
                    var disabledCount = ModuleManager.Instance.Modules
                        .Where(m => m != null && m.IsActive == false)
                        .Count();
                    score -= disabledCount * POINTS_PER_DISABLED_MODULE;
                }

                // Factor 2: Deduct for threats found in current session (-5 per threat, max -30)
                if (_lastScanThreats > 0)
                    score -= Math.Min(MAX_THREAT_PENALTY, _lastScanThreats * POINTS_PER_THREAT);

                // Factor 3: Deduct if last scan was stale (older than 7 days) (-15 points)
                if (_lastScanDate == null ||
                    DateTime.Now - _lastScanDate.Value > TimeSpan.FromDays(STALE_SCAN_DAYS))
                    score -= STALE_SCAN_PENALTY;

                // Factor 4: Deduct if threats are detected today (-20 points)
                // This represents active threats that need immediate attention
                if (ThreatBlockedToday > 0)
                    score -= ACTIVE_THREATS_PENALTY;

                // Clamp score between min (0) and max (100)
                DynamicSecurityScore = Math.Clamp(score, SECURITY_SCORE_MIN, SECURITY_SCORE_MAX);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error recalculating security score");
                // On error, maintain current score
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
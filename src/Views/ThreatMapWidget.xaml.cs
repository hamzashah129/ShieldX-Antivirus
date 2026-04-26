using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using ShieldX.Models;
using ShieldX.Services;

namespace ShieldX.Views
{
    public partial class ThreatMapWidget : UserControl, INotifyPropertyChanged
    {
        private readonly ThreatMapService _threatMapService;
        private readonly DispatcherTimer _updateTimer;
        private readonly ObservableCollection<KeyValuePair<string, int>> _topThreatTypes;
        private readonly ObservableCollection<KeyValuePair<string, int>> _topCountries;
        private readonly ObservableCollection<ThreatAlert> _recentAlerts;

        private int _totalActiveThreats;
        private ThreatLevel _globalThreatLevel;
        private DateTime _lastUpdated;

        public ThreatMapWidget()
        {
            InitializeComponent();
            DataContext = this;

            _threatMapService = new ThreatMapService();
            _topThreatTypes = new ObservableCollection<KeyValuePair<string, int>>();
            _topCountries = new ObservableCollection<KeyValuePair<string, int>>();
            _recentAlerts = new ObservableCollection<ThreatAlert>();

            // Update threat data every 30 seconds
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _updateTimer.Tick += async (s, e) => await UpdateThreatDataAsync();
            _updateTimer.Start();

            // Initial load
            Loaded += async (s, e) => await UpdateThreatDataAsync();
        }

        public int TotalActiveThreats
        {
            get => _totalActiveThreats;
            set
            {
                if (_totalActiveThreats == value) return;
                _totalActiveThreats = value;
                OnPropertyChanged();
            }
        }

        public ThreatLevel GlobalThreatLevel
        {
            get => _globalThreatLevel;
            set
            {
                if (_globalThreatLevel == value) return;
                _globalThreatLevel = value;
                OnPropertyChanged();
            }
        }

        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set
            {
                if (_lastUpdated == value) return;
                _lastUpdated = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<KeyValuePair<string, int>> TopThreatTypes => _topThreatTypes;
        public ObservableCollection<KeyValuePair<string, int>> TopCountries => _topCountries;
        public ObservableCollection<ThreatAlert> RecentAlerts => _recentAlerts;

        private async Task UpdateThreatDataAsync()
        {
            try
            {
                var stats = await _threatMapService.GetThreatMapStatsAsync();
                var alerts = await _threatMapService.GetRecentAlertsAsync(5);

                TotalActiveThreats = stats.TotalActiveThreats;
                GlobalThreatLevel = stats.GlobalThreatLevel;
                LastUpdated = stats.LastUpdated;

                // Update top threat types
                _topThreatTypes.Clear();
                foreach (var kvp in stats.ThreatsByType.OrderByDescending(x => x.Value).Take(3))
                {
                    _topThreatTypes.Add(kvp);
                }

                // Update top countries
                _topCountries.Clear();
                foreach (var kvp in stats.ThreatsByCountry.OrderByDescending(x => x.Value).Take(3))
                {
                    _topCountries.Add(kvp);
                }

                // Update recent alerts
                _recentAlerts.Clear();
                foreach (var alert in alerts)
                {
                    _recentAlerts.Add(alert);
                }

                // Update map visualization
                await UpdateMapVisualizationAsync();
            }
            catch (Exception ex)
            {
                LogService.Instance.AddError($"Failed to update threat map: {ex.Message}");
            }
        }

        private async Task UpdateMapVisualizationAsync()
        {
            try
            {
                var activeThreats = await _threatMapService.GetActiveThreatsAsync();

                // Clear existing threat indicators
                MapCanvas.Children.Clear();

                // Add threat indicators to map
                foreach (var threat in activeThreats)
                {
                    var indicator = CreateThreatIndicator(threat);
                    MapCanvas.Children.Add(indicator);
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.AddError($"Failed to update map visualization: {ex.Message}");
            }
        }

        private UIElement CreateThreatIndicator(ActiveThreat threat)
        {
            // Convert lat/lng to canvas coordinates (simplified projection)
            var x = (threat.Longitude + 180) / 360 * MapCanvas.ActualWidth;
            var y = (90 - threat.Latitude) / 180 * MapCanvas.ActualHeight;

            var indicator = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = GetSeverityColor(threat.Severity),
                Stroke = GetSeverityColor(threat.Severity),
                StrokeThickness = 1,
                Opacity = 0.8
            };

            // Position the indicator
            Canvas.SetLeft(indicator, Math.Max(0, Math.Min(x - 3, MapCanvas.ActualWidth - 6)));
            Canvas.SetTop(indicator, Math.Max(0, Math.Min(y - 3, MapCanvas.ActualHeight - 6)));

            // Add tooltip
            indicator.ToolTip = $"{threat.Name}\n{threat.Type}\n{threat.City}, {threat.Country}\nSeverity: {threat.Severity}";

            return indicator;
        }

        private Brush GetSeverityColor(ThreatSeverity severity)
        {
            return severity switch
            {
                ThreatSeverity.Low => new SolidColorBrush(Color.FromRgb(34, 197, 94)), // Green
                ThreatSeverity.Medium => new SolidColorBrush(Color.FromRgb(245, 158, 11)), // Yellow
                ThreatSeverity.High => new SolidColorBrush(Color.FromRgb(239, 68, 68)), // Red
                _ => new SolidColorBrush(Color.FromRgb(156, 163, 175)) // Gray
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
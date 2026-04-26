using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ShieldX.Models
{
    /// <summary>
    /// Represents a resource metric with trend history
    /// </summary>
    public class ResourceMetric : INotifyPropertyChanged
    {
        private double _currentValue;
        private double _averageValue;
        private double _maxValue;
        private string _status;
        private Queue<double> _trendHistory; // Stores last 20 values for sparkline
        private const int MaxTrendSamples = 20;

        /// <summary>Name of the resource (CPU, RAM, Disk, Memory)</summary>
        public string Name { get; set; } = "";

        /// <summary>Unit of measurement (%)</summary>
        public string Unit { get; set; } = "%";

        /// <summary>Current value</summary>
        public double CurrentValue
        {
            get => _currentValue;
            set
            {
                if (Math.Abs(_currentValue - value) > 0.01)
                {
                    _currentValue = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayValue));
                    
                    // Update trend
                    AddToTrend(value);
                    
                    // Update average and max
                    UpdateStatistics();
                }
            }
        }

        /// <summary>Average value over time</summary>
        public double AverageValue
        {
            get => _averageValue;
            private set { if (_averageValue != value) { _averageValue = value; OnPropertyChanged(); } }
        }

        /// <summary>Maximum value observed</summary>
        public double MaxValue
        {
            get => _maxValue;
            private set { if (_maxValue != value) { _maxValue = value; OnPropertyChanged(); } }
        }

        /// <summary>Status message (e.g., "Normal", "Warning", "Critical")</summary>
        public string Status
        {
            get => _status;
            set { if (_status != value) { _status = value; OnPropertyChanged(); } }
        }

        /// <summary>Trend history for sparkline (last 20 samples)</summary>
        public Queue<double> TrendHistory
        {
            get => _trendHistory;
            private set { _trendHistory = value; }
        }

        /// <summary>Formatted display value with unit</summary>
        public string DisplayValue => $"{CurrentValue:F1}{Unit}";

        /// <summary>Formatted average with unit</summary>
        public string AverageDisplay => $"{AverageValue:F1}{Unit}";

        /// <summary>Formatted max with unit</summary>
        public string MaxDisplay => $"{MaxValue:F1}{Unit}";

        /// <summary>Threshold for warning status (typically 70%)</summary>
        public double WarningThreshold { get; set; } = 70.0;

        /// <summary>Threshold for critical status (typically 90%)</summary>
        public double CriticalThreshold { get; set; } = 90.0;

        public ResourceMetric()
        {
            _trendHistory = new Queue<double>(MaxTrendSamples);
            Status = "Normal";
        }

        public ResourceMetric(string name, double warningThreshold = 70.0, double criticalThreshold = 90.0)
        {
            Name = name;
            WarningThreshold = warningThreshold;
            CriticalThreshold = criticalThreshold;
            _trendHistory = new Queue<double>(MaxTrendSamples);
            Status = "Normal";
        }

        /// <summary>
        /// Adds a value to the trend history
        /// </summary>
        private void AddToTrend(double value)
        {
            _trendHistory.Enqueue(value);
            if (_trendHistory.Count > MaxTrendSamples)
            {
                _trendHistory.Dequeue();
            }
            OnPropertyChanged(nameof(TrendHistory));
        }

        /// <summary>
        /// Updates statistics and status based on current value
        /// </summary>
        private void UpdateStatistics()
        {
            if (_trendHistory.Count == 0)
                return;

            // Calculate average
            double sum = 0;
            foreach (var value in _trendHistory)
            {
                sum += value;
            }
            AverageValue = sum / _trendHistory.Count;

            // Update max
            double localMax = 0;
            foreach (var value in _trendHistory)
            {
                if (value > localMax)
                    localMax = value;
            }
            MaxValue = localMax;

            // Update status based on thresholds
            if (CurrentValue >= CriticalThreshold)
            {
                Status = "🔴 Critical";
            }
            else if (CurrentValue >= WarningThreshold)
            {
                Status = "🟠 Warning";
            }
            else
            {
                Status = "🟢 Normal";
            }
        }

        /// <summary>
        /// Resets trend history
        /// </summary>
        public void ResetTrend()
        {
            _trendHistory.Clear();
            _averageValue = 0;
            _maxValue = 0;
            OnPropertyChanged(nameof(TrendHistory));
            OnPropertyChanged(nameof(AverageDisplay));
            OnPropertyChanged(nameof(MaxDisplay));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using ShieldX.Models;
using ShieldX.Services;

namespace ShieldX.ViewModels
{
    /// <summary>
    /// ViewModel for real-time resource monitoring with trend tracking
    /// </summary>
    public class ResourceMonitorViewModel : INotifyPropertyChanged
    {
        private readonly ResourceMonitorService _resourceMonitorService;
        private System.Timers.Timer _updateTimer;
        private const int MAX_HISTORY_POINTS = 60; // Keep 60 points for sparkline (1 per second)
        private const int UPDATE_INTERVAL_MS = 1000; // Update every 1 second

        // CPU Properties
        private double _cpuUsage;
        private ObservableCollection<double> _cpuHistory;
        private double _cpuAverage;
        private double _cpuPeak;

        // RAM Properties
        private double _ramUsage;
        private ObservableCollection<double> _ramHistory;
        private double _ramAverage;
        private double _ramPeak;
        private string _memoryInfo;

        // Disk Properties
        private double _diskUsage;
        private ObservableCollection<double> _diskHistory;
        private double _diskAverage;
        private double _diskPeak;
        private string _diskInfo;

        // ShieldX Process Properties
        private double _shieldXMemory;
        private string _resourceStatus;
        private bool _isHighLoad;

        public ResourceMonitorViewModel()
        {
            _resourceMonitorService = new ResourceMonitorService();
            
            // Initialize history collections
            _cpuHistory = new ObservableCollection<double>();
            _ramHistory = new ObservableCollection<double>();
            _diskHistory = new ObservableCollection<double>();
            
            // Initialize metrics
            _cpuUsage = 0;
            _ramUsage = 0;
            _diskUsage = 0;
            _shieldXMemory = 0;
            _memoryInfo = "Loading...";
            _diskInfo = "Loading...";
            _resourceStatus = "System healthy";
            _isHighLoad = false;
            _cpuPeak = 0;
            _ramPeak = 0;
            _diskPeak = 0;

            StartMonitoring();
        }

        // ==================== CPU Properties ====================
        public double CpuUsage
        {
            get { return _cpuUsage; }
            set { if (_cpuUsage != value) { _cpuUsage = value; OnPropertyChanged(); } }
        }

        public ObservableCollection<double> CpuHistory
        {
            get { return _cpuHistory; }
        }

        public double CpuAverage
        {
            get { return _cpuAverage; }
            set { if (_cpuAverage != value) { _cpuAverage = value; OnPropertyChanged(); } }
        }

        public double CpuPeak
        {
            get { return _cpuPeak; }
            set { if (_cpuPeak != value) { _cpuPeak = value; OnPropertyChanged(); } }
        }

        // ==================== RAM Properties ====================
        public double RamUsage
        {
            get { return _ramUsage; }
            set { if (_ramUsage != value) { _ramUsage = value; OnPropertyChanged(); } }
        }

        public ObservableCollection<double> RamHistory
        {
            get { return _ramHistory; }
        }

        public double RamAverage
        {
            get { return _ramAverage; }
            set { if (_ramAverage != value) { _ramAverage = value; OnPropertyChanged(); } }
        }

        public double RamPeak
        {
            get { return _ramPeak; }
            set { if (_ramPeak != value) { _ramPeak = value; OnPropertyChanged(); } }
        }

        public string MemoryInfo
        {
            get { return _memoryInfo; }
            set { if (_memoryInfo != value) { _memoryInfo = value; OnPropertyChanged(); } }
        }

        // ==================== Disk Properties ====================
        public double DiskUsage
        {
            get { return _diskUsage; }
            set { if (_diskUsage != value) { _diskUsage = value; OnPropertyChanged(); } }
        }

        public ObservableCollection<double> DiskHistory
        {
            get { return _diskHistory; }
        }

        public double DiskAverage
        {
            get { return _diskAverage; }
            set { if (_diskAverage != value) { _diskAverage = value; OnPropertyChanged(); } }
        }

        public double DiskPeak
        {
            get { return _diskPeak; }
            set { if (_diskPeak != value) { _diskPeak = value; OnPropertyChanged(); } }
        }

        public string DiskInfo
        {
            get { return _diskInfo; }
            set { if (_diskInfo != value) { _diskInfo = value; OnPropertyChanged(); } }
        }

        // ==================== ShieldX Process Properties ====================
        public double ShieldXMemory
        {
            get { return _shieldXMemory; }
            set { if (_shieldXMemory != value) { _shieldXMemory = value; OnPropertyChanged(); } }
        }

        // ==================== Status Properties ====================
        public string ResourceStatus
        {
            get { return _resourceStatus; }
            set { if (_resourceStatus != value) { _resourceStatus = value; OnPropertyChanged(); } }
        }

        public bool IsHighLoad
        {
            get { return _isHighLoad; }
            set { if (_isHighLoad != value) { _isHighLoad = value; OnPropertyChanged(); } }
        }

        // ==================== Methods ====================

        /// <summary>
        /// Starts the monitoring timer
        /// </summary>
        private void StartMonitoring()
        {
            if (_updateTimer != null)
                return;

            _updateTimer = new Timer(UPDATE_INTERVAL_MS);
            _updateTimer.Elapsed += UpdateMetrics;
            _updateTimer.AutoReset = true;
            _updateTimer.Start();

            // Initial update
            UpdateMetrics(null, null);
        }

        /// <summary>
        /// Updates resource metrics periodically
        /// </summary>
        private void UpdateMetrics(object sender, ElapsedEventArgs e)
        {
            try
            {
                // Get current readings
                double cpu = _resourceMonitorService.GetCpuUsage();
                double ram = _resourceMonitorService.GetRamUsage();
                double disk = _resourceMonitorService.GetDiskUsage();
                double shieldXMem = _resourceMonitorService.GetShieldXMemoryUsage();

                // Update current values (rounded to 1 decimal)
                CpuUsage = Math.Round(cpu, 1);
                RamUsage = Math.Round(ram, 1);
                DiskUsage = Math.Round(disk, 1);
                ShieldXMemory = Math.Round(shieldXMem, 1);

                // Update history and calculate stats
                UpdateHistory(_cpuHistory, cpu, out double cpuAvg);
                CpuAverage = Math.Round(cpuAvg, 1);
                if (cpu > CpuPeak)
                    CpuPeak = Math.Round(cpu, 1);

                UpdateHistory(_ramHistory, ram, out double ramAvg);
                RamAverage = Math.Round(ramAvg, 1);
                if (ram > RamPeak)
                    RamPeak = Math.Round(ram, 1);

                UpdateHistory(_diskHistory, disk, out double diskAvg);
                DiskAverage = Math.Round(diskAvg, 1);
                if (disk > DiskPeak)
                    DiskPeak = Math.Round(disk, 1);

                // Update text info
                MemoryInfo = _resourceMonitorService.GetMemoryInfo();
                DiskInfo = _resourceMonitorService.GetDiskInfo();

                // Update status
                UpdateResourceStatus(cpu, ram, disk);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating resource metrics: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates history collection with new value and calculates average
        /// </summary>
        private void UpdateHistory(ObservableCollection<double> history, double newValue, out double average)
        {
            history.Add(newValue);
            
            if (history.Count > MAX_HISTORY_POINTS)
                history.RemoveAt(0);

            // Calculate average
            double sum = 0;
            foreach (var value in history)
                sum += value;
            average = history.Count > 0 ? sum / history.Count : 0;
        }

        /// <summary>
        /// Updates the overall resource status based on current metrics
        /// </summary>
        private void UpdateResourceStatus(double cpu, double ram, double disk)
        {
            IsHighLoad = cpu > 80 || ram > 90 || disk > 95;

            if (cpu > 80)
                ResourceStatus = $"High CPU usage: {cpu:F1}%";
            else if (ram > 90)
                ResourceStatus = $"High memory usage: {ram:F1}%";
            else if (disk > 95)
                ResourceStatus = $"Critical disk usage: {disk:F1}%";
            else if (cpu > 60 || ram > 75 || disk > 80)
                ResourceStatus = $"Moderate resource usage";
            else
                ResourceStatus = "System healthy";
        }

        /// <summary>
        /// Resets all statistics and history
        /// </summary>
        public void ResetStatistics()
        {
            _cpuHistory.Clear();
            _ramHistory.Clear();
            _diskHistory.Clear();
            CpuAverage = 0;
            CpuPeak = 0;
            RamAverage = 0;
            RamPeak = 0;
            DiskAverage = 0;
            DiskPeak = 0;
        }

        /// <summary>
        /// Stops monitoring and cleans up resources
        /// </summary>
        public void Dispose()
        {
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer.Dispose();
                _updateTimer = null;
            }

            _resourceMonitorService?.Dispose();
        }

        /// <summary>
        /// Raises the PropertyChanged event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

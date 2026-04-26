using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ShieldX.Services
{
    /// <summary>
    /// Module Health and Status Service
    /// Tracks status and health metrics for all protection modules
    /// </summary>
    public class ModuleHealthService : INotifyPropertyChanged
    {
        private static readonly Lazy<ModuleHealthService> _instance = new Lazy<ModuleHealthService>(() => new ModuleHealthService());
        public static ModuleHealthService Instance => _instance.Value;

        public class ModuleStatus
        {
            public string ModuleName { get; set; }
            public bool IsRunning { get; set; }
            public bool IsHealthy { get; set; }
            public string Status { get; set; } // "Running", "Stopped", "Error", "Updating"
            public DateTime LastChecked { get; set; }
            public int IncidentsDetected { get; set; }
            public int IncidentsBlocked { get; set; }
            public double CpuUsage { get; set; }
            public long MemoryUsage { get; set; }
            public string LastStatusMessage { get; set; }
            public DateTime LastUpdateTime { get; set; }
        }

        private readonly Dictionary<string, ModuleStatus> _moduleStatuses = new();
        private ObservableCollection<ModuleStatus> _statusCollection;

        public ObservableCollection<ModuleStatus> StatusCollection
        {
            get => _statusCollection;
            set { _statusCollection = value; OnPropertyChanged(); }
        }

        public ModuleHealthService()
        {
            _statusCollection = new ObservableCollection<ModuleStatus>();
            InitializeModuleStatuses();
        }

        private void InitializeModuleStatuses()
        {
            var moduleNames = new[]
            {
                "RealTimeProtection", "WebShield", "RansomwareShield", "FirewallMonitor",
                "ExploitGuard", "EmailProtection", "DNSFilter", "BehaviorMonitor",
                "VulnerabilityScanner", "AIGuard"
            };

            foreach (var moduleName in moduleNames)
            {
                var status = new ModuleStatus
                {
                    ModuleName = moduleName,
                    IsRunning = false,
                    IsHealthy = true,
                    Status = "Stopped",
                    LastChecked = DateTime.Now,
                    IncidentsDetected = 0,
                    IncidentsBlocked = 0,
                    CpuUsage = 0,
                    MemoryUsage = 0,
                    LastStatusMessage = "Not running"
                };

                _moduleStatuses[moduleName] = status;
                _statusCollection.Add(status);
            }
        }

        public void UpdateModuleStatus(string moduleName, string status, string message = "")
        {
            if (_moduleStatuses.TryGetValue(moduleName, out var moduleStatus))
            {
                moduleStatus.Status = status;
                moduleStatus.LastStatusMessage = message;
                moduleStatus.LastChecked = DateTime.Now;
                moduleStatus.LastUpdateTime = DateTime.Now;
                moduleStatus.IsRunning = status == "Running";
                moduleStatus.IsHealthy = status != "Error";

                OnPropertyChanged(nameof(StatusCollection));
            }
        }

        public void IncrementIncidentsDetected(string moduleName, int count = 1)
        {
            if (_moduleStatuses.TryGetValue(moduleName, out var moduleStatus))
            {
                moduleStatus.IncidentsDetected += count;
                OnPropertyChanged(nameof(StatusCollection));
            }
        }

        public void IncrementIncidentsBlocked(string moduleName, int count = 1)
        {
            if (_moduleStatuses.TryGetValue(moduleName, out var moduleStatus))
            {
                moduleStatus.IncidentsBlocked += count;
                OnPropertyChanged(nameof(StatusCollection));
            }
        }

        public void UpdateResourceUsage(string moduleName, double cpuUsage, long memoryUsage)
        {
            if (_moduleStatuses.TryGetValue(moduleName, out var moduleStatus))
            {
                moduleStatus.CpuUsage = cpuUsage;
                moduleStatus.MemoryUsage = memoryUsage;
                OnPropertyChanged(nameof(StatusCollection));
            }
        }

        public ModuleStatus GetModuleStatus(string moduleName)
        {
            _moduleStatuses.TryGetValue(moduleName, out var status);
            return status;
        }

        public IEnumerable<ModuleStatus> GetAllModuleStatuses()
        {
            return _moduleStatuses.Values;
        }

        public int GetTotalIncidentsDetected()
        {
            return _moduleStatuses.Values.Sum(m => m.IncidentsDetected);
        }

        public int GetTotalIncidentsBlocked()
        {
            return _moduleStatuses.Values.Sum(m => m.IncidentsBlocked);
        }

        public int GetRunningModulesCount()
        {
            return _moduleStatuses.Values.Count(m => m.IsRunning);
        }

        public int GetHealthyModulesCount()
        {
            return _moduleStatuses.Values.Count(m => m.IsHealthy);
        }

        public double GetAverageCpuUsage()
        {
            var runningModules = _moduleStatuses.Values.Where(m => m.IsRunning).ToList();
            if (runningModules.Count == 0) return 0;
            return runningModules.Average(m => m.CpuUsage);
        }

        public long GetTotalMemoryUsage()
        {
            return _moduleStatuses.Values.Sum(m => m.MemoryUsage);
        }

        public bool AllModulesHealthy()
        {
            return _moduleStatuses.Values.All(m => m.IsHealthy || !m.IsRunning);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

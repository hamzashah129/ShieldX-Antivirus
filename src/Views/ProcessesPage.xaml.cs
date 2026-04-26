using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ShieldX.Services;

namespace ShieldX.Views
{
    public partial class ProcessesPage : Page
    {
        private readonly ObservableCollection<ProcessEntry> _processes = new();
        private ManagementEventWatcher _processCreationWatcher;
        private DispatcherTimer _refreshTimer;

        public ProcessesPage()
        {
            InitializeComponent();
            ProcessGrid.ItemsSource = _processes;
            LoadProcesses();
            InitializeRealtimeMonitoring();
        }
        
        private void InitializeRealtimeMonitoring()
        {
            try
            {
                // Watch for process creation events
                WqlEventQuery query = new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace");
                _processCreationWatcher = new ManagementEventWatcher(query);
                _processCreationWatcher.EventArrived += OnProcessCreated;
                _processCreationWatcher.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Real-time process monitoring disabled: {ex.Message}");
            }
            
            // Refresh every 30 seconds to catch process terminations
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _refreshTimer.Tick += (s, e) => RefreshProcessList();
            _refreshTimer.Start();
        }
        
        private void OnProcessCreated(object sender, EventArrivedEventArgs e)
        {
            try
            {
                string processName = e.NewEvent.Properties["ProcessName"]?.Value?.ToString() ?? "";
                
                Dispatcher.Invoke(() =>
                {
                    // Find if process already exists
                    var existingProcess = _processes.FirstOrDefault(p => 
                        p.Name.Equals(processName, StringComparison.OrdinalIgnoreCase));
                    
                    if (existingProcess == null)
                    {
                        // Get full process details
                        var proc = Process.GetProcessesByName(processName).FirstOrDefault();
                        if (proc != null)
                        {
                            LoadSingleProcess(proc);
                            System.Diagnostics.Debug.WriteLine($"[ProcessMonitor] New process detected: {processName}");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in process creation handler: {ex.Message}");
            }
        }
        
        private void RefreshProcessList()
        {
            Dispatcher.Invoke(() =>
            {
                var currentProcesses = Process.GetProcesses().Select(p => p.ProcessName).ToHashSet(StringComparer.OrdinalIgnoreCase);
                
                // Remove terminated processes
                var toRemove = _processes
                    .Where(p => !currentProcesses.Contains(p.Name))
                    .ToList();
                
                foreach (var item in toRemove)
                {
                    _processes.Remove(item);
                }
            });
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadProcesses();
        }

        private void LoadProcesses()
        {
            _processes.Clear();
            var processes = Process.GetProcesses().OrderBy(p => p.ProcessName);
            foreach (var process in processes)
            {
                LoadSingleProcess(process);
            }
        }
        
        private void LoadSingleProcess(Process process)
        {
            // Use ProcessAnomalyDetectionService for advanced risk assessment
            var riskScore = ProcessAnomalyDetectionService.Instance.AssessProcessRisk(process);
            var classification = ProcessAnomalyDetectionService.Instance.ClassifyProcess(process);
            var isSuspicious = riskScore >= 60; // Consider 60+ as suspicious

            try
            {
                _processes.Add(new ProcessEntry
                {
                    Name = process.ProcessName,
                    Id = process.Id,
                    MemoryMb = $"{process.WorkingSet64 / 1024.0 / 1024.0:F1} MB",
                    Threads = process.Threads.Count,
                    StartTime = process.StartTime.ToString("g"),
                    Status = classification,
                    IsSuspicious = isSuspicious,
                    RiskScore = riskScore
                });
            }
            catch
            {
                _processes.Add(new ProcessEntry
                {
                    Name = process.ProcessName,
                    Id = process.Id,
                    MemoryMb = "N/A",
                    Threads = process.Threads.Count,
                    StartTime = "N/A",
                    Status = classification,
                    IsSuspicious = isSuspicious,
                    RiskScore = riskScore
                });
            }
        }

        /// <summary>
        /// Cleanup resources when page is unloaded
        /// </summary>
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _processCreationWatcher?.Stop();
                _processCreationWatcher?.Dispose();
            }
            catch { }

            try
            {
                _refreshTimer?.Stop();
            }
            catch { }
        }
    }

    public class ProcessEntry
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public string MemoryMb { get; set; }
        public int Threads { get; set; }
        public string StartTime { get; set; }
        public string Status { get; set; }
        public bool IsSuspicious { get; set; }
        public int RiskScore { get; set; }
    }
}
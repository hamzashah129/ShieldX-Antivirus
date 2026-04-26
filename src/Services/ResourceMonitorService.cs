using System;
using System.Diagnostics;
using ShieldX.Models;

namespace ShieldX.Services
{
    /// <summary>
    /// Service for monitoring system resource usage in real-time
    /// </summary>
    public class ResourceMonitorService
    {
        private PerformanceCounter _cpuCounter;
        private PerformanceCounter _ramCounter;
        private PerformanceCounter _diskCounter;
        private Process _currentProcess;

        public ResourceMonitorService()
        {
            InitializePerformanceCounters();
            _currentProcess = Process.GetCurrentProcess();
        }

        /// <summary>
        /// Initializes performance counter instances
        /// </summary>
        private void InitializePerformanceCounters()
        {
            try
            {
                // CPU usage (total % for all processors)
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                _cpuCounter.NextValue(); // Warm up the counter
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing CPU counter: {ex.Message}");
            }

            try
            {
                // RAM usage (available memory in MB)
                _ramCounter = new PerformanceCounter("Memory", "Available MBytes", "", true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing RAM counter: {ex.Message}");
            }

            try
            {
                // Disk usage (% free space on C: drive)
                _diskCounter = new PerformanceCounter("PhysicalDisk", "% Free Space", "0 C:", true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing Disk counter: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the current CPU usage percentage
        /// </summary>
        public double GetCpuUsage()
        {
            try
            {
                if (_cpuCounter != null)
                {
                    float value = _cpuCounter.NextValue();
                    return Math.Max(0, Math.Min(100, value)); // Clamp 0-100
                }
            }
            catch { }

            return 0;
        }

        /// <summary>
        /// Gets the current RAM usage percentage
        /// </summary>
        public double GetRamUsage()
        {
            try
            {
                // Use performance counter for available memory
                if (_ramCounter != null)
                {
                    float availableMB = _ramCounter.NextValue();
                    
                    // Estimate usage based on available memory
                    // Assuming typical system has more than 4GB RAM
                    // If available < 2GB, usage is > 50% (rough estimate)
                    double estimatedUsage = 100.0 - Math.Min(100, (availableMB / 4000.0) * 100.0);
                    
                    return Math.Max(0, Math.Min(100, estimatedUsage));
                }
            }
            catch { }

            return 0;
        }

        /// <summary>
        /// Gets the current Disk C: usage percentage (used space)
        /// </summary>
        public double GetDiskUsage()
        {
            try
            {
                if (_diskCounter != null)
                {
                    float freePercent = _diskCounter.NextValue();
                    double usedPercent = 100 - freePercent;
                    return Math.Max(0, Math.Min(100, usedPercent)); // Clamp 0-100
                }

                // Fallback: calculate from drive info
                var driveInfo = new System.IO.DriveInfo("C:");
                if (driveInfo.IsReady)
                {
                    double usedPercent = (double)(driveInfo.TotalSize - driveInfo.AvailableFreeSpace) / driveInfo.TotalSize * 100;
                    return Math.Max(0, Math.Min(100, usedPercent)); // Clamp 0-100
                }
            }
            catch { }

            return 0;
        }

        /// <summary>
        /// Gets ShieldX process memory usage in MB
        /// </summary>
        public double GetShieldXMemoryUsage()
        {
            try
            {
                if (_currentProcess != null)
                {
                    _currentProcess.Refresh();
                    // Convert bytes to MB
                    return _currentProcess.WorkingSet64 / (1024.0 * 1024.0);
                }
            }
            catch { }

            return 0;
        }

        /// <summary>
        /// Gets total system memory in bytes
        /// </summary>
        private ulong GetTotalSystemMemory()
        {
            try
            {
                // Estimate based on available memory counter
                // In a real implementation, use WMI or other Windows APIs
                // For now, assume 8GB system
                return 8UL * 1024 * 1024 * 1024;
            }
            catch { }

            return 0;
        }

        /// <summary>
        /// Gets formatted disk info string
        /// </summary>
        public string GetDiskInfo()
        {
            try
            {
                var driveInfo = new System.IO.DriveInfo("C:");
                if (driveInfo.IsReady)
                {
                    long totalGB = driveInfo.TotalSize / (1024 * 1024 * 1024);
                    long usedGB = (driveInfo.TotalSize - driveInfo.AvailableFreeSpace) / (1024 * 1024 * 1024);
                    return $"{usedGB}GB / {totalGB}GB";
                }
            }
            catch { }

            return "N/A";
        }

        /// <summary>
        /// Gets formatted memory info string
        /// </summary>
        public string GetMemoryInfo()
        {
            try
            {
                // Return available memory info if counter is working
                if (_ramCounter != null)
                {
                    float availableMB = _ramCounter.NextValue();
                    long availableGB = (long)(availableMB / 1024);
                    // Assume 8GB total
                    long totalGB = 8;
                    long usedGB = totalGB - availableGB;
                    return $"{usedGB}GB / {totalGB}GB";
                }
            }
            catch { }

            return "N/A";
        }

        /// <summary>
        /// Disposes of performance counters
        /// </summary>
        public void Dispose()
        {
            _cpuCounter?.Dispose();
            _ramCounter?.Dispose();
            _diskCounter?.Dispose();
        }
    }
}

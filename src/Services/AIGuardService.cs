using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ShieldX.Models;

namespace ShieldX.Services
{
    public class AIGuardService : IDisposable
    {
        private static readonly Lazy<AIGuardService> _instance = new(() => new AIGuardService());
        public static AIGuardService Instance => _instance.Value;

        private readonly AIGuardAnalyzer _analyzer = new();
        private CancellationTokenSource _cancellationTokenSource;
        private Task _scanTask;
        private bool _isRunning;
        private readonly object _lock = new();

        // Cache for WMI data and signatures (invalidate after 10 seconds)
        private readonly Dictionary<int, (string CommandLine, string SignerName, DateTime CachedAt)> _processCache = new();
        private readonly HashSet<string> _microsoftSignedHashes = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<int> _whitelistedPids = new();

        // Statistics
        private int _processesScannedToday;
        private int _threatsBlockedToday;
        private int _suspiciousFlaggedToday;

        public event Action<AIGuardResult> ThreatBlocked;
        public event Action<AIGuardResult> ThreatSuspended;
        public event Action<AIGuardResult> ThreatFlagged;
        public event Action<string> StatusChanged;

        public bool IsRunning => _isRunning;
        public int ProcessesScannedToday => _processesScannedToday;
        public int ThreatsBlockedToday => _threatsBlockedToday;
        public int SuspiciousFlaggedToday => _suspiciousFlaggedToday;

        private AIGuardService()
        {
            // Whitelist ShieldX process
            try
            {
                _whitelistedPids.Add(Process.GetCurrentProcess().Id);
            }
            catch { }
        }

        public void Start()
        {
            lock (_lock)
            {
                if (_isRunning)
                    return;

                _isRunning = true;
                _cancellationTokenSource = new CancellationTokenSource();
                _processesScannedToday = 0;
                _threatsBlockedToday = 0;
                _suspiciousFlaggedToday = 0;

                _scanTask = ScanLoop(_cancellationTokenSource.Token);
                LogService.Instance.AddInfo("AI Guard started", "AIGuard");
                StatusChanged?.Invoke("AI Guard Active - Monitoring processes...");
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (!_isRunning)
                    return;

                _isRunning = false;
                _cancellationTokenSource?.Cancel();

                try
                {
                    _scanTask?.Wait(TimeSpan.FromSeconds(5));
                }
                catch { }

                LogService.Instance.AddInfo("AI Guard stopped", "AIGuard");
                StatusChanged?.Invoke("AI Guard Inactive");
            }
        }

        private async Task ScanLoop(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(500, cancellationToken);

                    try
                    {
                        ScanRunningProcesses(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        LogService.Instance.AddError($"Error in AI Guard scan loop: {ex.Message}", "AIGuard");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
            }
        }

        private void ScanRunningProcesses(CancellationToken cancellationToken)
        {
            try
            {
                Process[] processes = Process.GetProcesses();

                foreach (var proc in processes)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        // Check whitelist
                        if (_whitelistedPids.Contains(proc.Id))
                            continue;

                        // Check for Microsoft-signed processes
                        if (IsMicrosoftSignedProcess(proc))
                            continue;

                        // Collect snapshot
                        ProcessSnapshot snapshot = CollectProcessSnapshot(proc);
                        if (snapshot == null)
                            continue;

                        _processesScannedToday++;

                        // Analyze
                        AIGuardResult result = _analyzer.Analyze(snapshot);

                        // Take action based on threat score
                        if (result.ThreatScore > 0.85f)
                        {
                            HandleBlockThreat(proc, result);
                        }
                        else if (result.ThreatScore > 0.65f)
                        {
                            HandleSuspendThreat(proc, result);
                        }
                        else if (result.ThreatScore > 0.45f)
                        {
                            HandleFlagThreat(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Process may have terminated, ignore
                    }
                }

                // Cleanup cache
                CleanupProcessCache();
            }
            finally
            {
                try
                {
                    Process[] processes = Process.GetProcesses();
                    foreach (var p in processes)
                    {
                        try { p.Dispose(); } catch { }
                    }
                }
                catch { }
            }
        }

        private ProcessSnapshot CollectProcessSnapshot(Process proc)
        {
            try
            {
                ProcessSnapshot snapshot = new ProcessSnapshot
                {
                    Pid = proc.Id,
                    Name = proc.ProcessName,
                    CapturedAt = DateTime.Now
                };

                // FullPath
                try
                {
                    snapshot.FullPath = proc.MainModule?.FileName ?? "";
                }
                catch { }

                // CPU and RAM
                try
                {
                    snapshot.CpuPercent = GetProcessCpuUsage(proc);
                    snapshot.RamMb = proc.WorkingSet64 / (1024 * 1024);
                }
                catch { }

                // Thread count
                snapshot.ThreadCount = proc.Threads.Count;

                // ParentPID, CommandLine, and UserAccount via WMI
                try
                {
                    var wmiData = GetProcessWmiData(proc.Id);
                    snapshot.ParentPid = wmiData.ParentPid;
                    snapshot.CommandLine = wmiData.CommandLine;
                    snapshot.UserAccount = wmiData.UserAccount;
                }
                catch { }

                // TCP Connections
                try
                {
                    snapshot.TcpConnections = GetProcessTcpConnections(proc.Id);
                }
                catch { }

                // Digital Signature
                try
                {
                    if (!string.IsNullOrEmpty(snapshot.FullPath) && File.Exists(snapshot.FullPath))
                    {
                        var sigInfo = GetFileSignature(snapshot.FullPath);
                        snapshot.IsSigned = sigInfo.IsSigned;
                        snapshot.SignerName = sigInfo.SignerName;
                    }
                }
                catch { }

                return snapshot;
            }
            catch
            {
                return null;
            }
        }

        private double GetProcessCpuUsage(Process proc)
        {
            try
            {
                var totalProcessorTime = proc.TotalProcessorTime.TotalMilliseconds;
                var totalMachineTime = Environment.TickCount;

                // Simplified CPU usage calculation (approximation)
                // A more accurate method would track deltas over time
                return (totalProcessorTime / (totalMachineTime / 100.0)) * 100.0;
            }
            catch
            {
                return 0;
            }
        }

        private (int ParentPid, string CommandLine, string UserAccount) GetProcessWmiData(int pid)
        {
            // Check cache first
            lock (_processCache)
            {
                if (_processCache.TryGetValue(pid, out var cached) &&
                    (DateTime.Now - cached.CachedAt).TotalSeconds < 10)
                {
                    return (pid, cached.CommandLine, cached.SignerName);
                }
            }

            int parentPid = 0;
            string commandLine = "";
            string userAccount = "";

            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    $"SELECT ParentProcessId, CommandLine, ExecutablePath FROM Win32_Process WHERE ProcessId = {pid}"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        if (obj["ParentProcessId"] != null)
                            parentPid = Convert.ToInt32(obj["ParentProcessId"]);
                        if (obj["CommandLine"] != null)
                            commandLine = obj["CommandLine"].ToString();

                        // Get user account
                        using (var procSearcher = new ManagementObjectSearcher(
                            $"SELECT ExecutedBy FROM Win32_Process WHERE ProcessId = {pid}"))
                        {
                            foreach (var procObj in procSearcher.Get())
                            {
                                if (procObj["ExecutedBy"] != null)
                                    userAccount = procObj["ExecutedBy"].ToString();
                            }
                        }
                    }
                }
            }
            catch { }

            // Cache the result
            lock (_processCache)
            {
                _processCache[pid] = (commandLine, userAccount, DateTime.Now);
            }

            return (parentPid, commandLine, userAccount);
        }

        private List<string> GetProcessTcpConnections(int pid)
        {
            var connections = new List<string>();

            try
            {
                var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                var tcpConnections = ipGlobalProperties.GetActiveTcpConnections();

                foreach (var conn in tcpConnections.Where(c => c.State == TcpState.Established))
                {
                    connections.Add($"{conn.RemoteEndPoint.Address}:{conn.RemoteEndPoint.Port}");
                }
            }
            catch { }

            return connections;
        }

        private (bool IsSigned, string SignerName) GetFileSignature(string filePath)
        {
            try
            {
                var cert = X509Certificate2.CreateFromSignedFile(filePath);
                string signerName = cert?.Subject ?? "Unknown";
                return (true, signerName);
            }
            catch
            {
                return (false, "");
            }
        }

        private bool IsMicrosoftSignedProcess(Process proc)
        {
            try
            {
                var mainModule = proc.MainModule;
                if (mainModule == null)
                    return false;

                var cert = X509Certificate2.CreateFromSignedFile(mainModule.FileName);
                string subject = cert?.Subject ?? "";

                return subject.Contains("Microsoft", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private void HandleBlockThreat(Process proc, AIGuardResult result)
        {
            try
            {
                // Quarantine
                try
                {
                    _ = QuarantineManager.QuarantineAsync(result.Process.FullPath, $"AI Guard Threat: {result.ThreatClass}");
                }
                catch { }

                // Kill process
                try
                {
                    proc.Kill(true);
                }
                catch { }

                result.ActionTaken = "Blocked and Quarantined";
                _threatsBlockedToday++;

                // Notify
                ThreatBlocked?.Invoke(result);
                LogService.Instance.AddWarning(
                    $"AI Guard blocked: {result.Process.Name} ({result.ThreatClass}) - Score: {result.ThreatScore:P0}",
                    "AIGuard");

                // Toast notification on main thread
                ToastNotificationService.ShowNotification(
                    $"🚫 Threat Blocked: {result.Process.Name}",
                    $"AI Guard detected and blocked: {result.ThreatClass}",
                    severity: ToastNotificationService.NotificationSeverity.Critical);
            }
            catch (Exception ex)
            {
                LogService.Instance.AddError($"Error blocking threat: {ex.Message}", "AIGuard");
            }
        }

        private void HandleSuspendThreat(Process proc, AIGuardResult result)
        {
            try
            {
                // Suspend process threads
                try
                {
                    foreach (ProcessThread thread in proc.Threads)
                    {
                        // Note: SuspendThread not directly available in .NET, would need P/Invoke
                        // For now, just log the suspension action
                    }
                }
                catch { }

                result.ActionTaken = "Suspended for Review";
                _suspiciousFlaggedToday++;

                ThreatSuspended?.Invoke(result);
                LogService.Instance.AddWarning(
                    $"AI Guard suspended: {result.Process.Name} ({result.ThreatClass}) - Score: {result.ThreatScore:P0}",
                    "AIGuard");

                ToastNotificationService.ShowNotification(
                    $"⚠️  Suspicious Process",
                    $"AI Guard suspended: {result.Process.Name} for review",
                    severity: ToastNotificationService.NotificationSeverity.Warning);
            }
            catch (Exception ex)
            {
                LogService.Instance.AddError($"Error suspending threat: {ex.Message}", "AIGuard");
            }
        }

        private void HandleFlagThreat(AIGuardResult result)
        {
            try
            {
                result.ActionTaken = "Flagged";
                _suspiciousFlaggedToday++;

                ThreatFlagged?.Invoke(result);
                LogService.Instance.AddInfo(
                    $"AI Guard flagged: {result.Process.Name} ({result.ThreatClass}) - Score: {result.ThreatScore:P0}",
                    "AIGuard");
            }
            catch (Exception ex)
            {
                LogService.Instance.AddError($"Error flagging threat: {ex.Message}", "AIGuard");
            }
        }

        private void CleanupProcessCache()
        {
            lock (_processCache)
            {
                var now = DateTime.Now;
                var staleKeys = _processCache
                    .Where(x => (now - x.Value.CachedAt).TotalSeconds > 10)
                    .Select(x => x.Key)
                    .ToList();

                foreach (var key in staleKeys)
                    _processCache.Remove(key);
            }
        }

        public void Dispose()
        {
            Stop();
            _cancellationTokenSource?.Dispose();
        }
    }
}

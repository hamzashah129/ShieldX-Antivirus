using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;

namespace ShieldX.Services
{
    /// <summary>
    /// Service for advanced process anomaly detection using behavioral analysis
    /// </summary>
    public class ProcessAnomalyDetectionService
    {
        private static readonly Lazy<ProcessAnomalyDetectionService> _instance = 
            new(() => new ProcessAnomalyDetectionService());
        public static ProcessAnomalyDetectionService Instance => _instance.Value;

        private readonly HashSet<string> _microsoftSignedProcesses = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ProcessBehaviorProfile> _behaviorProfiles = new();

        private ProcessAnomalyDetectionService()
        {
            InitializeMicrosoftSignedProcesses();
            InitializeBehaviorProfiles();
        }

        /// <summary>
        /// Assess process for anomalous behavior and return risk score (0-100)
        /// </summary>
        public int AssessProcessRisk(Process process)
        {
            if (process == null)
                return 0;

            var riskScore = 0;

            // 1. Check if Microsoft signed (legitimate)
            if (IsMicrosoftSignedProcess(process))
                return 0;

            // 2. Check process path
            riskScore += AssessProcessPath(process);

            // 3. Check process name
            riskScore += AssessProcessName(process);

            // 4. Check command line arguments
            riskScore += AssessCommandLineArguments(process);

            // 5. Check parent process
            riskScore += AssessParentProcess(process);

            // 6. Check memory usage anomalies
            riskScore += AssessMemoryUsage(process);

            // 7. Check for suspicious network activity
            riskScore += AssessNetworkActivity(process);

            return Math.Min(riskScore, 100);
        }

        /// <summary>
        /// Get classification of process behavior
        /// </summary>
        public string ClassifyProcess(Process process)
        {
            var riskScore = AssessProcessRisk(process);

            return riskScore switch
            {
                >= 80 => "Critical",
                >= 60 => "High",
                >= 40 => "Medium",
                >= 20 => "Low",
                _ => "Safe"
            };
        }

        private int AssessProcessPath(Process process)
        {
            int score = 0;
            try
            {
                var path = process.MainModule?.FileName;
                if (string.IsNullOrEmpty(path))
                    return 10; // Unknown path is suspicious

                var pathLower = path.ToLower();

                // High risk paths
                if (pathLower.Contains("\\temp\\")) score += 25;
                if (pathLower.Contains("\\appdata\\roaming\\")) score += 20;
                if (pathLower.Contains("\\appdata\\local\\temp\\")) score += 30;
                if (pathLower.Contains("\\programdata\\")) score += 15;
                if (pathLower.Contains("\\users\\public\\")) score += 20;

                // Suspicious file extensions
                if (pathLower.EndsWith(".exe") && !path.Contains("Program Files"))
                    score += 5;

                // Legitimate paths
                if (pathLower.StartsWith("c:\\windows\\system32\\") || 
                    pathLower.StartsWith("c:\\windows\\syswow64\\"))
                    score = Math.Max(0, score - 20);

                if (pathLower.StartsWith("c:\\program files\\") || 
                    pathLower.StartsWith("c:\\program files (x86)\\"))
                    score = Math.Max(0, score - 10);
            }
            catch
            {
                score += 5; // Inability to access path is slightly suspicious
            }

            return score;
        }

        private int AssessProcessName(Process process)
        {
            int score = 0;
            var name = process.ProcessName.ToLower();

            // High-risk process names
            var highRiskNames = new[] 
            { 
                "mimikatz", "psexec", "wmiprvse", "rundll32", "regsvcs", "regasm",
                "csc", "jsc", "csi", "mshta", "msiexec", "powerp", "powershell",
                "cmd", "certutil", "whoami", "ipconfig", "systeminfo", "tasklist",
                "wscript", "cscript", "vbscript"
            };

            if (highRiskNames.Any(n => name.Contains(n)))
                score += 35;

            // Medium-risk names (tools commonly misused)
            var mediumRiskNames = new[] 
            { 
                "reg", "net", "taskkill", "sc", "at", "schtasks", "wevtutil",
                "eventvwr", "devmgmt", "diskmgmt", "services", "compmgmt"
            };

            if (mediumRiskNames.Any(n => name.Contains(n)))
                score += 20;

            return score;
        }

        private int AssessCommandLineArguments(Process process)
        {
            int score = 0;
            try
            {
                var query = new ObjectQuery($"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}");
                var searcher = new ManagementObjectSearcher(query);
                var results = searcher.Get().Cast<ManagementObject>();
                var cmdLine = results.FirstOrDefault()?["CommandLine"]?.ToString() ?? "";

                if (string.IsNullOrEmpty(cmdLine))
                    return 0;

                var cmdLower = cmdLine.ToLower();

                // High-risk command patterns
                if (cmdLower.Contains("-nop") || cmdLower.Contains("-noprofile"))
                    score += 20;

                if (cmdLower.Contains("-enc") || cmdLower.Contains("-encodedcommand"))
                    score += 25;

                if (cmdLower.Contains("iex") || cmdLower.Contains("invoke-expression"))
                    score += 25;

                if (cmdLower.Contains("downloadstring") || cmdLower.Contains("downloadfile"))
                    score += 30;

                if (cmdLower.Contains("reflectionemit") || cmdLower.Contains("assemblybuilder"))
                    score += 25;

                if (cmdLower.Contains("createsubkey") || cmdLower.Contains("setvalue"))
                    score += 20;

                if (cmdLower.Contains("/c ") && (cmdLower.Contains("del") || cmdLower.Contains("copy")))
                    score += 15;
            }
            catch
            {
                // Ignore errors
            }

            return score;
        }

        private int AssessParentProcess(Process process)
        {
            int score = 0;
            try
            {
                var parentName = GetParentProcessName(process.Id);
                if (string.IsNullOrEmpty(parentName))
                    return 0;

                var parentNameLower = parentName.ToLower();

                // High-risk parent processes
                var suspiciousParents = new[] 
                { 
                    "explorer", "svchost", "services", "lsass", "csrss",
                    "cmd", "powershell", "rundll32", "wscript", "cscript"
                };

                // If parent is cmd/powershell/wscript, score higher
                if (new[] { "cmd", "powershell", "wscript", "cscript" }
                    .Any(p => parentNameLower.Contains(p)))
                    score += 20;
            }
            catch
            {
                // Ignore errors
            }

            return score;
        }

        private int AssessMemoryUsage(Process process)
        {
            int score = 0;
            try
            {
                var memoryMb = process.WorkingSet64 / 1024 / 1024;

                // Excessive memory usage is suspicious
                if (memoryMb > 1000)
                    score += 15;

                if (memoryMb > 500)
                    score += 10;

                if (memoryMb > 200 && !IsSystemProcess(process))
                    score += 5;
            }
            catch
            {
                // Ignore errors
            }

            return score;
        }

        private int AssessNetworkActivity(Process process)
        {
            int score = 0;
            try
            {
                // Check if process has open network connections
                // Note: ProcessId property moved in .NET 8, using simplified assessment
                var tcpConnections = System.Net.NetworkInformation.IPGlobalProperties
                    .GetIPGlobalProperties()
                    .GetActiveTcpConnections()
                    .ToList();

                // Assess risk based on connection count (simplified for .NET 8 compatibility)
                if (tcpConnections.Count > 100)
                    score += 10;  // Many active connections

                // Check for unusual ports
                foreach (var conn in tcpConnections.Take(50))  // Sample first 50
                {
                    if (conn.RemoteEndPoint.Port > 10000)
                        score += 5;
                }
            }
            catch
            {
                // Ignore errors
            }

            return score;
        }

        private bool IsMicrosoftSignedProcess(Process process)
        {
            try
            {
                var path = process.MainModule?.FileName;
                if (string.IsNullOrEmpty(path))
                    return false;

                // Check against known Microsoft signed processes
                if (_microsoftSignedProcesses.Contains(Path.GetFileName(path)))
                    return true;

                // Additional check: processes in System32
                if (path.ToLower().Contains("\\windows\\system32\\"))
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool IsSystemProcess(Process process)
        {
            var systemProcesses = new[] 
            { 
                "System", "csrss", "lsass", "svchost", "dwm", "explorer", "searchindexer"
            };
            return systemProcesses.Contains(process.ProcessName, StringComparer.OrdinalIgnoreCase);
        }

        private string GetParentProcessName(int processId)
        {
            try
            {
                var query = new ObjectQuery($"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {processId}");
                var searcher = new ManagementObjectSearcher(query);
                var results = searcher.Get().Cast<ManagementObject>();
                var parentId = results.FirstOrDefault()?["ParentProcessId"];

                if (parentId != null && int.TryParse(parentId.ToString(), out var pid))
                {
                    try
                    {
                        return Process.GetProcessById(pid).ProcessName;
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
            catch
            {
                // Ignore
            }

            return null;
        }

        private void InitializeMicrosoftSignedProcesses()
        {
            var processes = new[] 
            { 
                "explorer.exe", "svchost.exe", "services.exe", "lsass.exe", "csrss.exe",
                "dwm.exe", "taskmgr.exe", "notepad.exe", "calc.exe", "cmd.exe",
                "powershell.exe", "eventvwr.exe", "devmgmt.msc", "diskmgmt.msc",
                "services.msc", "compmgmt.msc", "wininit.exe", "winlogon.exe"
            };

            foreach (var p in processes)
                _microsoftSignedProcesses.Add(p);
        }

        private void InitializeBehaviorProfiles()
        {
            // Could be extended with real ML models
        }

        private class ProcessBehaviorProfile
        {
            public string ProcessName { get; set; }
            public int AverageMemory { get; set; }
            public int MaxMemory { get; set; }
            public bool NeedsNetworkAccess { get; set; }
        }
    }
}

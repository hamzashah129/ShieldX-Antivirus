using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Win32;
using Serilog;

namespace ShieldX.Services
{
    /// <summary>
    /// Monitors Windows registry for suspicious persistence entries where malware hides.
    /// Scans run keys, startup folders, and other common malware persistence locations.
    /// </summary>
    public class RegistryMonitorService : IDisposable
    {
        private Timer? _timer;
        private bool _disposed;

        public event Action<string, string>? SuspiciousEntryFound;

        // Registry keys commonly exploited by malware for persistence
        private static readonly string[] RunKeys =
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\RunOnce",
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders",
            @"SYSTEM\CurrentControlSet\Services",
        };

        private readonly Dictionary<string, string> _baselineEntries = new();
        private readonly object _lockObject = new();

        public RegistryMonitorService()
        {
            Log.Information("RegistryMonitorService initialized");
        }

        /// <summary>
        /// Starts monitoring registry every 30 seconds for suspicious entries.
        /// </summary>
        public void Start()
        {
            _timer = new Timer(CheckRegistry, null,
                TimeSpan.Zero, TimeSpan.FromSeconds(30));

            Log.Information("Registry monitor started - scanning every 30 seconds");
        }

        /// <summary>
        /// Stops the registry monitoring timer.
        /// </summary>
        public void Stop()
        {
            _timer?.Dispose();
            _timer = null;
            Log.Information("Registry monitor stopped");
        }

        /// <summary>
        /// Scans registry keys for suspicious entries.
        /// </summary>
        private void CheckRegistry(object? state)
        {
            try
            {
                foreach (var keyPath in RunKeys)
                {
                    try
                    {
                        RegistryKey? key = null;

                        // Try HKCU first, then HKLM
                        if (keyPath.StartsWith("SOFTWARE"))
                        {
                            key = Registry.CurrentUser.OpenSubKey(keyPath);
                            if (key == null)
                                key = Registry.LocalMachine.OpenSubKey(keyPath);
                        }
                        else if (keyPath.StartsWith("SYSTEM"))
                        {
                            key = Registry.LocalMachine.OpenSubKey(keyPath);
                        }

                        if (key == null)
                            continue;

                        using (key)
                        {
                            foreach (var valueName in key.GetValueNames())
                            {
                                try
                                {
                                    var value = key.GetValue(valueName)?.ToString() ?? "";
                                    string entryKey = $"{keyPath}\\{valueName}";

                                    lock (_lockObject)
                                    {
                                        // New entry detected
                                        if (!_baselineEntries.ContainsKey(entryKey))
                                        {
                                            _baselineEntries[entryKey] = value;

                                            // Check if suspicious
                                            if (IsSuspiciousRegistryEntry(value))
                                            {
                                                Log.Warning($"[ShieldX Registry] SUSPICIOUS ENTRY: {valueName} = {value}");
                                                SuspiciousEntryFound?.Invoke(valueName, value);
                                            }
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Registry monitor error");
            }
        }

        /// <summary>
        /// Determines if a registry entry looks suspicious (malware persistence pattern).
        /// </summary>
        private static bool IsSuspiciousRegistryEntry(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string v = value.ToLowerInvariant();

            // Suspicious: Temp/Downloads paths
            if (v.Contains("temp\\") || v.Contains("tmp\\") ||
                v.Contains("appdata\\local\\temp") || v.Contains("%temp%"))
                return true;

            // Suspicious: PowerShell with encoding/hidden args
            if (v.Contains("powershell") && (v.Contains("-encoded") || v.Contains("-windowstyle hidden")))
                return true;

            // Suspicious: CMD with hidden execution
            if (v.Contains("cmd.exe") || v.Contains("cmd /c") && v.Contains("%"))
                return true;

            // Suspicious: Script interpreters
            if (v.Contains("wscript") || v.Contains("cscript") || v.Contains("mshta"))
                return true;

            // Suspicious: Registry modification
            if (v.Contains("regsvr32") || v.Contains("rundll32"))
                return true;

            // Suspicious: Known malware names
            string[] malwareIndicators = {
                "xmrig", "minerd", "cryptominer", "keylogger", "trojan", "ransomware",
                "backdoor", "spyware", "stealer", "grabber", "rat", "payload"
            };

            if (malwareIndicators.Any(ind => v.Contains(ind)))
                return true;

            // Suspicious: VBS/JS execution
            if (v.Contains(".vbs") || v.Contains(".js") || v.Contains(".jse"))
                return true;

            // Suspicious: UNC paths or remote execution
            if (v.Contains("\\\\") && !v.Contains("\\\\?\\"))
                return true;

            return false;
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            Stop();
            _disposed = true;
            Log.Information("RegistryMonitorService disposed");
        }
    }
}

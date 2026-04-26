using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShieldX.Services
{
    /// <summary>
    /// Service for managing network security including IP blocking and connection analysis
    /// </summary>
    public class NetworkSecurityService
    {
        private static readonly Lazy<NetworkSecurityService> _instance = new(() => new NetworkSecurityService());
        public static NetworkSecurityService Instance => _instance.Value;

        private readonly string _blockedIpsFile;
        private HashSet<string> _blockedIps = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _lock = new();

        private NetworkSecurityService()
        {
            _blockedIpsFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ShieldX",
                "blocked_ips.json");

            LoadBlockedIps();
        }

        /// <summary>
        /// Get list of currently blocked IPs
        /// </summary>
        public IReadOnlyList<string> GetBlockedIps()
        {
            lock (_lock)
            {
                return _blockedIps.ToList();
            }
        }

        /// <summary>
        /// Block an IP address with firewall rule and persistent storage
        /// </summary>
        public async Task<bool> BlockIpAsync(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return false;

            lock (_lock)
            {
                if (_blockedIps.Contains(ipAddress))
                    return true; // Already blocked
            }

            try
            {
                // Create firewall rule via Windows Firewall API
                var ruleName = $"ShieldX_Block_{DateTime.Now.Ticks}";
                var blockResult = await CreateFirewallRule(ipAddress, ruleName);

                if (blockResult)
                {
                    lock (_lock)
                    {
                        _blockedIps.Add(ipAddress);
                        SaveBlockedIps();
                    }

                    LogService.Instance.AddWarning($"Blocked IP: {ipAddress}", "NetworkSecurity");
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.AddError($"Failed to block IP {ipAddress}: {ex.Message}", "NetworkSecurity");
            }

            return false;
        }

        /// <summary>
        /// Unblock a previously blocked IP address
        /// </summary>
        public async Task<bool> UnblockIpAsync(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return false;

            lock (_lock)
            {
                if (!_blockedIps.Contains(ipAddress))
                    return true; // Already unblocked
            }

            try
            {
                var unblockResult = await RemoveFirewallRule(ipAddress);

                if (unblockResult)
                {
                    lock (_lock)
                    {
                        _blockedIps.Remove(ipAddress);
                        SaveBlockedIps();
                    }

                    LogService.Instance.AddInfo($"Unblocked IP: {ipAddress}", "NetworkSecurity");
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.AddError($"Failed to unblock IP {ipAddress}: {ex.Message}", "NetworkSecurity");
            }

            return false;
        }

        /// <summary>
        /// Assess risk level of an IP connection
        /// </summary>
        public string AssessConnectionRisk(string remoteIp, int remotePort, string connectionState)
        {
            if (string.IsNullOrWhiteSpace(remoteIp))
                return "Unknown";

            // Check if IP is blocked
            lock (_lock)
            {
                if (_blockedIps.Contains(remoteIp))
                    return "Blocked";
            }

            // Check if loopback or private
            if (IsPrivateOrLoopback(remoteIp))
                return "Local";

            // Check if trusted
            if (IsTrustedIp(remoteIp))
                return "Trusted";

            // Assess based on port
            var riskScore = 0;

            // Suspicious ports
            if (remotePort > 10000 || remotePort < 1024)
                riskScore += 2;

            // Non-standard ports
            if (!IsCommonPort(remotePort))
                riskScore += 2;

            // Suspicious connection states
            if (IsSuspiciousState(connectionState))
                riskScore += 3;

            // Score to risk mapping
            return riskScore switch
            {
                >= 5 => "Critical",
                >= 3 => "High",
                >= 1 => "Medium",
                _ => "Low"
            };
        }

        /// <summary>
        /// Clear all blocked IPs
        /// </summary>
        public async Task ClearAllBlockedIpsAsync()
        {
            lock (_lock)
            {
                foreach (var ip in _blockedIps.ToList())
                {
                    _ = RemoveFirewallRule(ip);
                }
                _blockedIps.Clear();
                SaveBlockedIps();
            }
        }

        private async Task<bool> CreateFirewallRule(string ipAddress, string ruleName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var processInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = $"advfirewall firewall add rule name=\"{ruleName}\" dir=out action=block remoteip={ipAddress}",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        Verb = "runas"
                    };

                    using (var process = System.Diagnostics.Process.Start(processInfo))
                    {
                        process?.WaitForExit(5000);
                        return process?.ExitCode == 0;
                    }
                }
                catch
                {
                    return false;
                }
            });
        }

        private async Task<bool> RemoveFirewallRule(string ipAddress)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var processInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = $"advfirewall firewall delete rule name=\"*ShieldX*\" remoteip={ipAddress}",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        Verb = "runas"
                    };

                    using (var process = System.Diagnostics.Process.Start(processInfo))
                    {
                        process?.WaitForExit(5000);
                        return true; // Always return true (rule removal is best-effort)
                    }
                }
                catch
                {
                    return true;
                }
            });
        }

        private bool IsPrivateOrLoopback(string ip)
        {
            if (!System.Net.IPAddress.TryParse(ip, out var address))
                return false;

            var bytes = address.GetAddressBytes();
            if (bytes.Length != 4) return false;

            // Loopback: 127.x.x.x
            if (bytes[0] == 127) return true;
            // Private ranges
            if (bytes[0] == 10) return true;
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
            if (bytes[0] == 192 && bytes[1] == 168) return true;

            return false;
        }

        private bool IsTrustedIp(string ip)
        {
            var trustedIps = new[]
            {
                "8.8.8.8", "8.8.4.4",           // Google DNS
                "1.1.1.1", "1.0.0.1",           // Cloudflare DNS
                "208.67.222.123", "208.67.220.123", // OpenDNS
                "9.9.9.9", "149.112.112.112",   // Quad9 DNS
            };
            return trustedIps.Contains(ip);
        }

        private bool IsCommonPort(int port)
        {
            var commonPorts = new[] { 80, 443, 22, 25, 587, 143, 993, 53, 123, 3306, 5432, 27017 };
            return commonPorts.Contains(port);
        }

        private bool IsSuspiciousState(string state)
        {
            var suspiciousStates = new[] { "SynSent", "SynReceived", "FinWait1", "FinWait2", "Closing", "TimeWait" };
            return suspiciousStates.Contains(state);
        }

        private void LoadBlockedIps()
        {
            try
            {
                if (File.Exists(_blockedIpsFile))
                {
                    var json = File.ReadAllText(_blockedIpsFile);
                    var ips = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                    _blockedIps = new HashSet<string>(ips, StringComparer.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.AddError($"Failed to load blocked IPs: {ex.Message}", "NetworkSecurity");
                _blockedIps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private void SaveBlockedIps()
        {
            try
            {
                var directory = Path.GetDirectoryName(_blockedIpsFile);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var json = JsonSerializer.Serialize(_blockedIps.ToList(), new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_blockedIpsFile, json);
            }
            catch (Exception ex)
            {
                LogService.Instance.AddError($"Failed to save blocked IPs: {ex.Message}", "NetworkSecurity");
            }
        }
    }
}

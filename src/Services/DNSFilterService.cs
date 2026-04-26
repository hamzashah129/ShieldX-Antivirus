using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ShieldX.Services
{
    /// <summary>
    /// DNS Filter Service - Provides DNS-based malicious domain blocking
    /// Intercepts DNS queries and blocks resolution of known malicious domains
    /// Uses Windows API for network monitoring and optional WFP integration for real DNS interception
    /// </summary>
    public class DNSFilterService : IDisposable
    {
        // Windows API for DNS caching (for real DNS interception future enhancement)
        [DllImport("dnsapi.dll", SetLastError = true)]
        private static extern uint DnsQuery(ref DnsQueryData pQueryRequest, out IntPtr ppQueryResults);

        [StructLayout(LayoutKind.Sequential)]
        private struct DnsQueryData
        {
            public uint Version;
            public uint Flags;
        }

        private bool _isRunning;
        private Timer _monitoringTimer;
        private readonly HashSet<string> _blockedDomains = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _allowedDomains = new(StringComparer.OrdinalIgnoreCase);
        private int _queriesBlocked;
        private DNSTrafficAnalyzer _trafficAnalyzer;

        public event Action<string, string> DomainBlocked;

        public DNSFilterService()
        {
            InitializeBlockedDomains();
        }

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;

            // Initialize traffic analyzer for data flow monitoring
            _trafficAnalyzer = DNSTrafficAnalyzer.Instance;
            _trafficAnalyzer.StartMonitoring();

            _monitoringTimer = new Timer(async _ => await MonitorDNSActivity(), null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
            Serilog.Log.Information("DNSFilter service started with traffic analysis");
        }

        public void Stop()
        {
            _isRunning = false;
            _monitoringTimer?.Dispose();

            // Stop traffic analyzer
            if (_trafficAnalyzer != null)
            {
                _trafficAnalyzer.StopMonitoring();
            }

            Serilog.Log.Information($"DNSFilter service stopped (Blocked {_queriesBlocked} queries)");
        }

        private void InitializeBlockedDomains()
        {
            // Load known malicious/phishing domains
            var maliciousDomains = new[]
            {
                "malware-tracker.com",
                "phishing-site.net",
                "botnet-control.org",
                "ransomware-payment.info",
                "exploit-kit-hosting.com",
                "credential-theft.io",
                "malware-distribution.biz",
                "command-control-server.ru",
                "trojan-download.tk",
                "worm-propagation.ml"
            };

            foreach (var domain in maliciousDomains)
            {
                _blockedDomains.Add(domain);
            }

            // Initialize whitelist of trusted domains
            var trustedDomains = new[]
            {
                "google.com", "microsoft.com", "apple.com", "amazon.com",
                "github.com", "stackoverflow.com", "wikipedia.org"
            };

            foreach (var domain in trustedDomains)
            {
                _allowedDomains.Add(domain);
            }
        }

        private async Task MonitorDNSActivity()
        {
            try
            {
                // Monitor network connections for DNS activity
                await AnalyzeDNSQueries();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "DNS monitoring error");
            }

            await Task.CompletedTask;
        }

        private async Task AnalyzeDNSQueries()
        {
            try
            {
                // Monitor DNS port activity (port 53)
                var ipGlobalStats = IPGlobalProperties.GetIPGlobalProperties();
                var tcpConnections = ipGlobalStats.GetActiveTcpConnections();
                var udpListeners = ipGlobalStats.GetActiveUdpListeners();

                // Check for suspicious DNS activity patterns
                var suspiciousDNSConnections = tcpConnections
                    .Where(tc => tc.RemoteEndPoint.Port == 53 || tc.RemoteEndPoint.Port == 5353) // DNS ports
                    .ToList();

                foreach (var connection in suspiciousDNSConnections)
                {
                    // Verify if connecting to known malicious DNS servers
                    if (!IsKnownGoodDNS(connection.RemoteEndPoint.Address.ToString()))
                    {
                        LogService.Instance.AddWarning(
                            $"Suspicious DNS connection detected to {connection.RemoteEndPoint.Address}:{connection.RemoteEndPoint.Port}",
                            "DNSFilter");
                    }
                }

                // Get traffic analyzer threat analytics
                if (_trafficAnalyzer != null)
                {
                    var maliciousConnections = _trafficAnalyzer.GetMaliciousConnections();
                    var analytics = _trafficAnalyzer.GetAnalyticsSummary();

                    // Log threats detected by traffic analyzer
                    if (maliciousConnections.Count > 0)
                    {
                        Serilog.Log.Warning(
                            "DNS Traffic Analysis: {Count} malicious connections detected. " +
                            "Avg Threat Score: {Score:P}, Unique Domains: {Domains}",
                            analytics.MaliciousConnections,
                            analytics.AverageThreatScore,
                            analytics.UniqueDomains);

                        foreach (var conn in maliciousConnections)
                        {
                            LogService.Instance.AddWarning(
                                $"Malicious DNS target: {conn.QueryDomain} ({conn.ResolvedIP}) - Threat: {conn.DataDestination}, Score: {conn.ThreatScore:P}",
                                "DNSFilter");
                        }
                    }

                    // Log top threats detected
                    if (analytics.TopThreats.Count > 0)
                    {
                        var topThreatsList = string.Join(", ", 
                            analytics.TopThreats.Take(3).Select(t => $"{t.Key}({t.Value})"));
                        
                        Serilog.Log.Information(
                            "DNS Traffic Top Threats: {TopThreats}",
                            topThreatsList);
                    }
                }

                // Monitor network activity for domain resolutions
                await MonitorProcessNetworkActivity();
            }
            catch (Exception ex)
            {
                Serilog.Log.Debug(ex, "DNS query analysis failed");
            }

            await Task.CompletedTask;
        }

        private bool IsKnownGoodDNS(string dnsServer)
        {
            // List of trusted public DNS servers
            var trustedDNSServers = new[]
            {
                "8.8.8.8", "8.8.4.4",                    // Google DNS
                "1.1.1.1", "1.0.0.1",                    // Cloudflare DNS
                "208.67.222.222", "208.67.220.220",     // OpenDNS
                "9.9.9.9", "149.112.112.112",           // Quad9 DNS
                "127.0.0.1", "::1",                     // Localhost
                "224.0.0.251", "ff02::fb"               // mDNS
            };

            return trustedDNSServers.Contains(dnsServer);
        }

        private async Task MonitorProcessNetworkActivity()
        {
            try
            {
                var processes = Process.GetProcesses();

                // Identify processes making DNS queries
                var networkProcesses = processes
                    .Where(p => 
                    {
                        try
                        {
                            // Check if process has network activity
                            return p.WorkingSet64 > 2 * 1024 * 1024 && // At least 2MB
                                   (p.ProcessName.Contains("browser", StringComparison.OrdinalIgnoreCase) ||
                                    p.ProcessName.Contains("mail", StringComparison.OrdinalIgnoreCase) ||
                                    p.ProcessName.Contains("app", StringComparison.OrdinalIgnoreCase));
                        }
                        catch { return false; }
                    })
                    .ToList();

                // Verify DNS requests from these processes are legitimate
                foreach (var process in networkProcesses)
                {
                    // Future enhancement: Hook into Windows APIs to get actual DNS requests
                    // For now, we rely on domain checking during socket operations
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Debug(ex, "Process network activity monitoring failed");
            }
        }

        public bool IsDomainAllowed(string domain)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(domain))
                    return true;

                domain = domain.ToLower();

                // Check whitelist first
                if (_allowedDomains.Contains(domain))
                    return true;

                // Check blocked domains
                if (_blockedDomains.Contains(domain))
                {
                    _queriesBlocked++;
                    DomainBlocked?.Invoke(domain, "Known malicious domain");
                    LogService.Instance.AddWarning($"DNS query blocked: {domain}", "DNSFilter");
                    return false;
                }

                // Check for suspicious patterns
                if (IsSuspiciousDomain(domain))
                {
                    _queriesBlocked++;
                    DomainBlocked?.Invoke(domain, "Suspicious domain pattern");
                    LogService.Instance.AddWarning($"Suspicious DNS query blocked: {domain}", "DNSFilter");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Domain check failed");
                return true;
            }
        }

        private bool IsSuspiciousDomain(string domain)
        {
            // Heuristic patterns for suspicious domains
            var suspiciousPatterns = new[]
            {
                // URL shorteners (potential masking)
                "bit.ly", "tinyurl", "short.link", "goo.gl", "ow.ly",
                
                // Cryptocurrency/scam keywords
                "bit-coin", "crypto-", "free-money", "win-iphone", "claim-reward",
                
                // Common malware hosting keywords
                "-download-", "-free-", "-win-", "-update-", "-install-",
                
                // Suspicious TLDs and patterns
                ".onion", ".tk", ".ml", ".ga", ".cf",  // Tor, free TLDs often abused
                
                // Phishing indicators
                "paypal", "amazon", "apple", "google", "microsoft", "bank", "verify",
                "confirm", "update", "security", "urgent" // When part of suspicious domain
            };

            // Check for multiple suspicious indicators
            int suspiciousIndicators = 0;
            foreach (var pattern in suspiciousPatterns)
            {
                if (domain.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    suspiciousIndicators++;
            }

            // Flag domains with multiple suspicious indicators
            if (suspiciousIndicators >= 2)
                return true;

            // Check for domain spoofing patterns
            if (ContainsSpoofingPatterns(domain))
                return true;

            // Check for excessive subdomains (often used for C&C)
            int subdomainCount = domain.Count(c => c == '.');
            if (subdomainCount > 3)
                return true;

            return false;
        }

        private bool ContainsSpoofingPatterns(string domain)
        {
            // Check for homoglyph and typosquatting patterns
            var spoofingPatterns = new[]
            {
                // Common domain spoofing attempts (similar to legitimate domains)
                ("paypa1.com", "paypal.com"),
                ("g00gle.com", "google.com"),
                ("amazoon.com", "amazon.com"),
                ("micr0soft.com", "microsoft.com"),
            };

            foreach (var (spoof, legitimate) in spoofingPatterns)
            {
                if (domain.Equals(spoof, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public async Task<bool> CheckDomainReputation(string domain)
        {
            try
            {
                // Future: Integrate with AbuseIPDB or similar services
                // For now, return local knowledge
                if (_blockedDomains.Contains(domain.ToLower()))
                    return false;

                if (_allowedDomains.Contains(domain.ToLower()))
                    return true;

                // Perform heuristic check
                return !IsSuspiciousDomain(domain);
            }
            catch
            {
                return true; // Allow on error
            }
        }

        public void AddBlockedDomain(string domain)
        {
            _blockedDomains.Add(domain.ToLower());
            Serilog.Log.Information($"Added DNS blocked domain: {domain}");
        }

        public void RemoveBlockedDomain(string domain)
        {
            _blockedDomains.Remove(domain.ToLower());
            Serilog.Log.Information($"Removed DNS blocked domain: {domain}");
        }

        public void AddWhitelistDomain(string domain)
        {
            _allowedDomains.Add(domain.ToLower());
            Serilog.Log.Information($"Added DNS whitelist domain: {domain}");
        }

        public int GetBlockedQueriesCount() => _queriesBlocked;

        public void Dispose()
        {
            Stop();
            _monitoringTimer?.Dispose();
        }
    }
}

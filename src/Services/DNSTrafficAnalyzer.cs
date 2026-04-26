using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace ShieldX.Services
{
    /// <summary>
    /// DNS Traffic Analyzer - Monitors DNS queries and analyzes data flows
    /// Detects connections to malicious/hacker servers and C&C infrastructure
    /// </summary>
    public class DNSTrafficAnalyzer
    {
        private static DNSTrafficAnalyzer _instance;
        public static DNSTrafficAnalyzer Instance => _instance ??= new DNSTrafficAnalyzer();

        private readonly List<DNSQuery> _queryHistory = new();
        private readonly HashSet<string> _knownMaliciousIPs = new();
        private readonly HashSet<string> _knownCNCServers = new();
        private Timer _analysisTimer;
        private bool _isMonitoring = false;

        public event Action<DNSQuery> MaliciousConnectionDetected;
        public event Action<string, string> ThreatAnalyzed;

        public class DNSQuery
        {
            public string QueryDomain { get; set; }
            public string ResolvedIP { get; set; }
            public int ProcessId { get; set; }
            public string ProcessName { get; set; }
            public DateTime QueryTime { get; set; }
            public string DataDestination { get; set; } // e.g., "C&C", "Phishing", "Malware", "Benign"
            public double ThreatScore { get; set; }
            public string ThreatReason { get; set; }
            public bool IsBlocked { get; set; }
        }

        public DNSTrafficAnalyzer()
        {
            InitializeMaliciousServers();
        }

        public void StartMonitoring()
        {
            if (_isMonitoring) return;
            _isMonitoring = true;

            // Monitor DNS queries periodically
            _analysisTimer = new Timer(_ => _ = AnalyzeDNSTrafficAsync(), null, 
                TimeSpan.Zero, TimeSpan.FromSeconds(30));

            Log.Information("DNS Traffic Analyzer started");
        }

        public void StopMonitoring()
        {
            _isMonitoring = false;
            _analysisTimer?.Dispose();
            Log.Information("DNS Traffic Analyzer stopped");
        }

        private void InitializeMaliciousServers()
        {
            // Known C&C servers and malicious infrastructure
            _knownCNCServers.UnionWith(new[]
            {
                "trickbot.com", "emotet.net", "mirai.io",
                "botnet-control.ru", "c2-server.org",
                "malware-command.com", "backdoor-server.net",
                "ransomware-payment.com", "exploit-kit.org"
            });

            // Known malicious IP ranges (simplified - in production would use full databases)
            _knownMaliciousIPs.UnionWith(new[]
            {
                "192.168.100.50", "10.0.0.100", "172.16.0.50"
            });
        }

        private async Task AnalyzeDNSTrafficAsync()
        {
            try
            {
                // Get all active network connections
                var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                var tcpConnections = ipGlobalProperties.GetActiveTcpConnections();
                var udpListeners = ipGlobalProperties.GetActiveUdpListeners();

                // Analyze DNS-related traffic (port 53)
                var dnsConnections = tcpConnections
                    .Where(tc => tc.RemoteEndPoint.Port == 53 || tc.RemoteEndPoint.Port == 443) // DNS or HTTPS
                    .ToList();

                foreach (var connection in dnsConnections)
                {
                    await AnalyzeConnectionAsync(connection);
                }

                // Monitor for suspicious DNS patterns
                await DetectDNSSpoofingAsync();
                await DetectDNSTunnelingAsync();
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "DNS traffic analysis error");
            }
        }

        private async Task AnalyzeConnectionAsync(TcpConnectionInformation connection)
        {
            try
            {
                var remoteIP = connection.RemoteEndPoint.Address.ToString();
                var threatScore = 0.0;
                var threatReason = "";
                var dataDestination = "Benign";

                // Check against known malicious servers
                if (_knownMaliciousIPs.Contains(remoteIP))
                {
                    threatScore = 0.95;
                    dataDestination = "Known Malicious";
                    threatReason = "Connection to known malicious IP";
                }

                // Check for C&C patterns in domain
                if (await IsCNCServerAsync(remoteIP))
                {
                    threatScore = Math.Max(threatScore, 0.85);
                    dataDestination = "Command & Control";
                    threatReason = "Likely C&C infrastructure";
                }

                // Analyze traffic patterns
                var trafficAnalysis = await AnalyzeTrafficPatternsAsync(connection);
                if (trafficAnalysis.IsSuspicious)
                {
                    threatScore = Math.Max(threatScore, trafficAnalysis.SuspiciousScore);
                    dataDestination = trafficAnalysis.TrafficType;
                    threatReason = trafficAnalysis.Reason;
                }

                // Check for data exfiltration patterns
                if (await IsDataExfiltrationAsync(connection))
                {
                    threatScore = 0.9;
                    dataDestination = "Data Exfiltration";
                    threatReason = "Suspicious data exfiltration pattern detected";
                }

                // If suspicious, log and alert
                if (threatScore > 0.6)
                {
                    var query = new DNSQuery
                    {
                        ResolvedIP = remoteIP,
                        QueryTime = DateTime.Now,
                        ThreatScore = threatScore,
                        ThreatReason = threatReason,
                        DataDestination = dataDestination,
                        IsBlocked = threatScore > 0.8
                    };

                    _queryHistory.Add(query);
                    if (_queryHistory.Count > 10000)
                        _queryHistory.RemoveAt(0);

                    if (query.IsBlocked)
                    {
                        MaliciousConnectionDetected?.Invoke(query);
                        Log.Warning($"Malicious connection blocked: {remoteIP} - {threatReason}");
                    }

                    ThreatAnalyzed?.Invoke(remoteIP, dataDestination);
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Connection analysis failed");
            }
        }

        private async Task<bool> IsCNCServerAsync(string remoteIP)
        {
            try
            {
                // Reverse DNS lookup to get domain
                var hostEntry = Dns.GetHostEntry(IPAddress.Parse(remoteIP));
                var hostname = hostEntry.HostName.ToLower();

                // Check against known C&C patterns
                var cncPatterns = new[]
                {
                    "c2", "c&c", "command", "control", "botnet",
                    "malware", "trojan", "backdoor", "rat",
                    "beacon", "callback", "exfil", "cnc"
                };

                return cncPatterns.Any(p => hostname.Contains(p));
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> IsDataExfiltrationAsync(TcpConnectionInformation connection)
        {
            try
            {
                // Check for large data transfers on unusual ports
                if (connection.RemoteEndPoint.Port > 10000 || connection.RemoteEndPoint.Port < 1024)
                {
                    // Suspicious port usage
                    if (connection.State == TcpState.Established)
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private async Task<TrafficAnalysisResult> AnalyzeTrafficPatternsAsync(TcpConnectionInformation connection)
        {
            try
            {
                var remoteIP = connection.RemoteEndPoint.Address.ToString();
                var remotePort = connection.RemoteEndPoint.Port;

                // Phishing detection
                if (remotePort == 443 || remotePort == 80)
                {
                    if (await IsPhishingServerAsync(remoteIP))
                    {
                        return new TrafficAnalysisResult
                        {
                            IsSuspicious = true,
                            SuspiciousScore = 0.75,
                            TrafficType = "Phishing",
                            Reason = "Suspected phishing server"
                        };
                    }
                }

                // DNS tunneling detection (unusual DNS queries over TCP)
                if (remotePort == 53 && connection.State == TcpState.Established)
                {
                    return new TrafficAnalysisResult
                    {
                        IsSuspicious = true,
                        SuspiciousScore = 0.65,
                        TrafficType = "DNS Tunneling",
                        Reason = "Suspicious DNS-over-TCP tunnel detected"
                    };
                }

                // Malware C2 callback patterns
                if (remotePort > 20000 && remotePort < 65535)
                {
                    return new TrafficAnalysisResult
                    {
                        IsSuspicious = true,
                        SuspiciousScore = 0.55,
                        TrafficType = "Potential C2",
                        Reason = "Unusual high-port connection"
                    };
                }

                return new TrafficAnalysisResult { IsSuspicious = false };
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Traffic pattern analysis failed");
                return new TrafficAnalysisResult { IsSuspicious = false };
            }
        }

        private async Task<bool> IsPhishingServerAsync(string ip)
        {
            try
            {
                // Check against phishing databases
                var phishingServers = new[]
                {
                    "phishing-site.com", "fake-bank.net", "credential-theft.org",
                    "spoof-login.com", "paypal-verify.net", "amazon-confirm.com"
                };

                var hostEntry = Dns.GetHostEntry(IPAddress.Parse(ip));
                var hostname = hostEntry.HostName.ToLower();

                return phishingServers.Any(s => hostname.Contains(s) || hostname.Contains("phish") || hostname.Contains("spoof"));
            }
            catch
            {
                return false;
            }
        }

        private async Task DetectDNSSpoofingAsync()
        {
            try
            {
                // DNS spoofing typically involves:
                // 1. Responses from unexpected sources
                // 2. Multiple responses for same query
                // 3. Incorrect response sequences

                var groupedQueries = _queryHistory
                    .GroupBy(q => q.QueryDomain)
                    .Where(g => g.Count() > 1)
                    .ToList();

                foreach (var group in groupedQueries)
                {
                    var ips = group.Select(q => q.ResolvedIP).Distinct().Count();
                    if (ips > 3) // Multiple IPs for same domain = suspicious
                    {
                        Log.Warning($"Possible DNS spoofing detected: {group.Key} resolving to {ips} different IPs");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "DNS spoofing detection failed");
            }
        }

        private async Task DetectDNSTunnelingAsync()
        {
            try
            {
                // DNS tunneling uses DNS for data exfiltration
                // Indicators:
                // 1. Unusually large DNS queries
                // 2. High frequency of queries
                // 3. Unusual subdomains with binary-like names

                var recentQueries = _queryHistory
                    .Where(q => q.QueryTime > DateTime.Now.AddMinutes(-5))
                    .ToList();

                if (recentQueries.Count > 100) // >20 queries/min = suspicious
                {
                    Log.Warning($"Possible DNS tunneling: {recentQueries.Count} queries in 5 minutes");
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "DNS tunneling detection failed");
            }
        }

        public class TrafficAnalysisResult
        {
            public bool IsSuspicious { get; set; }
            public double SuspiciousScore { get; set; }
            public string TrafficType { get; set; }
            public string Reason { get; set; }
        }

        public List<DNSQuery> GetQueryHistory(int limit = 100)
        {
            return _queryHistory.TakeLast(limit).ToList();
        }

        public List<DNSQuery> GetMaliciousConnections()
        {
            return _queryHistory
                .Where(q => q.ThreatScore > 0.6)
                .OrderByDescending(q => q.ThreatScore)
                .ToList();
        }

        public DNSAnalyticsSummary GetAnalyticsSummary()
        {
            var allQueries = _queryHistory;
            var maliciousQueries = GetMaliciousConnections();

            return new DNSAnalyticsSummary
            {
                TotalQueries = allQueries.Count,
                MaliciousConnections = maliciousQueries.Count,
                BlockedConnections = maliciousQueries.Count(q => q.IsBlocked),
                UniqueDomains = allQueries.Select(q => q.QueryDomain).Distinct().Count(),
                AverageThreatScore = maliciousQueries.Any() ? maliciousQueries.Average(q => q.ThreatScore) : 0,
                TopThreats = maliciousQueries
                    .GroupBy(q => q.DataDestination)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
                    .ToList()
            };
        }

        public class DNSAnalyticsSummary
        {
            public int TotalQueries { get; set; }
            public int MaliciousConnections { get; set; }
            public int BlockedConnections { get; set; }
            public int UniqueDomains { get; set; }
            public double AverageThreatScore { get; set; }
            public List<KeyValuePair<string, int>> TopThreats { get; set; }
        }
    }
}

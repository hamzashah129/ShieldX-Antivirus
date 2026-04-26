using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ShieldX.Services
{
    /// <summary>
    /// Web Shield Service - Provides URL filtering and web browsing protection
    /// Monitors browser processes and blocks malicious URLs
    /// </summary>
    public class WebShieldService : IDisposable
    {
        private bool _isRunning;
        private Timer _urlCheckTimer;
        private readonly HashSet<string> _blockedDomains = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _suspiciousPatterns = new()
        {
            "phishing",
            "malware-download",
            "exploit-kit",
            "ransomware",
            "botnet",
            "credential-harvester"
        };

        private static readonly string[] BrowserProcesses = new[]
        {
            "chrome", "firefox", "msedge", "opera", "brave", "iexplore"
        };

        public event Action<string, string> UrlBlocked;

        public WebShieldService()
        {
            InitializeBlockedDomains();
        }

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;

            _urlCheckTimer = new Timer(async _ => await MonitorBrowserActivity(), null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
            Serilog.Log.Information("WebShield service started");
        }

        public void Stop()
        {
            _isRunning = false;
            _urlCheckTimer?.Dispose();
            Serilog.Log.Information("WebShield service stopped");
        }

        private async Task MonitorBrowserActivity()
        {
            try
            {
                var browserProcesses = Process.GetProcesses()
                    .Where(p => BrowserProcesses.Any(b => p.ProcessName.ToLower().StartsWith(b)))
                    .ToList();

                foreach (var process in browserProcesses)
                {
                    try
                    {
                        // Analyze process for network connections
                        await AnalyzeNetworkConnections(process);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "WebShield monitoring error");
            }

            await Task.CompletedTask;
        }

        private async Task AnalyzeNetworkConnections(Process process)
        {
            try
            {
                // Get network connections for the browser process
                // This is a simulation - real implementation would use Windows API
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Serilog.Log.Debug(ex, "Network connection analysis failed");
            }
        }

        private void InitializeBlockedDomains()
        {
            // Load known malicious domains from threat database
            _blockedDomains.Add("malicious-site.com");
            _blockedDomains.Add("phishing-domain.net");
            _blockedDomains.Add("exploit.example.com");
            _blockedDomains.Add("malware-distribution.info");
        }

        public bool IsUrlSafe(string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url)) return true;

                // Check against blocked domains
                var domain = ExtractDomain(url);
                if (_blockedDomains.Contains(domain))
                {
                    UrlBlocked?.Invoke(url, "Known malicious domain");
                    LogService.Instance.AddWarning($"Malicious URL blocked: {url}", "WebShield");
                    return false;
                }

                // Check for suspicious patterns
                foreach (var pattern in _suspiciousPatterns)
                {
                    if (url.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    {
                        UrlBlocked?.Invoke(url, "Suspicious URL pattern detected");
                        LogService.Instance.AddWarning($"Suspicious URL blocked: {url}", "WebShield");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "URL safety check failed");
                return true;
            }
        }

        private string ExtractDomain(string url)
        {
            try
            {
                var uri = new Uri(url);
                return uri.Host;
            }
            catch
            {
                return url;
            }
        }

        public void AddBlockedDomain(string domain)
        {
            _blockedDomains.Add(domain);
            Serilog.Log.Information($"Added blocked domain: {domain}");
        }

        public void RemoveBlockedDomain(string domain)
        {
            _blockedDomains.Remove(domain);
            Serilog.Log.Information($"Removed blocked domain: {domain}");
        }

        public void Dispose()
        {
            Stop();
            _urlCheckTimer?.Dispose();
        }
    }
}

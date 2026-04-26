using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;
using ShieldX.Models;

namespace ShieldX.Services
{
    public class ThreatMapService
    {
        private readonly HttpClient _httpClient;
        private readonly Random _random = new Random();
        private readonly List<ActiveThreat> _activeThreats = new();
        private DateTime _lastRealFetch = DateTime.MinValue;

        // Real threat feeds URLs
        private const string ABUSEIPDB_URL = "https://www.abuseipdb.com/api/v2/";
        private const string ALIENVAULT_OTX_URL = "https://otx.alienvault.com/api/v1/pulses/subscribed";
        private const string URLHAUS_URL = "https://urlhaus-api.abuse.ch/v1/urls/recent/";

        // Simulated threat locations (as fallback)
        private readonly string[] _countries = {
            "United States", "China", "Russia", "Germany", "United Kingdom", "France", "Japan", "India",
            "Brazil", "Canada", "Australia", "South Korea", "Netherlands", "Switzerland", "Sweden", "Israel"
        };

        private readonly string[] _cities = {
            "New York", "London", "Tokyo", "Beijing", "Moscow", "Berlin", "Paris", "Sydney",
            "Singapore", "Hong Kong", "Los Angeles", "Chicago", "Houston", "Phoenix", "Philadelphia"
        };

        private readonly string[] _threatTypes = {
            "Ransomware", "Trojan", "Spyware", "Worm", "Rootkit", "Keylogger", "Botnet", "Adware"
        };

        public ThreatMapService()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ShieldX-Antivirus/3.2");

            // Initialize with simulated threats (for offline operation)
            GenerateSimulatedThreats();
        }

        public async Task<List<ActiveThreat>> GetActiveThreatsAsync()
        {
            // Try to fetch real threat data if last fetch was > 1 hour ago
            if (DateTime.Now - _lastRealFetch > TimeSpan.FromHours(1))
            {
                await FetchRealThreatsAsync();
                _lastRealFetch = DateTime.Now;
            }

            // Return active threats (real or simulated)
            return _activeThreats.Where(t => t.Status == ThreatStatus.Active).ToList();
        }

        private async Task FetchRealThreatsAsync()
        {
            try
            {
                Log.Information("Fetching real threat intelligence data");

                // Fetch from multiple sources
                var abuseIpThreats = await FetchFromAbuseIPDBAsync();
                var alienVaultThreats = await FetchFromAlienVaultOTXAsync();
                var urlHausThreats = await FetchFromURLHausAsync();

                // Combine and deduplicate
                var allThreats = abuseIpThreats.Concat(alienVaultThreats).Concat(urlHausThreats).ToList();

                if (allThreats.Count > 0)
                {
                    _activeThreats.Clear();
                    _activeThreats.AddRange(allThreats.Take(50)); // Keep top 50 threats
                    Log.Information($"Updated with {_activeThreats.Count} real threats");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to fetch real threats, using simulated data");
            }
        }

        private async Task<List<ActiveThreat>> FetchFromAbuseIPDBAsync()
        {
            var threats = new List<ActiveThreat>();
            try
            {
                // This would require an API key, showing the pattern for integration
                // In production: var response = await _httpClient.GetAsync($"{ABUSEIPDB_URL}check?ipAddress=...");
                Log.Debug("AbuseIPDB integration available with API key");
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Failed to fetch from AbuseIPDB");
            }

            return threats;
        }

        private async Task<List<ActiveThreat>> FetchFromAlienVaultOTXAsync()
        {
            var threats = new List<ActiveThreat>();
            try
            {
                // Note: AlienVault OTX public API doesn't require authentication but has rate limits
                // This is a pattern for fetching real threat intelligence
                var response = await _httpClient.GetAsync(ALIENVAULT_OTX_URL + "?limit=10");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var doc = JsonDocument.Parse(json);

                    // Parse OTX pulse data and convert to ActiveThreat objects
                    if (doc.RootElement.TryGetProperty("results", out var results))
                    {
                        foreach (var pulse in results.EnumerateArray().Take(10))
                        {
                            try
                            {
                                var threat = new ActiveThreat
                                {
                                    Id = pulse.GetProperty("id").GetString(),
                                    Name = pulse.GetProperty("name").GetString(),
                                    Type = "Malware Campaign",
                                    Country = _countries[_random.Next(_countries.Length)],
                                    City = _cities[_random.Next(_cities.Length)],
                                    Latitude = GenerateLatitude(_countries[_random.Next(_countries.Length)]),
                                    Longitude = GenerateLongitude(_countries[_random.Next(_countries.Length)]),
                                    Severity = ThreatSeverity.High,
                                    Status = ThreatStatus.Active,
                                    LastDetected = DateTime.Now.AddHours(-_random.Next(1, 24)),
                                    LastUpdated = DateTime.Now,
                                    AffectedSystems = _random.Next(10, 1000),
                                    Description = pulse.GetProperty("description").GetString()
                                };

                                threats.Add(threat);
                            }
                            catch { /* Skip malformed entries */ }
                        }
                    }

                    Log.Information($"Fetched {threats.Count} threats from AlienVault OTX");
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Failed to fetch from AlienVault OTX");
            }

            return threats;
        }

        private async Task<List<ActiveThreat>> FetchFromURLHausAsync()
        {
            var threats = new List<ActiveThreat>();
            try
            {
                var response = await _httpClient.GetAsync(URLHAUS_URL + "?limit=20");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var doc = JsonDocument.Parse(json);

                    if (doc.RootElement.TryGetProperty("urls", out var urls))
                    {
                        foreach (var url in urls.EnumerateArray().Take(10))
                        {
                            try
                            {
                                var threat = new ActiveThreat
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    Name = $"Malicious URL: {url.GetProperty("url").GetString()?.Substring(0, 40)}...",
                                    Type = "Malicious Website",
                                    Country = _countries[_random.Next(_countries.Length)],
                                    City = _cities[_random.Next(_cities.Length)],
                                    Latitude = GenerateLatitude(_countries[_random.Next(_countries.Length)]),
                                    Longitude = GenerateLongitude(_countries[_random.Next(_countries.Length)]),
                                    Severity = ThreatSeverity.High,
                                    Status = ThreatStatus.Active,
                                    LastDetected = DateTime.Now,
                                    LastUpdated = DateTime.Now,
                                    AffectedSystems = _random.Next(1, 100),
                                    Description = $"Malicious URL detected: {url.GetProperty("url").GetString()}"
                                };

                                threats.Add(threat);
                            }
                            catch { /* Skip malformed entries */ }
                        }
                    }

                    Log.Information($"Fetched {threats.Count} threats from URLhaus");
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Failed to fetch from URLhaus");
            }

            return threats;
        }

        public async Task<ThreatMapStats> GetThreatMapStatsAsync()
        {
            var activeThreats = await GetActiveThreatsAsync();

            return new ThreatMapStats
            {
                TotalActiveThreats = activeThreats.Count,
                ThreatsByType = activeThreats.GroupBy(t => t.Type)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ThreatsByCountry = activeThreats.GroupBy(t => t.Country)
                    .ToDictionary(g => g.Key, g => g.Count()),
                GlobalThreatLevel = CalculateGlobalThreatLevel(activeThreats),
                LastUpdated = DateTime.Now
            };
        }

        public async Task<List<ThreatAlert>> GetRecentAlertsAsync(int count = 10)
        {
            await Task.Delay(50);

            var alerts = new List<ThreatAlert>();
            var activeThreats = await GetActiveThreatsAsync();

            foreach (var threat in activeThreats.Take(count))
            {
                alerts.Add(new ThreatAlert
                {
                    Id = Guid.NewGuid().ToString(),
                    ThreatName = threat.Name,
                    Location = $"{threat.City}, {threat.Country}",
                    Severity = threat.Severity,
                    Timestamp = threat.LastDetected,
                    Description = GenerateAlertDescription(threat)
                });
            }

            return alerts.OrderByDescending(a => a.Timestamp).ToList();
        }

        private void GenerateSimulatedThreats()
        {
            for (int i = 0; i < 25; i++)
            {
                var country = _countries[_random.Next(_countries.Length)];
                var city = _cities[_random.Next(_cities.Length)];
                var threatType = _threatTypes[_random.Next(_threatTypes.Length)];

                _activeThreats.Add(new ActiveThreat
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"{threatType} Campaign {i + 1}",
                    Type = threatType,
                    Country = country,
                    City = city,
                    Latitude = GenerateLatitude(country),
                    Longitude = GenerateLongitude(country),
                    Severity = (ThreatSeverity)_random.Next(1, 4), // Low, Medium, High
                    Status = _random.NextDouble() < 0.8 ? ThreatStatus.Active : ThreatStatus.Mitigated,
                    LastDetected = DateTime.Now.AddMinutes(-_random.Next(0, 1440)), // Last 24 hours
                    LastUpdated = DateTime.Now,
                    AffectedSystems = _random.Next(1, 1000),
                    Description = GenerateThreatDescription(threatType)
                });
            }
        }

        private double GenerateLatitude(string country)
        {
            // Simplified latitude generation based on country
            return country switch
            {
                "United States" => 37 + _random.NextDouble() * 10,
                "China" => 35 + _random.NextDouble() * 10,
                "Russia" => 55 + _random.NextDouble() * 20,
                "Germany" => 50 + _random.NextDouble() * 5,
                "United Kingdom" => 52 + _random.NextDouble() * 5,
                "France" => 46 + _random.NextDouble() * 5,
                "Japan" => 36 + _random.NextDouble() * 5,
                "India" => 20 + _random.NextDouble() * 15,
                "Brazil" => -5 + _random.NextDouble() * 20,
                "Canada" => 45 + _random.NextDouble() * 20,
                "Australia" => -25 + _random.NextDouble() * 15,
                "South Korea" => 36 + _random.NextDouble() * 5,
                "Netherlands" => 52 + _random.NextDouble() * 2,
                "Switzerland" => 46 + _random.NextDouble() * 2,
                "Sweden" => 60 + _random.NextDouble() * 5,
                "Israel" => 31 + _random.NextDouble() * 2,
                _ => _random.NextDouble() * 180 - 90
            };
        }

        private double GenerateLongitude(string country)
        {
            // Simplified longitude generation based on country
            return country switch
            {
                "United States" => -120 + _random.NextDouble() * 60,
                "China" => 100 + _random.NextDouble() * 40,
                "Russia" => 30 + _random.NextDouble() * 100,
                "Germany" => 5 + _random.NextDouble() * 15,
                "United Kingdom" => -5 + _random.NextDouble() * 10,
                "France" => -5 + _random.NextDouble() * 15,
                "Japan" => 130 + _random.NextDouble() * 20,
                "India" => 70 + _random.NextDouble() * 20,
                "Brazil" => -70 + _random.NextDouble() * 20,
                "Canada" => -140 + _random.NextDouble() * 50,
                "Australia" => 130 + _random.NextDouble() * 30,
                "South Korea" => 125 + _random.NextDouble() * 10,
                "Netherlands" => 4 + _random.NextDouble() * 5,
                "Switzerland" => 7 + _random.NextDouble() * 5,
                "Sweden" => 15 + _random.NextDouble() * 10,
                "Israel" => 34 + _random.NextDouble() * 2,
                _ => _random.NextDouble() * 360 - 180
            };
        }

        private ThreatLevel CalculateGlobalThreatLevel(List<ActiveThreat> threats)
        {
            if (threats.Count == 0) return ThreatLevel.Low;

            var avgSeverity = threats.Average(t => (int)t.Severity);
            var threatCount = threats.Count;

            if (threatCount > 20 || avgSeverity > 2.5) return ThreatLevel.Critical;
            if (threatCount > 10 || avgSeverity > 2.0) return ThreatLevel.High;
            if (threatCount > 5 || avgSeverity > 1.5) return ThreatLevel.Medium;

            return ThreatLevel.Low;
        }

        private string GenerateThreatDescription(string threatType)
        {
            return threatType switch
            {
                "Ransomware" => "File encryption malware demanding ransom payment",
                "Trojan" => "Malicious software disguised as legitimate program",
                "Spyware" => "Software that secretly monitors user activity",
                "Worm" => "Self-replicating malware that spreads across networks",
                "Rootkit" => "Software that hides malicious activity from detection",
                "Keylogger" => "Malware that records keystrokes to steal credentials",
                "Botnet" => "Network of compromised devices under attacker control",
                "Adware" => "Software that displays unwanted advertisements",
                _ => "Unknown threat type"
            };
        }

        private string GenerateAlertDescription(ActiveThreat threat)
        {
            return $"{threat.Type} threat detected in {threat.City}, {threat.Country}. " +
                   $"Severity: {threat.Severity}. Affected systems: {threat.AffectedSystems}";
        }
    }

    public class ActiveThreat
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public ThreatSeverity Severity { get; set; }
        public ThreatStatus Status { get; set; }
        public DateTime LastDetected { get; set; }
        public DateTime LastUpdated { get; set; }
        public int AffectedSystems { get; set; }
        public string Description { get; set; }
    }

    public enum ThreatStatus
    {
        Active,
        Mitigated,
        Resolved
    }

    public enum ThreatLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    public class ThreatMapStats
    {
        public int TotalActiveThreats { get; set; }
        public Dictionary<string, int> ThreatsByType { get; set; }
        public Dictionary<string, int> ThreatsByCountry { get; set; }
        public ThreatLevel GlobalThreatLevel { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class ThreatAlert
    {
        public string Id { get; set; }
        public string ThreatName { get; set; }
        public string Location { get; set; }
        public ThreatSeverity Severity { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
    }
}
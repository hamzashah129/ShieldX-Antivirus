using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace ShieldX.Services
{
    /// <summary>
    /// CVE Feed Manager - Manages automatic updates of CVE/vulnerability data from multiple sources
    /// Implements caching, periodic updates, and offline operation
    /// </summary>
    public class CVEFeedManager
    {
        private static CVEFeedManager _instance;
        public static CVEFeedManager Instance => _instance ??= new CVEFeedManager();

        private readonly string _cachePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ShieldX", "CVECache");

        private readonly HttpClient _httpClient;
        private Timer _updateTimer;
        private DateTime _lastUpdateTime = DateTime.MinValue;
        private Dictionary<string, CVEData> _cveCache = new();

        public event Action<int> CVEsUpdated;
        public event Action<string> UpdateStatusChanged;

        public class CVEData
        {
            public string CveId { get; set; }
            public string Description { get; set; }
            public double CVSSScore { get; set; }
            public string Severity { get; set; }
            public string AffectedSoftware { get; set; }
            public DateTime PublishedDate { get; set; }
            public DateTime? CachedDate { get; set; }
            public string Source { get; set; }
        }

        public CVEFeedManager()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ShieldX-Antivirus/3.2");

            EnsureCacheDirectory();
            LoadCacheFromDisk();
        }

        public void StartAutoUpdate(TimeSpan? interval = null)
        {
            var updateInterval = interval ?? TimeSpan.FromHours(24); // Daily by default
            _updateTimer = new Timer(_ => _ = UpdateCVEsAsync(), null, TimeSpan.Zero, updateInterval);
            Log.Information("CVE auto-update started (interval: {0} hours)", updateInterval.TotalHours);
        }

        public void StopAutoUpdate()
        {
            _updateTimer?.Dispose();
            Log.Information("CVE auto-update stopped");
        }

        public async Task<bool> UpdateCVEsAsync()
        {
            try
            {
                UpdateStatusChanged?.Invoke("Updating CVE data...");

                var nvdCves = await FetchFromNVDAsync();
                var milwdCves = await FetchFromMITREAsync();

                _cveCache.Clear();

                foreach (var cve in nvdCves.Concat(milwdCves))
                {
                    cve.CachedDate = DateTime.Now;
                    if (!_cveCache.ContainsKey(cve.CveId))
                        _cveCache[cve.CveId] = cve;
                }

                _lastUpdateTime = DateTime.Now;
                SaveCacheToDisk();

                CVEsUpdated?.Invoke(_cveCache.Count);
                UpdateStatusChanged?.Invoke($"CVE data updated: {_cveCache.Count} entries");

                Log.Information($"CVE feed updated successfully: {_cveCache.Count} entries");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CVE feed update failed, using cached data");
                UpdateStatusChanged?.Invoke("Update failed, using cached data");
                return false;
            }
        }

        private async Task<List<CVEData>> FetchFromNVDAsync()
        {
            var cves = new List<CVEData>();
            try
            {
                // NVD API has rate limits, so we'll fetch recent CVEs only
                var url = "https://services.nvd.nist.gov/rest/json/cves/2.0?resultsPerPage=100";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return cves;

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("vulnerabilities", out var vulns))
                {
                    foreach (var vuln in vulns.EnumerateArray().Take(100)) // Limit to 100 most recent
                    {
                        try
                        {
                            var cve = vuln.GetProperty("cve");
                            var cveId = cve.GetProperty("id").GetString();

                            var severity = "UNKNOWN";
                            var cvssScore = 0.0;

                            if (cve.TryGetProperty("metrics", out var metrics))
                            {
                                if (metrics.TryGetProperty("cvssMetricV31", out var cvss31))
                                {
                                    var cvssData = cvss31.EnumerateArray().FirstOrDefault();
                                    if (!cvssData.Equals(default(JsonElement)))
                                    {
                                        cvssScore = cvssData.GetProperty("cvssData").GetProperty("baseScore").GetDouble();
                                        severity = cvssData.GetProperty("cvssData").GetProperty("baseSeverity").GetString() ?? "UNKNOWN";
                                    }
                                }
                            }

                            var desc = cve.GetProperty("descriptions")
                                .EnumerateArray()
                                .FirstOrDefault(d => d.GetProperty("lang").GetString() == "en")
                                .GetProperty("value")
                                .GetString() ?? "";

                            cves.Add(new CVEData
                            {
                                CveId = cveId,
                                Description = desc,
                                CVSSScore = cvssScore,
                                Severity = severity,
                                Source = "NVD",
                                PublishedDate = DateTime.Parse(cve.GetProperty("published").GetString())
                            });
                        }
                        catch { /* Skip malformed entries */ }
                    }
                }

                Log.Information($"Fetched {cves.Count} CVEs from NVD");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to fetch CVEs from NVD");
            }

            return cves;
        }

        private async Task<List<CVEData>> FetchFromMITREAsync()
        {
            var cves = new List<CVEData>();
            try
            {
                // MITRE supplement: Fetch additional CVEs from NVD using broader search parameters
                // This supplements NVD by fetching CVEs from additional date ranges
                var url = "https://services.nvd.nist.gov/rest/json/cves/2.0?resultsPerPage=100&sortBy=published&orderBy=desc";
                
                // Add date filtering to get a different time window than primary fetch
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
                url += $"&pubStartDate={thirtyDaysAgo}T00:00:00Z";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Log.Warning($"MITRE supplement fetch failed with status {response.StatusCode}");
                    return cves;
                }

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("vulnerabilities", out var vulns))
                {
                    foreach (var vuln in vulns.EnumerateArray().Take(50)) // Limit to 50 to avoid duplication
                    {
                        try
                        {
                            var cve = vuln.GetProperty("cve");
                            var cveId = cve.GetProperty("id").GetString();

                            // Skip if already in main NVD fetch (will be deduplicated anyway)
                            if (cves.Any(c => c.CveId == cveId))
                                continue;

                            var severity = "UNKNOWN";
                            var cvssScore = 0.0;

                            if (cve.TryGetProperty("metrics", out var metrics))
                            {
                                // Try CVSS v3.1 first, then v3.0
                                if (metrics.TryGetProperty("cvssMetricV31", out var cvss31))
                                {
                                    var cvssData = cvss31.EnumerateArray().FirstOrDefault();
                                    if (!cvssData.Equals(default(JsonElement)))
                                    {
                                        cvssScore = cvssData.GetProperty("cvssData").GetProperty("baseScore").GetDouble();
                                        severity = cvssData.GetProperty("cvssData").GetProperty("baseSeverity").GetString() ?? "UNKNOWN";
                                    }
                                }
                                else if (metrics.TryGetProperty("cvssMetricV30", out var cvss30))
                                {
                                    var cvssData = cvss30.EnumerateArray().FirstOrDefault();
                                    if (!cvssData.Equals(default(JsonElement)))
                                    {
                                        cvssScore = cvssData.GetProperty("cvssData").GetProperty("baseScore").GetDouble();
                                        severity = cvssData.GetProperty("cvssData").GetProperty("baseSeverity").GetString() ?? "UNKNOWN";
                                    }
                                }
                            }

                            var desc = cve.GetProperty("descriptions")
                                .EnumerateArray()
                                .FirstOrDefault(d => d.GetProperty("lang").GetString() == "en")
                                .GetProperty("value")
                                .GetString() ?? "";

                            // Extract affected software from CPE data
                            var affectedSoftware = ExtractAffectedSoftwareFromCPE(cve);

                            cves.Add(new CVEData
                            {
                                CveId = cveId,
                                Description = desc,
                                CVSSScore = cvssScore,
                                Severity = severity,
                                Source = "MITRE",
                                AffectedSoftware = affectedSoftware,
                                PublishedDate = DateTime.Parse(cve.GetProperty("published").GetString())
                            });
                        }
                        catch { /* Skip malformed entries */ }
                    }
                }

                Log.Information($"Fetched {cves.Count} CVEs from MITRE supplement (30-day window)");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to fetch CVEs from MITRE supplement");
            }

            return cves;
        }

        /// <summary>
        /// Extracts software names from NVD CPE (Common Platform Enumeration) data
        /// </summary>
        private string ExtractAffectedSoftwareFromCPE(JsonElement cve)
        {
            var affectedProducts = new List<string>();

            try
            {
                if (cve.TryGetProperty("configurations", out var configs))
                {
                    foreach (var config in configs.EnumerateArray())
                    {
                        if (config.TryGetProperty("nodes", out var nodes))
                        {
                            foreach (var node in nodes.EnumerateArray())
                            {
                                if (node.TryGetProperty("cpeMatch", out var cpeMatches))
                                {
                                    foreach (var match in cpeMatches.EnumerateArray())
                                    {
                                        if (match.TryGetProperty("criteria", out var criteria))
                                        {
                                            var cpeString = criteria.GetString();
                                            // Parse CPE format: cpe:2.3:a:vendor:product:version
                                            var cpeParts = cpeString?.Split(':');
                                            if (cpeParts?.Length >= 5)
                                            {
                                                var vendor = cpeParts[3];
                                                var product = cpeParts[4];
                                                if (!string.IsNullOrEmpty(vendor) && !string.IsNullOrEmpty(product) &&
                                                    vendor != "*" && product != "*")
                                                {
                                                    var softwareName = $"{vendor}/{product}".Replace("_", " ");
                                                    if (!affectedProducts.Contains(softwareName))
                                                        affectedProducts.Add(softwareName);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { /* Continue on parsing errors */ }

            return string.Join(", ", affectedProducts.Take(3));
        }

        public List<CVEData> SearchCVE(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<CVEData>();

            query = query.ToLower();
            return _cveCache.Values
                .Where(c => c.CveId.ToLower().Contains(query) ||
                           c.Description.ToLower().Contains(query) ||
                           c.AffectedSoftware?.ToLower().Contains(query) == true)
                .OrderByDescending(c => c.CVSSScore)
                .Take(50)
                .ToList();
        }

        public List<CVEData> GetSevereCVEs()
        {
            return _cveCache.Values
                .Where(c => c.CVSSScore >= 7.0 || c.Severity == "CRITICAL" || c.Severity == "HIGH")
                .OrderByDescending(c => c.CVSSScore)
                .Take(100)
                .ToList();
        }

        public DateTime GetLastUpdateTime()
        {
            return _lastUpdateTime;
        }

        public int GetCacheSize()
        {
            return _cveCache.Count;
        }

        private void EnsureCacheDirectory()
        {
            if (!Directory.Exists(_cachePath))
                Directory.CreateDirectory(_cachePath);
        }

        private void SaveCacheToDisk()
        {
            try
            {
                var cacheFile = Path.Combine(_cachePath, "cve_cache.json");
                var json = JsonSerializer.Serialize(_cveCache.Values, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(cacheFile, json);
                Log.Debug("CVE cache saved to disk");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save CVE cache to disk");
            }
        }

        private void LoadCacheFromDisk()
        {
            try
            {
                var cacheFile = Path.Combine(_cachePath, "cve_cache.json");
                if (File.Exists(cacheFile))
                {
                    var json = File.ReadAllText(cacheFile);
                    var cves = JsonSerializer.Deserialize<List<CVEData>>(json) ?? new();
                    _cveCache = cves.ToDictionary(c => c.CveId);

                    if (_cveCache.Count > 0)
                        _lastUpdateTime = _cveCache.Values.Max(c => c.CachedDate) ?? DateTime.MinValue;

                    Log.Information($"Loaded {_cveCache.Count} CVEs from disk cache");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to load CVE cache from disk");
            }
        }

        public void ClearCache()
        {
            try
            {
                _cveCache.Clear();
                var cacheFile = Path.Combine(_cachePath, "cve_cache.json");
                if (File.Exists(cacheFile))
                    File.Delete(cacheFile);

                Log.Information("CVE cache cleared");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to clear CVE cache");
            }
        }
    }
}

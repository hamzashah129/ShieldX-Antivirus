using System;using System.Collections.Generic;using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace ShieldX.Services
{
    public class UpdateInfo
    {
        public string LatestVersion  { get; set; } = "";
        public string CurrentVersion { get; set; } = "3.1.1";
        public string ReleaseNotes   { get; set; } = "";
        public string DownloadUrl    { get; set; } = "";
        public string ReleaseName    { get; set; } = "";
        public DateTime ReleaseDate  { get; set; }
        public bool IsUpdateAvailable =>
            !string.IsNullOrEmpty(LatestVersion) &&
            LatestVersion != CurrentVersion &&
            IsNewerVersion(LatestVersion, CurrentVersion);

        private static bool IsNewerVersion(string latest, string current)
        {
            try
            {
                return Version.Parse(latest.TrimStart('v')) >
                       Version.Parse(current.TrimStart('v'));
            }
            catch { return false; }
        }
    }

    public class UpdateService
    {
        private const string CurrentVersion = "3.1.1";
        private readonly AppConfig _config;
        private readonly HttpClient _http;
        private static readonly string UpdateCachePath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "ShieldX", "update_cache.json");

        // Retry configuration
        private const int MaxRetries = 3;
        private const int InitialDelayMs = 1000;
        private const double ExponentialBackoff = 1.5;

        public UpdateService()
        {
            _config = ConfigurationService.Current;
            _http = new HttpClient();
            _http.Timeout = TimeSpan.FromSeconds(15);
            _http.DefaultRequestHeaders.Add(
                "User-Agent", "ShieldX-Antivirus");

            if (!string.IsNullOrEmpty(_config.GitHub.Token))
            {
                _http.DefaultRequestHeaders.Add(
                    "Authorization", $"Bearer {_config.GitHub.Token}");
            }
        }

        private async Task<HttpResponseMessage> GetWithRetryAsync(string url)
        {
            int delayMs = InitialDelayMs;

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    Debug.WriteLine($"[Update] API request attempt {attempt + 1}/{MaxRetries}: {url}");
                    var response = await _http.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                        return response;

                    if ((int)response.StatusCode >= 500)
                    {
                        // Server error, retry
                        Debug.WriteLine($"[Update] Server error: {response.StatusCode}, retrying...");
                        if (attempt < MaxRetries - 1)
                        {
                            await Task.Delay(delayMs);
                            delayMs = (int)(delayMs * ExponentialBackoff);
                            continue;
                        }
                    }

                    return response;
                }
                catch (TaskCanceledException ex)
                {
                    Debug.WriteLine($"[Update] Timeout on attempt {attempt + 1}: {ex.Message}");
                    if (attempt < MaxRetries - 1)
                    {
                        await Task.Delay(delayMs);
                        delayMs = (int)(delayMs * ExponentialBackoff);
                        continue;
                    }
                    throw;
                }
                catch (HttpRequestException ex)
                {
                    Debug.WriteLine($"[Update] Network error on attempt {attempt + 1}: {ex.Message}");
                    if (attempt < MaxRetries - 1)
                    {
                        await Task.Delay(delayMs);
                        delayMs = (int)(delayMs * ExponentialBackoff);
                        continue;
                    }
                    throw;
                }
            }

            throw new InvalidOperationException("Max retries exceeded");
        }

        public async Task<UpdateInfo> CheckForUpdateAsync()
        {
            var info = new UpdateInfo
            {
                CurrentVersion = CurrentVersion
            };

            try
            {
                // Primary: GitHub API
                string apiUrl =
                    $"{_config.GitHub.ApiBaseUrl}/repos/{_config.GitHub.GetRepoUrl()}/releases/latest";

                try
                {
                    var response = await GetWithRetryAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        info.LatestVersion = root
                            .GetProperty("tag_name")
                            .GetString()?.TrimStart('v') ?? CurrentVersion;

                        info.ReleaseName = root
                            .TryGetProperty("name", out var name)
                            ? name.GetString() ?? "" : "";

                        info.ReleaseNotes = root
                            .TryGetProperty("body", out var body)
                            ? body.GetString() ?? "" : "";

                        if (root.TryGetProperty("assets", out var assets))
                        {
                            foreach (var asset in assets.EnumerateArray())
                            {
                                string assetName = asset
                                    .GetProperty("name")
                                    .GetString() ?? "";

                                if (assetName.EndsWith(".exe") ||
                                    assetName.EndsWith(".msi") ||
                                    assetName.Contains("Setup") ||
                                    assetName.Contains("Installer"))
                                {
                                    info.DownloadUrl = asset
                                        .GetProperty("browser_download_url")
                                        .GetString() ?? "";
                                    break;
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(info.DownloadUrl))
                        {
                            info.DownloadUrl = root
                                .TryGetProperty("html_url", out var url)
                                ? url.GetString() ?? "" : "";
                        }

                        if (root.TryGetProperty("published_at", out var date))
                        {
                            if (DateTime.TryParse(date.GetString(), out var dt))
                                info.ReleaseDate = dt;
                        }

                        CacheUpdateInfo(info);
                        Debug.WriteLine($"[Update] Successfully fetched latest version: {info.LatestVersion}");
                        return info;
                    }
                    else
                    {
                        Debug.WriteLine($"[Update] GitHub API error: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Update] GitHub API failed after retries: {ex.Message}");
                }

                // Fallback: version.json from repo
                try
                {
                    string versionCheckUrl =
                        $"https://raw.githubusercontent.com/" +
                        $"{_config.GitHub.GetRepoUrl()}/main/version.json";

                    Debug.WriteLine("[Update] Falling back to version.json");
                    var response = await GetWithRetryAsync(versionCheckUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var cached = JsonSerializer.Deserialize<UpdateInfo>(json);
                        if (cached != null)
                        {
                            cached.CurrentVersion = CurrentVersion;
                            CacheUpdateInfo(cached);
                            Debug.WriteLine($"[Update] version.json fallback successful: {cached.LatestVersion}");
                            return cached;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Update] version.json fallback failed: {ex.Message}");
                }

                // Final fallback: cached data
                var cachedInfo = LoadCachedInfo();
                if (cachedInfo != null)
                {
                    cachedInfo.CurrentVersion = CurrentVersion;
                    Debug.WriteLine($"[Update] Using cached data: {cachedInfo.LatestVersion}");
                    return cachedInfo;
                }

                Debug.WriteLine("[Update] No update data available, returning default");
                return info;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Update] Unexpected error in CheckForUpdateAsync: {ex.Message}");
                return LoadCachedInfo() ?? info;
            }
        }

        public async Task<bool> DownloadAndInstallAsync(
            UpdateInfo info,
            IProgress<int>? progress = null)
        {
            if (string.IsNullOrEmpty(info.DownloadUrl))
            {
                Debug.WriteLine("[Update] No download URL available, opening browser");
                Process.Start(new ProcessStartInfo
                {
                    FileName  = $"https://github.com/{_config.GitHub.GetRepoUrl()}/releases",
                    UseShellExecute = true
                });
                return false;
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "ShieldX");
            Directory.CreateDirectory(tempDir);

            string ext = info.DownloadUrl.EndsWith(".msi")
                ? ".msi" : ".exe";
            string installerPath = Path.Combine(tempDir,
                $"ShieldX_v{info.LatestVersion}_Setup{ext}");

            try
            {
                Debug.WriteLine($"[Update] Starting download from: {info.DownloadUrl}");
                Debug.WriteLine($"[Update] Target path: {installerPath}");

                using var response =
                    await _http.GetAsync(info.DownloadUrl,
                        HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                long? totalBytes =
                    response.Content.Headers.ContentLength;
                long downloadedBytes = 0;

                Debug.WriteLine($"[Update] Total bytes: {totalBytes}");

                using var stream =
                    await response.Content.ReadAsStreamAsync();
                using var file =
                    File.Create(installerPath);

                var buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                {
                    await file.WriteAsync(buffer.AsMemory(0, bytesRead));
                    downloadedBytes += bytesRead;

                    if (totalBytes.HasValue)
                    {
                        int pct = (int)(downloadedBytes * 100 / totalBytes.Value);
                        progress?.Report(pct);
                    }
                }

                progress?.Report(100);
                Debug.WriteLine($"[Update] Download complete: {downloadedBytes} bytes");

                var psi = new ProcessStartInfo
                {
                    FileName        = installerPath,
                    UseShellExecute = true,
                    Verb            = "runas"
                };

                Debug.WriteLine("[Update] Launching installer");
                Process.Start(psi);

                Application.Current.Dispatcher.Invoke(() =>
                    Application.Current.Shutdown());

                return true;
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"[Update] Network error during download: {ex.Message}");
                HandleDownloadError(info, "Network connection failed. Please check your internet connection.");
                return false;
            }
            catch (TaskCanceledException ex)
            {
                Debug.WriteLine($"[Update] Download timeout: {ex.Message}");
                HandleDownloadError(info, "Download took too long. Please try again.");
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"[Update] Permission denied: {ex.Message}");
                HandleDownloadError(info, "Permission denied. Administrator rights required.");
                return false;
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"[Update] File I/O error: {ex.Message}");
                HandleDownloadError(info, "Failed to write installer file. Please check disk space.");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Update] Unexpected error during download: {ex.Message}");
                HandleDownloadError(info, $"An unexpected error occurred: {ex.Message}");
                return false;
            }
        }

        private void HandleDownloadError(UpdateInfo info, string message)
        {
            Debug.WriteLine($"[Update] Opening browser for manual download due to error: {message}");

            Process.Start(new ProcessStartInfo
            {
                FileName = string.IsNullOrEmpty(info.DownloadUrl)
                    ? $"https://github.com/{_config.GitHub.GetRepoUrl()}/releases"
                    : info.DownloadUrl,
                UseShellExecute = true
            });
        }

        private void CacheUpdateInfo(UpdateInfo info)
        {
            try
            {
                Directory.CreateDirectory(
                    Path.GetDirectoryName(UpdateCachePath)!);
                File.WriteAllText(UpdateCachePath,
                    JsonSerializer.Serialize(info));
            }
            catch { }
        }

        public async Task<List<UpdateInfo>> GetReleaseHistoryAsync(int limit = 10)
        {
            var releases = new List<UpdateInfo>();

            try
            {
                string apiUrl =
                    $"{_config.GitHub.ApiBaseUrl}/repos/{_config.GitHub.GetRepoUrl()}/releases?per_page={limit}";

                var response = await _http.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);

                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var release in doc.RootElement.EnumerateArray())
                        {
                            var info = new UpdateInfo
                            {
                                CurrentVersion = CurrentVersion
                            };

                            info.LatestVersion = release
                                .GetProperty("tag_name")
                                .GetString()?.TrimStart('v') ?? "";

                            info.ReleaseName = release
                                .TryGetProperty("name", out var name)
                                ? name.GetString() ?? "" : "";

                            info.ReleaseNotes = release
                                .TryGetProperty("body", out var body)
                                ? body.GetString() ?? "" : "";

                            if (release.TryGetProperty("assets", out var assets))
                            {
                                foreach (var asset in assets.EnumerateArray())
                                {
                                    string assetName = asset
                                        .GetProperty("name")
                                        .GetString() ?? "";

                                    if (assetName.EndsWith(".exe") ||
                                        assetName.EndsWith(".msi") ||
                                        assetName.Contains("Setup") ||
                                        assetName.Contains("Installer"))
                                    {
                                        info.DownloadUrl = asset
                                            .GetProperty("browser_download_url")
                                            .GetString() ?? "";
                                        break;
                                    }
                                }
                            }

                            if (release.TryGetProperty("published_at", out var date))
                            {
                                if (DateTime.TryParse(date.GetString(), out var dt))
                                    info.ReleaseDate = dt;
                            }

                            releases.Add(info);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Update] Failed to fetch release history: {ex.Message}");
            }

            return releases;
        }

        /// <summary>
        /// Loads cached update information from disk
        /// </summary>
        private UpdateInfo LoadCachedInfo()
        {
            try
            {
                if (File.Exists(UpdateCachePath))
                {
                    var json = File.ReadAllText(UpdateCachePath);
                    return JsonSerializer.Deserialize<UpdateInfo>(json);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Update] Failed to load cached info: {ex.Message}");
            }
            return null;
        }
    }
}

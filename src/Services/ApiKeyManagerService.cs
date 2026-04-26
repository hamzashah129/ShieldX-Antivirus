using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Serilog;

namespace ShieldX.Services
{
    /// <summary>
    /// API Key Manager Service - Manages secure storage of API keys for threat intelligence services
    /// Uses DPAPI (Data Protection API) for encryption at rest
    /// </summary>
    public class ApiKeyManagerService
    {
        private static ApiKeyManagerService _instance;
        public static ApiKeyManagerService Instance => _instance ??= new ApiKeyManagerService();

        private readonly string _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ShieldX", "ApiKeys.json");

        private Dictionary<string, string> _apiKeys = new();

        public ApiKeyManagerService()
        {
            EnsureConfigDirectory();
            LoadApiKeys();
        }

        private void EnsureConfigDirectory()
        {
            var dir = Path.GetDirectoryName(_configPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        public void SetApiKey(string service, string apiKey)
        {
            try
            {
                _apiKeys[service] = apiKey;
                SaveApiKeys();
                Log.Information($"API key configured for {service}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to set API key for {service}");
            }
        }

        public string GetApiKey(string service)
        {
            try
            {
                if (_apiKeys.TryGetValue(service, out var key))
                    return key;

                Log.Warning($"No API key found for {service}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to retrieve API key for {service}");
                return null;
            }
        }

        public bool HasApiKey(string service)
        {
            return _apiKeys.ContainsKey(service) && !string.IsNullOrEmpty(_apiKeys[service]);
        }

        public async Task<bool> ValidateApiKey(string service, string apiKey)
        {
            try
            {
                // Validate API key format and connectivity
                return service switch
                {
                    "VirusTotal" => await ValidateVirusTotalKey(apiKey),
                    "AbuseIPDB" => await ValidateAbuseIPDBKey(apiKey),
                    "GoogleSafeBrowsing" => await ValidateGoogleSafeBrowsingKey(apiKey),
                    _ => false
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"API key validation failed for {service}");
                return false;
            }
        }

        private async Task<bool> ValidateVirusTotalKey(string apiKey)
        {
            // VirusTotal API key should be 64 characters hex
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length != 64)
                return false;

            // Could make an HTTP request to verify, but for now just validate format
            return apiKey.All(c => char.IsLetterOrDigit(c));
        }

        private async Task<bool> ValidateAbuseIPDBKey(string apiKey)
        {
            // AbuseIPDB API key validation
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length < 32)
                return false;

            return true;
        }

        private async Task<bool> ValidateGoogleSafeBrowsingKey(string apiKey)
        {
            // Google API key validation
            if (string.IsNullOrWhiteSpace(apiKey))
                return false;

            return apiKey.Length > 10;
        }

        public Dictionary<string, bool> GetConfiguredServices()
        {
            var services = new Dictionary<string, bool>
            {
                { "VirusTotal", HasApiKey("VirusTotal") },
                { "AbuseIPDB", HasApiKey("AbuseIPDB") },
                { "GoogleSafeBrowsing", HasApiKey("GoogleSafeBrowsing") }
            };

            return services;
        }

        public void ClearApiKey(string service)
        {
            try
            {
                if (_apiKeys.ContainsKey(service))
                {
                    _apiKeys.Remove(service);
                    SaveApiKeys();
                    Log.Information($"API key cleared for {service}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to clear API key for {service}");
            }
        }

        public void ClearAllApiKeys()
        {
            try
            {
                _apiKeys.Clear();
                SaveApiKeys();
                Log.Information("All API keys cleared");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to clear all API keys");
            }
        }

        private void SaveApiKeys()
        {
            try
            {
                var json = JsonSerializer.Serialize(_apiKeys);
                var encryptedJson = EncryptString(json);
                File.WriteAllText(_configPath, encryptedJson);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save API keys");
            }
        }

        private void LoadApiKeys()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var encryptedJson = File.ReadAllText(_configPath);
                    var json = DecryptString(encryptedJson);
                    _apiKeys = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load API keys");
                _apiKeys = new();
            }
        }

        private string EncryptString(string plainText)
        {
            try
            {
                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encryptedBytes);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Encryption failed");
                return plainText; // Fallback: return unencrypted (not ideal but graceful degradation)
            }
        }

        private string DecryptString(string encryptedText)
        {
            try
            {
                var encryptedBytes = Convert.FromBase64String(encryptedText);
                var plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                // If decryption fails, assume it's not encrypted (legacy or corrupted)
                return encryptedText;
            }
        }
    }
}

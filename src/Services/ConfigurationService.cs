using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ShieldX.Services
{
    public class AppConfig
    {
        public GitHubConfig GitHub { get; set; } = new();
        public UpdatesConfig Updates { get; set; } = new();
        public LoggingConfig Logging { get; set; } = new();
    }

    public class GitHubConfig
    {
        public string Owner { get; set; } = "";
        public string Repository { get; set; } = "";
        public string Token { get; set; } = "";
        public string ApiBaseUrl { get; set; } = "https://api.github.com";

        public string GetRepoUrl() => $"{Owner}/{Repository}";
    }

    public class UpdatesConfig
    {
        public bool AutoCheckEnabled { get; set; } = true;
        public int CheckIntervalHours { get; set; } = 24;
        public bool NotifyBeforeInstall { get; set; } = true;
        public bool AllowBetaUpdates { get; set; } = false;
    }

    public class LoggingConfig
    {
        public string Level { get; set; } = "Information";
    }

    public class ConfigurationService
    {
        private static AppConfig? _config;
        private static readonly string ConfigPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        public static AppConfig Load()
        {
            if (_config != null) return _config;

            try
            {
                if (!File.Exists(ConfigPath))
                {
                    throw new FileNotFoundException($"Configuration file not found: {ConfigPath}");
                }

                var json = File.ReadAllText(ConfigPath);
                _config = JsonSerializer.Deserialize<AppConfig>(json)
                    ?? throw new InvalidOperationException("Failed to deserialize configuration");

                // Replace environment variables in token
                if (_config.GitHub.Token.StartsWith("${") && _config.GitHub.Token.EndsWith("}"))
                {
                    string envVar = _config.GitHub.Token
                        .TrimStart('$', '{')
                        .TrimEnd('}');
                    string? envValue = Environment.GetEnvironmentVariable(envVar);

                    if (!string.IsNullOrEmpty(envValue))
                    {
                        _config.GitHub.Token = envValue;
                    }
                    else
                    {
                        // Try user secrets for development
                        _config.GitHub.Token = LoadUserSecret(envVar) ?? "";
                    }
                }

                return _config;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Config] Failed to load: {ex.Message}");
                // Return default config on failure
                return new AppConfig();
            }
        }

        private static string? LoadUserSecret(string key)
        {
            try
            {
                string userSecretsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Microsoft", "UserSecrets", "ShieldX");

                if (!Directory.Exists(userSecretsPath)) return null;

                foreach (var file in Directory.GetFiles(userSecretsPath, "*.json"))
                {
                    var json = File.ReadAllText(file);
                    using var doc = JsonDocument.Parse(json);
                    
                    if (doc.RootElement.TryGetProperty(key, out var value))
                    {
                        return value.GetString();
                    }
                }
            }
            catch { }
            return null;
        }

        public static AppConfig Current => _config ?? Load();
    }
}

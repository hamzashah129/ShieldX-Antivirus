using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;

namespace ShieldX.Utils
{
    /// <summary>
    /// Configuration validator for security and system settings.
    /// </summary>
    public static class ConfigurationValidator
    {
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> Errors { get; } = new();
            public List<string> Warnings { get; } = new();

            public override string ToString()
            {
                var lines = new List<string>();
                lines.Add($"Valid: {IsValid}");
                if (Errors.Any())
                {
                    lines.Add("Errors:");
                    foreach (var error in Errors)
                        lines.Add($"  - {error}");
                }
                if (Warnings.Any())
                {
                    lines.Add("Warnings:");
                    foreach (var error in Warnings)
                        lines.Add($"  - {error}");
                }
                return string.Join(Environment.NewLine, lines);
            }
        }

        /// <summary>
        /// Validates application configuration and environment.
        /// </summary>
        public static ValidationResult ValidateApplicationConfiguration()
        {
            var result = new ValidationResult();

            try
            {
                // Check .NET Framework
                if (!CheckDotNetVersion())
                {
                    result.Errors.Add(".NET 8.0 or higher is required");
                }

                // Check OS Version
                if (!CheckOperatingSystem())
                {
                    result.Errors.Add("Windows 7 SP1 or higher is required");
                }

                // Check User Privileges
                if (!CheckAdministratorPrivileges())
                {
                    result.Warnings.Add("Running without administrator privileges. Some features may be limited.");
                }

                // Check Disk Space
                try
                {
                    ValidationUtility.ValidateDiskSpace(
                        System.IO.Path.GetTempPath(), minFreeMb: 500);
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"Low disk space: {ex.Message}");
                }

                // Check Memory
                var memory = GC.GetTotalMemory(false) / (1024 * 1024);
                if (memory < 512)
                {
                    result.Warnings.Add($"Limited available memory ({memory}MB). Performance may be affected.");
                }

                // Check Security Features
                if (!CheckWindowsDefender())
                {
                    result.Warnings.Add("Windows Defender is not active. Consider enabling it for comprehensive protection.");
                }

                result.IsValid = result.Errors.Count == 0;

                Log.Information($"Configuration validation: {(result.IsValid ? "Valid" : "Invalid")}");
                foreach (var error in result.Errors)
                    Log.Error($"Config error: {error}");
                foreach (var warning in result.Warnings)
                    Log.Warning($"Config warning: {warning}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Configuration validation failed");
                result.Errors.Add($"Configuration validation error: {ex.Message}");
                result.IsValid = false;
            }

            return result;
        }

        private static bool CheckDotNetVersion()
        {
            try
            {
                var version = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
                return version.Contains("8.0") || version.Contains("9.0");
            }
            catch { return true; } // Assume ok if check fails
        }

        private static bool CheckOperatingSystem()
        {
            try
            {
                var os = System.Environment.OSVersion;
                return os.Platform == PlatformID.Win32NT && os.Version.Major >= 6;
            }
            catch { return true; }
        }

        private static bool CheckAdministratorPrivileges()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        private static bool CheckWindowsDefender()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows Defender\Real-Time Protection");
                if (key == null) return false;
                var value = key.GetValue("DisableRealtimeMonitoring");
                return value == null || (int)value != 1;
            }
            catch { return false; }
        }

        /// <summary>
        /// Validates scan operation parameters.
        /// </summary>
        public static ValidationResult ValidateScanParameters(string scanPath, int threadCount = 4)
        {
            var result = new ValidationResult();

            try
            {
                // Validate path
                if (!System.IO.Path.IsPathRooted(scanPath))
                {
                    result.Errors.Add($"Invalid scan path: {scanPath}");
                }
                else if (!System.IO.Directory.Exists(scanPath))
                {
                    result.Errors.Add($"Scan path does not exist: {scanPath}");
                }

                // Validate thread count
                if (threadCount < 1 || threadCount > Environment.ProcessorCount)
                {
                    result.Warnings.Add($"Thread count {threadCount} is outside recommended range (1-{Environment.ProcessorCount})");
                }

                // Check path accessibility
                try
                {
                    var testFile = System.IO.Path.Combine(scanPath, ".shieldx_test");
                    System.IO.File.Create(testFile).Dispose();
                    System.IO.File.Delete(testFile);
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"Limited access to scan path: {ex.Message}");
                }

                result.IsValid = result.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Scan parameter validation failed: {ex.Message}");
                result.IsValid = false;
            }

            return result;
        }

        /// <summary>
        /// Validates quarantine settings and vault access.
        /// </summary>
        public static ValidationResult ValidateQuarantineSettings(string vaultPath)
        {
            var result = new ValidationResult();

            try
            {
                ValidationUtility.ValidateDiskSpace(vaultPath, minFreeMb: 100);
                
                // Check vault accessibility
                var testFile = System.IO.Path.Combine(vaultPath, ".test");
                try
                {
                    System.IO.File.Create(testFile).Dispose();
                    System.IO.File.Delete(testFile);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Cannot write to vault path: {ex.Message}");
                }

                result.IsValid = result.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Quarantine settings validation failed: {ex.Message}");
                result.IsValid = false;
            }

            return result;
        }
    }
}

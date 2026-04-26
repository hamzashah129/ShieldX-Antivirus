using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using ShieldX.Models;

namespace ShieldX.Services
{
    /// <summary>
    /// Service for managing system startup programs
    /// </summary>
    public class StartupService
    {
        // Known system processes that shouldn't be disabled
        private static readonly HashSet<string> SystemProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "explorer.exe",
            "svchost.exe",
            "dwm.exe",
            "lsass.exe",
            "smss.exe"
        };

        // Known high-impact programs that are typically resource-intensive
        private static readonly HashSet<string> HighImpactPrograms = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bitdefender",
            "norton",
            "kaspersky",
            "avast",
            "avg",
            "trend micro",
            "cloudflare",
            "google",
            "dropbox",
            "steam",
            "slack",
            "teams",
            "chrome",
            "firefox",
            "edge"
        };

        /// <summary>
        /// Gets all startup entries from registry and startup folder
        /// </summary>
        public static List<StartupEntry> GetAllStartupEntries()
        {
            var entries = new List<StartupEntry>();

            try
            {
                // Read from HKCU Run key
                entries.AddRange(GetRegistryStartupEntries(RegistryHive.CurrentUser, 
                    @"Software\Microsoft\Windows\CurrentVersion\Run", "HKCU"));

                // Read from HKLM Run key (requires elevation for some entries)
                entries.AddRange(GetRegistryStartupEntries(RegistryHive.LocalMachine,
                    @"Software\Microsoft\Windows\CurrentVersion\Run", "HKLM"));

                // Read from Startup folder
                entries.AddRange(GetStartupFolderEntries());

                // Remove duplicates based on path
                entries = entries
                    .GroupBy(e => e.Path.ToLowerInvariant())
                    .Select(g => g.First())
                    .ToList();

                // Sort by name
                return entries.OrderBy(e => e.Name).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading startup entries: {ex.Message}");
                return entries;
            }
        }

        /// <summary>
        /// Reads startup entries from a specific registry key
        /// </summary>
        private static List<StartupEntry> GetRegistryStartupEntries(RegistryHive hive, string subKeyPath, string hiveName)
        {
            var entries = new List<StartupEntry>();

            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
                {
                    using (RegistryKey key = baseKey.OpenSubKey(subKeyPath))
                    {
                        if (key == null)
                            return entries;

                        foreach (string valueName in key.GetValueNames())
                        {
                            try
                            {
                                var value = key.GetValue(valueName);
                                if (value is string programPath && !string.IsNullOrWhiteSpace(programPath))
                                {
                                    var entry = ParseStartupEntry(programPath, valueName, hiveName, subKeyPath);
                                    entries.Add(entry);
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }

            return entries;
        }

        /// <summary>
        /// Reads startup entries from the Startup folder
        /// </summary>
        private static List<StartupEntry> GetStartupFolderEntries()
        {
            var entries = new List<StartupEntry>();

            try
            {
                string startupPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Microsoft\Windows\Start Menu\Programs\Startup");

                if (!Directory.Exists(startupPath))
                    return entries;

                // Read executable files
                foreach (var file in Directory.GetFiles(startupPath, "*.exe"))
                {
                    try
                    {
                        string fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                        var entry = ParseStartupEntry(file, fileName, "Startup Folder", "");
                        entries.Add(entry);
                    }
                    catch { }
                }

                // Read batch files
                foreach (var file in Directory.GetFiles(startupPath, "*.bat"))
                {
                    try
                    {
                        string fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                        var entry = ParseStartupEntry(file, fileName, "Startup Folder", "");
                        entries.Add(entry);
                    }
                    catch { }
                }

                // Read VBS scripts
                foreach (var file in Directory.GetFiles(startupPath, "*.vbs"))
                {
                    try
                    {
                        string fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                        var entry = ParseStartupEntry(file, fileName, "Startup Folder", "");
                        entries.Add(entry);
                    }
                    catch { }
                }
            }
            catch { }

            return entries;
        }

        /// <summary>
        /// Parses a startup entry from program path and metadata
        /// </summary>
        private static StartupEntry ParseStartupEntry(string programPath, string valueName, string hiveName, string subKeyPath)
        {
            // Extract executable path and arguments
            string execPath = ExtractExecutablePath(programPath);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(execPath);

            var entry = new StartupEntry
            {
                Name = valueName,
                Path = execPath,
                RegistryHive = hiveName,
                RegistrySubKey = subKeyPath,
                RegistryValueName = valueName,
                IsEnabled = true,
                DiscoveredDate = DateTime.Now,
                IsSystemEntry = IsSystemProgram(fileName),
                Publisher = GetPublisher(execPath),
                Impact = AnalyzeImpact(fileName, execPath)
            };

            return entry;
        }

        /// <summary>
        /// Extracts the executable path from a command line
        /// </summary>
        private static string ExtractExecutablePath(string commandLine)
        {
            if (string.IsNullOrEmpty(commandLine))
                return "";

            commandLine = commandLine.Trim();

            // Handle quoted paths
            if (commandLine.StartsWith("\""))
            {
                int endQuote = commandLine.IndexOf("\"", 1);
                if (endQuote > 0)
                    return commandLine.Substring(1, endQuote - 1);
            }

            // Handle unquoted paths (take first part before space)
            int spaceIndex = commandLine.IndexOf(" ");
            if (spaceIndex > 0)
                return commandLine.Substring(0, spaceIndex);

            return commandLine;
        }

        /// <summary>
        /// Analyzes impact level based on program characteristics
        /// </summary>
        private static StartupImpact AnalyzeImpact(string fileName, string filePath)
        {
            // Check if it's a high-impact program
            foreach (var program in HighImpactPrograms)
            {
                if (fileName.Contains(program, StringComparison.OrdinalIgnoreCase) ||
                    filePath.Contains(program, StringComparison.OrdinalIgnoreCase))
                {
                    return StartupImpact.High;
                }
            }

            // Check file size if available (large files are typically more resource-intensive)
            try
            {
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.Length > 50 * 1024 * 1024) // Over 50MB
                        return StartupImpact.High;
                    if (fileInfo.Length > 10 * 1024 * 1024) // Over 10MB
                        return StartupImpact.Medium;
                }
            }
            catch { }

            // Check for known medium-impact patterns
            if (fileName.Contains("update", StringComparison.OrdinalIgnoreCase) ||
                fileName.Contains("service", StringComparison.OrdinalIgnoreCase))
            {
                return StartupImpact.Medium;
            }

            return StartupImpact.Low;
        }

        /// <summary>
        /// Determines if a program is a critical system process
        /// </summary>
        private static bool IsSystemProgram(string fileName)
        {
            return SystemProcesses.Contains(fileName);
        }

        /// <summary>
        /// Gets the publisher name from file version info
        /// </summary>
        private static string GetPublisher(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return "Unknown";

                var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(filePath);
                if (!string.IsNullOrWhiteSpace(versionInfo.CompanyName))
                    return versionInfo.CompanyName;
            }
            catch { }

            return "Unknown";
        }

        /// <summary>
        /// Enables a startup entry in the registry
        /// </summary>
        public static bool EnableStartupEntry(StartupEntry entry)
        {
            try
            {
                if (entry.RegistryHive == "Startup Folder")
                    return true; // Already enabled

                RegistryHive hive = entry.RegistryHive == "HKCU" ? RegistryHive.CurrentUser : RegistryHive.LocalMachine;
                
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
                {
                    using (RegistryKey key = baseKey.OpenSubKey(entry.RegistrySubKey, true))
                    {
                        if (key != null)
                        {
                            key.SetValue(entry.RegistryValueName, entry.Path);
                            entry.IsEnabled = true;
                            return true;
                        }
                    }
                }
            }
            catch { }

            return false;
        }

        /// <summary>
        /// Disables a startup entry in the registry (removes it from Run key)
        /// </summary>
        public static bool DisableStartupEntry(StartupEntry entry)
        {
            try
            {
                if (entry.IsSystemEntry)
                    return false; // Cannot disable system entries

                if (entry.RegistryHive == "Startup Folder")
                {
                    // For startup folder items, move to a backup location
                    return DisableStartupFolderEntry(entry);
                }

                RegistryHive hive = entry.RegistryHive == "HKCU" ? RegistryHive.CurrentUser : RegistryHive.LocalMachine;
                
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
                {
                    using (RegistryKey key = baseKey.OpenSubKey(entry.RegistrySubKey, true))
                    {
                        if (key != null)
                        {
                            key.DeleteValue(entry.RegistryValueName, false);
                            entry.IsEnabled = false;
                            return true;
                        }
                    }
                }
            }
            catch { }

            return false;
        }

        /// <summary>
        /// Disables a startup folder entry by renaming it
        /// </summary>
        private static bool DisableStartupFolderEntry(StartupEntry entry)
        {
            try
            {
                if (File.Exists(entry.Path))
                {
                    string disabledPath = entry.Path + ".disabled";
                    if (!File.Exists(disabledPath))
                    {
                        File.Move(entry.Path, disabledPath);
                        entry.IsEnabled = false;
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        /// <summary>
        /// Registers ShieldX in Windows startup to launch automatically on boot
        /// </summary>
        public static bool RegisterShieldXStartup()
        {
            try
            {
                // Get the path to the current application executable
                string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                
                // If running from Release build output
                if (string.IsNullOrEmpty(appPath) || !File.Exists(appPath))
                {
                    appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                }

                if (string.IsNullOrEmpty(appPath) || !File.Exists(appPath))
                {
                    System.Diagnostics.Debug.WriteLine("ShieldX: Could not determine application path for startup registration");
                    return false;
                }

                // Register in HKCU\Software\Microsoft\Windows\CurrentVersion\Run
                // HKCU allows registration without admin elevation
                RegistryHive hive = RegistryHive.CurrentUser;
                string subKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
                string valueName = "ShieldX";

                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
                {
                    using (RegistryKey key = baseKey.OpenSubKey(subKey, true))
                    {
                        if (key != null)
                        {
                            key.SetValue(valueName, appPath);
                            System.Diagnostics.Debug.WriteLine($"ShieldX: Auto-start registered successfully at {appPath}");
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShieldX: Failed to register auto-start: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Unregisters ShieldX from Windows startup
        /// </summary>
        public static bool UnregisterShieldXStartup()
        {
            try
            {
                RegistryHive hive = RegistryHive.CurrentUser;
                string subKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
                string valueName = "ShieldX";

                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
                {
                    using (RegistryKey key = baseKey.OpenSubKey(subKey, true))
                    {
                        if (key != null)
                        {
                            key.DeleteValue(valueName, false);
                            System.Diagnostics.Debug.WriteLine("ShieldX: Auto-start unregistered successfully");
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShieldX: Failed to unregister auto-start: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if ShieldX is currently registered in Windows startup
        /// </summary>
        public static bool IsShieldXStartupEnabled()
        {
            try
            {
                RegistryHive hive = RegistryHive.CurrentUser;
                string subKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
                string valueName = "ShieldX";

                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
                {
                    using (RegistryKey key = baseKey.OpenSubKey(subKey))
                    {
                        if (key != null)
                        {
                            var value = key.GetValue(valueName);
                            return value != null;
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;

namespace ShieldX.Services
{
    public class ShellIntegrationService
    {
        private const string AppName = "ShieldX Professional Antivirus";
        private const string CommandName = "ScanWithShieldX";
        private const string MenuText = "Scan with ShieldX";

        // Registry paths for context menu integration
        private static readonly string[] RegistryPaths = {
            @"*\shell\ScanWithShieldX",           // All files
            @"Directory\shell\ScanWithShieldX",   // Directories
            @"Drive\shell\ScanWithShieldX"        // Drives
        };

        public static void RegisterContextMenu()
        {
            try
            {
                // Check if running as administrator
                if (!IsAdministrator())
                {
                    throw new UnauthorizedAccessException("Administrator privileges required for shell integration");
                }

                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                string command = $"\"{exePath}\" \"%1\"";

                foreach (string regPath in RegistryPaths)
                {
                    using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(regPath))
                    {
                        if (key != null)
                        {
                            // Set the menu text
                            key.SetValue(null, MenuText);
                            key.SetValue("Icon", exePath);

                            // Create command subkey
                            using (RegistryKey commandKey = key.CreateSubKey("command"))
                            {
                                commandKey.SetValue(null, command);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash the app
                LogService.Instance.AddError($"Failed to register context menu: {ex.Message}");
            }
        }

        public static void UnregisterContextMenu()
        {
            try
            {
                if (!IsAdministrator())
                {
                    throw new UnauthorizedAccessException("Administrator privileges required for shell integration");
                }

                foreach (string regPath in RegistryPaths)
                {
                    Registry.ClassesRoot.DeleteSubKeyTree(regPath, false);
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.AddError($"Failed to unregister context menu: {ex.Message}");
            }
        }

        public static bool IsContextMenuRegistered()
        {
            try
            {
                foreach (string regPath in RegistryPaths)
                {
                    using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(regPath))
                    {
                        if (key == null)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public static void HandleContextMenuScan(string[] args)
        {
            if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
            {
                string targetPath = args[0];

                // Validate the path exists
                if (File.Exists(targetPath) || Directory.Exists(targetPath))
                {
                    // Launch ShieldX with custom scan targeting this path
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = Process.GetCurrentProcess().MainModule.FileName,
                        Arguments = $"--custom-scan \"{targetPath}\"",
                        UseShellExecute = true
                    });
                }
            }
        }
    }
}
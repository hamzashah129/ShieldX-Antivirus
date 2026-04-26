using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;

namespace ShieldX.Utils
{
    public static class ContextMenuInstaller
    {
        private static readonly string ExePath = 
            System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

        private static readonly string MenuText = "Scan with ShieldX";

        private static readonly string[] RegistryPaths = new[]
        {
            @"*\shell\ScanWithShieldX",
            @"Directory\shell\ScanWithShieldX",
            @"Drive\shell\ScanWithShieldX",
            @"Directory\Background\shell\ScanWithShieldX"
        };

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private const uint SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_IDLIST = 0x0000;

        public static void Register()
        {
            if (!IsRunningAsAdmin())
                throw new UnauthorizedAccessException(
                    "Please run ShieldX as Administrator to register context menu.");

            try
            {
                foreach (var path in RegistryPaths)
                {
                    using var key = Registry.ClassesRoot.CreateSubKey(path);
                    key.SetValue("", MenuText);
                    key.SetValue("Icon", $"\"{ExePath}\",0");

                    string commandValue = path.Contains("Background")
                        ? $"\"{ExePath}\" --scan \"%V\""
                        : $"\"{ExePath}\" --scan \"%1\"";

                    using var commandKey = Registry.ClassesRoot.CreateSubKey(path + @"\command");
                    commandKey.SetValue("", commandValue);
                }

                // Add MultiSelectModel for Windows 11 context menu
                using var fileKey = Registry.ClassesRoot.OpenSubKey(@"*\shell\ScanWithShieldX", true);
                if (fileKey != null)
                {
                    fileKey.SetValue("MultiSelectModel", "Single");
                }

                // Refresh Windows Explorer shell
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to register context menu: {ex.Message}", ex);
            }
        }

        public static void Unregister()
        {
            if (!IsRunningAsAdmin())
                throw new UnauthorizedAccessException(
                    "Please run ShieldX as Administrator to unregister context menu.");

            try
            {
                foreach (var path in RegistryPaths)
                {
                    Registry.ClassesRoot.DeleteSubKeyTree(path, throwOnMissingSubKey: false);
                }

                // Refresh Windows Explorer shell
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to unregister context menu: {ex.Message}", ex);
            }
        }

        public static bool IsRegistered()
        {
            try
            {
                using var key = Registry.ClassesRoot.OpenSubKey(@"*\shell\ScanWithShieldX");
                return key != null;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsRunningAsAdmin()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}

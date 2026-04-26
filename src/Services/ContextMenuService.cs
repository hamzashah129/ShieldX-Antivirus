using System;
using System.Diagnostics;
using Microsoft.Win32;
using System.Security.Principal;
using System.Windows;
using System.Runtime.InteropServices;

namespace ShieldX.Services
{
    public static class ContextMenuService
    {
        private static string ExePath =>
            Process.GetCurrentProcess().MainModule?.FileName
            ?? System.Reflection.Assembly
                .GetExecutingAssembly().Location;

        public static bool IsAdmin =>
            new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator);

        public static bool IsRegistered
        {
            get
            {
                try
                {
                    using var key = Registry.ClassesRoot
                        .OpenSubKey(@"*\shell\ScanWithShieldX");
                    return key != null;
                }
                catch { return false; }
            }
        }

        public static bool Register()
        {
            if (!IsAdmin)
            {
                MessageBox.Show(
                    "Administrator rights required.\n" +
                    "Please restart ShieldX as Administrator.",
                    "ShieldX — Permission Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            try
            {
                string exe = $"\"{ExePath}\"";
                string menuText = "Scan with ShieldX";

                // Files (any file)
                SetKey(@"*\shell\ScanWithShieldX",
                    menuText, exe);
                SetKey(@"*\shell\ScanWithShieldX\command",
                    null, $"{exe} --scan \"%1\"");

                // Folders
                SetKey(@"Directory\shell\ScanWithShieldX",
                    menuText, exe);
                SetKey(@"Directory\shell\ScanWithShieldX\command",
                    null, $"{exe} --scan \"%1\"");

                // Background of folder
                SetKey(@"Directory\Background\shell\ScanWithShieldX",
                    menuText, exe);
                SetKey(@"Directory\Background\shell\ScanWithShieldX\command",
                    null, $"{exe} --scan \"%V\"");

                // Drives
                SetKey(@"Drive\shell\ScanWithShieldX",
                    menuText, exe);
                SetKey(@"Drive\shell\ScanWithShieldX\command",
                    null, $"{exe} --scan \"%1\"");

                // Refresh Windows Explorer
                RefreshExplorer();

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Registration failed:\n{ex.Message}",
                    "ShieldX", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }

        public static bool Unregister()
        {
            if (!IsAdmin) return false;

            try
            {
                string[] paths = {
                    @"*\shell\ScanWithShieldX",
                    @"Directory\shell\ScanWithShieldX",
                    @"Directory\Background\shell\ScanWithShieldX",
                    @"Drive\shell\ScanWithShieldX",
                };

                foreach (var path in paths)
                {
                    try
                    {
                        Registry.ClassesRoot
                            .DeleteSubKeyTree(path, false);
                    }
                    catch { }
                }

                RefreshExplorer();
                return true;
            }
            catch { return false; }
        }

        private static void SetKey(
            string path, string? name, string value)
        {
            using var key = Registry.ClassesRoot
                .CreateSubKey(path, true);
            if (name == null)
                key.SetValue("", value);
            else
            {
                key.SetValue("", name);
                key.SetValue("Icon",
                    $"\"{ExePath}\",0");
            }
        }

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(
            int eventId, uint flags,
            IntPtr item1, IntPtr item2);

        private static void RefreshExplorer()
        {
            try
            {
                SHChangeNotify(0x08000000, 0,
                    IntPtr.Zero, IntPtr.Zero);
            }
            catch { }
        }
    }
}

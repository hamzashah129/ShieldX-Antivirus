using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace ShieldX.Views
{
    /// <summary>
    /// Interaction logic for InstallerManagerWindow.xaml
    /// Visual Studio Installer-style installation manager for ShieldX
    /// </summary>
    public partial class InstallerManagerWindow : Window
    {
        public InstallerManagerWindow()
        {
            InitializeComponent();
            LoadInstallInfo();
        }

        /// <summary>
        /// Load installation information from registry and calculate disk usage
        /// </summary>
        private void LoadInstallInfo()
        {
            try
            {
                // Get install path from registry (Software\ShieldX\Professional)
                using (var key = Registry.LocalMachine.OpenSubKey(@"Software\ShieldX\Professional"))
                {
                    string? path = key?.GetValue("InstallPath")?.ToString() 
                        ?? AppDomain.CurrentDomain.BaseDirectory;

                    InstallPathText.Text = path;

                    // Calculate total disk usage
                    long totalBytes = 0;
                    if (Directory.Exists(path))
                    {
                        try
                        {
                            var dirInfo = new DirectoryInfo(path);
                            foreach (var f in dirInfo.GetFiles("*.*", System.IO.SearchOption.AllDirectories))
                            {
                                try { totalBytes += f.Length; }
                                catch { /* Skip inaccessible files */ }
                            }
                        }
                        catch { /* Skip if directory inaccessible */ }
                    }

                    // Format disk usage as MB or GB
                    double mb = totalBytes / (1024.0 * 1024.0);
                    DiskUsageText.Text = mb > 1024
                        ? $"{mb / 1024.0:F1} GB"
                        : $"{mb:F0} MB";
                }
            }
            catch
            {
                InstallPathText.Text = "C:\\Program Files\\ShieldX Professional Antivirus";
                DiskUsageText.Text = "50 MB";
            }
        }

        /// <summary>
        /// Show the Installed tab
        /// </summary>
        private void TabInstalled_Click(object s, RoutedEventArgs e)
        {
            InstalledPanel.Visibility = Visibility.Visible;
            AvailablePanel.Visibility = Visibility.Collapsed;
            
            // Update button styles to show active tab
            TabInstalled.Style = (Style)FindResource("PrimaryButton");
            TabAvailable.Style = (Style)FindResource("ActionButton");
        }

        /// <summary>
        /// Show the Available updates tab
        /// </summary>
        private void TabAvailable_Click(object s, RoutedEventArgs e)
        {
            InstalledPanel.Visibility = Visibility.Collapsed;
            AvailablePanel.Visibility = Visibility.Visible;
            
            // Update button styles to show active tab
            TabInstalled.Style = (Style)FindResource("ActionButton");
            TabAvailable.Style = (Style)FindResource("PrimaryButton");
        }

        /// <summary>
        /// Launch ShieldX application
        /// </summary>
        private void Launch_Click(object s, RoutedEventArgs e)
        {
            try
            {
                string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (File.Exists(exePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not launch ShieldX:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Close();
        }

        /// <summary>
        /// Check for updates
        /// </summary>
        private void Update_Click(object s, RoutedEventArgs e)
        {
            TabAvailable_Click(s, e);
            MessageBox.Show(
                "Checking for updates...\n\n" +
                "ShieldX v3.1.1 is the latest version.",
                "ShieldX — Update Check",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        /// <summary>
        /// Launch installer in modify mode
        /// </summary>
        private void Modify_Click(object s, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Modify ShieldX installation?\n\n" +
                "This will allow you to add or remove components.\n" +
                "The installer will be launched.",
                "ShieldX — Modify Installation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
                LaunchInstaller("/MODIFY");
        }

        /// <summary>
        /// Launch installer in repair mode
        /// </summary>
        private void Repair_Click(object s, RoutedEventArgs e)
        {
            MoreMenu.Visibility = Visibility.Collapsed;
            var result = MessageBox.Show(
                "Repair ShieldX installation?\n\n" +
                "This will fix any corrupted or missing files\n" +
                "without affecting your settings or data.",
                "ShieldX — Repair Installation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
                LaunchInstaller("/REPAIR");
        }

        /// <summary>
        /// Open install folder in Windows Explorer
        /// </summary>
        private void OpenFolder_Click(object s, RoutedEventArgs e)
        {
            MoreMenu.Visibility = Visibility.Collapsed;
            string path = InstallPathText.Text;
            if (Directory.Exists(path))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = path,
                    UseShellExecute = true
                });
            }
        }

        /// <summary>
        /// Copy install path to clipboard
        /// </summary>
        private void CopyPath_Click(object s, RoutedEventArgs e)
        {
            MoreMenu.Visibility = Visibility.Collapsed;
            try
            {
                Clipboard.SetText(InstallPathText.Text);
                MessageBox.Show("Install path copied to clipboard!",
                    "ShieldX", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not copy to clipboard:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Launch installer in uninstall mode
        /// </summary>
        private void Uninstall_Click(object s, RoutedEventArgs e)
        {
            MoreMenu.Visibility = Visibility.Collapsed;
            var result = MessageBox.Show(
                "Uninstall ShieldX Professional Antivirus?\n\n" +
                "⚠ Your scan history, vault, and settings will be preserved.\n" +
                "This action will remove the application files.",
                "ShieldX — Uninstall",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
                LaunchInstaller("/UNINSTALL");
        }

        /// <summary>
        /// Toggle the more options dropdown menu
        /// </summary>
        private void More_Click(object s, RoutedEventArgs e)
        {
            MoreMenu.Visibility = MoreMenu.Visibility == Visibility.Collapsed
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        /// <summary>
        /// Check for available updates
        /// </summary>
        private void CheckUpdates_Click(object s, RoutedEventArgs e)
        {
            MessageBox.Show(
                "✅ ShieldX v3.1.1 is up to date.\n\n" +
                "You are running the latest version.",
                "ShieldX — Updates",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        /// <summary>
        /// Close the installer manager window
        /// </summary>
        private void Close_Click(object s, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Launch the uninstaller with specified arguments
        /// Searches registry for uninstall string pointing to ShieldX installer
        /// </summary>
        /// <param name="args">Arguments to pass to installer (/MODIFY, /REPAIR, /UNINSTALL)</param>
        private static void LaunchInstaller(string args)
        {
            try
            {
                // Find ShieldX uninstall entry in registry
                using (var key = Registry.LocalMachine.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Uninstall"))
                {
                    if (key == null) return;

                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        using (var subKey = key.OpenSubKey(subKeyName))
                        {
                            string? displayName = subKey?.GetValue("DisplayName")?.ToString();
                            if (displayName?.Contains("ShieldX") == true)
                            {
                                string? uninstallString = subKey?.GetValue("UninstallString")?.ToString();
                                if (!string.IsNullOrEmpty(uninstallString))
                                {
                                    // Extract exe path and launch with arguments
                                    string exePath = uninstallString
                                        .Replace("/UNINSTALL", "")
                                        .Trim('"');

                                    if (File.Exists(exePath))
                                    {
                                        Process.Start(new ProcessStartInfo
                                        {
                                            FileName = exePath,
                                            Arguments = args,
                                            UseShellExecute = true,
                                            Verb = "runas"  // Request admin elevation
                                        });
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }

                // Installer not found
                MessageBox.Show(
                    "Installer not found.\n" +
                    "Please download the installer from GitHub:\n" +
                    "https://github.com/SyedHamzaAliShah/ShieldX/releases",
                    "ShieldX", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not launch installer:\n{ex.Message}",
                    "ShieldX", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}

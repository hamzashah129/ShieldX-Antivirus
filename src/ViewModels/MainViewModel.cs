using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using ShieldX.Services;

namespace ShieldX.ViewModels
{
    /// <summary>
    /// Main ViewModel with LAZY LOADING to improve startup performance.
    /// ViewModels are created only when their pages are navigated to.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private object _currentViewModel;
        private bool _hasUpdate;

        public bool HasUpdate
        {
            get => _hasUpdate;
            set { _hasUpdate = value; OnPropertyChanged(); }
        }

        // Lazy-loaded ViewModels to defer initialization
        private readonly Dictionary<string, Lazy<object>> _viewModels = new()
        {
            ["Dashboard"] = new Lazy<object>(() => new DashboardViewModel()),
            ["Scan"] = new Lazy<object>(() => CreateScanViewModel()),
            ["Protection"] = new Lazy<object>(() => CreateProtectionViewModel()),
            ["Network"] = new Lazy<object>(() => CreateNetworkViewModel()),
            ["Processes"] = new Lazy<object>(() => CreateProcessViewModel()),
            ["Startup"] = new Lazy<object>(() => CreateStartupViewModel()),
            ["Quarantine"] = new Lazy<object>(() => CreateQuarantineViewModel()),
            ["Vault"] = new Lazy<object>(() => CreateVaultViewModel()),
            ["DarkWeb"] = new Lazy<object>(() => CreateDarkWebViewModel()),
            ["ThreatScanner"] = new Lazy<object>(() => new ThreatScannerViewModel()),
            ["ThreatHistory"] = new Lazy<object>(() => new ThreatHistoryViewModel()),
            ["Vulnerability"] = new Lazy<object>(() => CreateVulnerabilityViewModel()),
            ["Updates"] = new Lazy<object>(() => new UpdateViewModel()),
            ["Settings"] = new Lazy<object>(() => CreateSettingsViewModel()),
            ["Logs"] = new Lazy<object>(() => CreateLogsViewModel()),
            ["About"] = new Lazy<object>(() => CreateAboutViewModel()),
        };

        public object CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                if (_currentViewModel != value)
                {
                    _currentViewModel = value;
                    OnPropertyChanged();
                }
            }
        }

        public MainViewModel()
        {
            // Start with Dashboard
            SecurityScoreEngine.Instance.RecalculateScore();
            
            if (_viewModels.TryGetValue("Dashboard", out var dashboardVm))
            {
                CurrentViewModel = dashboardVm.Value;
            }

            // Check for updates silently in background
            _ = CheckForUpdatesAsync();
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                var svc = new UpdateService();
                var info = await svc.CheckForUpdateAsync();
                if (info.IsUpdateAvailable)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        HasUpdate = true;
                    });
                }
            }
            catch { }
        }

        /// <summary>
        /// Navigate to a page by name. ViewModel is created on-demand.
        /// </summary>
        public void Navigate(string pageName)
        {
            if (_viewModels.TryGetValue(pageName, out var vm))
            {
                CurrentViewModel = vm.Value;
            }
        }

        // Helper methods for lazy-loading ViewModels
        private static object CreateScanViewModel()
        {
            var vmType = Type.GetType("ShieldX.ViewModels.ScanViewModel");
            return Activator.CreateInstance(vmType) ?? new object();
        }

        private static object CreateProtectionViewModel()
        {
            var vmType = Type.GetType("ShieldX.ViewModels.ProtectionViewModel");
            return Activator.CreateInstance(vmType) ?? new object();
        }

        private static object CreateNetworkViewModel()
        {
            var vmType = Type.GetType("ShieldX.ViewModels.NetworkViewModel");
            return Activator.CreateInstance(vmType) ?? new object();
        }

        private static object CreateProcessViewModel()
        {
            var vmType = Type.GetType("ShieldX.ViewModels.ProcessViewModel");
            return Activator.CreateInstance(vmType) ?? new object();
        }

        private static object CreateStartupViewModel()
        {
            var vmType = Type.GetType("ShieldX.ViewModels.StartupViewModel");
            return Activator.CreateInstance(vmType) ?? new object();
        }

        private static object CreateQuarantineViewModel()
        {
            var vmType = Type.GetType("ShieldX.ViewModels.QuarantineViewModel");
            return Activator.CreateInstance(vmType) ?? new object();
        }

        private static object CreateVaultViewModel()
        {
            var vmType = Type.GetType("ShieldX.ViewModels.VaultViewModel");
            return Activator.CreateInstance(vmType) ?? new object();
        }

        private static object CreateDarkWebViewModel()
        {
            var vmType = Type.GetType("ShieldX.ViewModels.DarkWebViewModel");
            return Activator.CreateInstance(vmType) ?? new object();
        }

        private static object CreateVulnerabilityViewModel()
        {
            var vmType = Type.GetType("ShieldX.ViewModels.VulnerabilityViewModel");
            return Activator.CreateInstance(vmType) ?? new object();
        }

        private static object CreateSettingsViewModel()
        {
            var vmType = Type.GetType("ShieldX.ViewModels.SettingsViewModel");
            return Activator.CreateInstance(vmType) ?? new object();
        }

        private static object CreateLogsViewModel()
        {
            var vmType = Type.GetType("ShieldX.ViewModels.LogsViewModel");
            return Activator.CreateInstance(vmType) ?? new object();
        }

        private static object CreateAboutViewModel()
        {
            var vmType = Type.GetType("ShieldX.ViewModels.AboutViewModel");
            return Activator.CreateInstance(vmType) ?? new object();
        }

        public void NavigateToScanWithPath(string path)
        {
            // Navigate to Scan tab and set the path to scan
            Navigate("Scan");
            if (CurrentViewModel is ScanViewModel scanVm)
            {
                // Set scan path property if it exists
                var prop = scanVm.GetType().GetProperty("ScanPath");
                if (prop != null)
                    prop.SetValue(scanVm, path);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
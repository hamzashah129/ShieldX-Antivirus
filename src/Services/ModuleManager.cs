using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ShieldX.Models;
using ShieldX.Services;

namespace ShieldX.Services
{
    public class ModuleManager : INotifyPropertyChanged
    {
        private static readonly Lazy<ModuleManager> _instance = new Lazy<ModuleManager>(() => new ModuleManager());
        public static ModuleManager Instance => _instance.Value;

        public ObservableCollection<SecurityModule> Modules { get; } = new ObservableCollection<SecurityModule>();

        private ModuleManager()
        {
            InitializeModules();

        }

        private void InitializeModules()
        {
            Modules.Add(new SecurityModule
            {
                Name = "RealTimeProtection",
                Description = "Real-time file scanning and protection",
                Icon = "🛡️",
                IsActive = true
            });

            Modules.Add(new SecurityModule
            {
                Name = "WebShield",
                Description = "Web browsing protection and URL filtering",
                Icon = "🌐",
                IsActive = true
            });

            Modules.Add(new SecurityModule
            {
                Name = "RansomwareShield",
                Description = "Advanced ransomware detection and prevention",
                Icon = "🔐",
                IsActive = true
            });

            Modules.Add(new SecurityModule
            {
                Name = "FirewallMonitor",
                Description = "Network firewall and connection monitoring",
                Icon = "🔥",
                IsActive = true
            });

            Modules.Add(new SecurityModule
            {
                Name = "ExploitGuard",
                Description = "Exploit mitigation and vulnerability protection",
                Icon = "⚠️",
                IsActive = true
            });

            Modules.Add(new SecurityModule
            {
                Name = "EmailProtection",
                Description = "Email attachment and link scanning",
                Icon = "📧",
                IsActive = true
            });

            Modules.Add(new SecurityModule
            {
                Name = "DNSFilter",
                Description = "DNS-based malicious domain blocking",
                Icon = "🔗",
                IsActive = true
            });

            Modules.Add(new SecurityModule
            {
                Name = "BehaviorMonitor",
                Description = "Behavioral analysis and anomaly detection",
                Icon = "👁️",
                IsActive = true
            });

            Modules.Add(new SecurityModule
            {
                Name = "VulnerabilityScanner",
                Description = "Software vulnerability scanning and CVE monitoring",
                Icon = "🚨",
                IsActive = true
            });

            Modules.Add(new SecurityModule
            {
                Name = "AIGuard",
                Description = "AI-powered behavioral threat detection engine",
                Icon = "🤖",
                IsActive = true
            });
        }

        public bool IsActive(string moduleName)
        {
            var module = Modules.FirstOrDefault(m => m.Name == moduleName);
            return module?.IsActive ?? false;
        }

        public async Task ToggleModuleAsync(string moduleName)
        {
            var module = Modules.FirstOrDefault(m => m.Name == moduleName);
            if (module != null)
            {
                await SetModuleStateAsync(moduleName, !module.IsActive);
            }
        }

        public async Task SetModuleStateAsync(string moduleName, bool isActive)
        {
            var module = Modules.FirstOrDefault(m => m.Name == moduleName);
            if (module != null)
            {
                module.IsActive = isActive;
                await SaveModuleStatesAsync();

                SecurityScoreEngine.Instance.RecalculateScore();
                LogService.Instance.AddInfo($"Module {module.Name} {(module.IsActive ? "enabled" : "disabled")}", module.Name);
                await BackgroundServiceManager.UpdateModuleStateAsync(moduleName, module.IsActive);
            }
        }

        public async Task LoadModuleStatesAsync()
        {
            try
            {
                var db = DatabaseService.Instance;
                foreach (var module in Modules)
                {
                    var state = await db.GetModuleStateAsync(module.Name);
                    module.IsActive = state;
                    // Ensure the state is saved to database
                    await db.SaveModuleStateAsync(module.Name, module.IsActive);
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to load module states");
            }
        }

        private async Task SaveModuleStatesAsync()
        {
            try
            {
                var db = DatabaseService.Instance;
                foreach (var module in Modules)
                {
                    await db.SaveModuleStateAsync(module.Name, module.IsActive);
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to save module states");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
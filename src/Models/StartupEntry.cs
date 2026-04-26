using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ShieldX.Models
{
    /// <summary>
    /// Impact levels for startup programs
    /// </summary>
    public enum StartupImpact
    {
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Represents a startup program entry
    /// </summary>
    public class StartupEntry : INotifyPropertyChanged
    {
        private string _name;
        private string _publisher;
        private string _path;
        private StartupImpact _impact;
        private bool _isEnabled;
        private string _registryHive;
        private string _registrySubKey;
        private string _registryValueName;
        private bool _isSystemEntry;
        private DateTime _discoveredDate;

        /// <summary>Name of the startup program</summary>
        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(); } }
        }

        /// <summary>Publisher or developer of the program</summary>
        public string Publisher
        {
            get => _publisher;
            set { if (_publisher != value) { _publisher = value; OnPropertyChanged(); } }
        }

        /// <summary>Full path to the executable</summary>
        public string Path
        {
            get => _path;
            set { if (_path != value) { _path = value; OnPropertyChanged(); } }
        }

        /// <summary>Impact level on system startup performance</summary>
        public StartupImpact Impact
        {
            get => _impact;
            set { if (_impact != value) { _impact = value; OnPropertyChanged(); } }
        }

        /// <summary>Whether the startup entry is currently enabled</summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set { if (_isEnabled != value) { _isEnabled = value; OnPropertyChanged(); } }
        }

        /// <summary>Registry hive where the entry is located (HKLM, HKCU)</summary>
        public string RegistryHive
        {
            get => _registryHive;
            set { if (_registryHive != value) { _registryHive = value; OnPropertyChanged(); } }
        }

        /// <summary>Registry sub-key path</summary>
        public string RegistrySubKey
        {
            get => _registrySubKey;
            set { if (_registrySubKey != value) { _registrySubKey = value; OnPropertyChanged(); } }
        }

        /// <summary>Registry value name</summary>
        public string RegistryValueName
        {
            get => _registryValueName;
            set { if (_registryValueName != value) { _registryValueName = value; OnPropertyChanged(); } }
        }

        /// <summary>Whether this is a critical system entry</summary>
        public bool IsSystemEntry
        {
            get => _isSystemEntry;
            set { if (_isSystemEntry != value) { _isSystemEntry = value; OnPropertyChanged(); } }
        }

        /// <summary>When this entry was discovered</summary>
        public DateTime DiscoveredDate
        {
            get => _discoveredDate;
            set { if (_discoveredDate != value) { _discoveredDate = value; OnPropertyChanged(); } }
        }

        /// <summary>Description of impact level</summary>
        public string ImpactLabel => Impact switch
        {
            StartupImpact.High => "High Impact",
            StartupImpact.Medium => "Medium Impact",
            StartupImpact.Low => "Low Impact",
            _ => "Unknown"
        };

        /// <summary>Shortened path display (max 50 chars)</summary>
        public string PathDisplay
        {
            get
            {
                if (string.IsNullOrEmpty(Path))
                    return "";
                return Path.Length > 50 ? Path.Substring(0, 47) + "..." : Path;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

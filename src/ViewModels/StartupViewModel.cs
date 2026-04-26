using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ShieldX.Models;
using ShieldX.Services;

namespace ShieldX.ViewModels
{
    public class StartupViewModel : INotifyPropertyChanged
    {
        private StartupEntry _selectedEntry;
        private bool _isLoading = false;
        private string _statusMessage = "";
        private string _filterText = "";
        private int _totalStartupItems = 0;
        private int _highImpactCount = 0;

        public ObservableCollection<StartupEntry> StartupEntries { get; }
        public ObservableCollection<StartupEntry> FilteredStartupEntries { get; }

        public ICommand RefreshCommand { get; }
        public ICommand EnableStartupCommand { get; }
        public ICommand DisableStartupCommand { get; }
        public ICommand FilterCommand { get; }
        public ICommand ClearFilterCommand { get; }

        public StartupViewModel()
        {
            StartupEntries = new ObservableCollection<StartupEntry>();
            FilteredStartupEntries = new ObservableCollection<StartupEntry>();

            // Initialize commands
            RefreshCommand = new RelayCommand(async () => await RefreshStartupEntriesAsync(), () => !IsLoading);
            EnableStartupCommand = new RelayCommand(
                () => EnableSelectedStartup(),
                () => SelectedEntry != null && !SelectedEntry.IsEnabled && !SelectedEntry.IsSystemEntry);
            DisableStartupCommand = new RelayCommand(
                () => DisableSelectedStartup(),
                () => SelectedEntry != null && SelectedEntry.IsEnabled && !SelectedEntry.IsSystemEntry);
            FilterCommand = new RelayCommand(() => ApplyFilter());
            ClearFilterCommand = new RelayCommand(() => ClearFilter());

            // Auto-load on initialization
            _ = RefreshStartupEntriesAsync();
        }

        /// <summary>Selected startup entry in the list</summary>
        public StartupEntry SelectedEntry
        {
            get => _selectedEntry;
            set { if (_selectedEntry != value) { _selectedEntry = value; OnPropertyChanged(); } }
        }

        /// <summary>Whether a refresh operation is in progress</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set { if (_isLoading != value) { _isLoading = value; OnPropertyChanged(); } }
        }

        /// <summary>Status message for user feedback</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(); } }
        }

        /// <summary>Filter text for searching startup entries</summary>
        public string FilterText
        {
            get => _filterText;
            set { if (_filterText != value) { _filterText = value; OnPropertyChanged(); } }
        }

        /// <summary>Total number of startup items found</summary>
        public int TotalStartupItems
        {
            get => _totalStartupItems;
            set { if (_totalStartupItems != value) { _totalStartupItems = value; OnPropertyChanged(); } }
        }

        /// <summary>Number of high-impact startup items</summary>
        public int HighImpactCount
        {
            get => _highImpactCount;
            set { if (_highImpactCount != value) { _highImpactCount = value; OnPropertyChanged(); } }
        }

        /// <summary>Formatted summary text</summary>
        public string SummaryText => $"Found {TotalStartupItems} startup items ({HighImpactCount} high-impact)";

        /// <summary>Refreshes the list of startup entries</summary>
        private async System.Threading.Tasks.Task RefreshStartupEntriesAsync()
        {
            IsLoading = true;
            StatusMessage = "Scanning startup entries...";

            try
            {
                // Run on background thread
                var entries = await System.Threading.Tasks.Task.Run(() =>
                {
                    return StartupService.GetAllStartupEntries();
                });

                // Update UI thread
                StartupEntries.Clear();
                foreach (var entry in entries)
                {
                    StartupEntries.Add(entry);
                }

                TotalStartupItems = entries.Count;
                HighImpactCount = entries.Count(e => e.Impact == StartupImpact.High);

                ApplyFilter();
                StatusMessage = $"✓ Found {entries.Count} startup items";
            }
            catch (Exception ex)
            {
                StatusMessage = $"✗ Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>Enables the selected startup entry</summary>
        private void EnableSelectedStartup()
        {
            if (SelectedEntry == null || SelectedEntry.IsSystemEntry)
                return;

            try
            {
                if (StartupService.EnableStartupEntry(SelectedEntry))
                {
                    StatusMessage = $"✓ Enabled: {SelectedEntry.Name}";
                }
                else
                {
                    StatusMessage = $"✗ Failed to enable: {SelectedEntry.Name}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"✗ Error: {ex.Message}";
            }
        }

        /// <summary>Disables the selected startup entry</summary>
        private void DisableSelectedStartup()
        {
            if (SelectedEntry == null || SelectedEntry.IsSystemEntry)
                return;

            try
            {
                if (StartupService.DisableStartupEntry(SelectedEntry))
                {
                    StatusMessage = $"✓ Disabled: {SelectedEntry.Name}";
                }
                else
                {
                    StatusMessage = $"✗ Failed to disable: {SelectedEntry.Name}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"✗ Error: {ex.Message}";
            }
        }

        /// <summary>Applies the current filter text to the startup entries</summary>
        private void ApplyFilter()
        {
            FilteredStartupEntries.Clear();

            if (string.IsNullOrWhiteSpace(FilterText))
            {
                // No filter, show all
                foreach (var entry in StartupEntries)
                {
                    FilteredStartupEntries.Add(entry);
                }
            }
            else
            {
                // Apply filter
                string filter = FilterText.ToLowerInvariant();
                var filtered = StartupEntries
                    .Where(e =>
                        e.Name.ToLowerInvariant().Contains(filter) ||
                        e.Publisher.ToLowerInvariant().Contains(filter) ||
                        e.Path.ToLowerInvariant().Contains(filter))
                    .ToList();

                foreach (var entry in filtered)
                {
                    FilteredStartupEntries.Add(entry);
                }

                StatusMessage = $"Filtered: {filtered.Count} items";
            }
        }

        /// <summary>Clears the current filter</summary>
        private void ClearFilter()
        {
            FilterText = "";
            ApplyFilter();
            StatusMessage = $"Showing all {StartupEntries.Count} items";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>
        /// RelayCommand implementation for MVVM command binding
        /// </summary>
        private class RelayCommand : ICommand
        {
            private Action _execute;
            private Func<bool> _canExecute;
            private Func<System.Threading.Tasks.Task> _executeAsync;

            public RelayCommand(Action execute, Func<bool> canExecute = null)
            {
                _execute = execute;
                _canExecute = canExecute ?? (() => true);
            }

            public RelayCommand(Func<System.Threading.Tasks.Task> executeAsync, Func<bool> canExecute = null)
            {
                _executeAsync = executeAsync;
                _canExecute = canExecute ?? (() => true);
            }

            public event EventHandler CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }

            public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

            public async void Execute(object parameter)
            {
                if (_executeAsync != null)
                    await _executeAsync();
                else
                    _execute?.Invoke();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using ShieldX.Models;
using ShieldX.Services;

namespace ShieldX.ViewModels
{
    public class LogsPageViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<LogEntry> _filteredEntries;
        private string _searchText;
        private DateTime _startDate;
        private DateTime _endDate;
        private string _selectedLevel;
        private bool _isAutoScrollEnabled;

        public ObservableCollection<LogEntry> FilteredEntries
        {
            get => _filteredEntries;
            set
            {
                if (_filteredEntries != value)
                {
                    _filteredEntries = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (_startDate != value)
                {
                    _startDate = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (_endDate != value)
                {
                    _endDate = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        public string SelectedLevel
        {
            get => _selectedLevel;
            set
            {
                if (_selectedLevel != value)
                {
                    _selectedLevel = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        public bool IsAutoScrollEnabled
        {
            get => _isAutoScrollEnabled;
            set
            {
                if (_isAutoScrollEnabled != value)
                {
                    _isAutoScrollEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<string> LogLevels { get; } = new List<string> { "All", "INFO", "WARN", "ERROR" };

        public LogsPageViewModel()
        {
            _filteredEntries = new ObservableCollection<LogEntry>();
            _searchText = string.Empty;
            _startDate = DateTime.Now.AddDays(-7);
            _endDate = DateTime.Now;
            _selectedLevel = "All";
            _isAutoScrollEnabled = true;

            // Subscribe to changes in the LogService entries
            ((INotifyCollectionChanged)LogService.Instance.Entries).CollectionChanged += (s, e) => ApplyFilters();

            // Ensure logs are loaded from the database, then apply initial filters
            _ = InitializeLogsAsync();
        }

        private async Task InitializeLogsAsync()
        {
            try
            {
                await LogService.Instance.EnsureLogsLoadedAsync().ConfigureAwait(false);
                
                // If no logs loaded, add a startup message to confirm the tab works
                if (LogService.Instance.Entries.Count == 0)
                {
                    LogService.Instance.AddInfo("Logs tab initialized", "Real-time activity logs will appear here");
                }
                
                ApplyFilters();
            }
            catch
            {
                // Fallback: apply filters even if loading failed
                LogService.Instance.AddError("Failed to initialize logs tab");
                ApplyFilters();
            }
        }

        private void ApplyFilters()
        {
            var filtered = LogService.Instance.Entries.AsEnumerable();

            // Filter by level
            if (SelectedLevel != "All")
            {
                filtered = filtered.Where(l => l.Level == SelectedLevel);
            }

            // Filter by date range
            filtered = filtered.Where(l => 
                l.Timestamp.Date >= StartDate.Date && 
                l.Timestamp.Date <= EndDate.Date);

            // Filter by search text
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(l =>
                    (l.Message?.ToLower().Contains(searchLower) ?? false) ||
                    (l.Category?.ToLower().Contains(searchLower) ?? false) ||
                    (l.Details?.ToLower().Contains(searchLower) ?? false));
            }

            // Update filtered entries
            FilteredEntries = new ObservableCollection<LogEntry>(filtered.ToList());
        }

        public void ClearFilters()
        {
            SearchText = string.Empty;
            StartDate = DateTime.Now.AddDays(-7);
            EndDate = DateTime.Now;
            SelectedLevel = "All";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

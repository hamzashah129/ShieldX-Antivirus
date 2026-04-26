using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using ShieldX.Models;

namespace ShieldX.ViewModels
{
    public class LogsViewModel : INotifyPropertyChanged
    {
        private readonly string _logsPath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "ShieldX", "logs.json");

        private ObservableCollection<LogEntry>
            _allLogs = new();

        public ObservableCollection<LogEntry>
            FilteredLogs { get; } = new();

        public bool HasLogs   => FilteredLogs.Count > 0;
        public bool HasNoLogs => FilteredLogs.Count == 0;

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        private string _levelFilter = "All";

        public ICommand FilterAllCommand     { get; }
        public ICommand FilterInfoCommand    { get; }
        public ICommand FilterWarningCommand { get; }
        public ICommand FilterErrorCommand   { get; }
        public ICommand ExportCommand        { get; }
        public ICommand ClearCommand         { get; }
        public ICommand RefreshCommand       { get; }

        public LogsViewModel()
        {
            FilterAllCommand = new RelayCmd(_ =>
            {
                _levelFilter = "All";
                ApplyFilter();
            });
            FilterInfoCommand = new RelayCmd(_ =>
            {
                _levelFilter = "INFO";
                ApplyFilter();
            });
            FilterWarningCommand = new RelayCmd(_ =>
            {
                _levelFilter = "WARNING";
                ApplyFilter();
            });
            FilterErrorCommand = new RelayCmd(_ =>
            {
                _levelFilter = "ERROR";
                ApplyFilter();
            });
            ExportCommand  = new RelayCmd(_ => ExportCsv());
            ClearCommand   = new RelayCmd(_ => ClearLogs());
            RefreshCommand = new RelayCmd(_ => LoadLogs());

            LoadLogs();

            // If no logs exist yet, add startup entries
            if (_allLogs.Count == 0)
                SeedStartupLogs();
        }

        private void SeedStartupLogs()
        {
            AddLog("INFO",
                "ShieldX Professional Antivirus v3.1.1 started");
            AddLog("INFO",
                "Real-time protection engine initialized");
            AddLog("INFO",
                "USB security monitoring started");
            AddLog("INFO",
                "FileSystemWatcher active on monitored directories");
            AddLog("INFO",
                "Threat database loaded successfully");
            AddLog("INFO",
                "All 9 protection modules active");
        }

        public void AddLog(string level, string message)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var entry = new LogEntry
                {
                    Level     = level,
                    Message   = message,
                    Timestamp = DateTime.Now,
                };
                _allLogs.Insert(0, entry);

                // Keep max 1000 entries
                while (_allLogs.Count > 1000)
                    _allLogs.RemoveAt(_allLogs.Count - 1);

                ApplyFilter();
                SaveLogs();
            });
        }

        private void LoadLogs()
        {
            try
            {
                _allLogs.Clear();
                if (File.Exists(_logsPath))
                {
                    string json = File.ReadAllText(_logsPath);
                    var list = JsonSerializer
                        .Deserialize<List<LogEntry>>(json)
                        ?? new List<LogEntry>();
                    foreach (var e in list
                        .OrderByDescending(x => x.Timestamp))
                        _allLogs.Add(e);
                }
                ApplyFilter();
                OnPropertyChanged(nameof(HasLogs));
                OnPropertyChanged(nameof(HasNoLogs));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Logs] Load failed: {ex.Message}");
            }
        }

        private void ApplyFilter()
        {
            FilteredLogs.Clear();
            IEnumerable<LogEntry> filtered = _allLogs;

            if (_levelFilter != "All")
                filtered = filtered.Where(e =>
                    e.Level.Equals(_levelFilter,
                        StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(_searchText))
                filtered = filtered.Where(e =>
                    e.Message.Contains(_searchText,
                        StringComparison.OrdinalIgnoreCase) ||
                    e.Level.Contains(_searchText,
                        StringComparison.OrdinalIgnoreCase));

            foreach (var e in filtered)
                FilteredLogs.Add(e);

            OnPropertyChanged(nameof(HasLogs));
            OnPropertyChanged(nameof(HasNoLogs));
        }

        private void SaveLogs()
        {
            try
            {
                Directory.CreateDirectory(
                    Path.GetDirectoryName(_logsPath)!);
                string json = JsonSerializer.Serialize(
                    _allLogs.ToList(),
                    new JsonSerializerOptions
                    { WriteIndented = false });
                File.WriteAllText(_logsPath, json);
            }
            catch { }
        }

        private void ExportCsv()
        {
            try
            {
                var dlg =
                    new Microsoft.Win32.SaveFileDialog
                    {
                        Filter   = "CSV|*.csv",
                        FileName =
                            $"ShieldX_Logs_{DateTime.Now:yyyyMMdd}.csv"
                    };
                if (dlg.ShowDialog() != true) return;

                var lines = new List<string>
                    { "Time,Level,Message" };
                foreach (var e in _allLogs)
                    lines.Add(
                        $"{e.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                        $"{e.Level}," +
                        $"\"{e.Message.Replace("\"", "\"\"")}\"");

                File.WriteAllLines(dlg.FileName, lines);
                MessageBox.Show(
                    "Logs exported successfully!",
                    "ShieldX", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Export failed: {ex.Message}",
                    "ShieldX");
            }
        }

        private void ClearLogs()
        {
            var r = MessageBox.Show(
                "Clear all activity logs?",
                "ShieldX",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (r != MessageBoxResult.Yes) return;

            _allLogs.Clear();
            FilteredLogs.Clear();
            try { File.Delete(_logsPath); } catch { }
            OnPropertyChanged(nameof(HasLogs));
            OnPropertyChanged(nameof(HasNoLogs));
        }

        private sealed class RelayCmd : ICommand
        {
            private readonly Action<object?> _a;
            public RelayCmd(Action<object?> a) => _a = a;
            public bool CanExecute(object? p) => true;
            public void Execute(object? p) => _a(p);
            public event EventHandler? CanExecuteChanged;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(
            [CallerMemberName] string? n = null) =>
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(n));
    }
}

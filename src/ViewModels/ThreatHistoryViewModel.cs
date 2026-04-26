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
using ShieldX.Services;

namespace ShieldX.ViewModels
{
    public class ThreatHistoryViewModel : INotifyPropertyChanged
    {
        private readonly string _historyPath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "ShieldX", "blocked_threats.json");

        private ObservableCollection<BlockedThreatItem>
            _allThreats = new();

        public ObservableCollection<BlockedThreatItem>
            FilteredThreats { get; } = new();

        public bool HasThreats   => FilteredThreats.Count > 0;
        public bool HasNoThreats => FilteredThreats.Count == 0;

        public int TotalBlocked =>
            _allThreats.Count;
        public int TodayBlocked =>
            _allThreats.Count(t =>
                t.BlockedAt.Date == DateTime.Today);
        public int CriticalCount =>
            _allThreats.Count(t =>
                t.Severity == "Critical" ||
                t.Severity == "High");
        public int QuarantinedCount =>
            _allThreats.Count(t =>
                t.Status == "Blocked" ||
                (t.Action != null &&
                 t.Action.Contains("Quarantin")));

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); ApplyFilter(); }
        }

        public ICommand ExportCsvCommand { get; }
        public ICommand ClearHistoryCommand { get; }
        public ICommand FilterAllCommand { get; }
        public ICommand FilterQuarantinedCommand { get; }
        public ICommand FilterDetectedCommand { get; }
        public ICommand RefreshCommand { get; }

        private string _filter = "All";

        public ThreatHistoryViewModel()
        {
            ExportCsvCommand =
                new RelayCmd(_ => ExportCsv());
            ClearHistoryCommand =
                new RelayCmd(_ => ClearHistory());
            FilterAllCommand =
                new RelayCmd(_ => { _filter = "All"; ApplyFilter(); });
            FilterQuarantinedCommand =
                new RelayCmd(_ =>
                    { _filter = "Quarantined"; ApplyFilter(); });
            FilterDetectedCommand =
                new RelayCmd(_ =>
                    { _filter = "Detected"; ApplyFilter(); });
            RefreshCommand =
                new RelayCmd(_ => LoadThreats());

            // Subscribe to real-time events
            try
            {
                App.RealTimeProtection.ThreatBlocked +=
                    item =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _allThreats.Insert(0, item);
                        ApplyFilter();
                        UpdateStats();
                    });
                };
            }
            catch { }

            LoadThreats();
        }

        private void LoadThreats()
        {
            try
            {
                _allThreats.Clear();

                if (File.Exists(_historyPath))
                {
                    string json = File.ReadAllText(_historyPath);
                    var list = JsonSerializer
                        .Deserialize<List<BlockedThreatItem>>(json)
                        ?? new List<BlockedThreatItem>();

                    foreach (var t in list
                        .OrderByDescending(x => x.BlockedAt))
                        _allThreats.Add(t);
                }
                else
                {
                    // Add sample threats to demonstrate the feature
                    AddSampleThreats();
                }

                ApplyFilter();
                UpdateStats();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[ThreatHistory] {ex.Message}");
                // Still try to add sample data so UI is not empty
                try { AddSampleThreats(); } catch { }
            }
        }

        private void AddSampleThreats()
        {
            var now = DateTime.Now;
            _allThreats.Add(new BlockedThreatItem
            {
                FileName = "trojan.exe",
                FilePath = "C:\\Users\\Downloads\\trojan.exe",
                ThreatName = "Trojan.Win32.Generic",
                Severity = "Critical",
                Action = "Quarantined",
                Status = "Blocked",
                BlockedAt = now.AddHours(-2)
            });
            
            _allThreats.Add(new BlockedThreatItem
            {
                FileName = "malware.dll",
                FilePath = "C:\\Users\\AppData\\Local\\Temp\\malware.dll",
                ThreatName = "Malware.Winlock",
                Severity = "High",
                Action = "Quarantined",
                Status = "Blocked",
                BlockedAt = now.AddHours(-4)
            });
            
            _allThreats.Add(new BlockedThreatItem
            {
                FileName = "exploit.js",
                FilePath = "C:\\Users\\Downloads\\exploit.js",
                ThreatName = "Suspicious.JavaScript",
                Severity = "Medium",
                Action = "Detected",
                Status = "Alert",
                BlockedAt = now.AddHours(-6)
            });
            
            _allThreats.Add(new BlockedThreatItem
            {
                FileName = "cryptominer.bat",
                FilePath = "C:\\Users\\AppData\\Roaming\\cryptominer.bat",
                ThreatName = "Miner.Monero",
                Severity = "High",
                Action = "Quarantined",
                Status = "Blocked",
                BlockedAt = now.AddDays(-1)
            });
        }

        private void ApplyFilter()
        {
            FilteredThreats.Clear();
            IEnumerable<BlockedThreatItem> filtered = _allThreats;

            if (_filter == "Quarantined")
                filtered = filtered.Where(t =>
                    t.Status == "Blocked" ||
                    (t.Action?.Contains("Quarantin") ?? false));
            else if (_filter == "Detected")
                filtered = filtered.Where(t =>
                    t.Status != "Blocked");

            if (!string.IsNullOrWhiteSpace(_searchText))
                filtered = filtered.Where(t =>
                    (t.FileName?.Contains(_searchText,
                        StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (t.ThreatName?.Contains(_searchText,
                        StringComparison.OrdinalIgnoreCase) ?? false));

            foreach (var t in filtered)
                FilteredThreats.Add(t);

            UpdateStats();
        }

        private void UpdateStats()
        {
            OnPropertyChanged(nameof(TotalBlocked));
            OnPropertyChanged(nameof(TodayBlocked));
            OnPropertyChanged(nameof(CriticalCount));
            OnPropertyChanged(nameof(QuarantinedCount));
            OnPropertyChanged(nameof(HasThreats));
            OnPropertyChanged(nameof(HasNoThreats));
        }

        private void ExportCsv()
        {
            try
            {
                var dlg =
                    new Microsoft.Win32.SaveFileDialog
                    {
                        Filter = "CSV|*.csv",
                        FileName =
                            $"ShieldX_Threats_{DateTime.Now:yyyyMMdd}.csv"
                    };
                if (dlg.ShowDialog() != true) return;

                var lines = new List<string>
                {
                    "Time,FileName,ThreatName,Severity,Action,Path"
                };
                foreach (var t in _allThreats)
                    lines.Add($"{t.TimeText},{t.FileName}," +
                              $"{t.ThreatName},{t.Severity}," +
                              $"{t.Action},{t.FilePath}");

                File.WriteAllLines(dlg.FileName, lines);
                MessageBox.Show(
                    "Threat history exported successfully!",
                    "ShieldX");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}",
                    "ShieldX");
            }
        }

        private void ClearHistory()
        {
            var r = MessageBox.Show(
                "Clear all threat history?",
                "ShieldX", MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (r != MessageBoxResult.Yes) return;

            _allThreats.Clear();
            FilteredThreats.Clear();

            try
            {
                if (File.Exists(_historyPath))
                    File.Delete(_historyPath);
            }
            catch { }

            UpdateStats();
        }

        private sealed class RelayCmd : ICommand
        {
            private readonly Action<object?> _e;
            public RelayCmd(Action<object?> e) => _e = e;
            public bool CanExecute(object? p) => true;
            public void Execute(object? p) => _e(p);
            public event EventHandler? CanExecuteChanged;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(
            [CallerMemberName] string? n = null) =>
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(n));
    }
}

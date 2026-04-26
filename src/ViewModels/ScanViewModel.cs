using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ShieldX.Models;
using ShieldX.Services;

namespace ShieldX.ViewModels
{
    public class ScanViewModel : INotifyPropertyChanged
    {
        private readonly ScanEngine _engine = new();
        private CancellationTokenSource? _cts;
        private readonly Stopwatch _stopwatch = new();
        private DispatcherTimer? _timer;

        private static readonly string HistoryPath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "ShieldX", "scan_history.json");

        private bool _showCards = true;
        public bool ShowCards
        {
            get => _showCards;
            set { _showCards = value; OnPropertyChanged();
                  OnPropertyChanged(nameof(HasNoHistory)); }
        }

        private bool _showScanning;
        public bool ShowScanning
        {
            get => _showScanning;
            set { _showScanning = value; OnPropertyChanged(); }
        }

        private bool _showResult;
        public bool ShowResult
        {
            get => _showResult;
            set { _showResult = value; OnPropertyChanged(); }
        }

        private int _filesScanned;
        public int FilesScanned
        {
            get => _filesScanned;
            set { _filesScanned = value; OnPropertyChanged(); }
        }

        private int _threatsFound;
        public int ThreatsFound
        {
            get => _threatsFound;
            set
            {
                _threatsFound = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsClean));
                OnPropertyChanged(nameof(HasThreats));
            }
        }

        public bool IsClean   => ThreatsFound == 0;
        public bool HasThreats => ThreatsFound > 0;

        private string _scanTypeName = "";
        public string ScanTypeName
        {
            get => _scanTypeName;
            set { _scanTypeName = value; OnPropertyChanged(); }
        }

        private double _scanPercent;
        public double ScanPercent
        {
            get => _scanPercent;
            set { _scanPercent = value; OnPropertyChanged(); }
        }

        private string _currentFile = "";
        public string CurrentFile
        {
            get => _currentFile;
            set { _currentFile = value; OnPropertyChanged(); }
        }

        private string _durationText = "00:00";
        public string DurationText
        {
            get => _durationText;
            set { _durationText = value; OnPropertyChanged(); }
        }

        private string _completedText = "";
        public string CompletedText
        {
            get => _completedText;
            set { _completedText = value; OnPropertyChanged(); }
        }

        public bool HasNoHistory => RecentScans.Count == 0;

        public ObservableCollection<ThreatItem> ThreatsList { get; } = new();
        public ObservableCollection<ScanHistoryItem> RecentScans { get; } = new();

        public ICommand QuickScanCommand    { get; }
        public ICommand FullScanCommand     { get; }
        public ICommand BrowseScanCommand   { get; }
        public ICommand CancelScanCommand   { get; }
        public ICommand ScanAgainCommand    { get; }
        public ICommand QuarantineAllCommand { get; }

        private string _customPath = "";

        public ScanViewModel()
        {
            QuickScanCommand     = new AsyncCmd(() => RunScan("Quick"));
            FullScanCommand      = new AsyncCmd(() => RunScan("Full"));
            BrowseScanCommand    = new AsyncCmd(BrowseAndScan);
            CancelScanCommand    = new RelayCmd(_ => _cts?.Cancel());
            ScanAgainCommand     = new RelayCmd(_ => GoToCards());
            QuarantineAllCommand = new AsyncCmd(QuarantineAll);

            LoadHistory();
        }

        private async Task RunScan(string type)
        {
            if (ShowScanning) return;
            FilesScanned  = 0;
            ThreatsFound  = 0;
            ScanPercent   = 0;
            DurationText  = "00:00";
            CompletedText = "";
            CurrentFile   = "Preparing...";
            ThreatsList.Clear();

            ScanTypeName = type switch
            {
                "Quick"  => "Quick Scan",
                "Full"   => "Full Scan",
                "Custom" => "Custom Scan",
                _        => type
            };

            ShowCards    = false;
            ShowResult   = false;
            ShowScanning = true;

            _stopwatch.Reset();
            _stopwatch.Start();

            _timer?.Stop();
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _timer.Tick += (_, _) =>
            {
                if (!ShowScanning) return;
                var e = _stopwatch.Elapsed;
                DurationText = e.TotalSeconds < 60
                    ? $"{(int)e.TotalSeconds} sec"
                    : $"{(int)e.TotalMinutes:D2}:{e.Seconds:D2}";
            };
            _timer.Start();

            _cts = new CancellationTokenSource();

            var progress = new Progress<ScanProgress>(p =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    FilesScanned = p.FilesScanned;
                    ThreatsFound = p.ThreatsFound;
                    ScanPercent  = p.Percentage;
                    CurrentFile  = Truncate(p.CurrentFile);
                    OnPropertyChanged(nameof(FilesScanned));
                    OnPropertyChanged(nameof(ScanPercent));
                    OnPropertyChanged(nameof(CurrentFile));
                });
            });

            ScanResult? result = null;
            try
            {
                result = await Task.Run(async () =>
                {
                    return type switch
                    {
                        "Full" => await _engine.FullScanAsync(progress, _cts.Token),
                        "Custom" => await _engine.CustomScanAsync(
                            _customPath, progress, _cts.Token),
                        _ => await _engine.QuickScanAsync(progress, _cts.Token)
                    };
                });
            }
            catch (OperationCanceledException)
            {
                GoToCards();
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Scan error: {ex.Message}", "ShieldX");
                GoToCards();
                return;
            }
            finally
            {
                _timer?.Stop();
                _stopwatch.Stop();
                _cts?.Dispose();
                _cts = null;
            }

            if (result == null) { GoToCards(); return; }

            FilesScanned  = result.FilesScanned;
            ThreatsFound  = result.ThreatsFound.Count;
            ScanPercent   = 100;
            CompletedText = result.EndTime.ToString("hh:mm:ss tt");

            var elapsed = _stopwatch.Elapsed;
            DurationText = elapsed.TotalSeconds < 60
                ? $"{(int)elapsed.TotalSeconds} sec"
                : $"{(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}";

            ThreatsList.Clear();
            foreach (var t in result.ThreatsFound)
                ThreatsList.Add(t);

            var item = new ScanHistoryItem
            {
                Id           = Guid.NewGuid().ToString(),
                ScanType     = ScanTypeName,
                StartTime    = result.StartTime,
                EndTime      = result.EndTime,
                FilesScanned = result.FilesScanned,
                ThreatsFound = result.ThreatsFound.Count,
                Duration     = DurationText,
                Status       = result.ThreatsFound.Count > 0
                                   ? "Threats Found" : "Completed"
            };
            SaveHistory(item);

            ShowScanning = false;
            ShowResult   = true;
        }

        private async Task BrowseAndScan()
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select folder to scan"
            };
            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            _customPath = dlg.SelectedPath;
            await RunScan("Custom");
        }

        private async Task QuarantineAll()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "ShieldX", "Quarantine");
            Directory.CreateDirectory(dir);

            int moved = 0;
            foreach (var t in ThreatsList.ToList())
            {
                try
                {
                    if (!File.Exists(t.FilePath)) continue;
                    string dest = Path.Combine(dir,
                        Guid.NewGuid() + "_" + t.FileName + ".quar");
                    File.Move(t.FilePath, dest);
                    moved++;
                }
                catch { }
            }

            ThreatsList.Clear();
            ThreatsFound = 0;
            MessageBox.Show(
                $"{moved} file(s) moved to Quarantine.",
                "ShieldX — Quarantine Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void GoToCards()
        {
            ShowScanning = false;
            ShowResult   = false;
            ShowCards    = true;
        }

        private void SaveHistory(ScanHistoryItem item)
        {
            try
            {
                Directory.CreateDirectory(
                    Path.GetDirectoryName(HistoryPath)!);

                var list = new List<ScanHistoryItem>();
                if (File.Exists(HistoryPath))
                    list = JsonSerializer.Deserialize<
                        List<ScanHistoryItem>>(
                        File.ReadAllText(HistoryPath)) ?? new();

                list.Insert(0, item);
                if (list.Count > 50) list = list.Take(50).ToList();

                File.WriteAllText(HistoryPath,
                    JsonSerializer.Serialize(list,
                        new JsonSerializerOptions
                        {
                            WriteIndented = true
                        }));

                RecentScans.Clear();
                foreach (var s in list) RecentScans.Add(s);
                OnPropertyChanged(nameof(HasNoHistory));
            }
            catch (Exception ex) { Debug.WriteLine(ex); }
        }

        private void LoadHistory()
        {
            try
            {
                if (!File.Exists(HistoryPath)) return;
                var list = JsonSerializer.Deserialize<
                    List<ScanHistoryItem>>(
                    File.ReadAllText(HistoryPath)) ?? new();
                foreach (var s in list) RecentScans.Add(s);
                OnPropertyChanged(nameof(HasNoHistory));
            }
            catch (Exception ex) { Debug.WriteLine(ex); }
        }

        private static string Truncate(string p, int max = 75)
        {
            if (string.IsNullOrEmpty(p)) return "";
            return p.Length <= max ? p : "..." + p[^max..];
        }

        private sealed class RelayCmd : ICommand
        {
            private readonly Action<object?> _exec;
            public RelayCmd(Action<object?> exec) => _exec = exec;
            public bool CanExecute(object? p) => true;
            public void Execute(object? p)    => _exec(p);
            public event EventHandler? CanExecuteChanged;
        }

        private sealed class AsyncCmd : ICommand
        {
            private readonly Func<Task> _exec;
            private bool _busy;
            public AsyncCmd(Func<Task> exec) => _exec = exec;
            public bool CanExecute(object? p) => !_busy;
            public async void Execute(object? p)
            {
                if (_busy) return;
                _busy = true;
                RaiseCanExecuteChanged();
                try   { await _exec(); }
                catch { }
                finally
                {
                    _busy = false;
                    RaiseCanExecuteChanged();
                }
            }
            public event EventHandler? CanExecuteChanged;
            private void RaiseCanExecuteChanged() =>
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(
            [CallerMemberName] string? n = null) =>
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(n));
    }
}

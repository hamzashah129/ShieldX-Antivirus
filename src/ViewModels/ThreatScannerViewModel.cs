using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ShieldX.Models;
using ShieldX.Services;
using Serilog;

namespace ShieldX.ViewModels
{
    public class ThreatScannerViewModel : INotifyPropertyChanged
    {
        private readonly ThreatScannerService _service = new();

        private string _inputText = "";
        private bool _isScanning = false;
        private bool _showResult = false;
        private string _statusText = "";
        private ThreatScanReport? _currentReport;

        public string InputText
        {
            get => _inputText;
            set 
            { 
                if (_inputText != value)
                {
                    _inputText = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsScanning
        {
            get => _isScanning;
            set 
            { 
                if (_isScanning != value)
                {
                    _isScanning = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowResult
        {
            get => _showResult;
            set 
            { 
                if (_showResult != value)
                {
                    _showResult = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusText
        {
            get => _statusText;
            set 
            { 
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged();
                }
            }
        }

        public ThreatScanReport? CurrentReport
        {
            get => _currentReport;
            set 
            { 
                if (_currentReport != value)
                {
                    _currentReport = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<EngineResult> EngineResults { get; } = new();

        public ICommand ScanUrlCommand    { get; }
        public ICommand ScanFileCommand   { get; }
        public ICommand ScanIpCommand     { get; }
        public ICommand ClearCommand      { get; }
        public ICommand CopyReportCommand { get; }

        public ThreatScannerViewModel()
        {
            ScanUrlCommand       = new AsyncRelayCommand(ScanUrl);
            ScanFileCommand      = new AsyncRelayCommand(BrowseAndScanFile);
            ScanIpCommand        = new AsyncRelayCommand(ScanIp);
            ClearCommand         = new RelayCommand(_ => Clear());
            CopyReportCommand    = new RelayCommand(_ => CopyReport());
        }

        private async Task ScanUrl()
        {
            if (string.IsNullOrWhiteSpace(InputText)) 
            {
                MessageBox.Show("Please enter a URL", "ThreatScanner");
                return;
            }
            string url = InputText.Trim();
            if (!url.StartsWith("http") && !url.StartsWith("ftp"))
                url = "https://" + url;
            
            await RunScan(() => _service.ScanUrlAsync(url), $"Scanning URL: {url}");
        }

        private async Task BrowseAndScanFile()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select file to scan",
                Filter = "All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() != true) return;
            InputText = dlg.FileName;
            await RunScan(
                () => _service.ScanFileAsync(dlg.FileName),
                $"Scanning: {System.IO.Path.GetFileName(dlg.FileName)}");
        }

        private async Task ScanIp()
        {
            if (string.IsNullOrWhiteSpace(InputText))
            {
                MessageBox.Show("Please enter an IP address", "ThreatScanner");
                return;
            }
            await RunScan(
                () => _service.ScanIpAsync(InputText.Trim()),
                $"Scanning IP: {InputText}");
        }

        private async Task RunScan(
            Func<Task<ThreatScanReport>> scanFunc, string status)
        {
            IsScanning = true;
            ShowResult = false;
            StatusText = status;
            EngineResults.Clear();

            try
            {
                var report = await scanFunc();
                CurrentReport = report;

                EngineResults.Clear();
                foreach (var r in report.EngineResults)
                    EngineResults.Add(r);

                ShowResult = true;
                Log.Information($"[ThreatScanner] Scan completed: {report.Target} - Rating: {report.OverallRating}");
            }
            catch (Exception ex)
            {
                StatusText = $"Error: {ex.Message}";
                Log.Error($"[ThreatScanner] Scan error: {ex.Message}");
                MessageBox.Show($"Scan failed: {ex.Message}", "ThreatScanner");
            }
            finally
            {
                IsScanning = false;
            }
        }

        private void Clear()
        {
            InputText = "";
            ShowResult = false;
            CurrentReport = null;
            EngineResults.Clear();
            StatusText = "";
        }

        private void CopyReport()
        {
            if (CurrentReport == null) return;
            var sb = new StringBuilder();
            sb.AppendLine($"ShieldX Threat Report — {DateTime.Now:g}");
            sb.AppendLine($"Target: {CurrentReport.Target}");
            sb.AppendLine($"Rating: {CurrentReport.OverallRating}");
            sb.AppendLine($"Malicious: {CurrentReport.MaliciousCount}/{CurrentReport.TotalEngines} engines");
            sb.AppendLine($"Suspicious: {CurrentReport.SuspiciousCount}/{CurrentReport.TotalEngines} engines");
            sb.AppendLine($"Clean: {CurrentReport.CleanCount}/{CurrentReport.TotalEngines} engines");
            sb.AppendLine();
            sb.AppendLine("─── Engine Results ───");
            foreach (var e in CurrentReport.EngineResults)
                sb.AppendLine($"{e.EngineName}: {e.Result} — {e.Category}");
            
            if (CurrentReport.Details.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("─── Details ───");
                foreach (var detail in CurrentReport.Details)
                    sb.AppendLine($"{detail.Key}: {detail.Value}");
            }

            try
            {
                Clipboard.SetText(sb.ToString());
                MessageBox.Show("Report copied to clipboard!", "ThreatScanner");
            }
            catch (Exception ex)
            {
                Log.Error($"[ThreatScanner] Copy error: {ex.Message}");
                MessageBox.Show("Failed to copy report", "ThreatScanner");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private sealed class RelayCommand : ICommand
        {
            private readonly Action<object?> _execute;

            public RelayCommand(Action<object?> execute) => _execute = execute;

            public bool CanExecute(object? parameter) => true;
            public void Execute(object? parameter) => _execute(parameter);
            public event EventHandler? CanExecuteChanged;
        }

        private sealed class AsyncRelayCommand : ICommand
        {
            private readonly Func<Task> _execute;
            private bool _isExecuting = false;

            public AsyncRelayCommand(Func<Task> execute) => _execute = execute;

            public bool CanExecute(object? parameter) => !_isExecuting;

            public async void Execute(object? parameter)
            {
                if (_isExecuting) return;
                _isExecuting = true;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                try
                {
                    await _execute();
                }
                catch (Exception ex)
                {
                    Log.Error($"[ThreatScanner] Command error: {ex.Message}");
                }
                finally
                {
                    _isExecuting = false;
                    CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                }
            }

            public event EventHandler? CanExecuteChanged;
        }
    }
}

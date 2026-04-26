using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace ShieldX.ViewModels
{
    public class QuarantineItem
    {
        public string Id           { get; set; } = Guid.NewGuid().ToString();
        public string FileName     { get; set; } = "";
        public string ThreatName   { get; set; } = "";
        public string OriginalPath { get; set; } = "";
        public string QuarantinePath { get; set; } = "";
        public DateTime DateIsolated { get; set; }
        public long FileSizeBytes  { get; set; }
        public string Severity     { get; set; } = "High";
        public string DateText     => DateIsolated.ToString("MM/dd/yyyy hh:mm tt");
        public string SizeText     => FileSizeBytes < 1024
            ? $"{FileSizeBytes} B"
            : FileSizeBytes < 1024 * 1024
                ? $"{FileSizeBytes / 1024.0:F1} KB"
                : $"{FileSizeBytes / (1024.0 * 1024):F1} MB";
    }

    public class QuarantineViewModel : INotifyPropertyChanged
    {
        private static readonly string QuarantineDir = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "ShieldX", "Quarantine");

        public ObservableCollection<QuarantineItem> Items { get; } = new();

        private QuarantineItem? _selected;
        public QuarantineItem? SelectedItem
        {
            get => _selected;
            set { _selected = value; OnPropertyChanged(); }
        }

        private string _statusText = "";
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public bool HasItems    => Items.Count > 0;
        public bool HasNoItems  => Items.Count == 0;
        public int  TotalItems  => Items.Count;
        public string TotalSize
        {
            get
            {
                long total = Items.Sum(i => i.FileSizeBytes);
                return total < 1024 * 1024
                    ? $"{total / 1024.0:F1} KB"
                    : $"{total / (1024.0 * 1024):F1} MB";
            }
        }

        public ICommand RefreshCommand        { get; }
        public ICommand RestoreCommand        { get; }
        public ICommand DeleteCommand         { get; }
        public ICommand DeleteAllCommand      { get; }
        public ICommand RestoreSelectedCommand { get; }
        public ICommand DeleteSelectedCommand  { get; }

        public QuarantineViewModel()
        {
            RefreshCommand = new RelayCmd(_ => LoadQuarantine());
            RestoreSelectedCommand = new RelayCmd(_ =>
                RestoreItem(SelectedItem));
            DeleteSelectedCommand = new RelayCmd(_ =>
                DeleteItem(SelectedItem));
            DeleteAllCommand = new RelayCmd(_ => DeleteAll());

            try
            {
                App.RealTimeProtection.ThreatBlocked += item =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        LoadQuarantine();
                    });
                };
            }
            catch { }

            LoadQuarantine();
        }

        public void LoadQuarantine()
        {
            try
            {
                Items.Clear();

                if (!Directory.Exists(QuarantineDir))
                {
                    Directory.CreateDirectory(QuarantineDir);
                    OnPropertyChanged(nameof(HasNoItems));
                    OnPropertyChanged(nameof(HasItems));
                    OnPropertyChanged(nameof(TotalItems));
                    StatusText = "Quarantine vault is empty";
                    return;
                }

                var files = Directory.GetFiles(
                    QuarantineDir, "*.quar",
                    SearchOption.TopDirectoryOnly);

                foreach (var qFile in files
                    .OrderByDescending(f =>
                        new FileInfo(f).LastWriteTime))
                {
                    try
                    {
                        var info = new FileInfo(qFile);

                        string quarName = info.Name;
                        string originalName = quarName;

                        int underscoreIdx = quarName.IndexOf('_');
                        if (underscoreIdx > 0 &&
                            underscoreIdx < quarName.Length - 1)
                        {
                            originalName = quarName
                                .Substring(underscoreIdx + 1)
                                .Replace(".quar", "");
                        }
                        else
                        {
                            originalName = quarName.Replace(".quar","");
                        }

                        string threatName = DetectThreatFromName(
                            originalName.ToLower());

                        Items.Add(new QuarantineItem
                        {
                            FileName       = originalName,
                            ThreatName     = threatName,
                            OriginalPath   = "Quarantined from system",
                            QuarantinePath = qFile,
                            DateIsolated   = info.LastWriteTime,
                            FileSizeBytes  = info.Length,
                            Severity       = "High"
                        });
                    }
                    catch { }
                }

                LoadMetadataFromHistory();

                OnPropertyChanged(nameof(HasItems));
                OnPropertyChanged(nameof(HasNoItems));
                OnPropertyChanged(nameof(TotalItems));
                OnPropertyChanged(nameof(TotalSize));
                StatusText = Items.Count > 0
                    ? $"{Items.Count} item(s) in quarantine"
                    : "Quarantine vault is empty";
            }
            catch (Exception ex)
            {
                StatusText = $"Error loading quarantine: {ex.Message}";
            }
        }

        private void LoadMetadataFromHistory()
        {
            try
            {
                var history = App.RealTimeProtection.LoadHistory();

                foreach (var item in Items.ToList())
                {
                    var match = history.FirstOrDefault(h =>
                        item.FileName.Contains(
                            Path.GetFileNameWithoutExtension(h.FileName),
                            StringComparison.OrdinalIgnoreCase));

                    if (match != null)
                    {
                        item.ThreatName   = match.ThreatName;
                        item.OriginalPath = match.FilePath;
                        item.Severity     = match.Severity;
                    }
                }
            }
            catch { }
        }

        private void RestoreItem(QuarantineItem? item)
        {
            if (item == null) return;

            var r = MessageBox.Show(
                $"Restore '{item.FileName}' to Downloads?\n\n" +
                $"WARNING: This file was flagged as:\n{item.ThreatName}\n\n" +
                "Only restore if you are sure it is safe.",
                "ShieldX — Restore File",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (r != MessageBoxResult.Yes) return;

            try
            {
                string dest = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.UserProfile),
                    "Downloads", item.FileName);

                File.Move(item.QuarantinePath, dest,
                    overwrite: true);
                Items.Remove(item);
                OnPropertyChanged(nameof(TotalItems));
                OnPropertyChanged(nameof(HasNoItems));
                OnPropertyChanged(nameof(HasItems));
                StatusText = $"Restored: {item.FileName}";
                MessageBox.Show(
                    $"File restored to Downloads:\n{dest}",
                    "ShieldX — Restored",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Restore failed: {ex.Message}",
                    "ShieldX", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DeleteItem(QuarantineItem? item)
        {
            if (item == null) return;

            var r = MessageBox.Show(
                $"Permanently delete '{item.FileName}'?\n\nThis cannot be undone.",
                "ShieldX — Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (r != MessageBoxResult.Yes) return;

            try
            {
                if (File.Exists(item.QuarantinePath))
                    File.Delete(item.QuarantinePath);

                Items.Remove(item);
                OnPropertyChanged(nameof(TotalItems));
                OnPropertyChanged(nameof(HasNoItems));
                OnPropertyChanged(nameof(HasItems));
                OnPropertyChanged(nameof(TotalSize));
                StatusText = $"Deleted: {item.FileName}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Delete failed: {ex.Message}",
                    "ShieldX", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DeleteAll()
        {
            if (Items.Count == 0) return;

            var r = MessageBox.Show(
                $"Permanently delete ALL {Items.Count} quarantined files?\n\nThis cannot be undone.",
                "ShieldX — Delete All",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (r != MessageBoxResult.Yes) return;

            int deleted = 0;
            foreach (var item in Items.ToList())
            {
                try
                {
                    if (File.Exists(item.QuarantinePath))
                        File.Delete(item.QuarantinePath);
                    deleted++;
                }
                catch { }
            }

            Items.Clear();
            OnPropertyChanged(nameof(TotalItems));
            OnPropertyChanged(nameof(HasNoItems));
            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(TotalSize));
            StatusText = $"Deleted {deleted} file(s)";
        }

        private static string DetectThreatFromName(string name)
        {
            string[] patterns = {
                "keylogger","trojan","malware","ransomware",
                "xworm","darkcomet","spyware","rootkit",
                "backdoor","mimikatz","stealer","injector",
                "crypter","payload","shellcode","rat",
                "cryptolocker","wannacry","netbus","virus"
            };
            foreach (var p in patterns)
                if (name.Contains(p))
                    return $"Suspicious.{char.ToUpper(p[0])}{p.Substring(1)}";
            return "Suspicious.UnknownThreat";
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
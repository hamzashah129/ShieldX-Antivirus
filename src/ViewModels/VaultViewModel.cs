using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using ShieldX.Services;
using ShieldX.Models;

namespace ShieldX.ViewModels
{
    public class VaultViewModel : INotifyPropertyChanged
    {
        private readonly VaultService _svc = new();
        private System.Collections.Generic.List<VaultEntry>
            _allEntries = new();
        private string _masterPassword = "";

        // ── Dialog state ──────────────────────────────────────
        private bool _showDialog = true;
        public bool ShowMasterPasswordDialog
        {
            get => _showDialog;
            set { _showDialog = value; OnPropertyChanged();
                  OnPropertyChanged(nameof(IsVaultUnlocked)); }
        }
        public bool IsVaultUnlocked => !_showDialog;
        public bool IsVaultSetup => VaultService.VaultExists();

        private bool _isCreating;
        public string VaultDialogTitle =>
            _isCreating ? "Create Your Vault" : "Unlock Vault";
        public string VaultDialogSubtitle =>
            _isCreating
                ? "Set a master password (min 4 chars)"
                : "Enter your master password";
        public string VaultActionButtonText =>
            _isCreating ? "Create Vault" : "Unlock";

        private string _errorMsg = "";
        public string VaultErrorMessage
        {
            get => _errorMsg;
            set { _errorMsg = value; OnPropertyChanged();
                  OnPropertyChanged(nameof(HasVaultError)); }
        }
        public bool HasVaultError =>
            !string.IsNullOrEmpty(_errorMsg);

        // ── Password input from PasswordBox ──────────────────
        private string _passwordInput = "";
        public string MasterPasswordInput
        {
            get => _passwordInput;
            set { _passwordInput = value; OnPropertyChanged(); }
        }

        // ── Entries ───────────────────────────────────────────
        public ObservableCollection<VaultEntry>
            FilteredEntries { get; } = new();
        public bool HasEntries  => FilteredEntries.Count > 0;
        public bool HasNoEntries => FilteredEntries.Count == 0;

        private string _search = "";
        public string SearchText
        {
            get => _search;
            set { _search = value; OnPropertyChanged();
                  ApplyFilter(); }
        }
        public bool IsSearchEmpty =>
            string.IsNullOrEmpty(_search);

        // ── Password generator ────────────────────────────────
        private int _pwLen = 16;
        public int PasswordLength
        {
            get => _pwLen;
            set { _pwLen = value; OnPropertyChanged(); }
        }
        public bool UseUppercase  { get; set; } = true;
        public bool UseNumbers    { get; set; } = true;
        public bool UseSymbols    { get; set; } = true;

        private string _generated = "";
        public string GeneratedPassword
        {
            get => _generated;
            set { _generated = value; OnPropertyChanged(); }
        }

        // ── Additional password properties ────────────────────
        private string _masterPasswordConfirm = "";
        public string MasterPasswordConfirm
        {
            get => _masterPasswordConfirm;
            set { _masterPasswordConfirm = value; OnPropertyChanged(); }
        }
        
        public int PasswordGeneratorLength
        {
            get => PasswordLength;
            set { PasswordLength = value; }
        }

        private VaultEntry? _selectedEntry;
        public VaultEntry? SelectedEntry
        {
            get => _selectedEntry;
            set { _selectedEntry = value; OnPropertyChanged(); }
        }

        // ── Weak/Duplicate counts ─────────────────────────────
        public int WeakPasswordCount =>
            _allEntries.Count(e => e.PasswordStrength == "Weak");
        public int DuplicateCount =>
            _allEntries.GroupBy(e => e.Password)
                .Count(g => g.Count() > 1);

        // ── Commands ──────────────────────────────────────────
        public ICommand ConfirmVaultPasswordCommand { get; }
        public ICommand CancelVaultCommand          { get; }
        public ICommand LockVaultCommand            { get; }
        public ICommand AddEntryCommand             { get; }
        public ICommand GeneratePasswordCommand     { get; }
        public ICommand CopyGeneratedCommand        { get; }
        public ICommand CopyPasswordCommand         { get; }
        public ICommand ToggleShowPasswordCommand   { get; }
        public ICommand DeleteEntryCommand          { get; }
        public ICommand UnlockVaultCommand          { get; }
        public ICommand CreateVaultCommand          { get; }

        public VaultViewModel()
        {
            _isCreating = !VaultService.VaultExists();

            ConfirmVaultPasswordCommand =
                new RelayCmd(_ => ConfirmPassword());
            CancelVaultCommand =
                new RelayCmd(_ =>
                    ShowMasterPasswordDialog = false);
            LockVaultCommand =
                new RelayCmd(_ => LockVault());
            AddEntryCommand =
                new AsyncCmd(ShowAddDialog);
            GeneratePasswordCommand =
                new RelayCmd(_ => Generate());
            CopyGeneratedCommand =
                new RelayCmd(_ => CopyToClipboard(
                    GeneratedPassword));
            CopyPasswordCommand =
                new RelayCmd(p => CopyToClipboard(
                    (p as VaultEntry)?.Password ?? ""));
            ToggleShowPasswordCommand =
                new RelayCmd(p =>
                {
                    (p as VaultEntry)?.ToggleShow();
                    FilteredEntries.Clear();
                    foreach (var e in _allEntries)
                        FilteredEntries.Add(e);
                    OnPropertyChanged(
                        nameof(FilteredEntries));
                });
            DeleteEntryCommand =
                new RelayCmd(p => DeleteEntry(
                    p as VaultEntry));
            UnlockVaultCommand =
                new RelayCmd(_ => ConfirmPassword());
            CreateVaultCommand =
                new RelayCmd(_ => ConfirmPassword());
        }

        // ── Auth ──────────────────────────────────────────────
        private void ConfirmPassword()
        {
            if (string.IsNullOrWhiteSpace(MasterPasswordInput))
            {
                VaultErrorMessage = "Password cannot be empty.";
                return;
            }
            if (MasterPasswordInput.Length < 4)
            {
                VaultErrorMessage =
                    "Minimum 4 characters required.";
                return;
            }

            VaultErrorMessage = "";
            _masterPassword   = MasterPasswordInput;

            if (_isCreating)
            {
                // Create empty vault
                try
                {
                    _svc.CreateVault(_masterPassword);
                    _allEntries = new System.Collections.Generic
                        .List<VaultEntry>();
                    Unlock();
                }
                catch (Exception ex)
                {
                    VaultErrorMessage = ex.Message;
                }
            }
            else
            {
                // Try to unlock
                try
                {
                    if (_svc.UnlockVault(_masterPassword))
                    {
                        _allEntries = _svc.GetAllEntries();
                        Unlock();
                    }
                    else
                    {
                        VaultErrorMessage = "Invalid password";
                    }
                }
                catch (Exception ex)
                {
                    VaultErrorMessage = ex.Message;
                    MasterPasswordInput = "";
                }
            }
        }

        private void Unlock()
        {
            ShowMasterPasswordDialog = false;
            ApplyFilter();
            OnPropertyChanged(nameof(WeakPasswordCount));
            OnPropertyChanged(nameof(DuplicateCount));
        }

        private void LockVault()
        {
            _masterPassword = "";
            _allEntries.Clear();
            FilteredEntries.Clear();
            MasterPasswordInput = "";
            VaultErrorMessage   = "";
            _isCreating = !VaultService.VaultExists();
            ShowMasterPasswordDialog = true;
        }

        // ── Entries ───────────────────────────────────────────
        private void ApplyFilter()
        {
            FilteredEntries.Clear();
            var filtered = string.IsNullOrWhiteSpace(_search)
                ? _allEntries
                : _allEntries.Where(e =>
                    e.Site.Contains(_search,
                        StringComparison.OrdinalIgnoreCase) ||
                    e.Username.Contains(_search,
                        StringComparison.OrdinalIgnoreCase));

            foreach (var e in filtered)
                FilteredEntries.Add(e);

            OnPropertyChanged(nameof(HasEntries));
            OnPropertyChanged(nameof(HasNoEntries));
            OnPropertyChanged(nameof(IsSearchEmpty));
        }

        private async Task ShowAddDialog()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var dialog = new Views.AddVaultEntryDialog
                {
                    Owner = Application.Current.MainWindow
                };

                if (dialog.ShowDialog() == true)
                {
                    var entry = new VaultEntry
                    {
                        Site     = dialog.SiteInput,
                        Username = dialog.UsernameInput,
                        Password = dialog.PasswordInput,
                        Category = string.IsNullOrWhiteSpace(
                            dialog.CategoryInput)
                            ? "General" : dialog.CategoryInput,
                        CreatedDate    = DateTime.Now,
                        ModifiedDate   = DateTime.Now,
                    };
                    try
                    {
                        _svc.AddEntry(entry);
                        _allEntries.Insert(0, entry);
                        ApplyFilter();
                        OnPropertyChanged(nameof(WeakPasswordCount));
                        OnPropertyChanged(nameof(DuplicateCount));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Failed to add entry: {ex.Message}",
                            "ShieldX Vault",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            });
        }

        private void DeleteEntry(VaultEntry? entry)
        {
            if (entry == null) return;
            var r = MessageBox.Show(
                $"Delete '{entry.Site}' entry?",
                "ShieldX Vault",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (r != MessageBoxResult.Yes) return;

            try
            {
                _svc.DeleteEntry(entry.Id);
                _allEntries.Remove(entry);
                ApplyFilter();
                OnPropertyChanged(nameof(WeakPasswordCount));
                OnPropertyChanged(nameof(DuplicateCount));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to delete entry: {ex.Message}",
                    "ShieldX Vault",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SaveVault()
        {
            try
            {
                foreach (var entry in _allEntries)
                {
                    _svc.UpdateEntry(entry);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Save failed: {ex.Message}",
                    "ShieldX Vault",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Generate()
        {
            GeneratedPassword = VaultService.GeneratePassword(
                PasswordLength, UseUppercase,
                UseNumbers, UseSymbols);
        }

        private static void CopyToClipboard(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            try
            {
                Clipboard.SetText(text);
                NotificationService.ShowSuccess(
                    "Copied to clipboard",
                    "Password copied. Clipboard clears in 30s.",
                    duration: 2);

                // Auto-clear clipboard after 30 seconds
                System.Threading.Tasks.Task.Delay(30000)
                    .ContinueWith(_ =>
                    Application.Current?.Dispatcher.Invoke(
                        () =>
                    {
                        if (Clipboard.GetText() == text)
                            Clipboard.Clear();
                    }));
            }
            catch { }
        }

        // ── Mini relay commands ───────────────────────────────
        private sealed class RelayCmd : ICommand
        {
            private readonly Action<object?> _e;
            public RelayCmd(Action<object?> e) => _e = e;
            public bool CanExecute(object? p) => true;
            public void Execute(object? p) => _e(p);
            public event EventHandler? CanExecuteChanged;
        }

        private sealed class AsyncCmd : ICommand
        {
            private readonly Func<Task> _e;
            private bool _busy;
            public AsyncCmd(Func<Task> e) => _e = e;
            public bool CanExecute(object? p) => !_busy;
            public async void Execute(object? p)
            {
                if (_busy) return; _busy = true;
                try { await _e(); }
                catch { }
                finally { _busy = false; }
            }
            public event EventHandler? CanExecuteChanged;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(
            [CallerMemberName] string? n = null) =>
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(n));
    }
}


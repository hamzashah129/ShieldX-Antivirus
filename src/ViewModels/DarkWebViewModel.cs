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
    public class DarkWebViewModel : INotifyPropertyChanged
    {
        private string _emailInput = "";
        private bool _isChecking = false;
        private bool _hasBreaches = false;
        private bool _isEmailClean = false;
        private string _statusMessage = "";
        private string _lastCheckedEmail = "";
        private DateTime? _lastCheckTime = null;
        private int _breachCount = 0;

        public ObservableCollection<BreachData> Breaches { get; }

        public ICommand CheckNowCommand { get; }
        public ICommand CopyEmailCommand { get; }
        public ICommand OpenBreachGuideCommand { get; }

        public DarkWebViewModel()
        {
            Breaches = new ObservableCollection<BreachData>();

            // Initialize commands
            CheckNowCommand = new RelayCommand(async () => await CheckEmailAsync(), CanExecuteCheck);
            CopyEmailCommand = new RelayCommand(() => CopyToClipboard(_lastCheckedEmail), CanExecuteCheck);
            OpenBreachGuideCommand = new RelayCommand(
                () => OpenUrl("https://haveibeenpwned.com/"), 
                () => true);
        }

        /// <summary>Email address to check for breaches</summary>
        public string EmailInput
        {
            get => _emailInput;
            set { if (_emailInput != value) { _emailInput = value; OnPropertyChanged(); } }
        }

        /// <summary>Whether a check operation is currently in progress</summary>
        public bool IsChecking
        {
            get => _isChecking;
            set { if (_isChecking != value) { _isChecking = value; OnPropertyChanged(); } }
        }

        /// <summary>Whether breaches were found for the email</summary>
        public bool HasBreaches
        {
            get => _hasBreaches;
            set { if (_hasBreaches != value) { _hasBreaches = value; OnPropertyChanged(); } }
        }

        /// <summary>Whether the email is clean (no breaches found)</summary>
        public bool IsEmailClean
        {
            get => _isEmailClean;
            set { if (_isEmailClean != value) { _isEmailClean = value; OnPropertyChanged(); } }
        }

        /// <summary>Status message to display to user</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(); } }
        }

        /// <summary>The last email that was checked</summary>
        public string LastCheckedEmail
        {
            get => _lastCheckedEmail;
            set { if (_lastCheckedEmail != value) { _lastCheckedEmail = value; OnPropertyChanged(); } }
        }

        /// <summary>When the last check was performed</summary>
        public DateTime? LastCheckTime
        {
            get => _lastCheckTime;
            set { if (_lastCheckTime != value) { _lastCheckTime = value; OnPropertyChanged(); } }
        }

        /// <summary>Number of breaches found</summary>
        public int BreachCount
        {
            get => _breachCount;
            set { if (_breachCount != value) { _breachCount = value; OnPropertyChanged(); } }
        }

        /// <summary>Text to display for results section</summary>
        public string ResultsText
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(LastCheckedEmail))
                {
                    if (IsEmailClean)
                    {
                        return $"✓ {LastCheckedEmail} is safe";
                    }
                    else if (HasBreaches)
                    {
                        return $"⚠ {LastCheckedEmail} found in {BreachCount} breach{(BreachCount != 1 ? "es" : "")}";
                    }
                }
                return "Enter email and click Check Now";
            }
        }

        /// <summary>
        /// Checks the provided email address for breaches using DarkWebService
        /// </summary>
        private async System.Threading.Tasks.Task CheckEmailAsync()
        {
            if (string.IsNullOrWhiteSpace(EmailInput))
            {
                StatusMessage = "Please enter an email address.";
                return;
            }

            IsChecking = true;
            StatusMessage = "Checking...";

            try
            {
                string email = EmailInput.Trim().ToLower();
                
                // Use DarkWebService for breach checking
                var (hasBreaches, breaches, errorMessage) = 
                    await DarkWebService.CheckEmailBreachesAsync(email, includeUnverified: true);

                LastCheckedEmail = email;
                LastCheckTime = DateTime.Now;

                if (hasBreaches)
                {
                    HasBreaches = true;
                    IsEmailClean = false;
                    BreachCount = breaches.Count;
                    StatusMessage = $"⚠ {email} was found in {BreachCount} breach{(BreachCount != 1 ? "es" : "")}!";
                    
                    Breaches.Clear();
                    foreach (var breach in breaches)
                    {
                        Breaches.Add(breach);
                    }
                    
                    LogService.Instance.AddWarning(
                        $"Email {email} found in {BreachCount} breach(es)", 
                        "DarkWebMonitor");
                }
                else if (!string.IsNullOrEmpty(errorMessage))
                {
                    // API error - try local database fallback
                    CheckLocalDatabase(email);
                }
                else
                {
                    HasBreaches = false;
                    IsEmailClean = true;
                    BreachCount = 0;
                    StatusMessage = $"✓ {email} is safe";
                    Breaches.Clear();
                    
                    LogService.Instance.AddInfo(
                        $"Email {email} checked - no breaches found", 
                        "DarkWebMonitor");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error checking email: {ex.Message}. Trying local database...";
                CheckLocalDatabase(EmailInput.Trim().ToLower());
                LogService.Instance.AddError($"Breach check error: {ex.Message}", "DarkWebMonitor");
            }
            finally
            {
                IsChecking = false;
                OnPropertyChanged(nameof(ResultsText));
            }
        }

        private void CheckLocalDatabase(string email)
        {
            var breachedDomains = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "yahoo.com",
                "linkedin.com",
                "adobe.com",
                "myspace.com",
                "tumblr.com",
                "dropbox.com",
                "canva.com",
                "quora.com",
                "uber.com",
                "ebay.com",
            };

            string domain = email.Contains("@") ? email.Split('@')[1] : "";

            if (breachedDomains.Contains(domain))
            {
                LastCheckedEmail = email;
                LastCheckTime = DateTime.Now;
                HasBreaches = true;
                IsEmailClean = false;
                BreachCount = 1;
                StatusMessage = $"⚠ {email} may be in known breaches!\nThe domain '{domain}' was involved in major data breaches.\nWe recommend changing your password immediately.";
                Breaches.Clear();
            }
            else
            {
                LastCheckedEmail = email;
                LastCheckTime = DateTime.Now;
                HasBreaches = false;
                IsEmailClean = true;
                BreachCount = 0;
                StatusMessage = $"✓ {email} appears safe\nNo matches found in our local breach database.\nFor comprehensive checking, visit haveibeenpwned.com";
                Breaches.Clear();
            }
        }

        /// <summary>
        /// Determines if the Check Now command can execute
        /// </summary>
        private bool CanExecuteCheck() => !IsChecking && !string.IsNullOrWhiteSpace(EmailInput);

        /// <summary>
        /// Copies text to clipboard
        /// </summary>
        private void CopyToClipboard(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            try
            {
                System.Windows.Forms.Clipboard.SetText(text);
                StatusMessage = "Email copied to clipboard";
            }
            catch
            {
                StatusMessage = "Failed to copy to clipboard";
            }
        }

        /// <summary>
        /// Opens a URL in the default browser
        /// </summary>
        private void OpenUrl(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { }
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

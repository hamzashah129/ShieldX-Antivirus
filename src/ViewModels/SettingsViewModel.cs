using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ShieldX.Services;
using ShieldX.Utils;

namespace ShieldX.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private string _contextMenuStatus = "Checking...";
        private string _contextMenuErrorMessage = string.Empty;
        private bool _hasContextMenuError;
        private System.Windows.Media.Brush _contextMenuStatusColor;
        private bool _isDarkTheme = true;

        public SettingsViewModel()
        {
            RegisterContextMenuCommand = new RelayCommand(_ => RegisterContextMenu());
            UnregisterContextMenuCommand = new RelayCommand(_ => UnregisterContextMenu());
            SetDarkThemeCommand = new RelayCommand(_ => SetDarkTheme());
            SetLightThemeCommand = new RelayCommand(_ => SetLightTheme());
            
            // Initialize theme state
            _isDarkTheme = ThemeService.IsDark;
            
            // Subscribe to theme changes
            ThemeService.ThemeChanged += theme =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsDarkTheme = theme == AppTheme.Dark;
                    OnPropertyChanged(nameof(CurrentThemeText));
                });
            };
            
            // Check initial status
            RefreshContextMenuStatus();
        }

        public System.Collections.ObjectModel.ObservableCollection<Models.SecurityModule> Modules => ModuleManager.Instance.Modules;

        public string ContextMenuStatus
        {
            get => _contextMenuStatus;
            set
            {
                if (_contextMenuStatus == value) return;
                _contextMenuStatus = value;
                OnPropertyChanged();
            }
        }

        public System.Windows.Media.Brush ContextMenuStatusColor
        {
            get => _contextMenuStatusColor;
            set
            {
                if (_contextMenuStatusColor == value) return;
                _contextMenuStatusColor = value;
                OnPropertyChanged();
            }
        }

        public string ContextMenuErrorMessage
        {
            get => _contextMenuErrorMessage;
            set
            {
                if (_contextMenuErrorMessage == value) return;
                _contextMenuErrorMessage = value;
                OnPropertyChanged();
            }
        }

        public bool HasContextMenuError
        {
            get => _hasContextMenuError;
            set
            {
                if (_hasContextMenuError == value) return;
                _hasContextMenuError = value;
                OnPropertyChanged();
            }
        }

        public ICommand RegisterContextMenuCommand { get; }
        public ICommand UnregisterContextMenuCommand { get; }
        public ICommand SetDarkThemeCommand { get; }
        public ICommand SetLightThemeCommand { get; }



        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (_isDarkTheme == value) return;
                _isDarkTheme = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentThemeText));
            }
        }

        public string CurrentThemeText =>
            IsDarkTheme
                ? "🌙 Dark Mode is active"
                : "☀️ Light Mode is active";

        private void SetDarkTheme()
        {
            ThemeService.ApplyTheme(AppTheme.Dark);
            IsDarkTheme = true;
        }

        private void SetLightTheme()
        {
            ThemeService.ApplyTheme(AppTheme.Light);
            IsDarkTheme = false;
        }

        private void RegisterContextMenu()
        {
            try
            {
                ContextMenuErrorMessage = string.Empty;
                HasContextMenuError = false;

                if (!ContextMenuInstaller.IsRunningAsAdmin())
                {
                    ContextMenuErrorMessage = "Administrator privileges required. Please restart the application as administrator.";
                    HasContextMenuError = true;
                    RefreshContextMenuStatus();
                    return;
                }

                ContextMenuInstaller.Register();
                RefreshContextMenuStatus();
            }
            catch (Exception ex)
            {
                ContextMenuErrorMessage = $"Failed to register context menu: {ex.Message}";
                HasContextMenuError = true;
                RefreshContextMenuStatus();
            }
        }

        private void UnregisterContextMenu()
        {
            try
            {
                ContextMenuErrorMessage = string.Empty;
                HasContextMenuError = false;

                if (!ContextMenuInstaller.IsRunningAsAdmin())
                {
                    ContextMenuErrorMessage = "Administrator privileges required. Please restart the application as administrator.";
                    HasContextMenuError = true;
                    RefreshContextMenuStatus();
                    return;
                }

                ContextMenuInstaller.Unregister();
                RefreshContextMenuStatus();
            }
            catch (Exception ex)
            {
                ContextMenuErrorMessage = $"Failed to unregister context menu: {ex.Message}";
                HasContextMenuError = true;
                RefreshContextMenuStatus();
            }
        }


        private void RefreshContextMenuStatus()
        {
            try
            {
                bool isRegistered = ContextMenuInstaller.IsRegistered();
                if (isRegistered)
                {
                    ContextMenuStatus = "✓ Registered";
                    ContextMenuStatusColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)); // Green
                }
                else
                {
                    ContextMenuStatus = "✗ Not Registered";
                    ContextMenuStatusColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54)); // Red
                }
            }
            catch
            {
                ContextMenuStatus = "? Unknown";
                ContextMenuStatusColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 152, 0)); // Orange
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private sealed class RelayCommand : ICommand
        {
            private readonly Action<object> _execute;
            private readonly Func<object, bool> _canExecute;

            public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;

            public void Execute(object parameter) => _execute(parameter);

            public event EventHandler CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }
        }
    }
}
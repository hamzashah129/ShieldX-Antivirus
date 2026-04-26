using System;
using System.Windows;
using System.Windows.Controls;
using ShieldX.Models;
using ShieldX.ViewModels;

namespace ShieldX.Views
{
    /// <summary>
    /// Dashboard page for displaying real-time security status, metrics, and active protection modules.
    /// Implements MVVM pattern with DashboardViewModel as DataContext.
    /// </summary>
    public partial class DashboardPage : Page
    {
        private DashboardViewModel _viewModel;

        public DashboardPage()
        {
            try
            {
                InitializeComponent();
                _viewModel = new DashboardViewModel();
                DataContext = _viewModel;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error initializing DashboardPage");
                MessageBox.Show("Failed to initialize Dashboard. Please restart the application.", 
                    "Dashboard Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void QuickScanButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigateToPage("Scan");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error starting quick scan");
                MessageBox.Show("Failed to start scan. Please try again.", "Scan Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FullScanButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigateToPage("Scan");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error starting full scan");
                MessageBox.Show("Failed to start scan. Please try again.", "Scan Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigateToPage("Updates");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error navigating to updates");
                MessageBox.Show("Failed to open Updates page. Please try again.", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NavigateToPage(string pageName)
        {
            try
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.NavigateToDashboardPage(pageName);
                }
                else
                {
                    throw new InvalidOperationException("Main window not found");
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Navigation error to page: {PageName}", pageName);
                MessageBox.Show($"Navigation failed. Please try again.", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AlertBorder_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Let the context menu handle the right-click
            e.Handled = false;
        }

        private void ViewQuarantine_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigateToPage("Quarantine");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error navigating to quarantine");
                MessageBox.Show("Failed to open Quarantine page. Please try again.", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewAlertDetails_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is MenuItem menuItem && menuItem.Tag is AlertItem alert)
                {
                    var message = $"Threat Details:\n\nFile: {alert.FileName}\nThreat: {alert.Threat}\nPath: {alert.Path}\nAction: {alert.Action}\nTime: {alert.Time}";
                    MessageBox.Show(message, "Threat Details", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error displaying alert details");
                MessageBox.Show("Failed to display alert details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DismissAlert_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is MenuItem menuItem && menuItem.Tag is AlertItem alert)
                {
                    _viewModel?.RemoveAlert(alert);
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error dismissing alert");
                MessageBox.Show("Failed to dismiss alert.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ModuleBorder_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Border border && border.DataContext is ProtectionModule module)
                {
                    _viewModel?.ToggleModule(module);
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error toggling protection module");
                MessageBox.Show("Failed to toggle module. Please try again.", "Module Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
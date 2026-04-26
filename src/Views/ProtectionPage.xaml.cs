using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using ShieldX.Services;
using ShieldX.ViewModels;

namespace ShieldX.Views
{
    public partial class ProtectionPage : Page
    {
        private ProtectionViewModel _viewModel;

        public ProtectionPage()
        {
            InitializeComponent();
            _viewModel = new ProtectionViewModel();
            DataContext = _viewModel;
        }

        private async void ModuleToggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggle && toggle.Tag is string moduleName)
            {
                await ModuleManager.Instance.SetModuleStateAsync(moduleName, toggle.IsChecked == true);
            }
        }
    }
}
using System.Windows.Controls;
using ShieldX.ViewModels;

namespace ShieldX.Views
{
    public partial class SettingsPage : Page
    {
        private SettingsViewModel _viewModel;

        public SettingsPage()
        {
            InitializeComponent();
            _viewModel = new SettingsViewModel();
            DataContext = _viewModel;
        }
    }
}
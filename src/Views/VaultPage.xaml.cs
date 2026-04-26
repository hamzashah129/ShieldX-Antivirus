using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ShieldX.ViewModels;

namespace ShieldX.Views
{
    public partial class VaultPage : Page
    {
        public VaultPage()
        {
            InitializeComponent();
            DataContext = new VaultViewModel();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is VaultViewModel vm && PasswordInput != null && vm.ShowMasterPasswordDialog)
            {
                PasswordInput.Focus();
            }
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && DataContext is VaultViewModel vm)
            {
                if (PasswordInput != null)
                {
                    vm.MasterPasswordInput = PasswordInput.Password;
                }
                if (MasterPasswordButton?.Command != null)
                {
                    MasterPasswordButton.Command.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }
}

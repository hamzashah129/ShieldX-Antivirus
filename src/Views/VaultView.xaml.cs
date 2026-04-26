using System.Windows;
using System.Windows.Controls;

namespace ShieldX.Views
{
    public partial class VaultView : UserControl
    {
        public VaultView() => InitializeComponent();

        private void MasterPasswordBox_PasswordChanged(
            object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.VaultViewModel vm &&
                sender is PasswordBox pb)
            {
                vm.MasterPasswordInput = pb.Password;
            }
        }
    }
}

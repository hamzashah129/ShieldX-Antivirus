using System.Windows;

namespace ShieldX.Views
{
    public partial class AddVaultEntryDialog : Window
    {
        public string SiteInput     { get; private set; } = "";
        public string UsernameInput { get; private set; } = "";
        public string PasswordInput { get; private set; } = "";
        public string CategoryInput { get; private set; } = "General";

        public AddVaultEntryDialog() => InitializeComponent();

        private void Save_Click(object s, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtSite.Text))
            {
                MessageBox.Show("Site/App name is required.",
                    "ShieldX Vault", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(TxtPassword.Password))
            {
                MessageBox.Show("Password is required.",
                    "ShieldX Vault", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
            SiteInput     = TxtSite.Text.Trim();
            UsernameInput = TxtUsername.Text.Trim();
            PasswordInput = TxtPassword.Password;
            CategoryInput = string.IsNullOrWhiteSpace(
                TxtCategory.Text) ? "General" : TxtCategory.Text;
            DialogResult  = true;
            Close();
        }

        private void Cancel_Click(object s, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

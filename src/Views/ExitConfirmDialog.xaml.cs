using System.Windows;

namespace ShieldX.Views
{
    public enum ExitDialogResult { Cancel, Tray, Exit }

    public partial class ExitConfirmDialog : Window
    {
        public ExitDialogResult Result { get; private set; }
            = ExitDialogResult.Cancel;

        public ExitConfirmDialog()
        {
            InitializeComponent();
        }

        private void Tray_Click(object s, RoutedEventArgs e)
        {
            Result = ExitDialogResult.Tray;
            DialogResult = true;
            Close();
        }

        private void Exit_Click(object s, RoutedEventArgs e)
        {
            Result = ExitDialogResult.Exit;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object s, RoutedEventArgs e)
        {
            Result = ExitDialogResult.Cancel;
            DialogResult = false;
            Close();
        }
    }
}

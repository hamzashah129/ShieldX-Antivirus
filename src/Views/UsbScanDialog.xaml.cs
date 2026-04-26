using System.Windows;

namespace ShieldX.Views
{
    public partial class UsbScanDialog : Window
    {
        public bool? DialogResult { get; set; }

        public UsbScanDialog()
        {
            InitializeComponent();
        }

        public static bool? ShowDialog(string driveName)
        {
            var dialog = new UsbScanDialog();
            dialog.DriveNameText.Text = $"{driveName} Drive";
            dialog.ShowDialog();
            return dialog.DialogResult;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

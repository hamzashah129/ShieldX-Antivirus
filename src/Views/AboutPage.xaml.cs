using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace ShieldX.Views
{
    public partial class AboutPage : Page
    {
        public AboutPage()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Uri.OriginalString))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.OriginalString,
                    UseShellExecute = true,
                    CreateNoWindow = true
                });
                e.Handled = true;
            }
        }

        private void EmailButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "mailto:syedhamzaalishah31324@gmail.com",
                UseShellExecute = true,
                CreateNoWindow = true
            });
        }
    }
}
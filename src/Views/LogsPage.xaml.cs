using System;
using System.Windows;
using System.Windows.Controls;
using ShieldX.ViewModels;

namespace ShieldX.Views
{
    public partial class LogsPage : Page
    {
        public LogsPage()
        {
            InitializeComponent();
            // DataContext will be set by MainWindow or Navigation
            if (this.DataContext == null)
            {
                this.DataContext = new LogsPageViewModel();
            }
        }
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ShieldX.ViewModels;

namespace ShieldX.Views
{
    /// <summary>
    /// Interaction logic for DarkWebView.xaml
    /// </summary>
    public partial class DarkWebView : Page
    {
        public DarkWebView()
        {
            InitializeComponent();
            DataContext = new DarkWebViewModel();
        }

        /// <summary>
        /// Handles Enter key press in email input to trigger check
        /// </summary>
        private void EmailInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && DataContext is DarkWebViewModel vm)
            {
                if (vm.CheckNowCommand.CanExecute(null))
                {
                    vm.CheckNowCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }
}

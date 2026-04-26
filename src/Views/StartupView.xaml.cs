using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ShieldX.ViewModels;

namespace ShieldX.Views
{
    /// <summary>
    /// Interaction logic for StartupView.xaml
    /// </summary>
    public partial class StartupView : Page
    {
        public StartupView()
        {
            InitializeComponent();
            DataContext = new StartupViewModel();
        }

        /// <summary>
        /// Handles Enter key press in filter input
        /// </summary>
        private void FilterInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && DataContext is StartupViewModel vm)
            {
                if (vm.FilterCommand.CanExecute(null))
                {
                    vm.FilterCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }
}

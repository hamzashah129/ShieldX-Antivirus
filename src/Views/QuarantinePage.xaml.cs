using System.Windows.Controls;
using ShieldX.ViewModels;

namespace ShieldX.Views
{
    public partial class QuarantinePage : Page
    {
        private QuarantineViewModel _viewModel;

        public QuarantinePage()
        {
            InitializeComponent();
            _viewModel = new QuarantineViewModel();
            DataContext = _viewModel;
        }
    }
}
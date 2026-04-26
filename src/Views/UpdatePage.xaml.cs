using System.Windows.Controls;
using ShieldX.ViewModels;

namespace ShieldX.Views
{
    public partial class UpdatePage : Page
    {
        private UpdateViewModel _viewModel;

        public UpdatePage()
        {
            InitializeComponent();
            _viewModel = new UpdateViewModel();
            DataContext = _viewModel;
        }
    }
}

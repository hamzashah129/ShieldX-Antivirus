using System.Windows.Controls;
using ShieldX.ViewModels;

namespace ShieldX.Views
{
    public partial class ScanView : UserControl
    {
        public ScanView()
        {
            InitializeComponent();
            DataContext = new ScanViewModel();
        }
    }
}

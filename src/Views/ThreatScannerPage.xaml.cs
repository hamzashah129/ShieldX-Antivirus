using System.Windows.Controls;
using ShieldX.ViewModels;

namespace ShieldX.Views
{
    public partial class ThreatScannerPage : Page
    {
        public ThreatScannerPage()
        {
            InitializeComponent();
            DataContext = new ThreatScannerViewModel();
        }
    }
}

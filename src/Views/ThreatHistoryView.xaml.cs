using System.Windows.Controls;
using ShieldX.ViewModels;

namespace ShieldX.Views
{
    public partial class ThreatHistoryView : UserControl
    {
        public ThreatHistoryView()
        {
            InitializeComponent();
            // Set DataContext to ThreatHistoryViewModel
            DataContext = new ThreatHistoryViewModel();
        }
    }
}

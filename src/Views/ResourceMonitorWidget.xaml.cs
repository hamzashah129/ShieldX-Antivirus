using System.Windows.Controls;
using ShieldX.ViewModels;

namespace ShieldX.Views
{
    /// <summary>
    /// ResourceMonitorWidget - displays real-time system resource usage
    /// </summary>
    public partial class ResourceMonitorWidget : UserControl
    {
        public ResourceMonitorWidget()
        {
            InitializeComponent();
            
            // Set the DataContext to the ViewModel
            this.DataContext = new ResourceMonitorViewModel();
        }
    }
}

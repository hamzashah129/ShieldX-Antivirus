using System.Windows.Controls;
using ShieldX.ViewModels;

namespace ShieldX.Views
{
    public partial class AIGuardPage : Page
    {
        public AIGuardPage()
        {
            InitializeComponent();
            DataContext = new AIGuardViewModel();
        }
    }
}

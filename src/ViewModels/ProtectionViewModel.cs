using System.ComponentModel;
using System.Runtime.CompilerServices;
using ShieldX.Services;

namespace ShieldX.ViewModels
{
    public class ProtectionViewModel : INotifyPropertyChanged
    {
        public ProtectionViewModel()
        {
            // Modules are bound from ModuleManager
        }

        public System.Collections.ObjectModel.ObservableCollection<Models.SecurityModule> Modules => ModuleManager.Instance.Modules;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
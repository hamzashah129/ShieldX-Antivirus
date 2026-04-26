using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace ShieldX.Models
{
    public enum ModuleStatus
    {
        Active,
        Inactive,
        Error
    }

    public class SecurityModule : INotifyPropertyChanged
    {
        private bool _isActive;
        private ModuleStatus _status;

        public string Name { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }

        public Brush StatusBrush => Status switch
        {
            ModuleStatus.Active => Brushes.Green,
            ModuleStatus.Inactive => Brushes.Gray,
            ModuleStatus.Error => Brushes.Red,
            _ => Brushes.Gray
        };

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    Status = value ? ModuleStatus.Active : ModuleStatus.Inactive;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public ModuleStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ShieldX.Models
{
    /// <summary>
    /// Represents a real-time threat alert for display in the dashboard.
    /// </summary>
    public class AlertItem : INotifyPropertyChanged
    {
        private string _time = "";
        private string _fileName = "";
        private string _threat = "";
        private string _path = "";
        private string _action = "";

        public string Time
        {
            get => _time;
            set
            {
                if (_time != value)
                {
                    _time = value;
                    OnPropertyChanged();
                }
            }
        }

        public string FileName
        {
            get => _fileName;
            set
            {
                if (_fileName != value)
                {
                    _fileName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Threat
        {
            get => _threat;
            set
            {
                if (_threat != value)
                {
                    _threat = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Path
        {
            get => _path;
            set
            {
                if (_path != value)
                {
                    _path = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Action
        {
            get => _action;
            set
            {
                if (_action != value)
                {
                    _action = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

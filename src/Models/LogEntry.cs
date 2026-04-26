using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ShieldX.Models
{
    public class LogEntry : INotifyPropertyChanged
    {
        public string   Level     { get; set; } = "INFO";
        public string   Message   { get; set; } = "";
        public string   Category  { get; set; } = "";
        public string   Details   { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string TimeText =>
            Timestamp.ToString("HH:mm:ss");

        public string FormattedTimestamp =>
            Timestamp.ToString("MMM dd yyyy  HH:mm");

        public string LevelColor =>
            Level == "ERROR"   ? "#EF4444" :
            Level == "WARNING" ? "#F59E0B" :
            Level == "INFO"    ? "#A0AEC0" : "#718096";

        public string LevelBg =>
            Level == "ERROR"   ? "#1A0D0D" :
            Level == "WARNING" ? "#1A150D" : "#0D1A2E";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}

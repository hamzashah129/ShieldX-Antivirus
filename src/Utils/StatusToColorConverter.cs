using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ShieldX.Models;

namespace ShieldX.Utils
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ModuleStatus status)
            {
                return status switch
                {
                    ModuleStatus.Active => Brushes.Green,
                    ModuleStatus.Inactive => Brushes.Gray,
                    ModuleStatus.Error => Brushes.Red,
                    _ => Brushes.Gray
                };
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ShieldX.Converters
{
    public class LogLevelBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string level)
            {
                return level.ToUpperInvariant() switch
                {
                    "INFO" => Brushes.White,
                    "WARN" => Brushes.Orange,
                    "ERROR" => Brushes.Red,
                    _ => Brushes.White
                };
            }
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                if (brush.Color.R > 200 && brush.Color.G < 100) // Red = Error
                    return "Error";
                if (brush.Color.R > 150 && brush.Color.G > 150) // Orange = Warning
                    return "Warning";
                if (brush.Color.G > 150) // Green = Information
                    return "Information";
            }
            return "Information";
        }
    }
}

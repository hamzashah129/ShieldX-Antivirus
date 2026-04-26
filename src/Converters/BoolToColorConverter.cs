using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ShieldX.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
                return Color.FromRgb(16, 185, 129); // green
            return Color.FromRgb(74, 85, 104);      // grey
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
            => value is Color color && color == Color.FromRgb(16, 185, 129);
    }
}

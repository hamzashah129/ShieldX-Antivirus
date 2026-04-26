using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ShieldX.Converters
{
    /// <summary>
    /// Converts non-zero numeric values to Visibility.
    /// Non-zero -> Visible, Zero -> Collapsed
    /// </summary>
    public class NonZeroToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                return intValue != 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            if (value is long longValue)
            {
                return longValue != 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            if (value is double doubleValue)
            {
                return doubleValue != 0.0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

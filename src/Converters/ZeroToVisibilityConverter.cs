using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ShieldX.Converters
{
    /// <summary>
    /// Returns Visibility.Visible when value is 0, Visibility.Collapsed otherwise
    /// </summary>
    public class ZeroToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, 
            object parameter, CultureInfo culture)
        {
            if (value is int n)
                return n == 0 ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, 
            object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

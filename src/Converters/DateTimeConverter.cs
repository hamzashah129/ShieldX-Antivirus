using System;
using System.Globalization;
using System.Windows.Data;

namespace ShieldX.Converters
{
    public class DateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dt && dt == DateTime.MinValue)
                return "Never";

            if (value is DateTime dateTime)
                return dateTime.ToString("MMM dd yyyy  HH:mm");

            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text && DateTime.TryParse(text, out var result))
                return result;

            return DateTime.MinValue;
        }
    }
}

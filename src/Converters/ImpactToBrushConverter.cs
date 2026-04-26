using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ShieldX.Models;

namespace ShieldX.Converters
{
    /// <summary>
    /// Converts StartupImpact enum values to color brushes for UI display
    /// </summary>
    public class ImpactToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is StartupImpact impact)
            {
                return impact switch
                {
                    StartupImpact.High => new SolidColorBrush(Color.FromRgb(0xD4, 0x53, 0x4F)), // Red
                    StartupImpact.Medium => new SolidColorBrush(Color.FromRgb(0xFF, 0xA5, 0x24)), // Orange
                    StartupImpact.Low => new SolidColorBrush(Color.FromRgb(0x2A, 0x5C, 0x3E)), // Green
                    _ => new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80)) // Gray
                };
            }

            return new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return StartupImpact.Medium; // Default to medium if conversion fails
        }
    }
}

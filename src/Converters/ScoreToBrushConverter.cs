using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ShieldX.Converters
{
    public class ScoreToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int score)
            {
                if (score >= 85)
                {
                    // Green gradient
                    return new LinearGradientBrush
                    {
                        StartPoint = new System.Windows.Point(0, 0),
                        EndPoint = new System.Windows.Point(1, 0),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromRgb(0x00, 0xD4, 0xAA), 0),
                            new GradientStop(Color.FromRgb(0x2E, 0xD5, 0x73), 1)
                        }
                    };
                }
                else if (score >= 60)
                {
                    // Orange gradient
                    return new LinearGradientBrush
                    {
                        StartPoint = new System.Windows.Point(0, 0),
                        EndPoint = new System.Windows.Point(1, 0),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromRgb(0xFF, 0xA5, 0x02), 0),
                            new GradientStop(Color.FromRgb(0xFF, 0x63, 0x48), 1)
                        }
                    };
                }
                else
                {
                    // Red gradient
                    return new LinearGradientBrush
                    {
                        StartPoint = new System.Windows.Point(0, 0),
                        EndPoint = new System.Windows.Point(1, 0),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromRgb(0xFF, 0x47, 0x57), 0),
                            new GradientStop(Color.FromRgb(0xC0, 0x39, 0x2B), 1)
                        }
                    };
                }
            }

            return new SolidColorBrush(Colors.White);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush solidBrush)
            {
                if (solidBrush.Color.R > 200 && solidBrush.Color.G < 100) // Red
                    return 0;
                if (solidBrush.Color.R > 200 && solidBrush.Color.G > 150) // Orange
                    return 75;
                if (solidBrush.Color.G > 200) // Green
                    return 90;
            }
            return 50;
        }
    }
}

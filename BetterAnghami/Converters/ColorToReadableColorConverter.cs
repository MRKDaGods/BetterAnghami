using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MRK.Converters
{
    public class ColorToReadableColorConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color c)
            {
                var brightness = (int)Math.Sqrt(
                    c.R * c.R * 0.299 +
                    c.G * c.G * 0.587 +
                    c.B * c.B * 0.114);

                return brightness > 130 ? Colors.Black : Colors.White;
            }

            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

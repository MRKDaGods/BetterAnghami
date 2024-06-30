using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MRK.Converters
{
    public class ColorToInvertedColorConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color col)
            {
                return Color.FromArgb(col.A, (byte)(col.R ^ 0xFF), (byte)(col.G ^ 0xFF), (byte)(col.B ^ 0xFF));
            }

            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

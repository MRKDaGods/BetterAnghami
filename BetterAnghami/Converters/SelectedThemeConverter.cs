using System;
using System.Globalization;
using System.Windows.Data;

namespace MRK.Converters
{
    public class SelectedThemeConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string)
            {
                return false;
            }

            var themesWindow = ThemesWindow.Instance;
            if (themesWindow == null || themesWindow.SelectedTheme == null)
            {
                return false;
            }

            return themesWindow.SelectedTheme.Id == (string)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

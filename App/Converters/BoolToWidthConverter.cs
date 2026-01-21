using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace App.Converters
{
    public class BoolToWidthConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isVisible && isVisible)
            {
                return new GridLength(300, GridUnitType.Absolute);
            }
            return new GridLength(0, GridUnitType.Absolute);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

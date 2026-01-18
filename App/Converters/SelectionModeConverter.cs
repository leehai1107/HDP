using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace App.Converters
{
    public class SelectionModeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isSelectionMode)
            {
                return isSelectionMode ? "✓ Select Mode" : "☐ Select Mode";
            }
            return "☐ Select Mode";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SelectionModeColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isSelectionMode)
            {
                return isSelectionMode ? Color.FromArgb("#9C27B0") : Color.FromArgb("#673AB7");
            }
            return Color.FromArgb("#673AB7");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Check if parameter is "empty" to convert string emptiness
            if (parameter?.ToString() == "empty")
            {
                if (value is string str)
                {
                    return !string.IsNullOrWhiteSpace(str);
                }
                return false;
            }
            
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }
}

using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace App.Converters
{
    public class TaskPanelToggleConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Always show menu icon (hamburger)
            return "☰"; // ☰ hamburger menu icon
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

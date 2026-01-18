using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace App.Converters
{
    public class TaskTextDecorationConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isDone && isDone)
            {
                return TextDecorations.Strikethrough;
            }
            return TextDecorations.None;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TaskOpacityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isDone && isDone)
            {
                return 0.5;
            }
            return 1.0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

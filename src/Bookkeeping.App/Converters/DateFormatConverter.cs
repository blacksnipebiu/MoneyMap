using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Bookkeeping.App.Converters;

public class DateFormatConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime date)
        {
            var format = parameter as string ?? "yyyy-MM-dd HH:mm";
            return date.ToString(format, culture);
        }
        
        return value?.ToString() ?? "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str && DateTime.TryParse(str, culture, DateTimeStyles.None, out var date))
        {
            return date;
        }
        
        return null;
    }
}
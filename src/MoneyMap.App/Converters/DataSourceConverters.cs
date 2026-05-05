using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using MoneyMap.Core.Enums;

namespace MoneyMap.App.Converters;

public class DataSourceIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DataSource source)
        {
            return source switch
            {
                DataSource.Alipay => "💙",
                DataSource.WeChatPay => "💚",
                DataSource.BankCard => "💳",
                DataSource.Manual => "✏️",
                DataSource.Other => "📁",
                _ => "📄"
            };
        }
        return "📄";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class DataSourceNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DataSource source)
        {
            return source switch
            {
                DataSource.Alipay => "支付宝",
                DataSource.WeChatPay => "微信支付",
                DataSource.BankCard => "银行卡",
                DataSource.Manual => "手动录入",
                DataSource.Other => "其他",
                _ => source.ToString()
            };
        }
        return value?.ToString() ?? "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class DataSourceBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DataSource source)
        {
            return source switch
            {
                DataSource.Alipay => new SolidColorBrush(Avalonia.Media.Color.FromRgb(0, 160, 233)), // 支付宝蓝
                DataSource.WeChatPay => new SolidColorBrush(Avalonia.Media.Color.FromRgb(7, 193, 96)), // 微信绿
                DataSource.BankCard => new SolidColorBrush(Avalonia.Media.Color.FromRgb(59, 130, 246)), // 银行卡蓝
                DataSource.Manual => new SolidColorBrush(Avalonia.Media.Color.FromRgb(107, 114, 128)), // 灰色
                DataSource.Other => new SolidColorBrush(Avalonia.Media.Color.FromRgb(156, 163, 175)),
                _ => new SolidColorBrush(Avalonia.Media.Color.FromRgb(156, 163, 175))
            };
        }
        return new SolidColorBrush(Avalonia.Media.Color.FromRgb(156, 163, 175));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class CountToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count == 0;
        }
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class HasItemsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count > 0;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class IsGreaterThanZeroConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count > 0;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Bookkeeping.Core.Enums;

namespace Bookkeeping.App.Converters;

public class TransactionTypeBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TransactionType type)
        {
            return type switch
            {
                TransactionType.Expense => new SolidColorBrush(Color.Parse("#EF4444")),
                TransactionType.Income => new SolidColorBrush(Color.Parse("#10B981")),
                TransactionType.Transfer => new SolidColorBrush(Color.Parse("#3B82F6")),
                _ => new SolidColorBrush(Color.Parse("#6B7280"))
            };
        }
        return new SolidColorBrush(Color.Parse("#6B7280"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class TransactionTypeIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TransactionType type)
        {
            return type switch
            {
                TransactionType.Expense => "↓",
                TransactionType.Income => "↑",
                TransactionType.Transfer => "↔",
                _ => "•"
            };
        }
        return "•";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class TransactionTypeNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TransactionType type)
        {
            return type switch
            {
                TransactionType.Expense => "支出",
                TransactionType.Income => "收入",
                TransactionType.Transfer => "转账",
                _ => "其他"
            };
        }
        return "其他";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class TransactionTypeColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TransactionType type)
        {
            return type switch
            {
                TransactionType.Expense => "#EF4444",
                TransactionType.Income => "#10B981",
                TransactionType.Transfer => "#3B82F6",
                _ => "#6B7280"
            };
        }
        return "#6B7280";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class TransactionTypeAmountColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TransactionType type)
        {
            return type switch
            {
                TransactionType.Expense => new SolidColorBrush(Color.Parse("#EF4444")),
                TransactionType.Income => new SolidColorBrush(Color.Parse("#10B981")),
                TransactionType.Transfer => new SolidColorBrush(Color.Parse("#3B82F6")),
                _ => new SolidColorBrush(Color.Parse("#6B7280"))
            };
        }
        return new SolidColorBrush(Color.Parse("#6B7280"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class TransactionStatusNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TransactionStatus status)
        {
            return status switch
            {
                TransactionStatus.Completed => "已完成",
                TransactionStatus.Refunded => "已退款",
                TransactionStatus.Pending => "待处理",
                TransactionStatus.Cancelled => "已关闭",
                TransactionStatus.Other => "其他",
                _ => status.ToString()
            };
        }
        return "未知";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class TransactionStatusColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TransactionStatus status)
        {
            return status switch
            {
                TransactionStatus.Completed => "#10B981",
                TransactionStatus.Refunded => "#F59E0B",
                TransactionStatus.Pending => "#3B82F6",
                TransactionStatus.Cancelled => "#6B7280",
                _ => "#9CA3AF"
            };
        }
        return "#9CA3AF";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class SelectionOpacityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected)
        {
            return isSelected ? 1.0 : 0.5;
        }
        return 1.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

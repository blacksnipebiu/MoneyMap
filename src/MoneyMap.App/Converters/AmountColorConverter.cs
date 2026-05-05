using System;
using System.Globalization;
using Avalonia.Data.Converters;
using MoneyMap.Core.Enums;

namespace MoneyMap.App.Converters;

public class AmountColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TransactionType type)
        {
            return type switch
            {
                TransactionType.Income => "#10B981",  // Green
                TransactionType.Expense => "#EF4444", // Red
                TransactionType.Transfer => "#6B7280", // Gray
                _ => "#111827"
            };
        }
        
        if (value is decimal amount)
        {
            return amount >= 0 ? "#10B981" : "#EF4444";
        }
        
        return "#111827";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
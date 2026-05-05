using MoneyMap.Core.Enums;

namespace MoneyMap.Core.DTOs;

public class CategorySummaryDto
{
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public TransactionType Type { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Percentage { get; set; }
    public int TransactionCount { get; set; }
}

public class CategoryTrendDto
{
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public List<MonthAmountDto> MonthlyData { get; set; } = new();
}

public class MonthAmountDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
}

public class TopCategoryDto
{
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
}
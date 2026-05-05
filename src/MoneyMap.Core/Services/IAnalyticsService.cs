using MoneyMap.Core.Enums;

namespace MoneyMap.Core.Services;

public interface IAnalyticsService
{
    Task<IEnumerable<CategoryBreakdown>> GetCategoryBreakdownAsync(DateTime from, DateTime to, TransactionType type);
    Task<IEnumerable<MonthlyTrend>> GetMonthlyTrendAsync(int year);
    Task<IEnumerable<DailyTrend>> GetDailyTrendAsync(DateTime from, DateTime to);
    Task<DashboardSummary> GetDashboardSummaryAsync();
}

public class CategoryBreakdown
{
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class MonthlyTrend
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Balance => Income - Expense;
}

public class DailyTrend
{
    public DateTime Date { get; set; }
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
}

public class DashboardSummary
{
    public decimal MonthIncome { get; set; }
    public decimal MonthExpense { get; set; }
    public decimal MonthBalance => MonthIncome - MonthExpense;
    public decimal TodayExpense { get; set; }
    public int TransactionCount { get; set; }
}

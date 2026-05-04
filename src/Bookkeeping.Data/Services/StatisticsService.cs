using Bookkeeping.Core.DTOs;
using Bookkeeping.Core.Enums;
using Bookkeeping.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace Bookkeeping.Data.Services;

public class StatisticsService : IStatisticsService
{
    private readonly BookkeepingDbContext _context;

    public StatisticsService(BookkeepingDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CategorySummaryDto>> GetCategorySummaryAsync(DateTime month, TransactionType type)
    {
        var startDate = new DateTime(month.Year, month.Month, 1);
        var endDate = startDate.AddMonths(1).AddTicks(-1);

        var query = _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.TransactionTime >= startDate && t.TransactionTime <= endDate)
            .Where(t => t.Type == type);

        var transactions = await query.ToListAsync();

        if (!transactions.Any())
            return Enumerable.Empty<CategorySummaryDto>();

        var grouped = transactions
            .GroupBy(t => t.CategoryId)
            .Select(g => new
            {
                CategoryId = g.Key ?? 0,
                CategoryName = g.First().CategoryName ?? "未分类",
                Icon = g.First().Category?.Icon,
                Type = type,
                TotalAmount = g.Sum(t => t.Amount),
                TransactionCount = g.Count()
            })
            .ToList();

        var totalAmount = grouped.Sum(g => g.TotalAmount);

        return grouped.Select(g => new CategorySummaryDto
        {
            CategoryId = g.CategoryId,
            CategoryName = g.CategoryName,
            Icon = g.Icon,
            Type = g.Type,
            TotalAmount = g.TotalAmount,
            Percentage = totalAmount > 0 ? Math.Round(g.TotalAmount / totalAmount * 100, 2) : 0,
            TransactionCount = g.TransactionCount
        }).OrderByDescending(x => x.TotalAmount);
    }

    public async Task<CategoryTrendDto> GetCategoryTrendAsync(long categoryId, int months)
    {
        var endDate = DateTime.Now;
        var startDate = endDate.AddMonths(-months + 1);
        startDate = new DateTime(startDate.Year, startDate.Month, 1);

        var category = await _context.Categories.FindAsync(categoryId);

        var transactions = await _context.Transactions
            .Where(t => t.CategoryId == categoryId)
            .Where(t => t.TransactionTime >= startDate && t.TransactionTime <= endDate)
            .ToListAsync();

        var monthlyData = new List<MonthAmountDto>();
        for (var date = startDate; date <= endDate; date = date.AddMonths(1))
        {
            var year = date.Year;
            var month = date.Month;
            var amount = transactions
                .Where(t => t.TransactionTime.Year == year && t.TransactionTime.Month == month)
                .Sum(t => t.Amount);

            monthlyData.Add(new MonthAmountDto
            {
                Year = year,
                Month = month,
                Amount = amount
            });
        }

        return new CategoryTrendDto
        {
            CategoryId = categoryId,
            CategoryName = category?.Name ?? "未知分类",
            MonthlyData = monthlyData
        };
    }

    public async Task<IEnumerable<TopCategoryDto>> GetTopCategoriesAsync(TransactionType type, int count, DateTime month)
    {
        var startDate = new DateTime(month.Year, month.Month, 1);
        var endDate = startDate.AddMonths(1).AddTicks(-1);

        var transactions = await _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.TransactionTime >= startDate && t.TransactionTime <= endDate)
            .Where(t => t.Type == type)
            .ToListAsync();

        if (!transactions.Any())
            return Enumerable.Empty<TopCategoryDto>();

        return transactions
            .GroupBy(t => t.CategoryId)
            .Select(g => new
            {
                CategoryId = g.Key ?? 0,
                CategoryName = g.First().CategoryName ?? "未分类",
                Icon = g.First().Category?.Icon,
                TotalAmount = g.Sum(t => t.Amount),
                TransactionCount = g.Count()
            })
            .OrderByDescending(g => g.TotalAmount)
            .Take(count)
            .Select(g => new TopCategoryDto
            {
                CategoryId = g.CategoryId,
                CategoryName = g.CategoryName,
                Icon = g.Icon,
                TotalAmount = g.TotalAmount,
                TransactionCount = g.TransactionCount
            });
    }
}
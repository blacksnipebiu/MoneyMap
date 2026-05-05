using MoneyMap.Core.DTOs;
using MoneyMap.Core.Enums;

namespace MoneyMap.Core.Services;

public interface IStatisticsService
{
    Task<IEnumerable<CategorySummaryDto>> GetCategorySummaryAsync(DateTime month, TransactionType type);
    Task<CategoryTrendDto> GetCategoryTrendAsync(long categoryId, int months);
    Task<IEnumerable<TopCategoryDto>> GetTopCategoriesAsync(TransactionType type, int count, DateTime month);
}
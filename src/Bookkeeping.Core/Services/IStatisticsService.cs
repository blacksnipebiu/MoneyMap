using Bookkeeping.Core.DTOs;
using Bookkeeping.Core.Enums;

namespace Bookkeeping.Core.Services;

public interface IStatisticsService
{
    Task<IEnumerable<CategorySummaryDto>> GetCategorySummaryAsync(DateTime month, TransactionType type);
    Task<CategoryTrendDto> GetCategoryTrendAsync(long categoryId, int months);
    Task<IEnumerable<TopCategoryDto>> GetTopCategoriesAsync(TransactionType type, int count, DateTime month);
}
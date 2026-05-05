using MoneyMap.Core.Enums;
using MoneyMap.Core.Models;

namespace MoneyMap.Core.Services;

public interface ICategoryService
{
    Task<IEnumerable<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(long id);
    Task<Category> CreateAsync(Category category);
    Task<Category> UpdateAsync(Category category);
    Task DeleteAsync(long id);
    Task<Category?> AutoCategorizeAsync(string description, string? counterparty, DataSource source);
    Task<IEnumerable<Category>> GetByTypeAsync(TransactionType type);
    Task<int> ReassignAndDeleteAsync(long deleteId, long targetId);
}

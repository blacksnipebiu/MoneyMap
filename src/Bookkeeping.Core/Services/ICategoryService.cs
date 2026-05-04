using Bookkeeping.Core.Enums;
using Bookkeeping.Core.Models;

namespace Bookkeeping.Core.Services;

public interface ICategoryService
{
    Task<IEnumerable<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(long id);
    Task<Category> CreateAsync(Category category);
    Task<Category> UpdateAsync(Category category);
    Task DeleteAsync(long id);
    Task<Category?> AutoCategorizeAsync(string description, string? counterparty, DataSource source);
    Task<IEnumerable<Category>> GetByTypeAsync(TransactionType type);
}

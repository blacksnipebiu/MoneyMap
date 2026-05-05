using Bookkeeping.Core.Models;
using Bookkeeping.Core.Services;

namespace Bookkeeping.Core.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(long id);
    Task<PagedResult<Transaction>> GetFilteredAsync(TransactionFilter filter, int page, int pageSize);
    Task AddAsync(Transaction transaction);
    Task AddRangeAsync(IEnumerable<Transaction> transactions);
    Task<bool> ExistsBySourceTransactionIdAsync(string sourceTransactionId);
    /// <summary>
    /// 批量查询已存在的 SourceTransactionId（用于导入去重）
    /// </summary>
    Task<HashSet<string>> GetExistingSourceIdsAsync(IEnumerable<string> sourceIds);
    Task<decimal> GetTotalByFilterAsync(TransactionFilter filter);
    Task UpdateAsync(Transaction transaction);
    Task DeleteAsync(long id);
}

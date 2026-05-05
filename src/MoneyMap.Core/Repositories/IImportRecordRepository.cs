using MoneyMap.Core.Models;

namespace MoneyMap.Core.Repositories;

public interface IImportRecordRepository
{
    Task<ImportRecord?> GetByIdAsync(long id);
    Task<IEnumerable<ImportRecord>> GetRecentAsync(int limit = 20);
    Task AddAsync(ImportRecord record);
    Task UpdateAsync(ImportRecord record);
}

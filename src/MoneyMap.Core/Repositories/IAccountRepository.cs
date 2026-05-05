using MoneyMap.Core.Enums;
using MoneyMap.Core.Models;

namespace MoneyMap.Core.Repositories;

public interface IAccountRepository
{
    Task<IEnumerable<Account>> GetAllAsync();
    Task<Account?> GetByIdAsync(long id);
    Task<Account?> GetBySourceAsync(DataSource source);
    Task AddAsync(Account account);
    Task UpdateAsync(Account account);
    Task DeleteAsync(long id);
}

using Bookkeeping.Core.Enums;
using Bookkeeping.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Bookkeeping.Data.Repositories;

public interface IAccountRepository
{
    Task<IEnumerable<Account>> GetAllAsync();
    Task<Account?> GetByIdAsync(long id);
    Task<Account?> GetBySourceAsync(DataSource source);
    Task AddAsync(Account account);
    Task UpdateAsync(Account account);
    Task DeleteAsync(long id);
}

public class AccountRepository : IAccountRepository
{
    private readonly BookkeepingDbContext _context;

    public AccountRepository(BookkeepingDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Account>> GetAllAsync()
    {
        return await _context.Accounts
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<Account?> GetByIdAsync(long id)
    {
        return await _context.Accounts.FindAsync(id);
    }

    public async Task<Account?> GetBySourceAsync(DataSource source)
    {
        return await _context.Accounts
            .FirstOrDefaultAsync(a => a.Source == source);
    }

    public async Task AddAsync(Account account)
    {
        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Account account)
    {
        _context.Accounts.Update(account);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var account = await _context.Accounts.FindAsync(id);
        if (account != null)
        {
            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();
        }
    }
}
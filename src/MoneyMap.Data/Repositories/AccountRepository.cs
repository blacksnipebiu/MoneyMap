using MoneyMap.Core.Enums;
using MoneyMap.Core.Models;
using MoneyMap.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MoneyMap.Data.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly MoneyMapDbContext _context;

    public AccountRepository(MoneyMapDbContext context)
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
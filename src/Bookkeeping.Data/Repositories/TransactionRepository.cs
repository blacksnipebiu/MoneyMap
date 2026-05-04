using Bookkeeping.Core.Enums;
using Bookkeeping.Core.Models;
using Bookkeeping.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace Bookkeeping.Data.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(long id);
    Task<PagedResult<Transaction>> GetFilteredAsync(TransactionFilter filter, int page, int pageSize);
    Task AddAsync(Transaction transaction);
    Task AddRangeAsync(IEnumerable<Transaction> transactions);
    Task<bool> ExistsBySourceTransactionIdAsync(string sourceTransactionId);
    Task<decimal> GetTotalByFilterAsync(TransactionFilter filter);
    Task UpdateAsync(Transaction transaction);
    Task DeleteAsync(long id);
}

public class TransactionRepository : ITransactionRepository
{
    private readonly BookkeepingDbContext _context;

    public TransactionRepository(BookkeepingDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction?> GetByIdAsync(long id)
    {
        return await _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.Account)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<PagedResult<Transaction>> GetFilteredAsync(TransactionFilter filter, int page, int pageSize)
    {
        var query = _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.Account)
            .AsQueryable();

        if (filter.DateFrom.HasValue)
            query = query.Where(t => t.TransactionTime >= filter.DateFrom.Value);
        
        if (filter.DateTo.HasValue)
            query = query.Where(t => t.TransactionTime <= filter.DateTo.Value);
        
        if (filter.Type.HasValue)
            query = query.Where(t => t.Type == filter.Type.Value);
        
        if (filter.Source.HasValue)
            query = query.Where(t => t.Source == filter.Source.Value);
        
        if (filter.CategoryId.HasValue)
            query = query.Where(t => t.CategoryId == filter.CategoryId.Value);
        
        if (filter.AccountId.HasValue)
            query = query.Where(t => t.AccountId == filter.AccountId.Value);
        
        if (!string.IsNullOrWhiteSpace(filter.SearchKeyword))
            query = query.Where(t => 
                (t.Description != null && t.Description.Contains(filter.SearchKeyword)) ||
                (t.Counterparty != null && t.Counterparty.Contains(filter.SearchKeyword)) ||
                (t.CategoryName != null && t.CategoryName.Contains(filter.SearchKeyword)));

        var totalCount = await query.CountAsync();
        
        var items = await query
            .OrderByDescending(t => t.TransactionTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Transaction>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task AddAsync(Transaction transaction)
    {
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Transaction> transactions)
    {
        await _context.Transactions.AddRangeAsync(transactions);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsBySourceTransactionIdAsync(string sourceTransactionId)
    {
        return await _context.Transactions
            .AnyAsync(t => t.SourceTransactionId == sourceTransactionId);
    }

    public async Task<decimal> GetTotalByFilterAsync(TransactionFilter filter)
    {
        var query = _context.Transactions.AsQueryable();

        if (filter.DateFrom.HasValue)
            query = query.Where(t => t.TransactionTime >= filter.DateFrom.Value);
        
        if (filter.DateTo.HasValue)
            query = query.Where(t => t.TransactionTime <= filter.DateTo.Value);
        
        if (filter.Type.HasValue)
            query = query.Where(t => t.Type == filter.Type.Value);

        return await query.SumAsync(t => t.Amount);
    }

    public async Task UpdateAsync(Transaction transaction)
    {
        _context.Transactions.Update(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var transaction = await _context.Transactions.FindAsync(id);
        if (transaction != null)
        {
            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
        }
    }
}
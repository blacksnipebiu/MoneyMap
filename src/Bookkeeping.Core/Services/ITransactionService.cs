using Bookkeeping.Core.Enums;
using Bookkeeping.Core.Models;

namespace Bookkeeping.Core.Services;

public interface ITransactionService
{
    Task<Transaction?> GetByIdAsync(long id);
    Task<PagedResult<Transaction>> GetFilteredAsync(TransactionFilter filter, int page, int pageSize);
    Task<IEnumerable<Transaction>> SearchAsync(string keyword, int limit = 50);
    Task<MonthlySummary> GetMonthlySummaryAsync(int year, int month);
    Task<Transaction> CreateAsync(Transaction transaction);
    Task<Transaction> UpdateAsync(Transaction transaction);
    Task DeleteAsync(long id);
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class TransactionFilter
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public TransactionType? Type { get; set; }
    public DataSource? Source { get; set; }
    public long? CategoryId { get; set; }
    public long? AccountId { get; set; }
    public string? SearchKeyword { get; set; }
}

public class MonthlySummary
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance => TotalIncome - TotalExpense;
    public int TransactionCount { get; set; }
}

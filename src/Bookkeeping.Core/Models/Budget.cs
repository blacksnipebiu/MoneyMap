namespace Bookkeeping.Core.Models;

public class Budget
{
    public long Id { get; set; }
    public long CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

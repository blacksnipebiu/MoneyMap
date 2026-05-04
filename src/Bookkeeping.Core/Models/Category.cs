using Bookkeeping.Core.Enums;

namespace Bookkeeping.Core.Models;

public class Category
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public TransactionType Type { get; set; }
    public long? ParentId { get; set; }
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public string? AutoMatchPattern { get; set; }
    public string? SourceCategoryMapping { get; set; }
    public int SortOrder { get; set; }
}

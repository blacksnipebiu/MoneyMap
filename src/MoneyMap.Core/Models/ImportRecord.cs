using MoneyMap.Core.Enums;

namespace MoneyMap.Core.Models;

public class ImportRecord
{
    public long Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public DataSource Source { get; set; }
    public DateTime ImportTime { get; set; }
    public int TotalRows { get; set; }
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public string? ErrorDetails { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

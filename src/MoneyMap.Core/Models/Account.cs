using MoneyMap.Core.Enums;

namespace MoneyMap.Core.Models;

public class Account
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DataSource Source { get; set; }
    public string? LastFourDigits { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

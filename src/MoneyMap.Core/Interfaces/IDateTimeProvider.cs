namespace MoneyMap.Core.Interfaces;

public interface IDateTimeProvider
{
    DateTime Now { get; }
    DateTime UtcNow { get; }
    DateTime Today { get; }
}

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime Now => DateTime.Now;
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Today => DateTime.Today;
}

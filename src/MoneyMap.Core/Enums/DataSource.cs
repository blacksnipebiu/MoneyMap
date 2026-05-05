namespace MoneyMap.Core.Enums;

public enum DataSource
{
    Alipay = 1,
    WeChatPay = 2,
    BankCard = 3,
    Manual = 4,
    Other = 99
}

public static class DataSourceHelper
{
    public static DataSource[] AllSources { get; } = new[]
    {
        DataSource.Alipay,
        DataSource.WeChatPay,
        DataSource.BankCard,
        DataSource.Manual,
        DataSource.Other
    };
}

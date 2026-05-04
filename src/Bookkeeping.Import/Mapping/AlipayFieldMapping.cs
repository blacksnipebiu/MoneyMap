namespace Bookkeeping.Import.Mapping;

public static class AlipayFieldMapping
{
    // Column names in Alipay CSV (Chinese)
    public const string TransactionTime = "交易时间";
    public const string Category = "交易分类";
    public const string Counterparty = "交易对方";
    public const string CounterpartyAccount = "对方账号";
    public const string Description = "商品说明";
    public const string Direction = "收/支";
    public const string Amount = "金额";
    public const string PaymentMethod = "收/付款方式";
    public const string Status = "交易状态";
    public const string TransactionId = "交易订单号";
    public const string MerchantOrderId = "商家订单号";
    public const string Remark = "备注";
}

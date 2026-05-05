namespace MoneyMap.Import.Mapping;

public static class WeChatFieldMapping
{
    // Column names in WeChat XLSX (Chinese)
    public const string TransactionTime = "交易时间";
    public const string TransactionType = "交易类型";
    public const string Counterparty = "交易对方";
    public const string Product = "商品";
    public const string Direction = "收/支";
    public const string Amount = "金额(元)";
    public const string PaymentMethod = "支付方式";
    public const string Status = "当前状态";
    public const string TransactionId = "交易单号";
    public const string MerchantOrderId = "商户单号";
    public const string Remark = "备注";
}

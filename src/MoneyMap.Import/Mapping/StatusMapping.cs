using MoneyMap.Core.Enums;

namespace MoneyMap.Import.Mapping;

/// <summary>
/// 各来源交易状态 → 标准 TransactionStatus 枚举映射
/// </summary>
public static class StatusMapping
{
    /// <summary>
    /// 支付宝状态映射
    /// 支付宝常见状态：交易成功、交易关闭、退款成功、等待付款等
    /// </summary>
    public static readonly Dictionary<string, TransactionStatus> AlipayStatusMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["交易成功"] = TransactionStatus.Completed,
        ["支付成功"] = TransactionStatus.Completed,
        ["已付款"] = TransactionStatus.Completed,
        ["已完成"] = TransactionStatus.Completed,
        ["退款成功"] = TransactionStatus.Refunded,
        ["已退款"] = TransactionStatus.Refunded,
        ["部分退款"] = TransactionStatus.Refunded,
        ["等待付款"] = TransactionStatus.Pending,
        ["待付款"] = TransactionStatus.Pending,
        ["待处理"] = TransactionStatus.Pending,
        ["交易关闭"] = TransactionStatus.Cancelled,
        ["已关闭"] = TransactionStatus.Cancelled,
        ["已取消"] = TransactionStatus.Cancelled,
    };

    /// <summary>
    /// 微信支付状态映射
    /// 微信常见状态：支付成功、已转账、已退款、交易关闭等
    /// </summary>
    public static readonly Dictionary<string, TransactionStatus> WeChatStatusMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["支付成功"] = TransactionStatus.Completed,
        ["已转账"] = TransactionStatus.Completed,
        ["已到账"] = TransactionStatus.Completed,
        ["已充值"] = TransactionStatus.Completed,
        ["已收款"] = TransactionStatus.Completed,
        ["已存入零钱"] = TransactionStatus.Completed,
        ["退款成功"] = TransactionStatus.Refunded,
        ["已退款"] = TransactionStatus.Refunded,
        ["已退回"] = TransactionStatus.Refunded,
        ["待付款"] = TransactionStatus.Pending,
        ["交易关闭"] = TransactionStatus.Cancelled,
        ["已关闭"] = TransactionStatus.Cancelled,
    };

    /// <summary>
    /// 将来源原始状态文本映射为标准 TransactionStatus
    /// </summary>
    public static TransactionStatus MapStatus(string rawStatus, DataSource source)
    {
        if (string.IsNullOrWhiteSpace(rawStatus))
            return TransactionStatus.Other;

        var map = source switch
        {
            DataSource.Alipay => AlipayStatusMap,
            DataSource.WeChatPay => WeChatStatusMap,
            _ => null
        };

        if (map != null && map.TryGetValue(rawStatus.Trim(), out var status))
            return status;

        // 模糊匹配：包含关键词
        var normalized = rawStatus.Trim();
        if (normalized.Contains("退款") || normalized.Contains("退回"))
            return TransactionStatus.Refunded;
        if (normalized.Contains("成功") || normalized.Contains("完成") || normalized.Contains("已到"))
            return TransactionStatus.Completed;
        if (normalized.Contains("等待") || normalized.Contains("待"))
            return TransactionStatus.Pending;
        if (normalized.Contains("关闭") || normalized.Contains("取消"))
            return TransactionStatus.Cancelled;

        return TransactionStatus.Other;
    }
}

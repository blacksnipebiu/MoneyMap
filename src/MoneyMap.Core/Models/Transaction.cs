using MoneyMap.Core.Enums;

namespace MoneyMap.Core.Models;

public class Transaction
{
    public long Id { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransactionTime { get; set; }
    public string? Counterparty { get; set; }
    /// <summary>
    /// 对方账号（支付宝有此字段，用于识别同一对方跨交易）
    /// </summary>
    public string? CounterpartyAccount { get; set; }
    public string? Description { get; set; }
    public string? CategoryName { get; set; }
    public long? CategoryId { get; set; }
    public Category? Category { get; set; }
    public DataSource Source { get; set; }
    /// <summary>
    /// 数据来源名称（记录该条数据来自哪个账户的导入，如"我的支付宝"）
    /// </summary>
    public string? DataSourceName { get; set; }
    /// <summary>
    /// 支付/收款方式，如：余额、余额宝、花呗、银行卡等
    /// </summary>
    public string? PaymentMethod { get; set; }
    /// <summary>
    /// 交易状态（标准化枚举）
    /// </summary>
    public TransactionStatus Status { get; set; } = TransactionStatus.Completed;
    /// <summary>
    /// 交易状态原始文本（保留来源原始值，如"交易成功"、"已退款"等）
    /// </summary>
    public string? RawStatus { get; set; }
    /// <summary>
    /// 备注（用户手动备注，区别于 Description 商品说明）
    /// </summary>
    public string? Remark { get; set; }
    /// <summary>
    /// 商家订单号（退款追溯、去重）
    /// </summary>
    public string? MerchantOrderId { get; set; }
    public long AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public long ImportRecordId { get; set; }
    public ImportRecord ImportRecord { get; set; } = null!;
    public string? RawLine { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? SourceTransactionId { get; set; }
}

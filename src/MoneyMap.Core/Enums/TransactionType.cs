namespace MoneyMap.Core.Enums;

public enum TransactionType
{
    Income = 1,
    Expense = 2,
    Transfer = 3
}

/// <summary>
/// 交易状态（标准化枚举，各来源原始状态文本通过 StatusMapping 映射到此枚举）
/// </summary>
public enum TransactionStatus
{
    /// <summary>交易完成</summary>
    Completed = 1,
    /// <summary>已退款</summary>
    Refunded = 2,
    /// <summary>待处理</summary>
    Pending = 3,
    /// <summary>已关闭/已取消</summary>
    Cancelled = 4,
    /// <summary>其他/无法识别</summary>
    Other = 99
}

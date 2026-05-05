using Bookkeeping.Core.Models;

namespace Bookkeeping.Core.Services;

/// <summary>
/// 导入数据验证服务
/// </summary>
public interface IImportValidationService
{
    /// <summary>
    /// 验证交易数据
    /// </summary>
    ValidationResult ValidateTransaction(Transaction transaction, int rowNumber);
    
    /// <summary>
    /// 批量验证交易数据
    /// </summary>
    BatchValidationResult ValidateTransactions(IEnumerable<Transaction> transactions);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = new();
    public List<ValidationWarning> Warnings { get; set; } = new();
    
    public static ValidationResult Success() => new() { IsValid = true };
    
    public static ValidationResult Failure(params ValidationError[] errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };
}

public class BatchValidationResult
{
    public int TotalCount { get; set; }
    public int ValidCount { get; set; }
    public int InvalidCount { get; set; }
    public List<TransactionValidationResult> Results { get; set; } = new();
    public List<ValidationError> AllErrors { get; set; } = new();
    public List<ValidationWarning> AllWarnings { get; set; } = new();
}

public class TransactionValidationResult
{
    public int RowNumber { get; set; }
    public Transaction Transaction { get; set; } = null!;
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = new();
    public List<ValidationWarning> Warnings { get; set; } = new();
}

public class ValidationError
{
    public int RowNumber { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? InvalidValue { get; set; }
}

public class ValidationWarning
{
    public int RowNumber { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? Value { get; set; }
}

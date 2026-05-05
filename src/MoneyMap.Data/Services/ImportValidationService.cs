using MoneyMap.Core.Models;
using MoneyMap.Core.Services;

namespace MoneyMap.Data.Services;

/// <summary>
/// 导入数据验证服务实现
/// </summary>
public class ImportValidationService : IImportValidationService
{
    private static readonly DateTime MinTransactionDate = new(2000, 1, 1);
    private static readonly DateTime MaxTransactionDate = DateTime.Now.AddDays(1); // 允许最多未来1天
    
    public ValidationResult ValidateTransaction(Transaction transaction, int rowNumber)
    {
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationWarning>();
        
        // 验证金额
        if (transaction.Amount <= 0)
        {
            errors.Add(new ValidationError
            {
                RowNumber = rowNumber,
                Field = "Amount",
                Message = "金额必须大于零",
                InvalidValue = transaction.Amount
            });
        }
        else if (transaction.Amount > 1000000)
        {
            warnings.Add(new ValidationWarning
            {
                RowNumber = rowNumber,
                Field = "Amount",
                Message = "金额异常大，请确认是否正确",
                Value = transaction.Amount
            });
        }
        
        // 验证交易时间
        if (transaction.TransactionTime < MinTransactionDate)
        {
            errors.Add(new ValidationError
            {
                RowNumber = rowNumber,
                Field = "TransactionTime",
                Message = $"交易时间过早，不能早于 {MinTransactionDate:yyyy-MM-dd}",
                InvalidValue = transaction.TransactionTime
            });
        }
        else if (transaction.TransactionTime > MaxTransactionDate)
        {
            errors.Add(new ValidationError
            {
                RowNumber = rowNumber,
                Field = "TransactionTime",
                Message = "交易时间不能在未来",
                InvalidValue = transaction.TransactionTime
            });
        }
        
        // 验证必填字段（AccountId 在导入时设置，不在此验证）
        if (string.IsNullOrWhiteSpace(transaction.Description) &&
            string.IsNullOrWhiteSpace(transaction.Counterparty))
        {
            warnings.Add(new ValidationWarning
            {
                RowNumber = rowNumber,
                Field = "Description/Counterparty",
                Message = "缺少交易描述和交易对方信息",
                Value = null
            });
        }
        
        // 验证 SourceTransactionId 格式（如果有）
        if (!string.IsNullOrEmpty(transaction.SourceTransactionId) &&
            transaction.SourceTransactionId.Length > 100)
        {
            errors.Add(new ValidationError
            {
                RowNumber = rowNumber,
                Field = "SourceTransactionId",
                Message = "来源交易ID过长",
                InvalidValue = transaction.SourceTransactionId
            });
        }
        
        // 验证 MerchantOrderId 格式（如果有）
        if (!string.IsNullOrEmpty(transaction.MerchantOrderId) &&
            transaction.MerchantOrderId.Length > 100)
        {
            errors.Add(new ValidationError
            {
                RowNumber = rowNumber,
                Field = "MerchantOrderId",
                Message = "商家订单号过长",
                InvalidValue = transaction.MerchantOrderId
            });
        }
        
        if (errors.Count > 0)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = errors,
                Warnings = warnings
            };
        }
        
        return new ValidationResult
        {
            IsValid = true,
            Warnings = warnings
        };
    }
    
    public BatchValidationResult ValidateTransactions(IEnumerable<Transaction> transactions)
    {
        var results = new List<TransactionValidationResult>();
        var allErrors = new List<ValidationError>();
        var allWarnings = new List<ValidationWarning>();
        int rowNumber = 1;
        
        foreach (var transaction in transactions)
        {
            var result = ValidateTransaction(transaction, rowNumber);
            
            var transactionResult = new TransactionValidationResult
            {
                RowNumber = rowNumber,
                Transaction = transaction,
                IsValid = result.IsValid,
                Errors = result.Errors,
                Warnings = result.Warnings
            };
            
            results.Add(transactionResult);
            allErrors.AddRange(result.Errors);
            allWarnings.AddRange(result.Warnings);
            rowNumber++;
        }
        
        return new BatchValidationResult
        {
            TotalCount = results.Count,
            ValidCount = results.Count(r => r.IsValid),
            InvalidCount = results.Count(r => !r.IsValid),
            Results = results,
            AllErrors = allErrors,
            AllWarnings = allWarnings
        };
    }
}
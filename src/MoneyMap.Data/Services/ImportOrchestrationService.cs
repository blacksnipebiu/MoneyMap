using MoneyMap.Core.Enums;
using MoneyMap.Core.Models;
using MoneyMap.Core.Repositories;
using MoneyMap.Core.Services;
using MoneyMap.Data.Repositories;

namespace MoneyMap.Data.Services;

/// <summary>
/// 导入协调服务 - 协调导入流程的各个步骤
/// </summary>
public interface IImportOrchestrationService
{
    /// <summary>
    /// 执行导入（包含验证、去重、保存）
    /// </summary>
    Task<ImportExecutionResult> ExecuteImportAsync(
        IEnumerable<Transaction> transactions,
        Account account,
        string fileName,
        DataSource source,
        string? filePath = null,
        long? importRecordId = null,
        IProgress<ImportProgressInfo>? progress = null);
}

public class ImportExecutionResult
{
    public bool Success { get; set; }
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int ValidationErrorCount { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public List<string> ValidationWarnings { get; set; } = new();
    public ImportRecord? ImportRecord { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ImportProgressInfo
{
    public int Progress { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
}

public class ImportOrchestrationService : IImportOrchestrationService
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly IImportRecordRepository _importRecordRepo;
    private readonly IImportValidationService _validationService;

    public ImportOrchestrationService(
        ITransactionRepository transactionRepo,
        IImportRecordRepository importRecordRepo,
        IImportValidationService validationService)
    {
        _transactionRepo = transactionRepo;
        _importRecordRepo = importRecordRepo;
        _validationService = validationService;
    }

    public async Task<ImportExecutionResult> ExecuteImportAsync(
        IEnumerable<Transaction> transactions,
        Account account,
        string fileName,
        DataSource source,
        string? filePath = null,
        long? importRecordId = null,
        IProgress<ImportProgressInfo>? progress = null)
    {
        var result = new ImportExecutionResult();
        var transactionList = transactions.ToList();
        int totalSteps = 4; // 验证、去重、准备、保存

        try
        {
            // Step 1: 验证数据
            progress?.Report(new ImportProgressInfo { Progress = 10, Status = "正在验证数据...", CurrentStep = 1, TotalSteps = totalSteps });

            var validationResult = _validationService.ValidateTransactions(transactionList);
            result.ValidationErrors = validationResult.AllErrors
                .Select(e => $"第 {e.RowNumber} 行 [{e.Field}]: {e.Message}")
                .ToList();
            result.ValidationWarnings = validationResult.AllWarnings
                .Select(w => $"第 {w.RowNumber} 行 [{w.Field}]: {w.Message}")
                .ToList();

            // 过滤掉无效数据
            var validTransactions = validationResult.Results
                .Where(r => r.IsValid)
                .Select(r => r.Transaction)
                .ToList();

            result.ValidationErrorCount = transactionList.Count - validTransactions.Count;

            if (validTransactions.Count == 0)
            {
                result.Success = false;
                result.ErrorMessage = "没有有效的交易记录可导入";
                return result;
            }

            // Step 2: 去重
            progress?.Report(new ImportProgressInfo { Progress = 30, Status = "正在检查重复记录...", CurrentStep = 2, TotalSteps = totalSteps });

            var sourceIds = validTransactions
                .Where(t => !string.IsNullOrEmpty(t.SourceTransactionId))
                .Select(t => t.SourceTransactionId!)
                .ToList();

            HashSet<string> existingIds = new();
            if (sourceIds.Count > 0)
            {
                existingIds = await _transactionRepo.GetExistingSourceIdsAsync(sourceIds);
            }

            var newTransactions = validTransactions
                .Where(t => string.IsNullOrEmpty(t.SourceTransactionId) || !existingIds.Contains(t.SourceTransactionId))
                .ToList();

            result.DuplicateCount = validTransactions.Count - newTransactions.Count;

            if (newTransactions.Count == 0)
            {
                result.Success = true;
                result.SkippedCount = transactionList.Count;
                result.ErrorMessage = "所有记录都是重复数据";
                return result;
            }

            // Step 3: 加载已有的 ImportRecord（解析阶段已创建），或创建新的
            progress?.Report(new ImportProgressInfo { Progress = 50, Status = "正在准备导入...", CurrentStep = 3, TotalSteps = totalSteps });

            ImportRecord? importRecord = null;
            if (importRecordId.HasValue && importRecordId.Value > 0)
            {
                importRecord = await _importRecordRepo.GetByIdAsync(importRecordId.Value);
            }

            if (importRecord == null)
            {
                importRecord = new ImportRecord
                {
                    FileName = fileName,
                    FilePath = filePath,
                    Source = source,
                    ImportTime = DateTime.Now,
                    TotalRows = transactionList.Count,
                };
                await _importRecordRepo.AddAsync(importRecord);
            }

            // 更新导入阶段的计数
            importRecord.ImportedCount = newTransactions.Count;
            importRecord.SkippedCount = result.ValidationErrorCount + result.DuplicateCount;
            importRecord.ErrorCount = result.ValidationErrorCount;

            foreach (var t in newTransactions)
            {
                t.ImportRecordId = importRecord.Id;
                t.ImportRecord = importRecord;
                t.AccountId = account.Id;
                t.DataSourceName = account.Name;
            }

            // Step 4: 保存到数据库
            progress?.Report(new ImportProgressInfo { Progress = 70, Status = "正在保存到数据库...", CurrentStep = 4, TotalSteps = totalSteps });

            await _transactionRepo.AddRangeAsync(newTransactions);

            // 导入完成后更新 ImportRecord
            await _importRecordRepo.UpdateAsync(importRecord);

            result.Success = true;
            result.ImportedCount = newTransactions.Count;
            result.SkippedCount = importRecord.SkippedCount;
            result.ImportRecord = importRecord;

            progress?.Report(new ImportProgressInfo { Progress = 100, Status = "导入完成！", CurrentStep = totalSteps, TotalSteps = totalSteps });

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"导入失败: {ex.Message}";
            return result;
        }
    }
}
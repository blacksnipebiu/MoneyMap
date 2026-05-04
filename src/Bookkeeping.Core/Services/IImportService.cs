using Bookkeeping.Core.Enums;
using Bookkeeping.Core.Models;

namespace Bookkeeping.Core.Services;

public interface IImportService
{
    Task<ImportResult> ImportFileAsync(string filePath, DataSource? forcedSource = null);
    Task<IEnumerable<ImportRecord>> GetImportHistoryAsync(int limit = 20);
    Task<ImportRecord?> GetImportRecordByIdAsync(long id);
}

public class ImportResult
{
    public bool Success { get; set; }
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public DataSource DetectedSource { get; set; }
    public string? ErrorMessage { get; set; }
    public ImportRecord? ImportRecord { get; set; }
}

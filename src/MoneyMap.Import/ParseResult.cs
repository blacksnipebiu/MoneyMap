using MoneyMap.Core.Enums;
using MoneyMap.Core.Models;

namespace MoneyMap.Import;

public class ParseResult
{
    public List<Transaction> Transactions { get; set; } = new();
    public List<ParseError> Errors { get; set; } = new();
    public int SkippedCount { get; set; }
    public int TotalRows { get; set; }
    public DataSource DetectedSource { get; set; }
}

public class ParseError
{
    public int RowNumber { get; set; }
    public string RawLine { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    /// <summary>
    /// 出错的字段名
    /// </summary>
    public string? FieldName { get; set; }
    /// <summary>
    /// 原始值
    /// </summary>
    public string? RawValue { get; set; }
    
    public override string ToString()
    {
        var location = RowNumber > 0 ? $"第 {RowNumber} 行" : "文件";
        var field = !string.IsNullOrEmpty(FieldName) ? $" [{FieldName}]" : "";
        return $"{location}{field}: {ErrorMessage}";
    }
}

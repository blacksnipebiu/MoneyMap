using Bookkeeping.Core.Enums;
using Bookkeeping.Core.Models;

namespace Bookkeeping.Import;

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
}

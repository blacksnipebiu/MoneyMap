using Bookkeeping.Core.Enums;

namespace Bookkeeping.Import;

public interface IRecordParser
{
    DataSource SupportedSource { get; }
    ParseResult Parse(Stream fileStream, string fileName);
    bool CanParse(Stream fileStream, string fileName);
}

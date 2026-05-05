using MoneyMap.Core.Enums;

namespace MoneyMap.Import;

public interface IRecordParser
{
    DataSource SupportedSource { get; }
    ParseResult Parse(Stream fileStream, string fileName);
    bool CanParse(Stream fileStream, string fileName);
}

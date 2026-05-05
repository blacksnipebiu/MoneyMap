using MoneyMap.Core.Enums;
using MoneyMap.Import.Parsers;

namespace MoneyMap.Import;

public class ParserFactory
{
    private readonly Dictionary<DataSource, IRecordParser> _parsers;

    public ParserFactory(IEnumerable<IRecordParser> parsers)
    {
        _parsers = parsers.ToDictionary(p => p.SupportedSource);
    }

    public IRecordParser? GetParser(DataSource source)
    {
        return _parsers.TryGetValue(source, out var parser) ? parser : null;
    }

    public IEnumerable<DataSource> SupportedSources => _parsers.Keys;
}
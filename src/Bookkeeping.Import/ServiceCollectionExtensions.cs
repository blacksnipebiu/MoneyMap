using Microsoft.Extensions.DependencyInjection;
using Bookkeeping.Import.Parsers;
using Bookkeeping.Import.Detection;

namespace Bookkeeping.Import;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBookkeepingImport(this IServiceCollection services)
    {
        services.AddSingleton<SourceDetector>();
        services.AddSingleton<IRecordParser, AlipayParser>();
        services.AddSingleton<IRecordParser, WeChatPayParser>();
        services.AddSingleton<ParserFactory>();

        return services;
    }
}
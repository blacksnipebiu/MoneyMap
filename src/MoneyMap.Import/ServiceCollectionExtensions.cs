using Microsoft.Extensions.DependencyInjection;
using MoneyMap.Import.Parsers;
using MoneyMap.Import.Detection;

namespace MoneyMap.Import;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMoneyMapImport(this IServiceCollection services)
    {
        services.AddSingleton<SourceDetector>();
        services.AddSingleton<IRecordParser, AlipayParser>();
        services.AddSingleton<IRecordParser, WeChatPayParser>();
        services.AddSingleton<ParserFactory>();

        return services;
    }
}
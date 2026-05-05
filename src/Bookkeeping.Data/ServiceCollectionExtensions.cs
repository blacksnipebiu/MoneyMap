using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Bookkeeping.Data.Repositories;
using Bookkeeping.Data.Services;
using Bookkeeping.Core.Enums;
using Bookkeeping.Core.Models;
using Bookkeeping.Core.Services;
using Bookkeeping.Core.Repositories;

namespace Bookkeeping.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBookkeepingData(this IServiceCollection services, string dbPath)
    {
        services.AddDbContext<BookkeepingDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IImportRecordRepository, ImportRecordRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IImportValidationService, ImportValidationService>();
        services.AddScoped<IImportOrchestrationService, ImportOrchestrationService>();
        services.AddScoped<IImportMappingService, ImportMappingService>();

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BookkeepingDbContext>();
        await context.Database.EnsureCreatedAsync();

        // Seed default categories
        await SeedDefaultCategoriesAsync(context);
    }

    private static async Task SeedDefaultCategoriesAsync(BookkeepingDbContext context)
    {
        if (await context.Categories.AnyAsync())
            return;

        var defaultCategories = new[]
        {
            // Expense categories
            new Category { Name = "餐饮美食", Type = TransactionType.Expense, Icon = "🍽️", SortOrder = 1, SourceCategoryMapping = "餐饮美食,美食,餐饮,外卖,快餐,零食", AutoMatchPattern = "餐饮|美食|外卖|快餐" },
            new Category { Name = "交通出行", Type = TransactionType.Expense, Icon = "🚗", SortOrder = 2, SourceCategoryMapping = "交通出行,交通,出行,打车,公交,地铁,加油", AutoMatchPattern = "交通|出行|打车|公交|地铁" },
            new Category { Name = "购物消费", Type = TransactionType.Expense, Icon = "🛒", SortOrder = 3, SourceCategoryMapping = "购物消费,购物,百货,数码,网购", AutoMatchPattern = "购物|百货|数码" },
            new Category { Name = "生活服务", Type = TransactionType.Expense, Icon = "🏠", SortOrder = 4, SourceCategoryMapping = "生活服务,生活,缴费,水电,物业,快递", AutoMatchPattern = "生活|缴费|水电|物业" },
            new Category { Name = "休闲娱乐", Type = TransactionType.Expense, Icon = "🎮", SortOrder = 5, SourceCategoryMapping = "休闲娱乐,娱乐,游戏,电影,KTV", AutoMatchPattern = "娱乐|游戏|电影|KTV" },
            new Category { Name = "医疗健康", Type = TransactionType.Expense, Icon = "🏥", SortOrder = 6, SourceCategoryMapping = "医疗健康,医疗,健康,药品,医院", AutoMatchPattern = "医疗|健康|药品|医院" },
            new Category { Name = "教育培训", Type = TransactionType.Expense, Icon = "📚", SortOrder = 7, SourceCategoryMapping = "教育培训,教育,培训,课程,学费", AutoMatchPattern = "教育|培训|课程|学费" },
            new Category { Name = "投资理财", Type = TransactionType.Expense, Icon = "💰", SortOrder = 8, SourceCategoryMapping = "投资理财,理财,基金,股票", AutoMatchPattern = "投资|理财|基金|股票" },
            new Category { Name = "其他支出", Type = TransactionType.Expense, Icon = "📦", SortOrder = 99, SourceCategoryMapping = "其他支出,其他,杂费", AutoMatchPattern = null },

            // Income categories
            new Category { Name = "工资收入", Type = TransactionType.Income, Icon = "💼", SortOrder = 1, SourceCategoryMapping = "工资收入,工资,薪水,薪酬", AutoMatchPattern = "工资|薪水|薪酬" },
            new Category { Name = "奖金收入", Type = TransactionType.Income, Icon = "🎁", SortOrder = 2, SourceCategoryMapping = "奖金收入,奖金,年终奖,绩效奖", AutoMatchPattern = "奖金|年终奖" },
            new Category { Name = "投资收益", Type = TransactionType.Income, Icon = "📈", SortOrder = 3, SourceCategoryMapping = "投资收益,理财收益,利息,分红", AutoMatchPattern = "收益|利息|分红|理财" },
            new Category { Name = "红包收入", Type = TransactionType.Income, Icon = "🧧", SortOrder = 4, SourceCategoryMapping = "红包收入,红包,转账收入", AutoMatchPattern = "红包" },
            new Category { Name = "退款收入", Type = TransactionType.Income, Icon = "↩️", SortOrder = 5, SourceCategoryMapping = "退款收入,退款,退回", AutoMatchPattern = "退款|退回" },
            new Category { Name = "其他收入", Type = TransactionType.Income, Icon = "💵", SortOrder = 99, SourceCategoryMapping = "其他收入,其他", AutoMatchPattern = null },
        };

        await context.Categories.AddRangeAsync(defaultCategories);
        await context.SaveChangesAsync();
    }
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Bookkeeping.Data.Repositories;
using Bookkeeping.Core.Enums;
using Bookkeeping.Core.Models;

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
            new Category { Name = "餐饮美食", Type = TransactionType.Expense, Icon = "🍽️", SortOrder = 1 },
            new Category { Name = "交通出行", Type = TransactionType.Expense, Icon = "🚗", SortOrder = 2 },
            new Category { Name = "购物消费", Type = TransactionType.Expense, Icon = "🛒", SortOrder = 3 },
            new Category { Name = "生活服务", Type = TransactionType.Expense, Icon = "🏠", SortOrder = 4 },
            new Category { Name = "休闲娱乐", Type = TransactionType.Expense, Icon = "🎮", SortOrder = 5 },
            new Category { Name = "医疗健康", Type = TransactionType.Expense, Icon = "🏥", SortOrder = 6 },
            new Category { Name = "教育培训", Type = TransactionType.Expense, Icon = "📚", SortOrder = 7 },
            new Category { Name = "投资理财", Type = TransactionType.Transfer, Icon = "💰", SortOrder = 8 },
            new Category { Name = "其他支出", Type = TransactionType.Expense, Icon = "📦", SortOrder = 99 },
            
            // Income categories
            new Category { Name = "工资收入", Type = TransactionType.Income, Icon = "💼", SortOrder = 1 },
            new Category { Name = "奖金收入", Type = TransactionType.Income, Icon = "🎁", SortOrder = 2 },
            new Category { Name = "投资收益", Type = TransactionType.Income, Icon = "📈", SortOrder = 3 },
            new Category { Name = "红包收入", Type = TransactionType.Income, Icon = "🧧", SortOrder = 4 },
            new Category { Name = "退款收入", Type = TransactionType.Income, Icon = "↩️", SortOrder = 5 },
            new Category { Name = "其他收入", Type = TransactionType.Income, Icon = "💵", SortOrder = 99 },
        };

        await context.Categories.AddRangeAsync(defaultCategories);
        await context.SaveChangesAsync();
    }
}
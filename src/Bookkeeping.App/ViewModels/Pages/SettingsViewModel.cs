using System;
using System.IO;
using System.Threading.Tasks;
using Bookkeeping.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bookkeeping.App.ViewModels.Pages;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    [ObservableProperty]
    private string _databasePath = "";

    [ObservableProperty]
    private string _appVersion = "1.0.0";

    [ObservableProperty]
    private int _transactionCount;

    [ObservableProperty]
    private bool _isResetting;

    public SettingsViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        LoadSettings();
    }

    private void LoadSettings()
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Bookkeeping", "bookkeeping.db");
        DatabasePath = dbPath;
        
        _ = LoadStatsAsync();
    }

    private async Task LoadStatsAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BookkeepingDbContext>();
            TransactionCount = await context.Transactions.CountAsync();
        }
        catch
        {
            TransactionCount = 0;
        }
    }

    [RelayCommand]
    private async Task ResetDatabaseAsync()
    {
        if (IsResetting) return;

        IsResetting = true;
        try
        {
            // Clear all SQLite connection pools to release file locks
            SqliteConnection.ClearAllPools();
            
            // Small delay to ensure connections are fully released
            await Task.Delay(100);

            // Delete database file
            if (File.Exists(DatabasePath))
            {
                File.Delete(DatabasePath);
            }

            // Ensure directory exists
            var dir = Path.GetDirectoryName(DatabasePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Recreate database with fresh connection
            var options = new DbContextOptionsBuilder<BookkeepingDbContext>()
                .UseSqlite($"Data Source={DatabasePath}")
                .Options;
            
            using var context = new BookkeepingDbContext(options);
            await context.Database.EnsureCreatedAsync();
            
            // Seed default categories
            await SeedDefaultCategoriesAsync(context);

            TransactionCount = 0;
            App.ToastService.ShowSuccess("数据库已重置");
        }
        catch (Exception ex)
        {
            App.ToastService.ShowError("重置失败：" + ex.Message);
        }
        finally
        {
            IsResetting = false;
        }
    }

    private async Task SeedDefaultCategoriesAsync(BookkeepingDbContext context)
    {
        if (await context.Categories.AnyAsync())
            return;

        var defaultCategories = new[]
        {
            new Core.Models.Category { Name = "餐饮美食", Type = Core.Enums.TransactionType.Expense, Icon = "🍽️", SortOrder = 1 },
            new Core.Models.Category { Name = "交通出行", Type = Core.Enums.TransactionType.Expense, Icon = "🚗", SortOrder = 2 },
            new Core.Models.Category { Name = "购物消费", Type = Core.Enums.TransactionType.Expense, Icon = "🛒", SortOrder = 3 },
            new Core.Models.Category { Name = "生活服务", Type = Core.Enums.TransactionType.Expense, Icon = "🏠", SortOrder = 4 },
            new Core.Models.Category { Name = "休闲娱乐", Type = Core.Enums.TransactionType.Expense, Icon = "🎮", SortOrder = 5 },
            new Core.Models.Category { Name = "医疗健康", Type = Core.Enums.TransactionType.Expense, Icon = "🏥", SortOrder = 6 },
            new Core.Models.Category { Name = "教育培训", Type = Core.Enums.TransactionType.Expense, Icon = "📚", SortOrder = 7 },
            new Core.Models.Category { Name = "投资理财", Type = Core.Enums.TransactionType.Transfer, Icon = "💰", SortOrder = 8 },
            new Core.Models.Category { Name = "其他支出", Type = Core.Enums.TransactionType.Expense, Icon = "📦", SortOrder = 99 },
            
            new Core.Models.Category { Name = "工资收入", Type = Core.Enums.TransactionType.Income, Icon = "💼", SortOrder = 1 },
            new Core.Models.Category { Name = "奖金收入", Type = Core.Enums.TransactionType.Income, Icon = "🎁", SortOrder = 2 },
            new Core.Models.Category { Name = "投资收益", Type = Core.Enums.TransactionType.Income, Icon = "📈", SortOrder = 3 },
            new Core.Models.Category { Name = "红包收入", Type = Core.Enums.TransactionType.Income, Icon = "🧧", SortOrder = 4 },
            new Core.Models.Category { Name = "退款收入", Type = Core.Enums.TransactionType.Income, Icon = "↩️", SortOrder = 5 },
            new Core.Models.Category { Name = "其他收入", Type = Core.Enums.TransactionType.Income, Icon = "💵", SortOrder = 99 },
        };

        await context.Categories.AddRangeAsync(defaultCategories);
        await context.SaveChangesAsync();
    }
}

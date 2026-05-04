using Bookkeeping.Core.DTOs;
using Bookkeeping.Core.Enums;
using Bookkeeping.Core.Models;
using Bookkeeping.Data;
using Bookkeeping.Data.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bookkeeping.Data.Tests;

public class StatisticsServiceTests
{
    private static async Task<(BookkeepingDbContext Context, StatisticsService Service)> CreateServiceWithContextAsync()
    {
        var options = new DbContextOptionsBuilder<BookkeepingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new BookkeepingDbContext(options);
        var service = new StatisticsService(context);

        return (context, service);
    }

    private static async Task<Account> CreateTestAccountAsync(BookkeepingDbContext context)
    {
        var account = new Account
        {
            Name = "测试账户",
            Source = DataSource.Alipay,
            CreatedAt = DateTime.UtcNow
        };
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
        return account;
    }

    private static async Task<ImportRecord> CreateTestImportRecordAsync(BookkeepingDbContext context)
    {
        var importRecord = new ImportRecord
        {
            FileName = "test.csv",
            Source = DataSource.Alipay,
            ImportTime = DateTime.UtcNow,
            TotalRows = 1,
            ImportedCount = 1
        };
        context.ImportRecords.Add(importRecord);
        await context.SaveChangesAsync();
        return importRecord;
    }

    private static async Task<Category> CreateCategoryAsync(BookkeepingDbContext context, string name, TransactionType type, string icon = "📦")
    {
        var category = new Category
        {
            Name = name,
            Type = type,
            Icon = icon,
            SortOrder = 1
        };
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        return category;
    }

    private static async Task<Transaction> CreateTransactionAsync(
        BookkeepingDbContext context,
        Account account,
        ImportRecord importRecord,
        Category category,
        decimal amount,
        DateTime transactionTime)
    {
        var transaction = new Transaction
        {
            Type = category.Type,
            Amount = amount,
            TransactionTime = transactionTime,
            Description = "测试交易",
            CategoryId = category.Id,
            CategoryName = category.Name,
            AccountId = account.Id,
            ImportRecordId = importRecord.Id,
            Source = DataSource.Alipay
        };
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();
        return transaction;
    }

    [Fact]
    public async Task GetCategorySummaryAsync_ReturnsCorrectSums()
    {
        // Arrange
        var (context, service) = await CreateServiceWithContextAsync();
        var account = await CreateTestAccountAsync(context);
        var importRecord = await CreateTestImportRecordAsync(context);
        var category = await CreateCategoryAsync(context, "餐饮", TransactionType.Expense, "🍽️");

        // Create transactions with amounts 10, 20, 30 = total 60
        await CreateTransactionAsync(context, account, importRecord, category, 10m, new DateTime(2026, 4, 15));
        await CreateTransactionAsync(context, account, importRecord, category, 20m, new DateTime(2026, 4, 20));
        await CreateTransactionAsync(context, account, importRecord, category, 30m, new DateTime(2026, 4, 25));

        // Act
        var result = (await service.GetCategorySummaryAsync(new DateTime(2026, 4, 1), TransactionType.Expense)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(60m, result[0].TotalAmount);
        Assert.Equal(3, result[0].TransactionCount);
    }

    [Fact]
    public async Task GetCategorySummaryAsync_EmptyMonth_ReturnsEmptyWithoutException()
    {
        // Arrange
        var (context, service) = await CreateServiceWithContextAsync();
        var account = await CreateTestAccountAsync(context);
        var importRecord = await CreateTestImportRecordAsync(context);
        var category = await CreateCategoryAsync(context, "餐饮", TransactionType.Expense, "🍽️");

        // Create transaction in March 2026
        await CreateTransactionAsync(context, account, importRecord, category, 50m, new DateTime(2026, 3, 15));

        // Act - Query for May 2026 (no transactions)
        var result = await service.GetCategorySummaryAsync(new DateTime(2026, 5, 1), TransactionType.Expense);

        // Assert - Should return empty enumerable without throwing
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCategorySummaryAsync_CalculatesPercentage()
    {
        // Arrange
        var (context, service) = await CreateServiceWithContextAsync();
        var account = await CreateTestAccountAsync(context);
        var importRecord = await CreateTestImportRecordAsync(context);
        var foodCategory = await CreateCategoryAsync(context, "餐饮", TransactionType.Expense, "🍽️");
        var shopCategory = await CreateCategoryAsync(context, "购物", TransactionType.Expense, "🛒");

        // Create transactions: 餐饮 = 75, 购物 = 25, total = 100
        await CreateTransactionAsync(context, account, importRecord, foodCategory, 75m, new DateTime(2026, 4, 15));
        await CreateTransactionAsync(context, account, importRecord, shopCategory, 25m, new DateTime(2026, 4, 20));

        // Act
        var result = (await service.GetCategorySummaryAsync(new DateTime(2026, 4, 1), TransactionType.Expense)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        var foodSummary = result.First(r => r.CategoryName == "餐饮");
        var shopSummary = result.First(r => r.CategoryName == "购物");
        Assert.Equal(75m, foodSummary.TotalAmount);
        Assert.Equal(75m, foodSummary.Percentage);
        Assert.Equal(25m, shopSummary.TotalAmount);
        Assert.Equal(25m, shopSummary.Percentage);
    }

    [Fact]
    public async Task GetTopCategoriesAsync_ReturnsTop5ByAmount()
    {
        // Arrange
        var (context, service) = await CreateServiceWithContextAsync();
        var account = await CreateTestAccountAsync(context);
        var importRecord = await CreateTestImportRecordAsync(context);

        // Create 7 categories with different amounts
        var categories = new List<Category>();
        for (int i = 1; i <= 7; i++)
        {
            var cat = new Category { Name = $"分类{i}", Type = TransactionType.Expense, Icon = "📦", SortOrder = i };
            context.Categories.Add(cat);
            categories.Add(cat);
        }
        await context.SaveChangesAsync();

        // Create transactions with amounts: 10, 20, 30, 40, 50, 60, 70
        for (int i = 0; i < 7; i++)
        {
            await CreateTransactionAsync(context, account, importRecord, categories[i], (decimal)(i + 1) * 10, new DateTime(2026, 4, 10 + i));
        }

        // Act
        var result = (await service.GetTopCategoriesAsync(TransactionType.Expense, 5, new DateTime(2026, 4, 1))).ToList();

        // Assert
        Assert.Equal(5, result.Count);
        // Should be sorted by amount descending: 70, 60, 50, 40, 30
        Assert.Equal("分类7", result[0].CategoryName);
        Assert.Equal(70m, result[0].TotalAmount);
        Assert.Equal("分类3", result[4].CategoryName);
        Assert.Equal(30m, result[4].TotalAmount);
    }

    [Fact]
    public async Task GetCategoryTrendAsync_Returns6MonthsData()
    {
        // Arrange
        var (context, service) = await CreateServiceWithContextAsync();
        var account = await CreateTestAccountAsync(context);
        var importRecord = await CreateTestImportRecordAsync(context);
        var category = await CreateCategoryAsync(context, "餐饮", TransactionType.Expense, "🍽️");

        // Create transactions in some months (use month 4 which is safely in the past)
        await CreateTransactionAsync(context, account, importRecord, category, 100m, new DateTime(2026, 1, 15));
        await CreateTransactionAsync(context, account, importRecord, category, 200m, new DateTime(2026, 3, 20));
        await CreateTransactionAsync(context, account, importRecord, category, 150m, new DateTime(2026, 4, 10));

        // Act - Get 6 months trend ending now
        var result = await service.GetCategoryTrendAsync(category.Id, 6);

        // Assert
        Assert.Equal(category.Id, result.CategoryId);
        Assert.Equal("餐饮", result.CategoryName);
        Assert.Equal(6, result.MonthlyData.Count);

        // Check that months with data have correct amounts
        var janData = result.MonthlyData.FirstOrDefault(m => m.Year == 2026 && m.Month == 1);
        var marData = result.MonthlyData.FirstOrDefault(m => m.Year == 2026 && m.Month == 3);
        var aprData = result.MonthlyData.FirstOrDefault(m => m.Year == 2026 && m.Month == 4);

        Assert.NotNull(janData);
        Assert.Equal(100m, janData.Amount);
        Assert.NotNull(marData);
        Assert.Equal(200m, marData.Amount);
        Assert.NotNull(aprData);
        Assert.Equal(150m, aprData.Amount);

        // Check that empty months have 0
        var febData = result.MonthlyData.FirstOrDefault(m => m.Year == 2026 && m.Month == 2);
        Assert.NotNull(febData);
        Assert.Equal(0m, febData.Amount);
    }
}
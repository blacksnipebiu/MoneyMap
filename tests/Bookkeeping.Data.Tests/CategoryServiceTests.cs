using Bookkeeping.Core.Enums;
using Bookkeeping.Core.Models;
using Bookkeeping.Data;
using Bookkeeping.Data.Repositories;
using Bookkeeping.Data.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bookkeeping.Data.Tests;

public class CategoryServiceTests
{
    private static async Task<(BookkeepingDbContext Context, CategoryService Service, ICategoryRepository Repository)> CreateServiceWithContextAsync()
    {
        var options = new DbContextOptionsBuilder<BookkeepingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new BookkeepingDbContext(options);
        var repository = new CategoryRepository(context);
        var service = new CategoryService(repository, context);

        return (context, service, repository);
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

    [Fact]
    public async Task CreateAsync_Success_CreatesCategoryAndPersists()
    {
        // Arrange
        var (context, service, _) = await CreateServiceWithContextAsync();
        var category = new Category
        {
            Name = "餐饮美食",
            Type = TransactionType.Expense,
            Icon = "🍽️"
        };

        // Act
        var result = await service.CreateAsync(category);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        var persisted = await context.Categories.FindAsync(result.Id);
        Assert.NotNull(persisted);
        Assert.Equal("餐饮美食", persisted.Name);
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_Throws()
    {
        // Arrange
        var (context, service, _) = await CreateServiceWithContextAsync();
        var category1 = new Category { Name = "餐饮美食", Type = TransactionType.Expense, Icon = "🍽️" };
        await service.CreateAsync(category1);

        var category2 = new Category { Name = "餐饮美食", Type = TransactionType.Expense, Icon = "🍴" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(category2));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_AutoIncrementsSortOrder()
    {
        // Arrange
        var (context, service, _) = await CreateServiceWithContextAsync();
        var category1 = new Category { Name = "餐饮", Type = TransactionType.Expense, Icon = "🍽️" };
        var category2 = new Category { Name = "交通", Type = TransactionType.Expense, Icon = "🚗" };
        var category3 = new Category { Name = "购物", Type = TransactionType.Expense, Icon = "🛒" };

        // Act
        var result1 = await service.CreateAsync(category1);
        var result2 = await service.CreateAsync(category2);
        var result3 = await service.CreateAsync(category3);

        // Assert
        Assert.Equal(1, result1.SortOrder);
        Assert.Equal(2, result2.SortOrder);
        Assert.Equal(3, result3.SortOrder);
    }

    [Fact]
    public async Task UpdateAsync_Success_UpdatesName()
    {
        // Arrange
        var (context, service, _) = await CreateServiceWithContextAsync();
        var category = new Category { Name = "餐饮美食", Type = TransactionType.Expense, Icon = "🍽️" };
        var created = await service.CreateAsync(category);

        // Act
        created.Name = "餐饮更新";
        var result = await service.UpdateAsync(created);

        // Assert
        Assert.Equal("餐饮更新", result.Name);
        var persisted = await context.Categories.FindAsync(created.Id);
        Assert.Equal("餐饮更新", persisted!.Name);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateName_ThrowsExcludingSelf()
    {
        // Arrange
        var (context, service, _) = await CreateServiceWithContextAsync();
        var category1 = new Category { Name = "餐饮", Type = TransactionType.Expense, Icon = "🍽️" };
        var category2 = new Category { Name = "交通", Type = TransactionType.Expense, Icon = "🚗" };
        await service.CreateAsync(category1);
        var created2 = await service.CreateAsync(category2);

        // Act & Assert - category2 tries to rename to "餐饮" (already exists)
        created2.Name = "餐饮";
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(created2));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_LastCategoryOfType_Throws()
    {
        // Arrange
        var (context, service, _) = await CreateServiceWithContextAsync();
        var category = new Category { Name = "唯一分类", Type = TransactionType.Expense, Icon = "🍽️" };
        var created = await service.CreateAsync(category);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(created.Id));
        Assert.Contains("Cannot delete the last category", ex.Message);
    }

    [Fact]
    public async Task ReassignAndDeleteAsync_ReassignsTransactions()
    {
        // Arrange
        var (context, service, _) = await CreateServiceWithContextAsync();
        var categoryFood = new Category { Name = "餐饮", Type = TransactionType.Expense, Icon = "🍽️" };
        var categoryTravel = new Category { Name = "交通", Type = TransactionType.Expense, Icon = "🚗" };
        await service.CreateAsync(categoryFood);
        var travelCreated = await service.CreateAsync(categoryTravel);

        var account = await CreateTestAccountAsync(context);
        var importRecord = await CreateTestImportRecordAsync(context);

        // Create 5 transactions with "餐饮" category
        for (int i = 0; i < 5; i++)
        {
            var transaction = new Transaction
            {
                Type = TransactionType.Expense,
                Amount = 10m * (i + 1),
                TransactionTime = DateTime.UtcNow.AddDays(-i),
                Description = $"测试餐饮{i}",
                CategoryId = categoryFood.Id,
                CategoryName = categoryFood.Name,
                AccountId = account.Id,
                ImportRecordId = importRecord.Id,
                Source = DataSource.Alipay
            };
            context.Transactions.Add(transaction);
        }
        await context.SaveChangesAsync();

        // Act - Delete "餐饮" category and reassign transactions to "交通"
        var reassignedCount = await service.ReassignAndDeleteAsync(categoryFood.Id, travelCreated.Id);

        // Assert
        Assert.Equal(5, reassignedCount);
        var remainingFoodCategory = await context.Categories.FindAsync(categoryFood.Id);
        Assert.Null(remainingFoodCategory); // Category should be deleted

        var transactions = await context.Transactions.Where(t => t.CategoryId == travelCreated.Id).ToListAsync();
        Assert.Equal(5, transactions.Count);
    }

    [Fact]
    public async Task AutoCategorizeAsync_ExactMatch()
    {
        // Arrange
        var (context, service, _) = await CreateServiceWithContextAsync();
        var category = new Category { Name = "餐饮美食", Type = TransactionType.Expense, Icon = "🍽️" };
        await service.CreateAsync(category);

        // Act
        var result = await service.AutoCategorizeAsync("在餐饮美食消费了100元", null, DataSource.Alipay);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("餐饮美食", result.Name);
    }

    [Fact]
    public async Task AutoCategorizeAsync_MappingMatch()
    {
        // Arrange
        var (context, service, _) = await CreateServiceWithContextAsync();
        var category = new Category
        {
            Name = "餐饮美食",
            Type = TransactionType.Expense,
            Icon = "🍽️",
            SourceCategoryMapping = "外卖,快餐,餐饮"
        };
        await service.CreateAsync(category);

        // Act
        var result = await service.AutoCategorizeAsync("今天点了外卖", null, DataSource.Alipay);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("餐饮美食", result.Name);
    }
}
using MoneyMap.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace MoneyMap.Data;

public class MoneyMapDbContext : DbContext
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<ImportRecord> ImportRecords => Set<ImportRecord>();
    public DbSet<Budget> Budgets => Set<Budget>();

    public MoneyMapDbContext(DbContextOptions<MoneyMapDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MoneyMapDbContext).Assembly);
    }
}
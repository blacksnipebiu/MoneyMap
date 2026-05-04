using Bookkeeping.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Bookkeeping.Data;

public class BookkeepingDbContext : DbContext
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<ImportRecord> ImportRecords => Set<ImportRecord>();
    public DbSet<Budget> Budgets => Set<Budget>();

    public BookkeepingDbContext(DbContextOptions<BookkeepingDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookkeepingDbContext).Assembly);
    }
}
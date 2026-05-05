using MoneyMap.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MoneyMap.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();
        
        builder.Property(t => t.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(t => t.TransactionTime).IsRequired();
        builder.Property(t => t.Counterparty).HasMaxLength(200);
        builder.Property(t => t.CounterpartyAccount).HasMaxLength(200);
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.CategoryName).HasMaxLength(100);
        builder.Property(t => t.PaymentMethod).HasMaxLength(100);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.RawStatus).HasMaxLength(50);
        builder.Property(t => t.Remark).HasMaxLength(500);
        builder.Property(t => t.MerchantOrderId).HasMaxLength(100);
        builder.Property(t => t.DataSourceName).HasMaxLength(100);
        builder.Property(t => t.RawLine).HasColumnType("TEXT");
        builder.Property(t => t.SourceTransactionId).HasMaxLength(100);
        builder.Property(t => t.CreatedAt).IsRequired();
        
        builder.HasIndex(t => t.TransactionTime);
        builder.HasIndex(t => t.Source);
        builder.HasIndex(t => t.CategoryId);
        builder.HasIndex(t => t.SourceTransactionId);
        builder.HasIndex(t => t.AccountId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.MerchantOrderId);
        
        builder.HasOne(t => t.Category)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasOne(t => t.Account)
            .WithMany(a => a.Transactions)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(t => t.ImportRecord)
            .WithMany(i => i.Transactions)
            .HasForeignKey(t => t.ImportRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
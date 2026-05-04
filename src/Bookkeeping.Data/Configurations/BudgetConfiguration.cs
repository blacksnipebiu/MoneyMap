using Bookkeeping.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookkeeping.Data.Configurations;

public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedOnAdd();
        
        builder.Property(b => b.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(b => b.Year).IsRequired();
        builder.Property(b => b.Month).IsRequired();
        builder.Property(b => b.CreatedAt).IsRequired();
        
        builder.HasOne(b => b.Category)
            .WithMany()
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasIndex(b => new { b.CategoryId, b.Year, b.Month }).IsUnique();
    }
}
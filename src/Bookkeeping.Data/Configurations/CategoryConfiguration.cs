using Bookkeeping.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookkeeping.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();
        
        builder.Property(c => c.Name).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Icon).HasMaxLength(100);
        builder.Property(c => c.AutoMatchPattern).HasMaxLength(500);
        builder.Property(c => c.SourceCategoryMapping).HasColumnType("TEXT");
        builder.Property(c => c.SortOrder).IsRequired();
        
        builder.HasIndex(c => c.Type);
        builder.HasIndex(c => c.ParentId);
        
        builder.HasOne(c => c.Parent)
            .WithMany(p => p.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
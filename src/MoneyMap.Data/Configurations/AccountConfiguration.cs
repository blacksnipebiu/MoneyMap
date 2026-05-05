using MoneyMap.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MoneyMap.Data.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd();
        
        builder.Property(a => a.Name).HasMaxLength(100).IsRequired();
        builder.Property(a => a.LastFourDigits).HasMaxLength(4);
        builder.Property(a => a.CreatedAt).IsRequired();
        
        builder.HasIndex(a => a.Source);
    }
}
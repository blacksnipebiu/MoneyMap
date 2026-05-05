using Bookkeeping.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookkeeping.Data.Configurations;

public class ImportRecordConfiguration : IEntityTypeConfiguration<ImportRecord>
{
    public void Configure(EntityTypeBuilder<ImportRecord> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedOnAdd();
        
        builder.Property(i => i.FileName).HasMaxLength(500).IsRequired();
        builder.Property(i => i.FilePath).HasMaxLength(1000);
        builder.Property(i => i.ImportTime).IsRequired();
        builder.Property(i => i.TotalRows).IsRequired();
        builder.Property(i => i.ImportedCount).IsRequired();
        builder.Property(i => i.SkippedCount).IsRequired();
        builder.Property(i => i.ErrorCount).IsRequired();
        builder.Property(i => i.ErrorDetails).HasColumnType("TEXT");
    }
}
using GroupLN.MarketData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupLN.MarketData.Persistence.Configurations;

public class CrawlerSourceStatusConfiguration : IEntityTypeConfiguration<CrawlerSourceStatus>
{
    public void Configure(EntityTypeBuilder<CrawlerSourceStatus> builder)
    {
        builder.ToTable("CrawlerSourceStatus");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.LastErrorMessage).HasMaxLength(2000);
        builder.Property(x => x.CurrentPhase).HasMaxLength(100);
        builder.Property(x => x.CurrentProgress).HasMaxLength(200);

        builder.HasIndex(x => x.SourceName)
            .IsUnique()
            .HasDatabaseName("UQ_CrawlerSourceStatus_SourceName");
    }
}

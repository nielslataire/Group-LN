using GroupLN.MarketData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupLN.MarketData.Persistence.Configurations;

public class CrawlerRunConfiguration : IEntityTypeConfiguration<CrawlerRun>
{
    public void Configure(EntityTypeBuilder<CrawlerRun> builder)
    {
        builder.ToTable("CrawlerRun");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .HasConversion<int>();

        builder.Property(x => x.LogMessage)
            .HasMaxLength(4000);

        builder.HasIndex(x => x.SourceId);
        builder.HasIndex(x => x.StartedAt);
        builder.HasIndex(x => x.Status);
    }
}

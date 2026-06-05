using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Persistence.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupLN.MarketData.Persistence.Configurations;

public class CrawlerSourceConfiguration : IEntityTypeConfiguration<CrawlerSource>
{
    public void Configure(EntityTypeBuilder<CrawlerSource> builder)
    {
        builder.ToTable("CrawlerSource");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.BaseUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(x => x.Name)
            .IsUnique();

        builder.HasMany(x => x.Runs)
            .WithOne(x => x.Source)
            .HasForeignKey(x => x.SourceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Listings worden geconfigureerd vanuit MarketListingConfiguration

        builder.HasData(CrawlerSourceSeed.GetSeedData());
    }
}

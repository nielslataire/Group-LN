using GroupLN.MarketData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupLN.MarketData.Persistence.Configurations;

public class MarketListingConfiguration : IEntityTypeConfiguration<MarketListing>
{
    public void Configure(EntityTypeBuilder<MarketListing> builder)
    {
        builder.ToTable("MarketListing");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ExternalId).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Url).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.Title).HasMaxLength(500);
        builder.Property(x => x.AskingPrice).HasPrecision(14, 2);

        builder.HasIndex(x => new { x.SourceId, x.ExternalId })
            .IsUnique()
            .HasDatabaseName("UQ_MarketListing_Source_ExternalId");
        builder.HasIndex(x => x.MarketAssetId);
        builder.HasIndex(x => x.SourceId);
        builder.HasIndex(x => x.Url);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.FirstSeenAt);
        builder.HasIndex(x => x.LastSeenAt);

        builder.HasOne(x => x.Source)
            .WithMany(x => x.Listings)
            .HasForeignKey(x => x.SourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Snapshots)
            .WithOne(x => x.Listing)
            .HasForeignKey(x => x.MarketListingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.PriceHistory)
            .WithOne(x => x.Listing)
            .HasForeignKey(x => x.MarketListingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using GroupLN.MarketData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupLN.MarketData.Persistence.Configurations;

public class MarketListingSnapshotConfiguration : IEntityTypeConfiguration<MarketListingSnapshot>
{
    public void Configure(EntityTypeBuilder<MarketListingSnapshot> builder)
    {
        builder.ToTable("MarketListingSnapshot");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AskingPrice).HasPrecision(14, 2);
        builder.Property(x => x.MaxPrice).HasPrecision(14, 2);
        builder.Property(x => x.PricePerSqm).HasPrecision(10, 2);
        builder.Property(x => x.LivingArea).HasPrecision(10, 2);
        builder.Property(x => x.LandArea).HasPrecision(10, 2);
        builder.Property(x => x.TerraceArea).HasPrecision(10, 2);
        builder.Property(x => x.GardenArea).HasPrecision(10, 2);
        builder.Property(x => x.EPCScore).HasPrecision(8, 2);
        builder.Property(x => x.EnergyFeatures).HasMaxLength(500);
        builder.Property(x => x.EPCLabel).HasConversion<int?>();
        builder.Property(x => x.SaleStatus).HasConversion<int?>();
        builder.Property(x => x.DescriptionHash).HasMaxLength(64);
        builder.Property(x => x.RawJson).HasColumnType("nvarchar(max)");

        builder.HasIndex(x => x.MarketListingId);
        builder.HasIndex(x => x.SnapshotDate);
    }
}

using GroupLN.MarketData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupLN.MarketData.Persistence.Configurations;

public class MarketAreaStatisticsConfiguration : IEntityTypeConfiguration<MarketAreaStatistics>
{
    public void Configure(EntityTypeBuilder<MarketAreaStatistics> builder)
    {
        builder.ToTable("MarketAreaStatistics");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PostalCode).HasMaxLength(10);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.PropertyType).HasConversion<int>();

        builder.Property(x => x.SoldPercentage).HasPrecision(8, 4);

        builder.Property(x => x.MinPrice).HasPrecision(14, 2);
        builder.Property(x => x.MaxPrice).HasPrecision(14, 2);
        builder.Property(x => x.AveragePrice).HasPrecision(14, 2);

        builder.Property(x => x.MinPricePerSqm).HasPrecision(10, 2);
        builder.Property(x => x.MaxPricePerSqm).HasPrecision(10, 2);
        builder.Property(x => x.AveragePricePerSqm).HasPrecision(10, 2);

        builder.Property(x => x.MinLivingArea).HasPrecision(10, 2);
        builder.Property(x => x.MaxLivingArea).HasPrecision(10, 2);
        builder.Property(x => x.AverageLivingArea).HasPrecision(10, 2);

        builder.HasIndex(x => new { x.PostalCode, x.PropertyType });
        builder.HasIndex(x => x.SnapshotDate);
        builder.HasIndex(x => x.AveragePricePerSqm);
    }
}

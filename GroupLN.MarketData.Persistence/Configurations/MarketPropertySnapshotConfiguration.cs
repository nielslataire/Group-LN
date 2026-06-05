using GroupLN.MarketData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupLN.MarketData.Persistence.Configurations;

public class MarketPropertySnapshotConfiguration : IEntityTypeConfiguration<MarketPropertySnapshot>
{
    public void Configure(EntityTypeBuilder<MarketPropertySnapshot> builder)
    {
        builder.ToTable("MarketPropertySnapshot");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AskingPrice)
            .HasPrecision(14, 2);

        builder.Property(x => x.LivingArea)
            .HasPrecision(10, 2);

        builder.Property(x => x.LandArea)
            .HasPrecision(10, 2);

        builder.Property(x => x.EPCScore)
            .HasPrecision(8, 2);

        builder.Property(x => x.EPCLabel)
            .HasConversion<int?>();

        builder.Property(x => x.DescriptionHash)
            .HasMaxLength(64);

        // RawJson kan groot zijn — gebruik nvarchar(max)
        builder.Property(x => x.RawJson)
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(x => x.MarketPropertyId);
        builder.HasIndex(x => x.SnapshotDate);
    }
}

using GroupLN.MarketData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupLN.MarketData.Persistence.Configurations;

public class CanonicalUnitAssetConfiguration : IEntityTypeConfiguration<CanonicalUnitAsset>
{
    public void Configure(EntityTypeBuilder<CanonicalUnitAsset> builder)
    {
        builder.ToTable("CanonicalUnitAsset");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ExternalId).HasMaxLength(200);
        builder.Property(x => x.MatchLevel).IsRequired().HasMaxLength(20);
        builder.Property(x => x.MatchScore).HasPrecision(7, 0);
        builder.Property(x => x.MatchReasons).HasMaxLength(500);
        builder.Property(x => x.IsFromLooseListing).HasDefaultValue(false);

        builder.HasIndex(x => x.CanonicalUnitId)
            .HasDatabaseName("IX_CanonicalUnitAsset_CanonicalUnitId");
        builder.HasIndex(x => x.MarketAssetId)
            .HasDatabaseName("IX_CanonicalUnitAsset_MarketAssetId");
        builder.HasIndex(x => new { x.CanonicalUnitId, x.MarketAssetId })
            .IsUnique()
            .HasDatabaseName("UQ_CanonicalUnitAsset_Unit_Asset");

        builder.HasOne(x => x.CanonicalUnit)
            .WithMany(u => u.SourceAssets)
            .HasForeignKey(x => x.CanonicalUnitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MarketAsset)
            .WithMany()
            .HasForeignKey(x => x.MarketAssetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using GroupLN.MarketData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupLN.MarketData.Persistence.Configurations;

public class CanonicalUnitConfiguration : IEntityTypeConfiguration<CanonicalUnit>
{
    public void Configure(EntityTypeBuilder<CanonicalUnit> builder)
    {
        builder.ToTable("CanonicalUnit");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(500);
        builder.Property(x => x.UnitNumber).HasMaxLength(100);
        builder.Property(x => x.Area).HasPrecision(10, 2);
        builder.Property(x => x.Price).HasPrecision(14, 2);
        builder.Property(x => x.PricePerSqm).HasPrecision(10, 2);
        builder.Property(x => x.ConflictSummary).HasMaxLength(1000);
        builder.Property(x => x.IsAmbiguous).HasDefaultValue(false);

        builder.HasIndex(x => x.CanonicalProjectId)
            .HasDatabaseName("IX_CanonicalUnit_CanonicalProjectId");
        builder.HasIndex(x => x.RepresentativeMarketAssetId)
            .HasDatabaseName("IX_CanonicalUnit_RepresentativeMarketAssetId");

        builder.HasOne(x => x.CanonicalProject)
            .WithMany()
            .HasForeignKey(x => x.CanonicalProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<MarketAsset>()
            .WithMany()
            .HasForeignKey(x => x.RepresentativeMarketAssetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

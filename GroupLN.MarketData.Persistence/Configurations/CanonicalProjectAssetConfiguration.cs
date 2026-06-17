using GroupLN.MarketData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupLN.MarketData.Persistence.Configurations;

public class CanonicalProjectAssetConfiguration : IEntityTypeConfiguration<CanonicalProjectAsset>
{
    public void Configure(EntityTypeBuilder<CanonicalProjectAsset> builder)
    {
        builder.ToTable("CanonicalProjectAsset");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MatchLevel).IsRequired().HasMaxLength(20);
        builder.Property(x => x.MatchScore).HasPrecision(5, 4);
        builder.Property(x => x.MatchReason).HasMaxLength(500);
        builder.Property(x => x.Source).HasMaxLength(50);

        builder.HasIndex(x => x.MarketAssetId)
            .HasDatabaseName("IX_CanonicalProjectAsset_MarketAssetId");
        builder.HasIndex(x => x.CanonicalProjectId)
            .HasDatabaseName("IX_CanonicalProjectAsset_CanonicalProjectId");
        builder.HasIndex(x => new { x.CanonicalProjectId, x.MarketAssetId })
            .IsUnique()
            .HasDatabaseName("UQ_CanonicalProjectAsset_Project_Asset");
        builder.HasIndex(x => x.IsPrimary)
            .HasDatabaseName("IX_CanonicalProjectAsset_IsPrimary");

        builder.HasOne(x => x.MarketAsset)
            .WithMany()
            .HasForeignKey(x => x.MarketAssetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

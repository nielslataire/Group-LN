using GroupLN.MarketData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupLN.MarketData.Persistence.Configurations;

public class MarketAssetConfiguration : IEntityTypeConfiguration<MarketAsset>
{
    public void Configure(EntityTypeBuilder<MarketAsset> builder)
    {
        builder.ToTable("MarketAsset");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AssetKey)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.PropertyType).HasConversion<int>();
        builder.Property(x => x.PropertySubType).HasConversion<int>();
        builder.Property(x => x.TransactionType).HasConversion<int>();
        builder.Property(x => x.EPCLabel).HasConversion<int?>();

        builder.Property(x => x.Country).HasMaxLength(2).HasDefaultValue("BE");
        builder.Property(x => x.PostalCode).HasMaxLength(10);
        builder.Property(x => x.City).HasMaxLength(150);
        builder.Property(x => x.Street).HasMaxLength(250);
        builder.Property(x => x.HouseNumber).HasMaxLength(20);
        builder.Property(x => x.Latitude).HasPrecision(9, 6);
        builder.Property(x => x.Longitude).HasPrecision(9, 6);
        builder.Property(x => x.LivingArea).HasPrecision(10, 2);
        builder.Property(x => x.LandArea).HasPrecision(10, 2);
        builder.Property(x => x.TerraceArea).HasPrecision(10, 2);
        builder.Property(x => x.GardenArea).HasPrecision(10, 2);
        builder.Property(x => x.MaxPrice).HasPrecision(14, 2);
        builder.Property(x => x.EPCScore).HasPrecision(8, 2);
        builder.Property(x => x.EnergyFeatures).HasMaxLength(500);
        builder.Property(x => x.DeveloperName).HasMaxLength(200);
        builder.Property(x => x.DeveloperWebsite).HasMaxLength(500);
        builder.Property(x => x.DeveloperPhone).HasMaxLength(50);

        // ProjectGroup / unit kolommen
        builder.Property(x => x.IsProjectGroup).HasDefaultValue(false);
        builder.Property(x => x.ProjectExternalId).HasMaxLength(50);
        builder.Property(x => x.UnitExternalId).HasMaxLength(50);
        builder.Property(x => x.SaleStatus).HasConversion<int?>();

        builder.HasIndex(x => x.AssetKey).IsUnique().HasDatabaseName("UQ_MarketAsset_AssetKey");
        builder.HasIndex(x => x.PostalCode);
        builder.HasIndex(x => x.City);
        builder.HasIndex(x => x.PropertyType);
        builder.HasIndex(x => x.TransactionType);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.FirstSeenAt);
        builder.HasIndex(x => x.LastSeenAt);
        builder.HasIndex(x => new { x.Latitude, x.Longitude }).HasDatabaseName("IX_MarketAsset_Coordinates");
        builder.HasIndex(x => x.ProjectExternalId).HasDatabaseName("IX_MarketAsset_ProjectExternalId");
        builder.HasIndex(x => x.ParentMarketAssetId).HasDatabaseName("IX_MarketAsset_ParentMarketAssetId");

        builder.HasMany(x => x.Listings)
            .WithOne(x => x.Asset)
            .HasForeignKey(x => x.MarketAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.MatchCandidates)
            .WithOne(x => x.ExistingAsset)
            .HasForeignKey(x => x.ExistingMarketAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ParentAsset)
            .WithMany(x => x.ChildUnits)
            .HasForeignKey(x => x.ParentMarketAssetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

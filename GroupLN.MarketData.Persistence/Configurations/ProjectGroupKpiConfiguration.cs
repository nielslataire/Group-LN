using GroupLN.MarketData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupLN.MarketData.Persistence.Configurations;

public class ProjectGroupKpiConfiguration : IEntityTypeConfiguration<ProjectGroupKpi>
{
    public void Configure(EntityTypeBuilder<ProjectGroupKpi> builder)
    {
        builder.ToTable("ProjectGroupKpi");
        builder.HasKey(x => x.Id);

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

        builder.HasOne(x => x.Asset)
            .WithMany()
            .HasForeignKey(x => x.MarketAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.MarketAssetId);
        builder.HasIndex(x => x.SnapshotDate);
        builder.HasIndex(x => x.SoldPercentage);
    }
}

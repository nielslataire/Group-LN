using GroupLN.MarketData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupLN.MarketData.Persistence.Configurations;

public class ProjectGroupSnapshotConfiguration : IEntityTypeConfiguration<ProjectGroupSnapshot>
{
    public void Configure(EntityTypeBuilder<ProjectGroupSnapshot> builder)
    {
        builder.ToTable("ProjectGroupSnapshot");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SoldPercentage).HasPrecision(8, 4);

        builder.HasOne(x => x.Asset)
            .WithMany()
            .HasForeignKey(x => x.MarketAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.MarketAssetId);
        builder.HasIndex(x => x.SnapshotDate);
    }
}

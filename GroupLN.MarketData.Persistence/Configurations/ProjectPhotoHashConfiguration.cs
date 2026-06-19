using GroupLN.MarketData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupLN.MarketData.Persistence.Configurations;

public class ProjectPhotoHashConfiguration : IEntityTypeConfiguration<ProjectPhotoHash>
{
    public void Configure(EntityTypeBuilder<ProjectPhotoHash> builder)
    {
        builder.ToTable("ProjectPhotoHash");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ProjectExternalId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ImageUrl).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.NormalizedImageUrl).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.HashAlgorithm).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ContentHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.PerceptualHashVersion).HasMaxLength(20);

        builder.HasIndex(x => x.MarketAssetId)
            .HasDatabaseName("IX_ProjectPhotoHash_MarketAssetId");
        builder.HasIndex(x => new { x.SourceName, x.ProjectExternalId })
            .HasDatabaseName("IX_ProjectPhotoHash_Source_ExternalId");
        builder.HasIndex(x => x.NormalizedImageUrl)
            .HasDatabaseName("IX_ProjectPhotoHash_NormalizedImageUrl");
        builder.HasIndex(x => x.ContentHash)
            .HasDatabaseName("IX_ProjectPhotoHash_ContentHash");
        builder.HasIndex(x => x.PerceptualHash)
            .HasDatabaseName("IX_ProjectPhotoHash_PerceptualHash");

        builder.HasOne(x => x.MarketAsset)
            .WithMany()
            .HasForeignKey(x => x.MarketAssetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

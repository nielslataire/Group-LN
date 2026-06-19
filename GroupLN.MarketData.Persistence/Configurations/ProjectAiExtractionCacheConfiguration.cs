using GroupLN.MarketData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupLN.MarketData.Persistence.Configurations;

public class ProjectAiExtractionCacheConfiguration : IEntityTypeConfiguration<ProjectAiExtractionCache>
{
    public void Configure(EntityTypeBuilder<ProjectAiExtractionCache> builder)
    {
        builder.ToTable("ProjectAiExtractionCache");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ExternalId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Url).HasMaxLength(1000);
        builder.Property(x => x.InputHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Model).HasMaxLength(100).IsRequired();
        builder.Property(x => x.RawTitle).HasMaxLength(500);
        builder.Property(x => x.ExtractedProjectName).HasMaxLength(300);
        builder.Property(x => x.ExtractedStreet).HasMaxLength(250);
        builder.Property(x => x.ExtractedHouseNumber).HasMaxLength(20);
        builder.Property(x => x.ExtractedPostalCode).HasMaxLength(10);
        builder.Property(x => x.ExtractedCity).HasMaxLength(100);
        builder.Property(x => x.ExtractedDeveloper).HasMaxLength(200);

        builder.HasIndex(x => x.InputHash)
            .IsUnique()
            .HasDatabaseName("IX_ProjectAiExtractionCache_InputHash");
        builder.HasIndex(x => new { x.SourceName, x.ExternalId })
            .HasDatabaseName("IX_ProjectAiExtractionCache_Source_ExternalId");
    }
}

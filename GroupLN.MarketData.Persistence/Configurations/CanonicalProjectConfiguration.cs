using GroupLN.MarketData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupLN.MarketData.Persistence.Configurations;

public class CanonicalProjectConfiguration : IEntityTypeConfiguration<CanonicalProject>
{
    public void Configure(EntityTypeBuilder<CanonicalProject> builder)
    {
        builder.ToTable("CanonicalProject");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CanonicalName).IsRequired().HasMaxLength(300);
        builder.Property(x => x.NormalizedName).IsRequired().HasMaxLength(300);
        builder.Property(x => x.PostalCode).HasMaxLength(10);
        builder.Property(x => x.Street).HasMaxLength(250);
        builder.Property(x => x.HouseNumber).HasMaxLength(20);
        builder.Property(x => x.Latitude).HasPrecision(9, 6);
        builder.Property(x => x.Longitude).HasPrecision(9, 6);
        builder.Property(x => x.DeveloperName).HasMaxLength(200);

        builder.HasIndex(x => x.NormalizedName)
            .HasDatabaseName("IX_CanonicalProject_NormalizedName");
        builder.HasIndex(x => x.GeoMunicipalityId)
            .HasDatabaseName("IX_CanonicalProject_GeoMunicipalityId");
        builder.HasIndex(x => x.GeoMunicipalSectionId)
            .HasDatabaseName("IX_CanonicalProject_GeoMunicipalSectionId");
        builder.HasIndex(x => x.IsActive);

        builder.HasOne(x => x.GeoMunicipality)
            .WithMany()
            .HasForeignKey(x => x.GeoMunicipalityId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.GeoMunicipalSection)
            .WithMany()
            .HasForeignKey(x => x.GeoMunicipalSectionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Assets)
            .WithOne(x => x.CanonicalProject)
            .HasForeignKey(x => x.CanonicalProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

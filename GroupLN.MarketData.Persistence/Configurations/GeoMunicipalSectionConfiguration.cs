using GroupLN.MarketData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupLN.MarketData.Persistence.Configurations;

public class GeoMunicipalSectionConfiguration : IEntityTypeConfiguration<GeoMunicipalSection>
{
    public void Configure(EntityTypeBuilder<GeoMunicipalSection> builder)
    {
        builder.ToTable("GeoMunicipalSection");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PseudoNis).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ZipCode).HasMaxLength(10);
        builder.Property(x => x.NisCodeMunicipality).HasMaxLength(20);
        builder.Property(x => x.NameDutch).HasMaxLength(150);
        builder.Property(x => x.NameFrench).HasMaxLength(150);
        builder.Property(x => x.NameGerman).HasMaxLength(150);
        builder.Property(x => x.Boundary).HasColumnType("geometry").IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.PseudoNis).IsUnique().HasDatabaseName("UQ_GeoMunicipalSection_PseudoNis");
        builder.HasIndex(x => x.ZipCode).HasDatabaseName("IX_GeoMunicipalSection_ZipCode");
        builder.HasIndex(x => x.NisCodeMunicipality).HasDatabaseName("IX_GeoMunicipalSection_NisCodeMunicipality");
    }
}

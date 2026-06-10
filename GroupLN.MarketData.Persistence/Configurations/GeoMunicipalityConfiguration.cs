using GroupLN.MarketData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupLN.MarketData.Persistence.Configurations;

public class GeoMunicipalityConfiguration : IEntityTypeConfiguration<GeoMunicipality>
{
    public void Configure(EntityTypeBuilder<GeoMunicipality> builder)
    {
        builder.ToTable("GeoMunicipality");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.NisCode).IsRequired().HasMaxLength(20);
        builder.Property(x => x.NameDutch).HasMaxLength(150);
        builder.Property(x => x.NameFrench).HasMaxLength(150);
        builder.Property(x => x.NameGerman).HasMaxLength(150);
        builder.Property(x => x.Boundary).HasColumnType("geometry").IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.NisCode).IsUnique().HasDatabaseName("UQ_GeoMunicipality_NisCode");
    }
}

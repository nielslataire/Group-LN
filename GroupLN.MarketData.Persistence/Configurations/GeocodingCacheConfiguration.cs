using GroupLN.MarketData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupLN.MarketData.Persistence.Configurations;

public class GeocodingCacheConfiguration : IEntityTypeConfiguration<GeocodingCache>
{
    public void Configure(EntityTypeBuilder<GeocodingCache> builder)
    {
        builder.ToTable("GeocodingCache");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AddressKey).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Street).HasMaxLength(200);
        builder.Property(x => x.HouseNumber).HasMaxLength(20);
        builder.Property(x => x.PostalCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.City).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Country).HasMaxLength(10).IsRequired().HasDefaultValue("BE");
        builder.Property(x => x.Latitude).HasPrecision(9, 6);
        builder.Property(x => x.Longitude).HasPrecision(9, 6);
        builder.Property(x => x.Provider).HasMaxLength(100);

        builder.HasIndex(x => x.AddressKey).IsUnique();
    }
}

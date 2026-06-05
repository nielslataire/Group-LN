using GroupLN.MarketData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupLN.MarketData.Persistence.Configurations;

public class MarketPropertyConfiguration : IEntityTypeConfiguration<MarketProperty>
{
    public void Configure(EntityTypeBuilder<MarketProperty> builder)
    {
        builder.ToTable("MarketProperty");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ExternalId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Url)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.Title)
            .HasMaxLength(500);

        builder.Property(x => x.PropertyType)
            .HasConversion<int>();

        builder.Property(x => x.PropertySubType)
            .HasConversion<int>();

        builder.Property(x => x.TransactionType)
            .HasConversion<int>();

        builder.Property(x => x.Country)
            .HasMaxLength(2)
            .HasDefaultValue("BE");

        builder.Property(x => x.PostalCode)
            .HasMaxLength(10);

        builder.Property(x => x.City)
            .HasMaxLength(150);

        builder.Property(x => x.Street)
            .HasMaxLength(250);

        builder.Property(x => x.HouseNumber)
            .HasMaxLength(20);

        builder.Property(x => x.Latitude)
            .HasPrecision(9, 6);

        builder.Property(x => x.Longitude)
            .HasPrecision(9, 6);

        // Unieke combinatie van bron + extern ID
        builder.HasIndex(x => new { x.SourceId, x.ExternalId })
            .IsUnique()
            .HasDatabaseName("UQ_MarketProperty_Source_ExternalId");

        builder.HasIndex(x => x.PostalCode);
        builder.HasIndex(x => x.City);
        builder.HasIndex(x => x.PropertyType);
        builder.HasIndex(x => x.TransactionType);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.FirstSeenAt);
        builder.HasIndex(x => x.LastSeenAt);

        builder.HasMany(x => x.Snapshots)
            .WithOne(x => x.Property)
            .HasForeignKey(x => x.MarketPropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.PriceHistory)
            .WithOne(x => x.Property)
            .HasForeignKey(x => x.MarketPropertyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

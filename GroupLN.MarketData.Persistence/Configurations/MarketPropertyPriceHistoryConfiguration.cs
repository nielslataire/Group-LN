using GroupLN.MarketData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupLN.MarketData.Persistence.Configurations;

public class MarketPropertyPriceHistoryConfiguration : IEntityTypeConfiguration<MarketPropertyPriceHistory>
{
    public void Configure(EntityTypeBuilder<MarketPropertyPriceHistory> builder)
    {
        builder.ToTable("MarketPropertyPriceHistory");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AskingPrice)
            .HasPrecision(14, 2);

        builder.Property(x => x.PreviousPrice)
            .HasPrecision(14, 2);

        builder.Property(x => x.PriceChangeAmount)
            .HasPrecision(14, 2);

        builder.Property(x => x.PriceChangePercentage)
            .HasPrecision(8, 4);

        builder.HasIndex(x => x.MarketPropertyId);
        builder.HasIndex(x => x.DetectedAt);
    }
}

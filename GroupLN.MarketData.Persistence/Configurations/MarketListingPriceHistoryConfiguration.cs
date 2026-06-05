using GroupLN.MarketData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupLN.MarketData.Persistence.Configurations;

public class MarketListingPriceHistoryConfiguration : IEntityTypeConfiguration<MarketListingPriceHistory>
{
    public void Configure(EntityTypeBuilder<MarketListingPriceHistory> builder)
    {
        builder.ToTable("MarketListingPriceHistory");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AskingPrice).HasPrecision(14, 2);
        builder.Property(x => x.PreviousPrice).HasPrecision(14, 2);
        builder.Property(x => x.PriceChangeAmount).HasPrecision(14, 2);
        builder.Property(x => x.PriceChangePercentage).HasPrecision(8, 4);

        builder.HasIndex(x => x.MarketListingId);
        builder.HasIndex(x => x.DetectedAt);
    }
}

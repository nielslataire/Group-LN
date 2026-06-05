namespace GroupLN.MarketData.Core.Entities;

public class MarketListingPriceHistory
{
    public long Id { get; set; }
    public long MarketListingId { get; set; }

    public DateTime DetectedAt { get; set; }
    public decimal AskingPrice { get; set; }
    public decimal? PreviousPrice { get; set; }
    public decimal? PriceChangeAmount { get; set; }
    public decimal? PriceChangePercentage { get; set; }

    public MarketListing Listing { get; set; } = null!;
}

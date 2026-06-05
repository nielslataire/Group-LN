namespace GroupLN.MarketData.Core.Entities;

public class MarketPropertyPriceHistory
{
    public long Id { get; set; }
    public long MarketPropertyId { get; set; }

    public DateTime DetectedAt { get; set; }

    public decimal AskingPrice { get; set; }
    public decimal? PreviousPrice { get; set; }

    public decimal? PriceChangeAmount { get; set; }
    public decimal? PriceChangePercentage { get; set; }

    public MarketProperty Property { get; set; } = null!;
}

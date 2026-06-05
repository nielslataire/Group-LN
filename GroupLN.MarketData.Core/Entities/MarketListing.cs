namespace GroupLN.MarketData.Core.Entities;

public class MarketListing
{
    public long Id { get; set; }
    public long MarketAssetId { get; set; }
    public int SourceId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
    public decimal? AskingPrice { get; set; }

    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? RemovedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public MarketAsset Asset { get; set; } = null!;
    public CrawlerSource Source { get; set; } = null!;
    public ICollection<MarketListingSnapshot> Snapshots { get; set; } = new List<MarketListingSnapshot>();
    public ICollection<MarketListingPriceHistory> PriceHistory { get; set; } = new List<MarketListingPriceHistory>();
}

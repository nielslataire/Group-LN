using GroupLN.MarketData.Core.Enums;

namespace GroupLN.MarketData.Core.Entities;

public class MarketProperty
{
    public long Id { get; set; }
    public int SourceId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }

    public PropertyType PropertyType { get; set; }
    public PropertySubType PropertySubType { get; set; }
    public TransactionType TransactionType { get; set; }

    public string Country { get; set; } = "BE";
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Street { get; set; }
    public string? HouseNumber { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime? RemovedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public CrawlerSource Source { get; set; } = null!;
    public ICollection<MarketPropertySnapshot> Snapshots { get; set; } = new List<MarketPropertySnapshot>();
    public ICollection<MarketPropertyPriceHistory> PriceHistory { get; set; } = new List<MarketPropertyPriceHistory>();
}

using GroupLN.MarketData.Core.Enums;

namespace GroupLN.MarketData.Core.Entities;

public class MarketAsset
{
    public long Id { get; set; }
    public string AssetKey { get; set; } = string.Empty;

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

    public decimal? LivingArea { get; set; }
    public decimal? LandArea { get; set; }
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public int? ConstructionYear { get; set; }
    public decimal? EPCScore { get; set; }
    public EPCLabel? EPCLabel { get; set; }
    public bool NewBuild { get; set; }

    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<MarketListing> Listings { get; set; } = new List<MarketListing>();
    public ICollection<MarketAssetMatchCandidate> MatchCandidates { get; set; } = new List<MarketAssetMatchCandidate>();
}

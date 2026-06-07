using GroupLN.MarketData.Core.Enums;

namespace GroupLN.MarketData.Core.Entities;

public class MarketListingSnapshot
{
    public long Id { get; set; }
    public long MarketListingId { get; set; }
    public DateTime SnapshotDate { get; set; }

    public decimal? AskingPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? LivingArea { get; set; }
    public decimal? LandArea { get; set; }
    public decimal? TerraceArea { get; set; }
    public decimal? GardenArea { get; set; }
    public int? Floor { get; set; }
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public int? ShowerCount { get; set; }
    public int? ToiletCount { get; set; }
    public int? GarageCount { get; set; }
    public int? ConstructionYear { get; set; }
    public decimal? EPCScore { get; set; }
    public EPCLabel? EPCLabel { get; set; }
    public bool IsNewBuild { get; set; }
    public SaleStatus? SaleStatus { get; set; }
    public string? EnergyFeatures { get; set; }

    public string? DescriptionHash { get; set; }
    public string? RawJson { get; set; }

    public MarketListing Listing { get; set; } = null!;
}

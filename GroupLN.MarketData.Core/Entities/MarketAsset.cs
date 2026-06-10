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
    public decimal? MaxPrice { get; set; }
    public string? EnergyFeatures { get; set; }
    public string? DeveloperName { get; set; }
    public string? DeveloperWebsite { get; set; }
    public string? DeveloperPhone { get; set; }
    public bool NewBuild { get; set; }

    // ProjectGroup en unit-relatie
    public bool IsProjectGroup { get; set; }
    public long? ParentMarketAssetId { get; set; }
    public string? ProjectExternalId { get; set; }
    public string? UnitExternalId { get; set; }
    public SaleStatus? SaleStatus { get; set; }

    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public DateTime? FirstSoldAt { get; set; }
    public DateTime? StatusChangedAt { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Geo-locatie op basis van AdminVector polygonen
    public int? GeoMunicipalityId { get; set; }
    public int? GeoMunicipalSectionId { get; set; }
    public DateTime? LocationResolvedAt { get; set; }
    public string? LocationResolutionSource { get; set; }
    public int? LocationResolutionConfidence { get; set; }

    public GeoMunicipality? GeoMunicipality { get; set; }
    public GeoMunicipalSection? GeoMunicipalSection { get; set; }

    public ICollection<MarketListing> Listings { get; set; } = new List<MarketListing>();
    public ICollection<MarketAssetMatchCandidate> MatchCandidates { get; set; } = new List<MarketAssetMatchCandidate>();

    public MarketAsset? ParentAsset { get; set; }
    public ICollection<MarketAsset> ChildUnits { get; set; } = new List<MarketAsset>();
}

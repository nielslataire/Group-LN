using GroupLN.MarketData.Core.Enums;

namespace GroupLN.MarketData.Core.DTOs;

public class NormalizedPropertyDto
{
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
    public int? Floor { get; set; }
    public string? UnitNumber { get; set; }
    public bool IsProjectListing { get; set; }
    public bool IsProjectUnit { get; set; }
    public string? ProjectExternalId { get; set; }
    public string? UnitExternalId { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public decimal? AskingPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? LivingArea { get; set; }
    public decimal? LandArea { get; set; }
    public decimal? TerraceArea { get; set; }
    public decimal? GardenArea { get; set; }

    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public int? ShowerCount { get; set; }
    public int? ToiletCount { get; set; }
    public int? GarageCount { get; set; }

    public int? ConstructionYear { get; set; }

    public decimal? EPCScore { get; set; }
    public EPCLabel? EPCLabel { get; set; }

    public bool IsNewBuild { get; set; }

    public string? DeveloperName { get; set; }
    public string? DeveloperWebsite { get; set; }
    public string? DeveloperPhone { get; set; }

    public string? EnergyFeatures { get; set; }
    public string? DescriptionHash { get; set; }
    public string? RawJson { get; set; }

    // Foto-URL's van de projectdetailpagina (alleen voor projectgroepen)
    public List<string> PhotoUrls { get; set; } = new();
}

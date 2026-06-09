namespace GroupLN.MarketData.Core.DTOs;

public class ListingDto
{
    public string ExternalId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }

    public string? PropertyTypeRaw { get; set; }
    public string? PropertySubTypeRaw { get; set; }
    public string? TransactionTypeRaw { get; set; }

    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Street { get; set; }
    public string? HouseNumber { get; set; }
    public int? Floor { get; set; }
    public string? UnitNumber { get; set; }

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
    public string? EPCLabelRaw { get; set; }

    public bool? IsNewBuild { get; set; }
    public string? IsNewBuildSource { get; set; }

    // cluster.projectInfo.projectName — alleen gevuld voor ProjectGroups
    public string? ProjectName { get; set; }

    public string? DeveloperName { get; set; }
    public string? DeveloperWebsite { get; set; }
    public string? DeveloperPhone { get; set; }

    public string? EnergyFeatures { get; set; }
    public string? Description { get; set; }
    public string? RawJson { get; set; }
}

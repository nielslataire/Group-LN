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

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public decimal? AskingPrice { get; set; }
    public decimal? LivingArea { get; set; }
    public decimal? LandArea { get; set; }

    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public int? GarageCount { get; set; }

    public int? ConstructionYear { get; set; }

    public decimal? EPCScore { get; set; }
    public string? EPCLabelRaw { get; set; }

    public bool? IsNewBuild { get; set; }

    public string? Description { get; set; }
    public string? RawJson { get; set; }
}

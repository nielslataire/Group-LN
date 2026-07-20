using GroupLN.MarketData.Core.Enums;

namespace GroupLN.MarketData.Core.DTOs;

public class ProjectGroupUnitDto
{
    public string ParentProjectId { get; init; } = string.Empty;
    public string? ParentProjectName { get; init; }
    public string UnitId { get; init; } = string.Empty;
    /// <summary>Canonical URL van de unit zelf (bv. /nl/brugge-8000/te-koop/appartement/KG294/).</summary>
    public string? Url { get; init; }
    /// <summary>Weergavenummer uit de projecttabel (bv. "0101"). Null als niet beschikbaar.</summary>
    public string? UnitNumber { get; init; }
    public string? RawGroupType { get; init; }
    public string? RawSubType { get; init; }
    public PropertyType MappedPropertyType { get; init; }
    public PropertySubType MappedPropertySubType { get; init; }
    public SaleStatus SaleStatus { get; init; }
    public decimal? Price { get; init; }
    public int? BedroomCount { get; init; }
    public decimal? Surface { get; init; }
    public decimal? TerraceArea { get; init; }
    public decimal? LandArea { get; init; }
    public decimal? GardenArea { get; init; }
    public int? Floor { get; init; }
    public string? Phase { get; init; }
}

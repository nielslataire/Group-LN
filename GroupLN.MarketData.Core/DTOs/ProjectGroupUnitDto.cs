using GroupLN.MarketData.Core.Enums;

namespace GroupLN.MarketData.Core.DTOs;

public class ProjectGroupUnitDto
{
    public string ParentProjectId { get; init; } = string.Empty;
    public string? ParentProjectName { get; init; }
    public string UnitId { get; init; } = string.Empty;
    public string? RawGroupType { get; init; }
    public string? RawSubType { get; init; }
    public PropertyType MappedPropertyType { get; init; }
    public PropertySubType MappedPropertySubType { get; init; }
    public SaleStatus SaleStatus { get; init; }
    public decimal? Price { get; init; }
    public int? BedroomCount { get; init; }
    public decimal? Surface { get; init; }
    public int? Floor { get; init; }
    public string? Phase { get; init; }
}

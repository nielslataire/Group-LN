namespace GroupLN.MarketData.Core.DTOs;

public class LocationResolutionResult
{
    public int? GeoMunicipalityId { get; set; }
    public int? GeoMunicipalSectionId { get; set; }
    public DateTime ResolvedAt { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = string.Empty;
    public int? Confidence { get; set; }

    public bool Resolved => GeoMunicipalSectionId.HasValue || GeoMunicipalityId.HasValue;
}

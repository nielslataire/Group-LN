namespace GroupLN.MarketData.Core.Entities;

public class CanonicalProject
{
    public long Id { get; set; }
    public string CanonicalName { get; set; } = "";
    public string NormalizedName { get; set; } = "";

    public int? GeoMunicipalityId { get; set; }
    public int? GeoMunicipalSectionId { get; set; }
    public string? PostalCode { get; set; }
    public string? Street { get; set; }
    public string? HouseNumber { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? DeveloperName { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    public GeoMunicipality? GeoMunicipality { get; set; }
    public GeoMunicipalSection? GeoMunicipalSection { get; set; }
    public ICollection<CanonicalProjectAsset> Assets { get; set; } = new List<CanonicalProjectAsset>();
}

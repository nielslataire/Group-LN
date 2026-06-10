using NetTopologySuite.Geometries;

namespace GroupLN.MarketData.Core.Entities;

public class GeoMunicipalSection
{
    public int Id { get; set; }
    public string PseudoNis { get; set; } = string.Empty;
    public string? ZipCode { get; set; }
    public string? NisCodeMunicipality { get; set; }
    public string? NameDutch { get; set; }
    public string? NameFrench { get; set; }
    public string? NameGerman { get; set; }
    public Geometry Boundary { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public ICollection<MarketAsset> MarketAssets { get; set; } = new List<MarketAsset>();
}

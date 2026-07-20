using GroupLN.MarketData.Core.Enums;

namespace GroupLN.MarketData.Core.DTOs;

/// <summary>
/// Een gededupliceerde unit binnen een canonical project.
/// Bevat alle bronvarianten zodat CPM Core prijsvergelijking kan tonen.
/// </summary>
public class CanonicalUnitDto
{
    public long AssetId { get; set; }
    public string? UnitExternalId { get; set; }
    public PropertyType PropertyType { get; set; }
    public PropertySubType PropertySubType { get; set; }

    public int? Floor { get; set; }
    public string? UnitNumber { get; set; }
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public decimal? LivingArea { get; set; }
    public decimal? TerraceArea { get; set; }

    public SaleStatus? SaleStatus { get; set; }
    public decimal? AskingPrice { get; set; }

    /// <summary>Alle bronvermeldingen van deze unit (Immoweb, Zimmo, ...).</summary>
    public List<CanonicalUnitSourceDto> Sources { get; set; } = new();
}

/// <summary>
/// Bronvermelding van een unit op een specifieke platform.
/// </summary>
public class CanonicalUnitSourceDto
{
    public string SourceName { get; set; } = "";
    public string ExternalId { get; set; } = "";
    public string Url { get; set; } = "";
    public decimal? Price { get; set; }
    public SaleStatus? SaleStatus { get; set; }
    public DateTime LastSeenAt { get; set; }
}

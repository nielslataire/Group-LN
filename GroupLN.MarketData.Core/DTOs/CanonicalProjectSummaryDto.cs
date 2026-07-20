namespace GroupLN.MarketData.Core.DTOs;

/// <summary>
/// Samenvatting van één canonical project — bedoeld voor lijstweergaven in CPM Core.
/// Eén rij = één echt project, ongeacht hoeveel bronnen het publiceren.
/// </summary>
public class CanonicalProjectSummaryDto
{
    public long CanonicalProjectId { get; set; }
    public string CanonicalName { get; set; } = "";
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Street { get; set; }
    public string? HouseNumber { get; set; }
    public string? DeveloperName { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    public int TotalUnits { get; set; }
    public int AvailableUnits { get; set; }
    public int SoldUnits { get; set; }
    public int ReservedUnits { get; set; }
    public decimal Verkoopgraad { get; set; }

    /// <summary>Aantal gekoppelde bronprojecten (Immoweb, Zimmo, ...).</summary>
    public int LinkedSourceCount { get; set; }
    /// <summary>Namen van de bronnen die dit project publiceren.</summary>
    public List<string> SourceNames { get; set; } = new();

    public DateTime LastSeenAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

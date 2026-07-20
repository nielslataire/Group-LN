namespace GroupLN.MarketData.Core.Entities;

/// <summary>
/// Koppelt één MarketAsset-unit aan een CanonicalUnit.
/// Eén rij per (CanonicalUnit, MarketAsset) combinatie.
/// </summary>
public class CanonicalUnitAsset
{
    public long Id { get; set; }
    public long CanonicalUnitId { get; set; }
    public long MarketAssetId { get; set; }

    public string SourceName { get; set; } = "";
    public string? ExternalId { get; set; }

    /// <summary>"Exact", "Probable", "Possible"</summary>
    public string MatchLevel { get; set; } = "Exact";
    public decimal MatchScore { get; set; }
    public string? MatchReasons { get; set; }

    public bool IsRepresentative { get; set; }

    /// <summary>True als dit een losse listing is (geen ParentMarketAssetId) die achteraf gekoppeld werd.</summary>
    public bool IsFromLooseListing { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public CanonicalUnit CanonicalUnit { get; set; } = null!;
    public MarketAsset MarketAsset { get; set; } = null!;
}

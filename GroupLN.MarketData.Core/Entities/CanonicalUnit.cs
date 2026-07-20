using GroupLN.MarketData.Core.Enums;

namespace GroupLN.MarketData.Core.Entities;

/// <summary>
/// Groepeert dezelfde fysieke woning/appartement over meerdere bronnen heen.
/// Één CanonicalUnit per unieke echte unit binnen een CanonicalProject.
/// MarketAsset-records worden nooit verwijderd of samengevoegd.
/// </summary>
public class CanonicalUnit
{
    public long Id { get; set; }
    public long CanonicalProjectId { get; set; }

    /// <summary>De meest volledige bronunit — gebruikt voor weergave-velden.</summary>
    public long RepresentativeMarketAssetId { get; set; }

    public string? Title { get; set; }
    public string? UnitNumber { get; set; }
    public PropertyType PropertyType { get; set; }
    public int? Bedrooms { get; set; }
    public decimal? Area { get; set; }
    public decimal? Price { get; set; }
    public decimal? PricePerSqm { get; set; }
    public SaleStatus? Status { get; set; }
    public int? Floor { get; set; }

    // Conflictdetectie
    public bool HasPriceConflict { get; set; }
    public bool HasStatusConflict { get; set; }
    public bool HasAreaConflict { get; set; }
    public string? ConflictSummary { get; set; }

    /// <summary>Meerdere even goede kandidaten bij matching — niet automatisch samengevoegd.</summary>
    public bool IsAmbiguous { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public CanonicalProject CanonicalProject { get; set; } = null!;
    public ICollection<CanonicalUnitAsset> SourceAssets { get; set; } = new List<CanonicalUnitAsset>();
}

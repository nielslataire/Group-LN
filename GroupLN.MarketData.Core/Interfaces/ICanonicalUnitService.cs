using GroupLN.MarketData.Core.DTOs;

namespace GroupLN.MarketData.Core.Interfaces;

public interface ICanonicalUnitService
{
    /// <summary>
    /// Bouwt CanonicalUnits opnieuw op voor alle (of één) CanonicalProject.
    /// Verwijdert bestaande CanonicalUnit/CanonicalUnitAsset rows en herberekent.
    /// Wijzigt geen MarketAsset/MarketListing/Snapshot data.
    /// </summary>
    Task<CanonicalUnitRebuildResult> RebuildCanonicalUnitsAsync(
        long? canonicalProjectId = null,
        CancellationToken cancellationToken = default);
}

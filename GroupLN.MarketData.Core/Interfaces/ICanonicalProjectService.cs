using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Entities;

namespace GroupLN.MarketData.Core.Interfaces;

/// <summary>
/// CPM Core mag nooit rechtstreeks op individuele MarketAssets werken voor projecten.
/// Alle project-gerelateerde queries verlopen via CanonicalProject.
///
/// Hiërarchie:
///   CanonicalProject → CanonicalProjectAssets → MarketAssets → MarketListings → Snapshots/PriceHistory
/// </summary>
public interface ICanonicalProjectService
{
    /// <summary>
    /// Herberekent alle canonical projects op basis van actieve projectgroepen
    /// en bestaande MarketAssetMatchCandidate records (Exact + Probable ≥ 0.80).
    /// Verwijdert GEEN MarketAssets of MarketListings — enkel de koppeltabel CanonicalProjectAssets.
    /// </summary>
    Task<RebuildCanonicalResult> RebuildCanonicalProjectsAsync(CancellationToken ct = default);

    // ── CPM Core lijstweergave ────────────────────────────────────────────────

    /// <summary>
    /// Lijst van alle actieve canonical projects met geaggregeerde metadata.
    /// Eén item = één echt project, ongeacht hoeveel bronnen het publiceren.
    /// </summary>
    Task<List<CanonicalProjectSummaryDto>> GetCanonicalProjectsAsync(CancellationToken ct = default);

    // ── CPM Core detailweergave ───────────────────────────────────────────────

    /// <summary>
    /// Geeft het canonical project terug voor een specifieke MarketAsset.
    /// Null als het asset geen canonical associatie heeft.
    /// </summary>
    Task<CanonicalProject?> GetCanonicalProjectForAssetAsync(long marketAssetId, CancellationToken ct = default);

    /// <summary>
    /// Alle bronprojecten (MarketAssets) gekoppeld aan een canonical project.
    /// Toont per bron: ExternalId, URL, match-level, is-primary, AI-naam, foto-count.
    /// </summary>
    Task<List<CanonicalProjectSourceDto>> GetCanonicalProjectSourcesAsync(long canonicalProjectId, CancellationToken ct = default);

    /// <summary>
    /// Alle gededupliceerde units van een canonical project, met bronvarianten.
    /// KPI's (verkoop, beschikbaar, gereserveerd) worden pas berekend ná unit-matching.
    /// </summary>
    Task<List<CanonicalUnitDto>> GetCanonicalProjectUnitsAsync(long canonicalProjectId, CancellationToken ct = default);

    /// <summary>
    /// Eén unit met alle bronvermeldingen (prijs, status, URL per bron).
    /// </summary>
    Task<CanonicalUnitDto?> GetCanonicalUnitAsync(long unitAssetId, CancellationToken ct = default);

    /// <summary>
    /// Chronologische tijdlijn van prijswijzigingen en statusevents voor een canonical project,
    /// gecombineerd over alle gekoppelde bronprojecten.
    /// </summary>
    Task<CanonicalProjectTimelineDto?> GetCanonicalProjectTimelineAsync(long canonicalProjectId, CancellationToken ct = default);

    // ── Statistieken ─────────────────────────────────────────────────────────

    /// <summary>
    /// Geeft geaggregeerde statistieken (units, verkoopgraad, prijs) voor een canonical project.
    /// </summary>
    Task<CanonicalProjectStatisticsDto?> GetCanonicalProjectStatisticsAsync(long canonicalProjectId, CancellationToken ct = default);
}

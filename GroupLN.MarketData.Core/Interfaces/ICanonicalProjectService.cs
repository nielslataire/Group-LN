using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Entities;

namespace GroupLN.MarketData.Core.Interfaces;

public interface ICanonicalProjectService
{
    /// <summary>
    /// Herberekent alle canonical projects op basis van actieve projectgroepen
    /// en bestaande MarketAssetMatchCandidate records (Exact + Probable ≥ 0.80).
    /// Possible-matches worden overgeslagen en gelogd.
    /// </summary>
    Task<RebuildCanonicalResult> RebuildCanonicalProjectsAsync(CancellationToken ct = default);

    /// <summary>
    /// Geeft het canonical project terug voor een specifieke MarketAsset.
    /// Null als het asset geen canonical associatie heeft.
    /// </summary>
    Task<CanonicalProject?> GetCanonicalProjectForAssetAsync(long marketAssetId, CancellationToken ct = default);

    /// <summary>
    /// Geeft geaggregeerde statistieken (units, verkoopgraad, prijs) voor een canonical project,
    /// over alle gekoppelde projectgroep-assets heen.
    /// </summary>
    Task<CanonicalProjectStatisticsDto?> GetCanonicalProjectStatisticsAsync(long canonicalProjectId, CancellationToken ct = default);
}

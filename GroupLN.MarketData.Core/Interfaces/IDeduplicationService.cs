using GroupLN.MarketData.Core.DTOs;

namespace GroupLN.MarketData.Core.Interfaces;

public interface IDeduplicationService
{
    /// <summary>
    /// Detecteert dubbele projecten en units voor alle assets die gewijzigd zijn
    /// sinds <paramref name="since"/> (typisch de start van de huidige crawl).
    /// Slaat match candidates op — verwijdert of mergt niets.
    /// </summary>
    Task<DeduplicationSummary> RunAsync(DateTime since, CancellationToken ct = default);
}

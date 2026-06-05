using GroupLN.MarketData.Core.DTOs;

namespace GroupLN.MarketData.Core.Interfaces;

public interface IMarketPropertyService
{
    // Geeft true terug als een nieuw pand aangemaakt werd, false als bijgewerkt
    Task<bool> UpsertPropertyAsync(NormalizedPropertyDto property, CancellationToken cancellationToken = default);

    Task MarkInactiveAsync(int sourceId, IEnumerable<string> activeExternalIds, CancellationToken cancellationToken = default);

    Task<int> MarkStalePropertiesInactiveAsync(int sourceId, int afterDays, CancellationToken cancellationToken = default);
}

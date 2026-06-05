using GroupLN.MarketData.Core.DTOs;

namespace GroupLN.MarketData.Core.Interfaces;

public interface IMarketListingService
{
    // Geeft true terug als een nieuw record aangemaakt werd, false als bijgewerkt
    Task<bool> UpsertListingAsync(NormalizedPropertyDto property, CancellationToken cancellationToken = default);

    Task MarkInactiveAsync(int sourceId, IEnumerable<string> activeExternalIds, CancellationToken cancellationToken = default);

    Task<int> MarkStaleListingsInactiveAsync(int sourceId, int afterDays, CancellationToken cancellationToken = default);
}

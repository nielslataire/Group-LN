using GroupLN.MarketData.Core.DTOs;

namespace GroupLN.MarketData.Core.Interfaces;

public interface ILocationResolver
{
    Task<LocationResolutionResult?> ResolveAsync(
        decimal? latitude,
        decimal? longitude,
        string? rawCity,
        string? postalCode,
        CancellationToken cancellationToken = default);
}

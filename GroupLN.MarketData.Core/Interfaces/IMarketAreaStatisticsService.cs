namespace GroupLN.MarketData.Core.Interfaces;

public interface IMarketAreaStatisticsService
{
    Task UpdateStatisticsAsync(CancellationToken cancellationToken = default);
}

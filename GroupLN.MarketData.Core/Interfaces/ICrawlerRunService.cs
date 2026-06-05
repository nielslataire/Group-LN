using GroupLN.MarketData.Core.DTOs;

namespace GroupLN.MarketData.Core.Interfaces;

public interface ICrawlerRunService
{
    Task<long> StartRunAsync(int sourceId, CancellationToken cancellationToken = default);

    Task CompleteRunAsync(long runId, CrawlerResult result, CancellationToken cancellationToken = default);

    Task FailRunAsync(long runId, string errorMessage, CancellationToken cancellationToken = default);
}

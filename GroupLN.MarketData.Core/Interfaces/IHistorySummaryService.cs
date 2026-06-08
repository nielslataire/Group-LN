namespace GroupLN.MarketData.Core.Interfaces;

public interface IHistorySummaryService
{
    Task WriteHistorySummaryAsync(DateTime runStartedAt, CancellationToken cancellationToken = default);
    Task WriteTopProjectsReportAsync(CancellationToken cancellationToken = default);
}

using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Enums;
using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Persistence;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.Services;

public class CrawlerRunService : ICrawlerRunService
{
    private readonly MarketDataDbContext _context;
    private readonly ILogger<CrawlerRunService> _logger;

    public CrawlerRunService(MarketDataDbContext context, ILogger<CrawlerRunService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<long> StartRunAsync(int sourceId, CancellationToken cancellationToken = default)
    {
        var run = new CrawlerRun
        {
            SourceId = sourceId,
            StartedAt = DateTime.UtcNow,
            Status = CrawlerStatus.Running
        };

        _context.CrawlerRuns.Add(run);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("CrawlerRun {RunId} gestart voor bron {SourceId}.", run.Id, sourceId);
        return run.Id;
    }

    public async Task CompleteRunAsync(long runId, CrawlerResult result, CancellationToken cancellationToken = default)
    {
        var run = await _context.CrawlerRuns.FindAsync(new object[] { runId }, cancellationToken);
        if (run is null) return;

        run.FinishedAt = DateTime.UtcNow;
        run.ListingsFound = result.ListingsFound;
        run.ListingsCreated = result.ListingsCreated;
        run.ListingsUpdated = result.ListingsUpdated;
        run.Errors = result.Errors;
        run.Status = result.Errors > 0 && result.ListingsFound == 0
            ? CrawlerStatus.Failed
            : result.Errors > 0
                ? CrawlerStatus.PartialSuccess
                : CrawlerStatus.Completed;

        if (result.ErrorMessages.Count > 0)
            run.LogMessage = string.Join(" | ", result.ErrorMessages.Take(10));

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task FailRunAsync(long runId, string errorMessage, CancellationToken cancellationToken = default)
    {
        var run = await _context.CrawlerRuns.FindAsync(new object[] { runId }, cancellationToken);
        if (run is null) return;

        run.FinishedAt = DateTime.UtcNow;
        run.Status = CrawlerStatus.Failed;
        run.LogMessage = errorMessage.Length > 4000 ? errorMessage[..4000] : errorMessage;

        await _context.SaveChangesAsync(cancellationToken);
    }
}

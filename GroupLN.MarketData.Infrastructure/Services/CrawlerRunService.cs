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
        // Een afgebroken of onvolledige run (time-out, annulering, testmodus) is nooit "Voltooid":
        // de stap die verdwenen listings inactief zet is dan overgeslagen en dat moet zichtbaar zijn.
        run.Status = result.Errors > 0 && result.ListingsFound == 0
            ? CrawlerStatus.Failed
            : result.Errors > 0 || !result.Success || result.IsPartialRun
                ? CrawlerStatus.PartialSuccess
                : CrawlerStatus.Completed;

        var meldingen = new List<string>();
        if (!string.IsNullOrWhiteSpace(result.Message)) meldingen.Add(result.Message);
        if (!string.IsNullOrWhiteSpace(result.MarkInactiveSkipReason)) meldingen.Add("Inactief-markering overgeslagen: " + result.MarkInactiveSkipReason);
        meldingen.AddRange(result.ErrorMessages.Take(10));
        if (meldingen.Count > 0)
        {
            var log = string.Join(" | ", meldingen.Distinct());
            run.LogMessage = log.Length > 4000 ? log[..4000] : log;
        }

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

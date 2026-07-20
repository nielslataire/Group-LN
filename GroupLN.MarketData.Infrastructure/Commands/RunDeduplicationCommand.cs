using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.Commands;

public class RunDeduplicationCommand
{
    private readonly IDeduplicationService _dedup;
    private readonly ILogger<RunDeduplicationCommand> _logger;

    public RunDeduplicationCommand(
        IDeduplicationService dedup,
        ILogger<RunDeduplicationCommand> logger)
    {
        _dedup  = dedup;
        _logger = logger;
    }

    /// <param name="since">
    /// Alleen assets gewijzigd na dit tijdstip. null / DateTime.MinValue = volledige scan.
    /// </param>
    public async Task<DeduplicationSummary> ExecuteAsync(
        DateTime? since = null,
        CancellationToken ct = default)
    {
        var sinceDate = since ?? DateTime.MinValue;

        _logger.LogInformation(
            "[Deduplicatie] Gestart. Since={Since}",
            sinceDate == DateTime.MinValue ? "alle records" : sinceDate.ToString("yyyy-MM-dd HH:mm"));

        var result = await _dedup.RunAsync(sinceDate, ct);

        _logger.LogInformation(
            "[Deduplicatie] Klaar. " +
            "Gescand={Scanned} | ProjectCandidates={Projects} | UnitCandidates={Units} | Opgeslagen={Saved}",
            result.NewAssetsScanned,
            result.ProjectCandidatesFound,
            result.UnitCandidatesFound,
            result.CandidatesSaved);

        return result;
    }
}

using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.Commands;

public class MatchLooseListingsCommand
{
    private readonly ILooseListingMatchingService _service;
    private readonly ILogger<MatchLooseListingsCommand> _logger;

    public MatchLooseListingsCommand(
        ILooseListingMatchingService service,
        ILogger<MatchLooseListingsCommand> logger)
    {
        _service = service;
        _logger  = logger;
    }

    public async Task<LooseListingMatchResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MatchLooseListingsCommand gestart.");

        var result = await _service.MatchLooseListingsAsync(cancellationToken);

        _logger.LogInformation(
            "MatchLooseListingsCommand klaar. " +
            "Candidates={C} | Matched={M} | Ambiguous={A} | Unmatched={U}",
            result.CandidatesEvaluated,
            result.Matched,
            result.Ambiguous,
            result.Unmatched);

        return result;
    }
}

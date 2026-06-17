using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.Commands;

public class RebuildCanonicalProjectsCommand
{
    private readonly ICanonicalProjectService _service;
    private readonly ILogger<RebuildCanonicalProjectsCommand> _logger;

    public RebuildCanonicalProjectsCommand(
        ICanonicalProjectService service,
        ILogger<RebuildCanonicalProjectsCommand> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task<RebuildCanonicalResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("RebuildCanonicalProjectsCommand gestart.");
        var result = await _service.RebuildCanonicalProjectsAsync(cancellationToken);
        _logger.LogInformation(
            "RebuildCanonicalProjectsCommand klaar. " +
            "Groepen={Groups} | Aangemaakt={Created} | Bijgewerkt={Updated} | Assets={Linked} | Overgeslagen={Skipped}",
            result.ProjectGroupsLoaded, result.CanonicalProjectsCreated,
            result.CanonicalProjectsUpdated, result.AssetsLinked, result.PossibleMatchesSkipped);
        return result;
    }
}

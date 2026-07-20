using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.Commands;

public class RebuildCanonicalUnitsCommand
{
    private readonly ICanonicalUnitService _service;
    private readonly ILogger<RebuildCanonicalUnitsCommand> _logger;

    public RebuildCanonicalUnitsCommand(
        ICanonicalUnitService service,
        ILogger<RebuildCanonicalUnitsCommand> logger)
    {
        _service = service;
        _logger  = logger;
    }

    public async Task<CanonicalUnitRebuildResult> ExecuteAsync(
        long? canonicalProjectId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "RebuildCanonicalUnitsCommand gestart. CanonicalProjectId={Id}",
            canonicalProjectId?.ToString() ?? "alle");

        var result = await _service.RebuildCanonicalUnitsAsync(canonicalProjectId, cancellationToken);

        _logger.LogInformation(
            "RebuildCanonicalUnitsCommand klaar. " +
            "Projects={P} | SourceUnits={S} | CanonicalUnits={C} | Merged={M} | Conflicts={Conf}",
            result.ProjectsProcessed,
            result.SourceUnitsScanned,
            result.CanonicalUnitsCreated,
            result.UnitsMerged,
            result.ConflictsDetected);

        return result;
    }
}

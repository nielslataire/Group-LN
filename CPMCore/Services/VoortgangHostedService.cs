using FacadeCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CPMCore.Services;

/// <summary>
/// Dagelijkse achtergrondtaak die de fysieke/financiële voortgang
/// voor alle actieve (niet-opgeleverde) projecten herberekent en opslaat.
/// Draait elke 24 uur; eerste run 10 minuten na opstarten.
///
/// Elk project wordt verwerkt in een eigen DI-scope zodat de EF Core change tracker
/// na elk project wordt vrijgegeven. Dit voorkomt onbeperkte geheugenaccumulatie
/// bij grote projectenportefeuilles in productie.
/// </summary>
public class VoortgangHostedService : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VoortgangHostedService> _logger;

    public VoortgangHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<VoortgangHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("VoortgangHostedService gestart.");

        await Task.Delay(InitialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunAsync(stoppingToken);
            await Task.Delay(PollInterval, stoppingToken);
        }

        _logger.LogInformation("VoortgangHostedService gestopt.");
    }

    internal async Task RunAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Voortgang herberekening gestart.");
        int succeeded = 0, failed = 0;

        try
        {
            // Stap 1: project IDs ophalen in eigen scope (scope direct vrijgegeven na ToList)
            List<int> projectIds;
            using (var idScope = _scopeFactory.CreateScope())
            {
                var svc = idScope.ServiceProvider.GetRequiredService<IProjectVoortgangService>();
                projectIds = svc.GetActiveProjectIds();
            }

            _logger.LogInformation("Voortgang: {Count} actieve projecten gevonden.", projectIds.Count);

            // Stap 2: elk project in eigen scope zodat de EF change tracker na elk project
            // wordt vrijgegeven en geheugen niet accumuleert over de volledige batch.
            foreach (var id in projectIds)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    using var projectScope = _scopeFactory.CreateScope();
                    var svc = projectScope.ServiceProvider.GetRequiredService<IProjectVoortgangService>();
                    svc.CalculateAndSave(id);
                    succeeded++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Voortgang fout voor project {ProjectId}.", id);
                    failed++;
                }
            }

            _logger.LogInformation(
                "Voortgang herberekening voltooid. Geslaagd={Succeeded}, Mislukt={Failed}.",
                succeeded, failed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout tijdens voortgang herberekening.");
        }
    }
}

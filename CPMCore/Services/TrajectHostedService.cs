using FacadeCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CPMCore.Services;

/// <summary>
/// Dagelijkse achtergrondtaak die per actief project (1) de afgeleide mijlpaal-data van het
/// projecttraject herberekent — streefdata uit ankers, status/werkelijke datum uit de
/// bron-bindingen (<c>BronBinding</c>) — en (2) daarna de mijlpaal-triggers evalueert en
/// afvuurt. Analoog aan <see cref="VoortgangHostedService"/>: elk project in een eigen
/// DI-scope zodat de EF Core change tracker wordt vrijgegeven.
/// </summary>
public class TrajectHostedService : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(12);
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TrajectHostedService> _logger;

    public TrajectHostedService(IServiceScopeFactory scopeFactory, ILogger<TrajectHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TrajectHostedService gestart.");
        await Task.Delay(InitialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunAsync("scheduler", stoppingToken);
            await Task.Delay(PollInterval, stoppingToken);
        }

        _logger.LogInformation("TrajectHostedService gestopt.");
    }

    /// <summary>
    /// Herberekent alle actieve trajecten en evalueert daarna hun triggers, elk project in een eigen
    /// scope. Retourneert het aantal gewijzigde mijlpalen (herberekening) — het aantal afgevuurde
    /// triggers gaat enkel naar de log/audit (MijlpaalTriggerRun), niet naar de aanroeper.
    /// </summary>
    internal async Task<int> RunAsync(string triggeredBy, CancellationToken ct = default)
    {
        _logger.LogInformation("Traject-herberekening gestart ({TriggeredBy}).", triggeredBy);
        int gewijzigd = 0, geslaagd = 0, mislukt = 0, afgevuurd = 0;

        try
        {
            List<int> projectIds;
            using (var idScope = _scopeFactory.CreateScope())
            {
                var svc = idScope.ServiceProvider.GetRequiredService<ITrajectRecalculationService>();
                projectIds = await svc.GetActiveProjectIds();
            }

            _logger.LogInformation("Traject: {Count} actieve trajecten gevonden.", projectIds.Count);

            foreach (var id in projectIds)
            {
                if (ct.IsCancellationRequested) break;
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var recalc = scope.ServiceProvider.GetRequiredService<ITrajectRecalculationService>();
                    gewijzigd += await recalc.RecalculateProject(id, triggeredBy);

                    var dispatcher = scope.ServiceProvider.GetRequiredService<ITrajectTriggerDispatcher>();
                    afgevuurd += await dispatcher.EvaluateAndDispatchProject(id, triggeredBy, ct);

                    geslaagd++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Traject-herberekening/triggers fout voor project {ProjectId}.", id);
                    mislukt++;
                }
            }

            _logger.LogInformation(
                "Traject-herberekening voltooid. Geslaagd={Geslaagd}, Mislukt={Mislukt}, GewijzigdeMijlpalen={Gewijzigd}, AfgevuurdeTriggers={Afgevuurd}.",
                geslaagd, mislukt, gewijzigd, afgevuurd);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout tijdens traject-herberekening.");
        }

        return gewijzigd;
    }
}

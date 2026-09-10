using FacadeCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CPMCore.Services;

/// <summary>
/// Dagelijkse achtergrondtaak die per actief project de afgeleide mijlpaal-data van het
/// projecttraject herberekent: streefdata uit ankers, en status/werkelijke datum uit de
/// bron-bindingen (<c>BronBinding</c>). Analoog aan <see cref="VoortgangHostedService"/>:
/// elk project in een eigen DI-scope zodat de EF Core change tracker wordt vrijgegeven.
/// Triggers/notificaties komen in een latere increment.
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

    /// <summary>Herberekent alle actieve trajecten, elk in een eigen scope. Retourneert het aantal gewijzigde mijlpalen.</summary>
    internal async Task<int> RunAsync(string triggeredBy, CancellationToken ct = default)
    {
        _logger.LogInformation("Traject-herberekening gestart ({TriggeredBy}).", triggeredBy);
        int gewijzigd = 0, geslaagd = 0, mislukt = 0;

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
                    var svc = scope.ServiceProvider.GetRequiredService<ITrajectRecalculationService>();
                    gewijzigd += await svc.RecalculateProject(id, triggeredBy);
                    geslaagd++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Traject-herberekening fout voor project {ProjectId}.", id);
                    mislukt++;
                }
            }

            _logger.LogInformation(
                "Traject-herberekening voltooid. Geslaagd={Geslaagd}, Mislukt={Mislukt}, GewijzigdeMijlpalen={Gewijzigd}.",
                geslaagd, mislukt, gewijzigd);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout tijdens traject-herberekening.");
        }

        return gewijzigd;
    }
}

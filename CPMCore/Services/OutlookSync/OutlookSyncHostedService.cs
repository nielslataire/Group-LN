namespace CPMCore.Services.OutlookSync;

/// <summary>Draait elke <see cref="OutlookSyncService.Interval"/> een sync-ronde voor alle gebruikers met de Outlook-koppeling aan (spec #2). Een fout in een ronde
/// stopt de service niet: de volgende ronde probeert opnieuw. Geen webhooks; enkel polling.</summary>
public class OutlookSyncHostedService : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutlookSyncHostedService> _logger;

    public OutlookSyncHostedService(IServiceScopeFactory scopeFactory, ILogger<OutlookSyncHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutlookSyncHostedService gestart.");
        try
        {
            await Task.Delay(InitialDelay, stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var svc = scope.ServiceProvider.GetRequiredService<OutlookSyncService>();
                    var n = await svc.SynchroniseerAlleAsync(DateTime.UtcNow, stoppingToken);
                    if (n > 0) _logger.LogInformation("Outlook-sync: {Aantal} gebruikers verwerkt.", n);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Outlook-sync ronde mislukt; volgende ronde probeert opnieuw.");
                }
                await Task.Delay(OutlookSyncService.Interval, stoppingToken);
            }
        }
        catch (OperationCanceledException) { }
        _logger.LogInformation("OutlookSyncHostedService gestopt.");
    }
}

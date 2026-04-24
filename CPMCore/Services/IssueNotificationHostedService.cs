using FacadeCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CPMCore.Services;

/// <summary>
/// Background service that polls every 10 minutes and fires any due notification schedules.
/// Because IIssueNotificationSchedulerService is Scoped, we use IServiceScopeFactory
/// to create a fresh scope on each tick (IHostedService itself is Singleton).
/// </summary>
public class IssueNotificationHostedService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(10);
    // Prevents double execution when the internal scheduler and the external HTTP trigger fire simultaneously.
    private readonly SemaphoreSlim _runLock = new(1, 1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IssueNotificationHostedService> _logger;

    public IssueNotificationHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<IssueNotificationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("IssueNotificationHostedService gestart.");

        // Stagger initial run by 30 seconds to allow app pool to fully initialise
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunJobsAsync();
            await Task.Delay(PollInterval, stoppingToken);
        }

        _logger.LogInformation("IssueNotificationHostedService gestopt.");
    }

    internal async Task RunJobsAsync(string triggeredBy = "scheduler")
    {
        if (!await _runLock.WaitAsync(TimeSpan.Zero))
        {
            _logger.LogWarning("RunJobsAsync ({TriggeredBy}) overgeslagen: vorige run is nog actief.", triggeredBy);
            return;
        }

        try
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var scheduler = scope.ServiceProvider.GetRequiredService<IIssueNotificationSchedulerService>();
                await scheduler.RunDueJobsAsync(triggeredBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout tijdens uitvoering van IssueNotificationScheduler.");
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var digest = scope.ServiceProvider.GetRequiredService<IContractorPortalDigestService>();
                await digest.RunAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout tijdens uitvoering van ContractorPortalDigest.");
            }
        }
        finally
        {
            _runLock.Release();
        }
    }
}

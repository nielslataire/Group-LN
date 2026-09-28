using FacadeCore.Signing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CPMCore.Services;

/// <summary>
/// Achtergrondjob van de signingmodule (fase 3, ONDERTEKENEN_VOORSTEL.md/-VOORTGANG.md): sluit
/// verlopen dossiers, verstuurt vervallen herinneringen, past de bewaarregel toe, en probeert
/// dossiers die regel-compleet zijn maar door een eerdere fout nooit voltooid raakten opnieuw te
/// finaliseren. Zelfde patroon als <see cref="IssueNotificationHostedService"/>: <c>ISigningService</c>
/// is Scoped, dus een verse <see cref="IServiceScopeFactory"/>-scope per sub-job; een
/// <see cref="SemaphoreSlim"/> voorkomt overlap tussen de interne timer en de externe HTTP-trigger
/// (<c>/api/trigger/signing</c>). 15 minuten i.p.v. de 10 min van IssueNotification: verlopen/
/// herinneringen/retentie zijn dag-granulariteit, maar een mislukte finalisatie moet zich binnen het
/// uur kunnen herstellen.
/// </summary>
public class SigningHostedService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(15);
    private readonly SemaphoreSlim _runLock = new(1, 1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SigningHostedService> _logger;

    public SigningHostedService(IServiceScopeFactory scopeFactory, ILogger<SigningHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SigningHostedService gestart.");

        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunJobsAsync();
            await Task.Delay(PollInterval, stoppingToken);
        }

        _logger.LogInformation("SigningHostedService gestopt.");
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
            await RunJobAsync("ExpireOverdueCasesAsync", (s, ct) => s.ExpireOverdueCasesAsync(ct));
            await RunJobAsync("SendDueRemindersAsync", (s, ct) => s.SendDueRemindersAsync(ct));
            await RunJobAsync("ApplyRetentionScrubAsync", (s, ct) => s.ApplyRetentionScrubAsync(ct));
            await RunJobAsync("RetryStuckFinalizationsAsync", (s, ct) => s.RetryStuckFinalizationsAsync(ct));
        }
        finally
        {
            _runLock.Release();
        }
    }

    private async Task RunJobAsync(string name, Func<ISigningService, CancellationToken, Task<int>> job)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var signing = scope.ServiceProvider.GetRequiredService<ISigningService>();
            var count = await job(signing, CancellationToken.None);
            if (count > 0) _logger.LogInformation("Signing {Job}: {Count} dossier(s).", name, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout tijdens Signing {Job}.", name);
        }
    }
}

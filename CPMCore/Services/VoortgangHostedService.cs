using FacadeCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CPMCore.Services;

/// <summary>
/// Dagelijkse achtergrondtaak die de fysieke/financiële voortgang
/// voor alle actieve (niet-opgeleverde) projecten herberekent en opslaat.
/// Draait elke 24 uur; eerste run 2 minuten na opstarten.
/// </summary>
public class VoortgangHostedService : BackgroundService
{
    private static readonly TimeSpan InitialDelay  = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan PollInterval  = TimeSpan.FromHours(24);

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
            await RunAsync();
            await Task.Delay(PollInterval, stoppingToken);
        }

        _logger.LogInformation("VoortgangHostedService gestopt.");
    }

    internal async Task RunAsync()
    {
        _logger.LogInformation("Voortgang herberekening gestart voor alle actieve projecten.");
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IProjectVoortgangService>();
            await Task.Run(() => service.CalculateAllProjects());
            _logger.LogInformation("Voortgang herberekening voltooid.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout tijdens voortgang herberekening.");
        }
    }
}

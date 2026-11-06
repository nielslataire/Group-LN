using FacadeCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CPMCore.Services;

/// <summary>
/// Achtergrondtaak voor terugkerende facturen (design-handoff 49/50): elk uur kijkt ze welke actieve schema's een concept moeten klaarzetten
/// (factuurdatum min "concept klaar" valt vandaag of eerder) en maakt dat concept aan. Het concept verschijnt als Proforma in de lijst met facturen.
/// Elke run in een eigen DI-scope; dubbel aanmaken kan niet door de unieke index op Invoices (schema + factuurdatum).
/// </summary>
public class RecurringInvoiceHostedService : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecurringInvoiceHostedService> _logger;

    public RecurringInvoiceHostedService(IServiceScopeFactory scopeFactory, ILogger<RecurringInvoiceHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RecurringInvoiceHostedService gestart.");
        await Task.Delay(InitialDelay, stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IRecurringInvoiceService>();
                var n = await svc.GenerateDueAsync(DateOnly.FromDateTime(DateTime.Today), stoppingToken);
                if (n > 0) _logger.LogInformation("Terugkerende facturen: {Count} concept(en) klaargezet.", n);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Terugkerende facturen: run mislukt.");
            }
            await Task.Delay(PollInterval, stoppingToken);
        }
    }
}

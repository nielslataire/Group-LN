using FacadeCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CPMCore.Services;

/// <summary>
/// Haalt de gezondheidsindex van Statbel op voor het indexeren van terugkerende facturen: op de laatste dag van elke maand (de 31ste, of de 30ste, 29ste of 28ste in kortere maanden;
/// Statbel publiceert de index van een maand rond het einde van die maand) en bij het opstarten als er nog niets of niets recents bewaard is. Mislukt het, dan probeert ze het
/// de volgende run opnieuw (elke 6 uur) tot het lukt. Bij een mislukte poging gaat er (maximaal één keer per dag) een e-mail naar Niels (HealthIndex:AlertEmail, standaard niels.lataire@groupln.be).
/// </summary>
public class HealthIndexHostedService : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(6);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HealthIndexHostedService> _logger;
    private const string DefaultAlertEmail = "niels.lataire@groupln.be";
    private DateOnly _lastAlert = DateOnly.MinValue;

    public HealthIndexHostedService(IServiceScopeFactory scopeFactory, ILogger<HealthIndexHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(InitialDelay, stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IHealthIndexService>();
                var isMonthEnd = today.Day == DateTime.DaysInMonth(today.Year, today.Month);
                var thisMonth = new DateOnly(today.Year, today.Month, 1);
                var (latest, _) = await svc.StatusAsync(stoppingToken);
                // Op de laatste dag van de maand moet de index van die maand er zijn; Statbel publiceert hem soms pas later die dag.
                // Is hij er nog niet, of ontbreekt zelfs die van vorige maand (of alles), dan probeert de volgende run (6 uur later) het opnieuw.
                var needCurrent = isMonthEnd && (latest is null || latest.Value < thisMonth);
                var behind = latest is null || latest.Value < thisMonth.AddMonths(-1);
                if (needCurrent || behind)
                {
                    var r = await svc.RefreshAsync(stoppingToken);
                    if (r.Ok) { _logger.LogInformation("Gezondheidsindex opgehaald: {Rows} nieuwe of gewijzigde rijen, laatste maand {Latest}.", r.Rows, r.Latest); }
                    else
                    {
                        _logger.LogWarning("Gezondheidsindex ophalen mislukt: {Error}", r.Error);
                        await AlertAsync(scope.ServiceProvider, today, r.Error ?? "onbekende fout");
                    }
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gezondheidsindex: run mislukt.");
                try { using var scope = _scopeFactory.CreateScope(); await AlertAsync(scope.ServiceProvider, DateOnly.FromDateTime(DateTime.Today), ex.Message); } catch { /* de log bevat de fout al */ }
            }
            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    /// <summary>Mailt Niels dat het ophalen mislukte, hoogstens één keer per dag, met wat er dan niet kan: indexeringen wachten op de index.</summary>
    private async Task AlertAsync(IServiceProvider sp, DateOnly today, string reason)
    {
        if (_lastAlert == today) return;
        try
        {
            var to = sp.GetRequiredService<IConfiguration>()["HealthIndex:AlertEmail"];
            if (string.IsNullOrWhiteSpace(to)) to = DefaultAlertEmail;
            var html = "<p>Het ophalen van de gezondheidsindex bij Statbel is mislukt.</p>"
                + $"<p>Reden: {System.Net.WebUtility.HtmlEncode(reason)}</p>"
                + "<p>Terugkerende facturen met indexering wachten tot de index bekend is en worden dan niet klaargezet. "
                + "Het CRM probeert het over 6 uur opnieuw. Je kan het ook meteen proberen via Facturen › Terugkerend › Instellingen › Nu ophalen.</p>";
            await sp.GetRequiredService<IEmailSender>().SendEmailAsync(to, "Gezondheidsindex ophalen mislukt", html);
            _lastAlert = today;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gezondheidsindex: de waarschuwingsmail kon niet verstuurd worden.");
        }
    }
}

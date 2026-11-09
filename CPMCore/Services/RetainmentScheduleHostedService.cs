using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CPMCore.Services;

/// <summary>
/// Controle van de inhoudingsplicht (30bis/30ter) volgens een schema: alle leveranciers met een contract of een ontvangen factuur bij een project in uitvoering.
/// De volledige lijst wordt eerst opgemaakt en ontdubbeld (op ondernemingsnummer); wie nog een geldige controle heeft wordt overgeslagen.
/// Een controle zonder inhoudingsplicht is geldig tot de laatste donderdag van de maand, dus hoogstens één keer per maand per leverancier wordt de RSZ bevraagd.
/// Wat bevraagd wordt, komt als één controlelijst (bewijs) in Documentencentrum › Inhoudingsplicht, onder "Systeem (planning)".
/// Loopt enkel in productie (<c>RszBillRetainment:UseSimulation</c> = false) en kan uitgezet worden met <c>RszBillRetainment:ScheduleEnabled</c> = false.
/// </summary>
public class RetainmentScheduleHostedService : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(6);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<RetainmentScheduleHostedService> _logger;

    public RetainmentScheduleHostedService(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<RetainmentScheduleHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(InitialDelay, stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var production = string.Equals(_config["RszBillRetainment:UseSimulation"], "false", StringComparison.OrdinalIgnoreCase);
                var enabled = !string.Equals(_config["RszBillRetainment:ScheduleEnabled"], "false", StringComparison.OrdinalIgnoreCase);
                if (production && enabled) await RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Geplande controle inhoudingsplicht mislukt."); }
            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRetainmentCheckService>();
        if (!service.IsConfigured) return;
        var db = scope.ServiceProvider.GetRequiredService<cpmRunningContext>();

        var inputs = await CollectSuppliersAsync(db, ct);
        if (inputs.Count == 0) return;

        var batch = await service.EnsureCheckedBatchAsync(inputs, "Systeem (planning)", ct);
        if (batch == null)
        {
            _logger.LogInformation("Inhoudingsplicht: {Count} leveranciers, allemaal nog geldig gecontroleerd.", inputs.Count);
            return;
        }
        _logger.LogInformation("Inhoudingsplicht: {Count} leveranciers bevraagd, {Obligation} met inhoudingsplicht, {Failed} mislukt.",
            batch.Items.Count, batch.Items.Count(i => i.HasObligation), batch.Items.Count(i => i.Failed));
    }

    /// <summary>Contractpartijen en facturerende leveranciers van alle projecten in uitvoering, ontdubbeld op btw-nummer.</summary>
    private static async Task<List<RetainmentCheckInput>> CollectSuppliersAsync(cpmRunningContext db, CancellationToken ct)
    {
        var projectIds = db.Project.AsNoTracking()
            .Where(p => p.StatusId == (int)BOCore.ProjectStatusType.Uitvoering)
            .Select(p => p.ProjectId);

        var fromContracts = await db.Contract.AsNoTracking()
            .Where(c => projectIds.Contains(c.ProjectId))
            .Select(c => new { Name = c.Company.BedrijfsNaam, Vat = c.Company.VatNumber })
            .ToListAsync(ct);

        var fromInvoices = await db.IncommingInvoices.AsNoTracking()
            .Where(i => (i.ProjectId != null && projectIds.Contains(i.ProjectId.Value))
                     || (i.ContractId != null && db.Contract.Any(c => c.Id == i.ContractId && projectIds.Contains(c.ProjectId))))
            .Select(i => new { Name = i.SupplierName, Vat = i.SupplierVatNumber })
            .Distinct()
            .ToListAsync(ct);

        return fromContracts.Concat(fromInvoices)
            .Where(x => !string.IsNullOrWhiteSpace(x.Vat))
            .Select(x => new RetainmentCheckInput { SupplierName = x.Name, Identifier = x.Vat })
            .ToList();
    }
}

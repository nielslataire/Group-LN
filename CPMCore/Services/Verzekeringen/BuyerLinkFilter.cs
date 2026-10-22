using DALCore.Models;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CPMCore.Services.Verzekeringen;

/// <summary>Eenheden waarvan in dit verzoek een koper gezet is. Gevuld door <see cref="BuyerLinkInterceptor"/>, leeggemaakt door <see cref="BuyerLinkActionFilter"/>.</summary>
public class PendingBuyerLinks
{
    public List<Units> Units { get; } = new();
}

/// <summary>Merkt op wanneer een eenheid een klant krijgt, waar ook in de applicatie dat gebeurt (klant toevoegen, eenheid bewerken, ondertekend verkoopcontract),
/// zodat de tienjarige polissen en attesten van die eenheid daarna aan de nieuwe koper gekoppeld worden. Er gebeurt hier zelf niets in de database:
/// de eenheid wordt enkel onthouden voor <see cref="BuyerLinkActionFilter"/>, dat pas werkt als de actie klaar is (en dus ook een transactie).</summary>
public class BuyerLinkInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _http;

    public BuyerLinkInterceptor(IHttpContextAccessor http) { _http = http; }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Collect(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Collect(eventData.Context);
        return new ValueTask<InterceptionResult<int>>(result);
    }

    private void Collect(DbContext? context)
    {
        try
        {
            var pending = _http.HttpContext?.RequestServices.GetService<PendingBuyerLinks>();
            if (pending == null || context == null) return;
            foreach (var e in context.ChangeTracker.Entries<Units>())
            {
                if (e.State == EntityState.Added && e.Entity.ClientAccountId != null) pending.Units.Add(e.Entity);
                else if (e.State == EntityState.Modified)
                {
                    var p = e.Property(x => x.ClientAccountId);
                    if (p.IsModified && p.CurrentValue != null && !Equals(p.OriginalValue, p.CurrentValue)) pending.Units.Add(e.Entity);
                }
            }
        }
        catch { /* een fout hier mag het opslaan nooit blokkeren */ }
    }
}

/// <summary>Koppelt na elke actie de tienjarige polissen en attesten aan de koper van eenheden die in die actie een koper kregen (zie <see cref="BuyerLinkInterceptor"/>).</summary>
public class BuyerLinkActionFilter : IAsyncActionFilter
{
    private readonly ILogger<BuyerLinkActionFilter> _logger;

    public BuyerLinkActionFilter(ILogger<BuyerLinkActionFilter> logger) { _logger = logger; }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await next();
        try
        {
            var pending = context.HttpContext.RequestServices.GetService<PendingBuyerLinks>();
            if (pending == null || pending.Units.Count == 0) return;
            var ids = pending.Units.Where(u => u.Id > 0).Select(u => u.Id).Distinct().ToList();
            pending.Units.Clear();
            var svc = context.HttpContext.RequestServices.GetRequiredService<VerzekeringService>();
            foreach (var id in ids) await svc.KoppelEenheidAanKoperAsync(id);
        }
        catch (Exception ex)
        {
            // Bv. migratie 110 nog niet gedraaid: de koppeling gebeurt dan later via "Aan kopers koppelen" bij Verzekeringen.
            _logger.LogWarning(ex, "Polissen konden niet aan de nieuwe koper gekoppeld worden.");
        }
    }
}

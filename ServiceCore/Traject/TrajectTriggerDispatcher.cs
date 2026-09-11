using System.Text.Json;
using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Traject;

/// <summary>
/// Evalueert mijlpaal-triggers voor één project en voert ze uit via de geregistreerde
/// <see cref="ITrajectTriggerAction"/>-handlers (één per <c>TriggerActie</c>; ontbrekende handlers
/// — nog niet beschikbare acties zoals MaakTaak/MaakDossier vóór hun increment — worden als
/// "Overgeslagen" gelogd, niet als fout).
/// </summary>
public class TrajectTriggerDispatcher : ITrajectTriggerDispatcher
{
    private readonly cpmRunningContext _db;
    private readonly Dictionary<int, ITrajectTriggerAction> _actions;

    private static readonly int MpBereikt = (int)MijlpaalStatus.Bereikt;
    private static readonly int MpNvt = (int)MijlpaalStatus.NietVanToepassing;
    private static readonly int FaseAfgerond = (int)FaseStatus.Afgerond;

    public TrajectTriggerDispatcher(cpmRunningContext db, IEnumerable<ITrajectTriggerAction> actions)
    {
        _db = db;
        _actions = actions.ToDictionary(a => a.Actie);
    }

    public async Task<int> EvaluateAndDispatchProject(int projectId, string? triggeredBy = null, CancellationToken ct = default)
    {
        var project = await _db.Project.FirstOrDefaultAsync(p => p.ProjectId == projectId, ct);
        if (project == null) return 0;

        var traject = await _db.Projecttraject
            .Include(t => t.Fases)
            .Include(t => t.Mijlpalen).ThenInclude(m => m.ProjecttrajectFase)
            .Include(t => t.Mijlpalen).ThenInclude(m => m.Triggers)
            .FirstOrDefaultAsync(t => t.ProjectId == projectId, ct);
        if (traject == null) return 0;

        var vandaag = DateOnly.FromDateTime(DateTime.Today);
        var now = DateTime.UtcNow;
        int afgevuurd = 0;

        foreach (var m in traject.Mijlpalen)
        {
            foreach (var trigger in m.Triggers.Where(t => t.IsActief))
            {
                if (!IsGebeurtenisIngetreden((TriggerEvent)trigger.TriggerEvent, m, trigger, vandaag)) continue;

                TriggerActieResultaat resultaat;
                try
                {
                    if (_actions.TryGetValue(trigger.TriggerActie, out var handler))
                        resultaat = await handler.ExecuteAsync(m, trigger, project, triggeredBy, ct);
                    else
                        resultaat = TriggerActieResultaat.Skip(
                            $"Actie {(TriggerActie)trigger.TriggerActie} is nog niet beschikbaar in deze versie.");
                }
                catch (Exception ex)
                {
                    resultaat = TriggerActieResultaat.Fail(ex.Message);
                }

                _db.MijlpaalTriggerRun.Add(new MijlpaalTriggerRun
                {
                    MijlpaalTriggerId = trigger.Id,
                    MijlpaalId = m.Id,
                    Uitgevoerd = now,
                    Status = (int)(resultaat.Overgeslagen ? TriggerRunStatus.Overgeslagen
                              : resultaat.Geslaagd ? TriggerRunStatus.Geslaagd
                              : TriggerRunStatus.Mislukt),
                    Resultaat = resultaat.Resultaat,
                    Fout = resultaat.Fout
                });

                if (resultaat.Geslaagd)
                {
                    // Geslaagd of bewust overgeslagen (nog niet beschikbaar): niet opnieuw proberen.
                    // Enkel een echte fout (Fail) blijft LaatstGevuurdOp leeg zodat de volgende tick herprobeert.
                    trigger.LaatstGevuurdOp = now;
                    afgevuurd++;
                }

                await AddMijlpaalHistorySafe(m.Id, trigger, resultaat, triggeredBy, now);
            }
        }

        await _db.SaveChangesAsync(ct);
        return afgevuurd;
    }

    private bool IsGebeurtenisIngetreden(TriggerEvent evt, Mijlpaal m, MijlpaalTrigger trigger, DateOnly vandaag)
    {
        if (trigger.LaatstGevuurdOp != null && evt != TriggerEvent.BijStatuswijziging) return false;

        bool nietBereikt = m.Status != MpBereikt && m.Status != MpNvt;
        var effectief = m.Doeldatum ?? m.DoeldatumBerekend;

        return evt switch
        {
            TriggerEvent.BijBereiken => m.Status == MpBereikt,
            TriggerEvent.BijOverschrijding => nietBereikt && effectief != null && effectief.Value < vandaag,
            TriggerEvent.XDagenVoorDoeldatum => nietBereikt && effectief != null
                && vandaag >= effectief.Value.AddDays(-(trigger.OffsetDagen ?? 0)),
            TriggerEvent.BijFaseAfronding => m.ProjecttrajectFase?.Status == FaseAfgerond,
            // Benadering: elke mijlpaal-wijziging sinds de laatste keuring geldt als "statuswijziging".
            TriggerEvent.BijStatuswijziging => m.ModifiedDate != null
                && (trigger.LaatstGevuurdOp == null || m.ModifiedDate > trigger.LaatstGevuurdOp),
            _ => false
        };
    }

    private async Task AddMijlpaalHistorySafe(int mijlpaalId, MijlpaalTrigger trigger, TriggerActieResultaat resultaat, string? userId, DateTime now)
    {
        _db.MijlpaalHistoriek.Add(new MijlpaalHistoriek
        {
            MijlpaalId = mijlpaalId,
            Actie = (int)MijlpaalHistoriekActie.TriggerUitgevoerd,
            UserId = userId,
            Timestamp = now,
            NewValueJson = JsonSerializer.Serialize(new
            {
                trigger = (TriggerActie)trigger.TriggerActie,
                gebeurtenis = (TriggerEvent)trigger.TriggerEvent,
                resultaat.Geslaagd,
                resultaat.Overgeslagen
            }),
            Opmerking = resultaat.Resultaat ?? resultaat.Fout
        });
        await Task.CompletedTask;
    }
}

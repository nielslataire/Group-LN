using System.Text.Json;
using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Traject;

public class TrajectRecalculationService : ITrajectRecalculationService
{
    private readonly cpmRunningContext _db;
    private readonly IMijlpaalBindingResolver _resolver;

    private const int StatusOpgeleverd = 1; // BOCore.ProjectStatusType.Opgeleverd
    private static readonly int MpOpen = (int)MijlpaalStatus.Open;
    private static readonly int MpBezig = (int)MijlpaalStatus.Bezig;
    private static readonly int MpBereikt = (int)MijlpaalStatus.Bereikt;
    private static readonly int MpNvt = (int)MijlpaalStatus.NietVanToepassing;
    private const string AnkerProjectCreated = "PROJECT_CREATED";

    public TrajectRecalculationService(cpmRunningContext db, IMijlpaalBindingResolver resolver)
    {
        _db = db;
        _resolver = resolver;
    }

    public async Task<List<int>> GetActiveProjectIds() =>
        await _db.Projecttraject
            .Join(_db.Project, t => t.ProjectId, p => p.ProjectId, (t, p) => new { t.ProjectId, p.StatusId })
            .Where(x => x.StatusId != StatusOpgeleverd)
            .Select(x => x.ProjectId)
            .ToListAsync();

    public async Task<int> RecalculateAll(string? triggeredBy = null)
    {
        var ids = await GetActiveProjectIds();
        int total = 0;
        foreach (var id in ids)
            total += await RecalculateProject(id, triggeredBy);
        return total;
    }

    public async Task<int> RecalculateProject(int projectId, string? triggeredBy = null)
    {
        var traject = await _db.Projecttraject
            .Include(t => t.Fases)
            .Include(t => t.Mijlpalen)
            .FirstOrDefaultAsync(t => t.ProjectId == projectId);
        if (traject == null) return 0;

        var ctx = await BuildContext(projectId);
        if (ctx == null) return 0;

        var ankerBySjabloonId = traject.TrajectSjabloonId is int sid
            ? await _db.TrajectSjabloonMijlpaal
                .Where(s => s.TrajectSjabloonFase.TrajectSjabloonId == sid)
                .Select(s => new { s.Id, s.DoeldatumAnkerCode, s.DoeldatumOffsetDagen })
                .ToDictionaryAsync(s => s.Id, s => (s.DoeldatumAnkerCode, s.DoeldatumOffsetDagen))
            : new Dictionary<int, (string?, int?)>();

        var now = DateTime.UtcNow;
        var vandaag = DateOnly.FromDateTime(DateTime.Today);
        int changed = 0;
        var historiek = new List<MijlpaalHistoriek>();

        // 1. bindings -> automatisch op "bereikt" zetten (nooit terugdraaien)
        foreach (var m in traject.Mijlpalen)
        {
            var u = _resolver.Resolve(m, ctx);
            m.LastComputedDate = now;
            if (u == null) continue;

            if (u.Bereikt && (m.Status == MpOpen || m.Status == MpBezig))
            {
                var oud = new { m.Status, m.WerkelijkeDatum };
                m.Status = MpBereikt;
                m.WerkelijkeDatum = u.WerkelijkeDatum ?? m.WerkelijkeDatum ?? vandaag;
                m.ModifiedByUserId = triggeredBy;
                m.ModifiedDate = now;
                historiek.Add(new MijlpaalHistoriek
                {
                    MijlpaalId = m.Id,
                    Actie = (int)MijlpaalHistoriekActie.AutomatischAfgeleid,
                    UserId = triggeredBy,
                    Timestamp = now,
                    OldValueJson = JsonSerializer.Serialize(oud),
                    NewValueJson = JsonSerializer.Serialize(new { m.Status, m.WerkelijkeDatum }),
                    Opmerking = $"Automatisch bereikt via binding {(ComputedBinding)m.BronBinding!.Value}"
                });
                changed++;
            }
        }

        // 2. streefdata herberekenen uit ankers (enkel waar geen handmatige Doeldatum staat)
        changed += HerberekenDoeldatums(traject.Mijlpalen.ToList(), ankerBySjabloonId,
            traject.GestartOp ?? vandaag);

        // 3. fase-status bijwerken
        HerberekenFases(traject, vandaag);

        // 4. trajectniveau
        traject.HerberekendOp = now;
        traject.Waarschuwingen = BouwWaarschuwingen(traject.Mijlpalen, vandaag);
        traject.ModifiedDate = now;

        if (historiek.Count > 0) _db.MijlpaalHistoriek.AddRange(historiek);
        await _db.SaveChangesAsync();
        return changed;
    }

    // ---------------------------------------------------------------

    private async Task<TrajectBronContext?> BuildContext(int projectId)
    {
        var project = await _db.Project.AsNoTracking().FirstOrDefaultAsync(p => p.ProjectId == projectId);
        if (project == null) return null;

        var voortgang = await _db.ProjectVoortgang.AsNoTracking().FirstOrDefaultAsync(v => v.ProjectId == projectId);
        var docs = await _db.ProjectDocs.AsNoTracking().Where(d => d.ProjectId == projectId).ToListAsync();

        var sectieIds = await _db.PlanningSectie.AsNoTracking()
            .Where(s => s.ProjectId == projectId).Select(s => s.Id).ToListAsync();
        var secties = await _db.PlanningSectie.AsNoTracking().Where(s => s.ProjectId == projectId).ToListAsync();
        var taken = await _db.PlanningTaak.AsNoTracking().Where(t => sectieIds.Contains(t.PlanningSectieId)).ToListAsync();

        var units = await _db.Units.AsNoTracking().Where(u => u.ProjectId == projectId).ToListAsync();
        var clientAccountIds = units.Where(u => u.ClientAccountId != null).Select(u => u.ClientAccountId!.Value).Distinct().ToList();
        var clientAccounts = await _db.ClientAccount.AsNoTracking().Where(c => clientAccountIds.Contains(c.Id)).ToListAsync();
        var caById = clientAccounts.ToDictionary(c => c.Id);
        var caPerUnit = units.Where(u => u.ClientAccountId != null && caById.ContainsKey(u.ClientAccountId!.Value))
            .ToDictionary(u => u.Id, u => caById[u.ClientAccountId!.Value]);

        var paymentGroupIds = await _db.InvoicingPaymentGroup.AsNoTracking()
            .Where(g => g.ProjectId == projectId).Select(g => g.Id).ToListAsync();
        var stages = await _db.InvoicingPaymentStages.AsNoTracking()
            .Where(s => paymentGroupIds.Contains(s.GroupId)).ToListAsync();

        var openIssues = await _db.ConstructionIssue.AsNoTracking()
            .Where(i => i.ProjectId == projectId && i.Status != (int)ConstructionIssueStatus.Closed)
            .Select(i => i.IssuePhase).ToListAsync();

        var settlements = await _db.ConnectionSettlement.AsNoTracking()
            .Where(c => c.ProjectId == projectId).Select(c => c.CreatedOn).ToListAsync();

        var dossiers = await _db.ProjectDossier.AsNoTracking().Where(d => d.ProjectId == projectId).ToListAsync();
        var dossierIds = dossiers.Select(d => d.Id).ToList();
        var substappen = await _db.ProjectDossierSubstap.AsNoTracking()
            .Where(s => dossierIds.Contains(s.ProjectDossierId)).ToListAsync();

        return new TrajectBronContext
        {
            Project = project,
            Voortgang = voortgang,
            Docs = docs,
            PlanningTaken = taken,
            PlanningSecties = secties,
            PaymentStages = stages,
            ClientAccountPerUnit = caPerUnit,
            ClientAccounts = clientAccounts,
            OpenIssuesPerPhase = openIssues.GroupBy(p => p).ToDictionary(g => g.Key, g => g.Count()),
            OpenIssuesTotaal = openIssues.Count,
            HeeftConnectionSettlement = settlements.Count > 0,
            LaatsteConnectionSettlement = settlements.Count > 0
                ? DateOnly.FromDateTime(settlements.Max())
                : null,
            DossiersById = dossiers.ToDictionary(d => d.Id),
            SubstappenPerDossier = substappen.GroupBy(s => s.ProjectDossierId).ToDictionary(g => g.Key, g => g.ToList())
        };
    }

    /// <summary>Herberekent <see cref="Mijlpaal.DoeldatumBerekend"/>. Retourneert het aantal gewijzigde waarden.</summary>
    private static int HerberekenDoeldatums(
        List<Mijlpaal> mijlpalen,
        Dictionary<int, (string? Anker, int? Offset)> ankerBySjabloonId,
        DateOnly anchorBase)
    {
        var byCode = mijlpalen.Where(m => !string.IsNullOrWhiteSpace(m.Code))
                              .ToLookup(m => m.Code!, StringComparer.OrdinalIgnoreCase);

        DateOnly? Effectief(Mijlpaal m) => m.WerkelijkeDatum ?? m.Doeldatum ?? m.DoeldatumBerekend;

        Mijlpaal? AnkerVoor(Mijlpaal m, string code)
        {
            var k = byCode[code].ToList();
            if (k.Count == 0) return null;
            if (m.UnitId != null)
                return k.FirstOrDefault(x => x.UnitId == m.UnitId) ?? k.FirstOrDefault(x => x.UnitId == null) ?? k[0];
            return k.FirstOrDefault(x => x.UnitId == null) ?? k[0];
        }

        // Nieuwe berekende waarden opbouwen (bestaande DoeldatumBerekend wordt vervangen; handmatige Doeldatum blijft)
        var nieuw = new Dictionary<int, DateOnly?>();
        for (int pass = 0; pass < mijlpalen.Count + 2; pass++)
        {
            bool changed = false;
            foreach (var m in mijlpalen)
            {
                if (m.Doeldatum != null) continue; // handmatig gezet — niet aanraken
                if (nieuw.ContainsKey(m.Id)) continue;
                if (m.SjabloonMijlpaalId is not int smid || !ankerBySjabloonId.TryGetValue(smid, out var a)) continue;

                var (ankerCode, offset) = a;
                int off = offset ?? 0;

                if (string.IsNullOrWhiteSpace(ankerCode) || ankerCode.Equals(AnkerProjectCreated, StringComparison.OrdinalIgnoreCase))
                {
                    nieuw[m.Id] = anchorBase.AddDays(off);
                    changed = true;
                    continue;
                }
                var ankerM = AnkerVoor(m, ankerCode);
                if (ankerM == null) continue;
                var basis = nieuw.TryGetValue(ankerM.Id, out var nb) ? nb : Effectief(ankerM);
                if (basis is DateOnly b)
                {
                    nieuw[m.Id] = b.AddDays(off);
                    changed = true;
                }
            }
            if (!changed) break;
        }

        int gewijzigd = 0;
        foreach (var m in mijlpalen)
        {
            if (m.Doeldatum != null) continue;
            if (!nieuw.TryGetValue(m.Id, out var d)) continue;
            if (m.DoeldatumBerekend != d)
            {
                m.DoeldatumBerekend = d;
                gewijzigd++;
            }
        }
        return gewijzigd;
    }

    private static void HerberekenFases(Projecttraject traject, DateOnly vandaag)
    {
        var perFase = traject.Mijlpalen.Where(m => m.ProjecttrajectFaseId != null)
            .GroupBy(m => m.ProjecttrajectFaseId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var fasesGeordend = traject.Fases.OrderBy(f => f.Volgorde).ToList();
        bool actieveGezet = false;
        foreach (var f in fasesGeordend)
        {
            perFase.TryGetValue(f.Id, out var items);
            items ??= new List<Mijlpaal>();
            var verplicht = items.Where(m => m.IsVerplicht).ToList();

            bool afgerond = verplicht.Count > 0 && verplicht.All(m => m.Status == MpBereikt || m.Status == MpNvt);

            if (afgerond)
            {
                if (f.Status != (int)FaseStatus.Overgeslagen)
                    f.Status = (int)FaseStatus.Afgerond;
                var datums = verplicht.Select(m => m.WerkelijkeDatum).Where(d => d != null).Select(d => d!.Value).ToList();
                if (datums.Count > 0) f.EindWerkelijk = datums.Max();
            }
            else if (!actieveGezet && f.Status != (int)FaseStatus.Overgeslagen)
            {
                f.Status = (int)FaseStatus.Actief;
                f.StartWerkelijk ??= vandaag;
                actieveGezet = true;
            }
            else if (f.Status != (int)FaseStatus.Overgeslagen && f.Status != (int)FaseStatus.Afgerond)
            {
                f.Status = (int)FaseStatus.Gepland;
            }
        }
    }

    private static string? BouwWaarschuwingen(ICollection<Mijlpaal> mijlpalen, DateOnly vandaag)
    {
        var codes = new List<string>();
        int achterstallig = mijlpalen.Count(m =>
            m.IsVerplicht && m.Status != MpBereikt && m.Status != MpNvt
            && (m.Doeldatum ?? m.DoeldatumBerekend) is DateOnly d && d < vandaag);
        if (achterstallig > 0) codes.Add($"ACHTERSTALLIG:{achterstallig}");
        return codes.Count > 0 ? string.Join("|", codes) : null;
    }
}

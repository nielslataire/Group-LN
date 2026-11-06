using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Services.OutlookSync;

/// <summary>Een afspraak op een regieproject die een regie-uur kan worden (of al is geworden).</summary>
public sealed class UrenVoorstel
{
    public string EventId { get; set; } = "";
    public string Titel { get; set; } = "";
    public DateTime Start { get; set; }
    public DateTime Einde { get; set; }
    public int ProjectId { get; set; }
    public string ProjectNaam { get; set; } = "";
    /// <summary>De duur van de afspraak, afgerond op 15 minuten (minstens 15).</summary>
    public int Minuten { get; set; }
    public decimal Uren => Minuten / 60m;
    /// <summary>Er bestaat al een regie-uur voor deze afspraak (<see cref="UurId"/>).</summary>
    public bool Goedgekeurd { get; set; }
    public int? UurId { get; set; }
    /// <summary>Het uur staat al op een factuur: het blijft altijd ongewijzigd.</summary>
    public bool Gefactureerd { get; set; }
    /// <summary>De gebruiker heeft op dit project geen uurtarief: goedkeuren kan, maar er valt niets te factureren tot er een tarief is.</summary>
    public bool GeenTarief { get; set; }
    /// <summary>Titel van een andere afspraak waar deze mee overlapt, of null.</summary>
    public string? Overlap { get; set; }
    /// <summary>Een goedgekeurd, nog niet gefactureerd uur waarvan de afspraak nadien gewijzigd is of verdwenen. Uitleg voor de gebruiker.</summary>
    public string? Gewijzigd { get; set; }
    /// <summary>Bij een gewijzigde afspraak: de nieuwe duur (afgerond), waarop "Uur bijwerken" het uur zet. Null als de afspraak verdwenen is.</summary>
    public int? NieuweMinuten { get; set; }
}

public sealed class UrenOverzicht
{
    public List<UrenVoorstel> Voorstellen { get; set; } = new();
    public string? Fout { get; set; }
}

/// <summary>
/// Regie-uren uit Outlook-afspraken (spec #2, ticket #9): voorstellen voor gekoppelde afspraken op projecten met een regie- of gemengd coördinatiecontract, goedkeuren in de weekreview.
/// Geen voorstel voor afgewezen of geannuleerde afspraken, afspraken die de hele dag duren, "beschikbaar" (vrij) of privé. De duur wordt afgerond op 15 minuten. Goedkeuren maakt een
/// <see cref="ProjectRegieUur"/> met het event-id als dedup-sleutel (nooit twee uren voor dezelfde afspraak); de factureerflow blijft ongewijzigd, want het is een gewoon regie-uur.
/// </summary>
public class OutlookUrenService
{
    private const int ProjectContractRegie = 2, ProjectContractGemengd = 3;   // BOCore.CoordinationContractType
    private readonly cpmRunningContext _db;
    private readonly IGraphIngang _graph;

    public OutlookUrenService(cpmRunningContext db, IGraphIngang graph)
    {
        _db = db;
        _graph = graph;
    }

    public static int Afronden(DateTime start, DateTime einde)
    {
        var minuten = (einde - start).TotalMinutes;
        return (int)Math.Max(15, Math.Round(minuten / 15.0, MidpointRounding.AwayFromZero) * 15);
    }

    /// <summary>Voorstellen en goedgekeurde uren voor de week die begint op <paramref name="maandag"/>, op de projecten waar de gebruiker aan kan.</summary>
    public async Task<UrenOverzicht> OverzichtAsync(int userId, string userStr, string? agendaGebruiker, DateOnly maandag, IReadOnlyCollection<int> toegestaneProjecten, CancellationToken ct = default)
    {
        var van = maandag.ToDateTime(TimeOnly.MinValue);
        var tot = van.AddDays(7);
        var resultaat = new UrenOverzicht();
        var agenda = await _graph.GetAgendaAsync(agendaGebruiker, van, tot, ct);
        if (!agenda.Gekoppeld) { resultaat.Fout = agenda.Fout; return resultaat; }

        var ids = agenda.Afspraken.Select(a => a.Id).Where(i => i != "").ToList();
        var gekoppeld = await _db.OutlookAfspraak.AsNoTracking().Where(k => k.UserId == userId && ids.Contains(k.EventId)).ToDictionaryAsync(k => k.EventId, k => k.ProjectId, ct);
        var vanDatum = maandag; var totDatum = maandag.AddDays(7);
        var uren = await _db.ProjectRegieUur.AsNoTracking().Where(u => u.UserId == userStr && u.OutlookEventId != null && u.Date >= vanDatum && u.Date < totDatum).ToListAsync(ct);
        var projectIds = gekoppeld.Values.Concat(uren.Select(u => u.ProjectId)).Distinct().Where(toegestaneProjecten.Contains).ToList();
        var projecten = await _db.Project.AsNoTracking().Where(p => projectIds.Contains(p.ProjectId) && (p.ContractType == ProjectContractRegie || p.ContractType == ProjectContractGemengd))
            .Select(p => new { p.ProjectId, p.ProjectName }).ToDictionaryAsync(p => p.ProjectId, p => p.ProjectName ?? "", ct);
        var metTarief = (await _db.ProjectHourlyRate.AsNoTracking().Where(r => r.UserId == userStr && projectIds.Contains(r.ProjectId)).Select(r => r.ProjectId).ToListAsync(ct)).ToHashSet();
        var perEvent = agenda.Afspraken.Where(a => a.Id != "").GroupBy(a => a.Id).ToDictionary(g => g.Key, g => g.First());

        // Goedgekeurde uren: ook zichtbaar als de afspraak nadien gewijzigd of verdwenen is.
        var goedgekeurd = new HashSet<string>(StringComparer.Ordinal);
        foreach (var u in uren.Where(u => projecten.ContainsKey(u.ProjectId)).OrderBy(u => u.OutlookStart ?? u.Date.ToDateTime(TimeOnly.MinValue)))
        {
            goedgekeurd.Add(u.OutlookEventId!);
            var v = new UrenVoorstel
            {
                EventId = u.OutlookEventId!, Titel = u.Description ?? "", ProjectId = u.ProjectId, ProjectNaam = projecten[u.ProjectId],
                Start = u.OutlookStart ?? u.Date.ToDateTime(TimeOnly.MinValue), Einde = u.OutlookEinde ?? u.Date.ToDateTime(TimeOnly.MinValue).AddHours((double)u.Hours),
                Minuten = (int)Math.Round(u.Hours * 60m), Goedgekeurd = true, UurId = u.Id, Gefactureerd = u.InvoiceId != null, GeenTarief = !metTarief.Contains(u.ProjectId)
            };
            if (!v.Gefactureerd)
            {
                if (!perEvent.TryGetValue(u.OutlookEventId!, out var a)) v.Gewijzigd = "Deze afspraak staat niet meer in je agenda op deze week.";
                else if (a.Start != u.OutlookStart || a.Einde != u.OutlookEinde)
                {
                    v.Gewijzigd = "Deze afspraak is gewijzigd na het goedkeuren.";
                    v.NieuweMinuten = Afronden(a.Start, a.Einde);
                }
            }
            resultaat.Voorstellen.Add(v);
        }

        // Nieuwe voorstellen: gekoppelde afspraken op een regieproject die aan de regels voldoen en nog geen uur hebben.
        var kandidaten = agenda.Afspraken.Where(a => a.Id != "" && !goedgekeurd.Contains(a.Id) && gekoppeld.TryGetValue(a.Id, out var p) && projecten.ContainsKey(p)
            && !a.HeleDag && a.ToonAls != "free" && !a.Prive && !a.Afgewezen).OrderBy(a => a.Start).ToList();
        foreach (var a in kandidaten)
        {
            var anderen = kandidaten.Where(x => x.Id != a.Id && x.Start < a.Einde && x.Einde > a.Start).Select(x => x.Titel).ToList();
            var project = gekoppeld[a.Id];
            resultaat.Voorstellen.Add(new UrenVoorstel
            {
                EventId = a.Id, Titel = a.Titel, Start = a.Start, Einde = a.Einde, ProjectId = project, ProjectNaam = projecten[project],
                Minuten = Afronden(a.Start, a.Einde), GeenTarief = !metTarief.Contains(project), Overlap = anderen.FirstOrDefault()
            });
        }
        resultaat.Voorstellen = resultaat.Voorstellen.OrderBy(v => v.Start).ToList();
        return resultaat;
    }

    /// <summary>Maakt van de gekozen voorstellen regie-uren. De voorstellen worden opnieuw uit de agenda gelezen: tijden en projecten van de browser tellen nooit. Een afspraak die al een uur heeft, wordt overgeslagen.</summary>
    /// <returns>Het aantal gemaakte uren.</returns>
    public async Task<int> KeurGoedAsync(int userId, string userStr, string? agendaGebruiker, DateOnly maandag, IReadOnlyCollection<int> toegestaneProjecten, IReadOnlyCollection<string> eventIds, CancellationToken ct = default)
    {
        var overzicht = await OverzichtAsync(userId, userStr, agendaGebruiker, maandag, toegestaneProjecten, ct);
        var gemaakt = 0;
        foreach (var v in overzicht.Voorstellen.Where(v => !v.Goedgekeurd && eventIds.Contains(v.EventId)))
        {
            if (await _db.ProjectRegieUur.AnyAsync(u => u.UserId == userStr && u.OutlookEventId == v.EventId, ct)) continue;
            _db.ProjectRegieUur.Add(new ProjectRegieUur
            {
                ProjectId = v.ProjectId, UserId = userStr, Date = DateOnly.FromDateTime(v.Start), Hours = v.Uren,
                Description = v.Titel.Length > 500 ? v.Titel[..500] : v.Titel, CreatedAt = DateTime.UtcNow,
                OutlookEventId = v.EventId, OutlookStart = v.Start, OutlookEinde = v.Einde
            });
            gemaakt++;
        }
        if (gemaakt > 0) await _db.SaveChangesAsync(ct);
        return gemaakt;
    }

    /// <summary>Zet een goedgekeurd, nog niet gefactureerd uur op de gewijzigde afspraak. Geeft een foutmelding terug, of null bij succes.</summary>
    public async Task<string?> BijwerkenAsync(int userId, string userStr, string? agendaGebruiker, int uurId, IReadOnlyCollection<int> toegestaneProjecten, CancellationToken ct = default)
    {
        var uur = await _db.ProjectRegieUur.FirstOrDefaultAsync(u => u.Id == uurId && u.UserId == userStr && u.OutlookEventId != null, ct);
        if (uur == null || !toegestaneProjecten.Contains(uur.ProjectId)) return "Dit uur bestaat niet.";
        if (uur.InvoiceId != null) return "Dit uur staat al op een factuur en blijft ongewijzigd.";
        var week = DateOnly.FromDateTime(uur.OutlookStart ?? uur.Date.ToDateTime(TimeOnly.MinValue));
        var maandag = MijnTaken.MijnTakenAssistent.Maandag(week);
        var agenda = await _graph.GetAgendaAsync(agendaGebruiker, maandag.ToDateTime(TimeOnly.MinValue), maandag.ToDateTime(TimeOnly.MinValue).AddDays(7), ct);
        var a = agenda.Afspraken.FirstOrDefault(x => x.Id == uur.OutlookEventId);
        if (!agenda.Gekoppeld || a == null) return "De afspraak staat niet meer in je agenda. Verwijder het uur op de coördinatiepagina van het project.";
        uur.Date = DateOnly.FromDateTime(a.Start);
        uur.Hours = Afronden(a.Start, a.Einde) / 60m;
        uur.OutlookStart = a.Start; uur.OutlookEinde = a.Einde;
        if (!string.IsNullOrWhiteSpace(a.Titel)) uur.Description = a.Titel.Length > 500 ? a.Titel[..500] : a.Titel;
        await _db.SaveChangesAsync(ct);
        return null;
    }
}

using System.Globalization;
using System.Security.Claims;
using BOCore;
using CPMCore.Helpers;
using CPMCore.Models;
using CPMCore.Models.MijnTaken;
using CPMCore.Services.OutlookSync;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Services.MijnTaken;

/// <summary>Gegevens van de ingelogde gebruiker die elke Mijn taken-pagina nodig heeft.</summary>
public class MtContext
{
    public int UserId { get; set; }
    /// <summary>Waarde zoals ProjectTaak.ToegewezenAanUserId die bewaart (de CPM-gebruikers-id als tekst).</summary>
    public string UserStr { get; set; } = "";
    public string UserCode { get; set; } = "";
    public List<int> Rollen { get; set; } = new();
    public List<int> ProjectIds { get; set; } = new();
    public Dictionary<int, MtProject> Projecten { get; set; } = new();
    public UserProfileSettings Inst { get; set; } = new();
    public DateTime Nu { get; set; } = DateTime.Now;
    /// <summary>Entra-object-id (of e-mailadres) van de mailbox voor de Outlook-agenda.</summary>
    public string? AgendaGebruiker { get; set; }
}

/// <summary>Mijn taken als persoonlijke assistent (design-handoff punt 2): vastleggen, verwerken, plannen, doen en terugblikken.
/// Bouwt op ProjectTaak (extra kolommen, migratie 120), TaakTijd (timer) en TaakWeekReview. Tijden zijn lokale servertijd.</summary>
public class MijnTakenAssistent
{
    private readonly cpmRunningContext _db;
    private readonly IProjectService _projectService;
    private readonly IGraphIngang _graph;
    private static readonly CultureInfo Nl = new("nl-BE");
    private static readonly string[] Kleuren = { "#00532D", "#B8935A", "#2F4F7A", "#7A9E6E", "#8A5A7A", "#B04040", "#4A8A8A", "#6B7A3A" };
    public const string GeenKleur = "#9aa898";
    public static readonly string[] StandaardContexten = { "@bureau", "@telefoon", "@werf" };
    public static readonly string[] ReviewPunten =
    {
        "Inbox leegmaken|Alles verwerkt tot nul.",
        "Agenda vorige week overlopen|Wat moet nog opgevolgd worden?",
        "Planning van de komende 2 weken bekijken|Voorbereiding of taken nodig?",
        "“Wachten op” opvolgen|Is er iets te laat?",
        "Projecten overlopen|Heeft elk project een volgende actie?",
        "Ooit / misschien nalezen|Iets dat nu wel kan?"
    };

    private const int StatusOpen = (int)TaakStatus.Open, StatusBezig = (int)TaakStatus.Bezig, StatusWachtend = (int)TaakStatus.Wachtend,
        StatusAfgerond = (int)TaakStatus.Afgerond, StatusGeannuleerd = (int)TaakStatus.Geannuleerd;

    public MijnTakenAssistent(cpmRunningContext db, IProjectService projectService, IGraphIngang graph)
    {
        _db = db;
        _projectService = projectService;
        _graph = graph;
    }

    public static string KleurVoor(int? projectId) => projectId == null ? GeenKleur : Kleuren[projectId.Value % Kleuren.Length];

    // ── Context ─────────────────────────────────────────────────────────────────────────────────

    public async Task<MtContext?> Context(ClaimsPrincipal user)
    {
        var id = user.GetCpmUserId();
        if (id == null) return null;
        var code = user.GetCpmUserCode() ?? "";
        var c = new MtContext { UserId = id.Value, UserStr = id.Value.ToString(), UserCode = code, Nu = DateTime.Now };

        var gebruiker = await _db.Users.AsNoTracking().Where(u => u.Id == id.Value).Select(u => new { u.DashboardType, u.EntraObjectId, u.Email }).FirstOrDefaultAsync();
        var dashboardType = gebruiker?.DashboardType;
        c.AgendaGebruiker = IGraphIngang.GebruikerSleutel(gebruiker?.EntraObjectId, gebruiker?.Email);
        InterneRol? rol = dashboardType.HasValue ? (DashboardType)dashboardType.Value switch
        {
            DashboardType.Projectleider => InterneRol.Projectleider,
            DashboardType.CeoCfo => InterneRol.CeoCfo,
            DashboardType.Boekhouding => InterneRol.Boekhouder,
            DashboardType.Ontwikkelaar => InterneRol.Projectontwikkelaar,
            DashboardType.Verkoper => InterneRol.Verkoper,
            _ => (InterneRol?)null
        } : null;
        if (rol.HasValue) c.Rollen.Add((int)rol.Value);

        var response = _projectService.GetProjectsForList(0, 0, code);
        c.ProjectIds = response.Success ? response.Values.Select(p => p.Id).ToList() : new List<int>();
        c.Projecten = (await _db.Project.AsNoTracking().Where(p => c.ProjectIds.Contains(p.ProjectId))
                .Select(p => new { p.ProjectId, p.ProjectName }).OrderBy(p => p.ProjectName).ToListAsync())
            .ToDictionary(p => p.ProjectId, p => new MtProject { Id = p.ProjectId, Naam = p.ProjectName ?? "", Kleur = KleurVoor(p.ProjectId) });

        c.Inst = await _db.UserProfileSettings.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == id.Value) ?? new UserProfileSettings();
        return c;
    }

    /// <summary>Taken van deze gebruiker: aan hem toegewezen, aan zijn rol (zonder persoon) of door hem gemaakt zonder toewijzing.</summary>
    public IQueryable<ProjectTaak> Mijn(MtContext c) =>
        _db.ProjectTaak.Include(t => t.Project)
            .Where(t => t.ProjectId == null || c.ProjectIds.Contains(t.ProjectId.Value))
            .Where(t => t.ToegewezenAanUserId == c.UserStr
                     || (t.ToegewezenAanUserId == null && t.ToegewezenAanRol != null && c.Rollen.Contains(t.ToegewezenAanRol.Value))
                     || (t.ToegewezenAanUserId == null && t.ToegewezenAanRol == null && t.CreatedByUserId == c.UserStr));

    /// <summary>Een taak die van deze gebruiker is (voor wijzigende acties).</summary>
    public Task<ProjectTaak?> EigenTaak(MtContext c, int id) => Mijn(c).FirstOrDefaultAsync(t => t.Id == id);

    private static bool Open(ProjectTaak t) => t.Status != StatusAfgerond && t.Status != StatusGeannuleerd;

    private MtTaak Map(ProjectTaak t, MtContext c) => new()
    {
        Id = t.Id, Titel = t.Titel, ProjectId = t.ProjectId, Project = t.Project?.ProjectName ?? "", Kleur = KleurVoor(t.ProjectId),
        Context = t.Context, Schatting = t.SchattingMinuten, GeplandStart = t.GeplandStart, GeplandMinuten = t.GeplandMinuten,
        Vervaldatum = t.Vervaldatum, Prioriteit = t.Prioriteit, Status = t.Status, WachtOp = t.WachtOp, WachtOpDatum = t.WachtOpDatum,
        InboxBron = t.InboxBron, InboxMeta = t.InboxMeta, Aangemaakt = t.CreatedDate.ToLocalTime()
    };

    // ── Gemeenschappelijk ───────────────────────────────────────────────────────────────────────

    public static DateOnly Maandag(DateOnly d) => d.AddDays(-(((int)d.DayOfWeek + 6) % 7));
    public static int Weeknummer(DateOnly d) => ISOWeek.GetWeekOfYear(d.ToDateTime(TimeOnly.MinValue));
    public static string DagNaam(DateOnly d) => Nl.DateTimeFormat.GetAbbreviatedDayName(d.DayOfWeek).TrimEnd('.');
    public static string Vol(DateOnly d) => Nl.TextInfo.ToTitleCase(d.ToString("dddd d MMMM", Nl)).Replace(" ", " ");

    /// <summary>Wat er in de Inbox nog te verwerken is: notities plus ontvangen, niet-afgehandelde mails uit Outlook. Enkel een teller, geen melding. Ook bedoeld voor een latere zijbalk.</summary>
    public async Task<int> InboxAantal(MtContext c) =>
        await Mijn(c).CountAsync(t => t.IsInbox && t.Status != StatusAfgerond && t.Status != StatusGeannuleerd)
        + await _db.OutlookMail.CountAsync(m => m.UserId == c.UserId && m.Richting == "in" && m.Status != OutlookMailStatus.Afgehandeld);

    public async Task<MtShell> Shell(MtContext c, string tab, string ondertitel)
    {
        var inbox = await InboxAantal(c);
        var shell = new MtShell { Tab = tab, Ondertitel = ondertitel, InboxAantal = inbox, Projecten = c.Projecten.Values.ToList() };

        var laatste = await _db.TaakTijd.AsNoTracking().Where(x => x.UserId == c.UserId).OrderByDescending(x => x.Start).FirstOrDefaultAsync();
        if (laatste != null)
        {
            var lopend = laatste.Einde == null;
            var taakOpen = laatste.TaakId == null || await _db.ProjectTaak.AnyAsync(t => t.Id == laatste.TaakId && t.Status != StatusAfgerond && t.Status != StatusGeannuleerd);
            if (lopend || (taakOpen && laatste.Einde!.Value > c.Nu.AddHours(-6)))
                shell.Timer = Blok(laatste, c);
        }
        return shell;
    }

    private MtBlok Blok(TaakTijd x, MtContext c) => new()
    {
        TaakId = x.TaakId, Titel = x.Titel, Project = x.ProjectId != null && c.Projecten.TryGetValue(x.ProjectId.Value, out var p) ? p.Naam : "",
        Kleur = KleurVoor(x.ProjectId), Start = x.Start, Einde = x.Einde ?? c.Nu, Loopt = x.Einde == null
    };

    /// <summary>Werkuren per dag (minuten) uit het profiel: werkuren min middagpauze.</summary>
    public static int DagCapaciteit(UserProfileSettings s)
    {
        static int Min(string? v) => TimeOnly.TryParse(v, out var t) ? t.Hour * 60 + t.Minute : 0;
        var dag = Min(s.WerkTot) - Min(s.WerkVan);
        var pauze = Min(s.PauzeTot) - Min(s.PauzeVan);
        return Math.Max(0, dag - Math.Max(0, pauze));
    }

    private static bool IsWerkdag(UserProfileSettings s, DateOnly d) => (s.WerkDagen & (1 << (((int)d.DayOfWeek + 6) % 7))) != 0;

    // ── Vandaag ─────────────────────────────────────────────────────────────────────────────────

    public async Task<MtVandaagVm> Vandaag(MtContext c)
    {
        var vandaag = DateOnly.FromDateTime(c.Nu);
        var dagStart = vandaag.ToDateTime(TimeOnly.MinValue);
        var vm = new MtVandaagVm { Datum = vandaag };
        vm.Shell = await Shell(c, "vandaag", $"{Vol(vandaag)} · week {Weeknummer(vandaag)}");

        var taken = await Mijn(c).Where(t => t.Status != StatusAfgerond && t.Status != StatusGeannuleerd && !t.IsInbox).ToListAsync();
        vm.Acties = taken.Where(t => !t.Ooit && t.Status != StatusWachtend && t.GeplandStart == null)
            .OrderBy(t => t.Vervaldatum ?? DateOnly.MaxValue).ThenByDescending(t => t.Prioriteit).Take(60).Select(t => Map(t, c)).ToList();
        vm.Contexten = StandaardContexten.Concat(taken.Select(t => t.Context).Where(x => !string.IsNullOrEmpty(x)).Cast<string>())
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        vm.Wachten = taken.Where(t => t.Status == StatusWachtend && !string.IsNullOrWhiteSpace(t.WachtOp))
            .OrderBy(t => t.WachtOpDatum ?? DateTime.MaxValue).Select(t => Map(t, c)).ToList();

        vm.Gepland = (await Mijn(c).Where(t => t.GeplandStart != null && t.GeplandStart >= dagStart && t.GeplandStart < dagStart.AddDays(1)).ToListAsync())
            .Select(t => new MtBlok
            {
                TaakId = t.Id, Titel = t.Titel, Project = t.Project?.ProjectName ?? "", Kleur = KleurVoor(t.ProjectId), Start = t.GeplandStart!.Value,
                Einde = t.GeplandStart!.Value.AddMinutes(t.GeplandMinuten ?? t.SchattingMinuten ?? 30)
            }).OrderBy(b => b.Start).ToList();
        vm.GeplandMinuten = (int)vm.Gepland.Sum(b => (b.Einde - b.Start).TotalMinutes);

        var tijden = await _db.TaakTijd.AsNoTracking().Where(x => x.UserId == c.UserId && x.Start >= dagStart && x.Start < dagStart.AddDays(1)).OrderBy(x => x.Start).ToListAsync();
        vm.Gewerkt = tijden.Select(x => Blok(x, c)).ToList();
        vm.WerkMinuten = (int)vm.Gewerkt.Sum(b => (b.Einde - b.Start).TotalMinutes);
        vm.PerProject = vm.Gewerkt.GroupBy(b => (b.Project, b.Kleur)).Select(g => (g.Key.Project == "" ? "Zonder project" : g.Key.Project, g.Key.Kleur, (int)g.Sum(b => (b.Einde - b.Start).TotalMinutes)))
            .OrderByDescending(x => x.Item3).ToList();

        vm.Deadlines = await Deadlines(c, 3);

        // Outlook-agenda: één aanroep voor vandaag tot het einde van de werkweek.
        var vrijdagEind = Maandag(vandaag).AddDays(5).ToDateTime(TimeOnly.MinValue);
        var agenda = await _graph.GetAgendaAsync(c.AgendaGebruiker, dagStart, vrijdagEind > dagStart ? vrijdagEind : dagStart.AddDays(1));
        vm.AgendaFout = agenda.Fout;
        var vandaagAfspraken = agenda.Afspraken.Where(a => a.Start < dagStart.AddDays(1) && a.Einde > dagStart).ToList();
        vm.HeleDag = vandaagAfspraken.Where(a => a.HeleDag).ToList();
        vm.Afspraken = vandaagAfspraken.Where(a => !a.HeleDag).OrderBy(a => a.Start).ToList();

        // Deze week nog: taken met een vervaldatum later deze week, en mijlpalen van jouw rol.
        var vrijdag = Maandag(vandaag).AddDays(4);
        var rest = taken.Where(t => t.Vervaldatum != null && t.Vervaldatum > vandaag && t.Vervaldatum <= vrijdag)
            .Select(t => (t.Vervaldatum!.Value, t.Titel, "Taak")).ToList();
        var mijl = await MijlpaalQuery(c).Where(m => (m.Doeldatum ?? m.DoeldatumBerekend) > vandaag && (m.Doeldatum ?? m.DoeldatumBerekend) <= vrijdag).Take(10).ToListAsync();
        rest.AddRange(mijl.Select(m => ((m.Doeldatum ?? m.DoeldatumBerekend)!.Value, m.Naam, "Mijlpaal")));
        rest.AddRange(agenda.Afspraken.Where(a => DateOnly.FromDateTime(a.Start) > vandaag).Select(a => (DateOnly.FromDateTime(a.Start), a.Titel, "Afspraak")));
        vm.DezeWeek = rest.OrderBy(r => r.Item1).Take(8).Select(r => (DagNaam(r.Item1), r.Item2, r.Item3)).ToList();
        return vm;
    }

    // ── Deadlines van jouw rol ──────────────────────────────────────────────────────────────────

    private IQueryable<Mijlpaal> MijlpaalQuery(MtContext c)
    {
        var rollen = c.Rollen;
        return _db.Mijlpaal.Include(m => m.Projecttraject).ThenInclude(t => t.Project)
            .Where(m => c.ProjectIds.Contains(m.Projecttraject.ProjectId))
            .Where(m => m.Status != (int)MijlpaalStatus.Bereikt && m.Status != (int)MijlpaalStatus.NietVanToepassing)
            .Where(m => (m.Doeldatum ?? m.DoeldatumBerekend) != null)
            .Where(m => m.VerantwoordelijkeUserId == c.UserStr || m.VerantwoordelijkeUserId == c.UserCode
                     || (m.VerantwoordelijkeRol != null && rollen.Contains(m.VerantwoordelijkeRol.Value)));
    }

    public async Task<List<MtDeadline>> Deadlines(MtContext c, int aantal)
    {
        var vandaag = DateOnly.FromDateTime(c.Nu);
        var grens = vandaag.AddYears(5);
        var lijst = await MijlpaalQuery(c).Where(m => (m.Doeldatum ?? m.DoeldatumBerekend) <= grens)
            .OrderBy(m => m.Doeldatum ?? m.DoeldatumBerekend).Take(aantal).ToListAsync();
        var ids = lijst.Select(m => m.Id).ToList();
        var metTaak = await Mijn(c).Where(t => t.MijlpaalId != null && ids.Contains(t.MijlpaalId.Value)).Select(t => t.MijlpaalId!.Value).Distinct().ToListAsync();
        return lijst.Select(m =>
        {
            var d = (m.Doeldatum ?? m.DoeldatumBerekend)!.Value;
            var pid = m.Projecttraject.ProjectId;
            return new MtDeadline
            {
                MijlpaalId = m.Id, ProjectId = pid, Project = m.Projecttraject.Project?.ProjectName ?? "", Kleur = KleurVoor(pid), Naam = m.Naam, Datum = d,
                HeeftTaak = metTaak.Contains(m.Id), Dagen = d.DayNumber - vandaag.DayNumber
            };
        }).ToList();
    }

    public async Task<(bool Ok, string? Fout)> MaakTaakVanMijlpaal(MtContext c, int mijlpaalId)
    {
        var m = await _db.Mijlpaal.Include(x => x.Projecttraject).FirstOrDefaultAsync(x => x.Id == mijlpaalId);
        if (m == null || !c.ProjectIds.Contains(m.Projecttraject.ProjectId)) return (false, "Mijlpaal niet gevonden.");
        if (await Mijn(c).AnyAsync(t => t.MijlpaalId == mijlpaalId && t.Status != StatusAfgerond && t.Status != StatusGeannuleerd)) return (true, null);
        var d = m.Doeldatum ?? m.DoeldatumBerekend;
        _db.ProjectTaak.Add(new ProjectTaak
        {
            ProjectId = m.Projecttraject.ProjectId, MijlpaalId = m.Id, Titel = Kort($"Voorbereiden: {m.Naam}"), Status = StatusOpen,
            Prioriteit = (int)TaakPrioriteit.Normaal, ToegewezenAanUserId = c.UserStr, Vervaldatum = d, Herkomst = (int)TaakHerkomst.Mijlpaal,
            Context = "@bureau", CreatedByUserId = c.UserStr, CreatedDate = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    private static string Kort(string s) => s.Length <= 200 ? s : s[..197] + "…";

    // ── Deadlines per project (design-handoff 1b) ───────────────────────────────────────────────

    public async Task<MtDeadlinesVm> DeadlinesOverzicht(MtContext c)
    {
        var vandaag = DateOnly.FromDateTime(c.Nu);
        var grens = vandaag.AddYears(5);
        var lijst = c.ProjectIds.Count == 0 ? new List<Mijlpaal>() : await _db.Mijlpaal.AsNoTracking()
            .Include(m => m.Unit).Include(m => m.Projecttraject).ThenInclude(t => t.Project)
            .Where(m => c.ProjectIds.Contains(m.Projecttraject.ProjectId)).ToListAsync();
        var metTaak = (await Mijn(c).Where(t => t.MijlpaalId != null && t.Status != StatusAfgerond && t.Status != StatusGeannuleerd).Select(t => t.MijlpaalId!.Value).Distinct().ToListAsync()).ToHashSet();

        var vm = new MtDeadlinesVm();
        foreach (var g in lijst.GroupBy(m => m.Projecttraject.ProjectId).OrderBy(g => g.First().Projecttraject.Project?.ProjectName))
        {
            var p = new MtDeadlineProject { Id = g.Key, Naam = g.First().Projecttraject.Project?.ProjectName ?? "Project " + g.Key, Kleur = KleurVoor(g.Key) };
            foreach (var m in g)
            {
                var d = m.Doeldatum ?? m.DoeldatumBerekend;
                var klaar = m.Status == (int)MijlpaalStatus.Bereikt || m.Status == (int)MijlpaalStatus.NietVanToepassing;
                var dagen = d == null ? 0 : d.Value.DayNumber - vandaag.DayNumber;
                p.Rijen.Add(new MtMijlpaalRij
                {
                    Id = m.Id, Naam = m.Naam, Eenheid = m.Unit?.Name, Datum = d, Status = m.Status, Dagen = dagen, Klaar = klaar,
                    Laat = !klaar && d != null && dagen < 0, Onwaarschijnlijk = d != null && d.Value > grens,
                    Rol = m.VerantwoordelijkeRol is > 0 ? ((InterneRol)m.VerantwoordelijkeRol.Value).GetDisplayName() : "—",
                    Mijn = m.VerantwoordelijkeUserId == c.UserStr || m.VerantwoordelijkeUserId == c.UserCode || (m.VerantwoordelijkeRol != null && c.Rollen.Contains(m.VerantwoordelijkeRol.Value)),
                    HeeftTaak = metTaak.Contains(m.Id)
                });
            }
            // Open en achterstallig eerst (oudste datum eerst), bereikte onderaan.
            p.Rijen = p.Rijen.OrderBy(r => r.Klaar).ThenBy(r => r.Datum ?? DateOnly.MaxValue).ThenBy(r => r.Naam).ToList();
            vm.Projecten.Add(p);
        }
        // Projecten met achterstallige mijlpalen bovenaan.
        vm.Projecten = vm.Projecten.OrderByDescending(p => p.Laat > 0).ThenBy(p => p.Naam).ToList();
        var alle = vm.Projecten.SelectMany(p => p.Rijen).ToList();
        vm.Totaal = alle.Count;
        vm.Bereikt = alle.Count(r => r.Klaar);
        vm.Achterstallig = alle.Count(r => r.Laat);
        vm.Komende30 = alle.Count(r => !r.Klaar && r.Datum != null && r.Dagen >= 0 && r.Dagen <= 30);
        vm.NakijkenProject = vm.Projecten.OrderByDescending(p => p.Onwaarschijnlijk).FirstOrDefault(p => p.Onwaarschijnlijk > 0);
        vm.NakijkenAantal = vm.NakijkenProject?.Onwaarschijnlijk ?? 0;
        return vm;
    }

    // ── Week ────────────────────────────────────────────────────────────────────────────────────

    public async Task<MtWeekVm> Week(MtContext c, DateOnly maandag)
    {
        var vandaag = DateOnly.FromDateTime(c.Nu);
        var vm = new MtWeekVm { Maandag = maandag, Weeknummer = Weeknummer(maandag) };
        var eind = maandag.AddDays(4);
        vm.Shell = await Shell(c, "week", $"Week {vm.Weeknummer} · {maandag.Day} – {eind.Day} {Nl.DateTimeFormat.GetMonthName(eind.Month)}");

        var van = maandag.ToDateTime(TimeOnly.MinValue);
        var gepland = await Mijn(c).Where(t => t.GeplandStart != null && t.GeplandStart >= van && t.GeplandStart < van.AddDays(7)).ToListAsync();
        var tijden = await _db.TaakTijd.AsNoTracking().Where(x => x.UserId == c.UserId && x.Start >= van && x.Start < van.AddDays(7)).OrderBy(x => x.Start).ToListAsync();
        var mijl = await MijlpaalQuery(c).Where(m => (m.Doeldatum ?? m.DoeldatumBerekend) >= maandag && (m.Doeldatum ?? m.DoeldatumBerekend) <= eind.AddDays(2)).ToListAsync();
        var vervallen = await Mijn(c).Where(t => t.Vervaldatum != null && t.Vervaldatum >= maandag && t.Vervaldatum <= eind.AddDays(2) && t.Status != StatusAfgerond && t.Status != StatusGeannuleerd && t.MijlpaalId == null && t.Prioriteit >= (int)TaakPrioriteit.Hoog).ToListAsync();

        var agenda = await _graph.GetAgendaAsync(c.AgendaGebruiker, van, van.AddDays(7));
        vm.AgendaFout = agenda.Fout;

        for (var i = 0; i < 5; i++)
        {
            var d = maandag.AddDays(i);
            var dag = new MtWeekDag { Datum = d };
            var dagStartW = d.ToDateTime(TimeOnly.MinValue);
            var dagAfspraken = agenda.Afspraken.Where(a => a.Start < dagStartW.AddDays(1) && a.Einde > dagStartW).ToList();
            dag.HeleDag = dagAfspraken.Where(a => a.HeleDag).ToList();
            dag.Afspraken = dagAfspraken.Where(a => !a.HeleDag).OrderBy(a => a.Start).ToList();
            dag.Gepland = gepland.Where(t => DateOnly.FromDateTime(t.GeplandStart!.Value) == d).OrderBy(t => t.GeplandStart)
                .Select(t => new MtBlok { TaakId = t.Id, Titel = t.Titel, Project = t.Project?.ProjectName ?? "", Kleur = KleurVoor(t.ProjectId), Start = t.GeplandStart!.Value, Einde = t.GeplandStart!.Value.AddMinutes(t.GeplandMinuten ?? t.SchattingMinuten ?? 30) }).ToList();
            dag.Gewerkt = tijden.Where(x => DateOnly.FromDateTime(x.Start) == d).Select(x => Blok(x, c)).ToList();
            dag.GeplandMinuten = (int)dag.Gepland.Sum(b => (b.Einde - b.Start).TotalMinutes);
            dag.Deadlines = mijl.Where(m => (m.Doeldatum ?? m.DoeldatumBerekend) == d).Select(m => m.Naam)
                .Concat(vervallen.Where(t => t.Vervaldatum == d).Select(t => t.Titel)).ToList();
            vm.Dagen.Add(dag);
        }

        var open = await Mijn(c).Where(t => t.Status != StatusAfgerond && t.Status != StatusGeannuleerd && !t.IsInbox && !t.Ooit && t.Status != StatusWachtend && t.GeplandStart == null)
            .OrderBy(t => t.Vervaldatum ?? DateOnly.MaxValue).ThenByDescending(t => t.Prioriteit).Take(40).ToListAsync();
        vm.Ongepland = open.Select(t => Map(t, c)).ToList();
        var dagen = Enumerable.Range(0, 5).Count(i => IsWerkdag(c.Inst, maandag.AddDays(i)));
        vm.CapaciteitMinuten = Math.Max(1, DagCapaciteit(c.Inst) * Math.Max(1, dagen));
        return vm;
    }

    // ── Inbox ───────────────────────────────────────────────────────────────────────────────────

    public async Task<MtInboxVm> Inbox(MtContext c)
    {
        var items = await Mijn(c).Where(t => t.IsInbox && t.Status != StatusAfgerond && t.Status != StatusGeannuleerd).OrderByDescending(t => t.CreatedDate).ToListAsync();
        var vm = new MtInboxVm { Items = items.Select(t => Map(t, c)).ToList() };
        vm.Mails = await MailsVan(c, "in", 100);
        vm.Verzonden = await MailsVan(c, "uit", 20);
        var totaal = items.Count + vm.Mails.Count;
        vm.Shell = await Shell(c, "inbox", totaal == 0 ? "Inbox leeg" : totaal == 1 ? "1 item te verwerken" : totaal + " items te verwerken");
        vm.Contexten = StandaardContexten.ToList();
        return vm;
    }

    private async Task<List<MtMail>> MailsVan(MtContext c, string richting, int max)
    {
        var query = _db.OutlookMail.AsNoTracking().Where(m => m.UserId == c.UserId && m.Richting == richting);
        if (richting == "in") query = query.Where(m => m.Status != OutlookMailStatus.Afgehandeld);
        var rijen = await query.OrderByDescending(m => m.OntvangenOp).Take(max).ToListAsync();

        var klanten = rijen.Where(m => m.KlantId != null).Select(m => m.KlantId!.Value).Distinct().ToList();
        var leveranciers = rijen.Where(m => m.LeverancierId != null).Select(m => m.LeverancierId!.Value).Distinct().ToList();
        var klantNamen = klanten.Count == 0 ? new Dictionary<int, string>() : await _db.ClientAccount.AsNoTracking().Where(k => klanten.Contains(k.Id)).ToDictionaryAsync(k => k.Id, k => k.Name ?? "");
        var leverancierNamen = leveranciers.Count == 0 ? new Dictionary<int, string>() : await _db.CompanyInfo.AsNoTracking().Where(b => leveranciers.Contains(b.CompanyId)).ToDictionaryAsync(b => b.CompanyId, b => b.BedrijfsNaam ?? "");

        return rijen.Select(m => new MtMail
        {
            Id = m.Id, Richting = m.Richting, WebLink = m.WebLink, ProjectId = m.ProjectId,
            Afzender = string.IsNullOrWhiteSpace(m.AfzenderNaam) ? m.Afzender ?? "" : m.AfzenderNaam,
            Onderwerp = string.IsNullOrWhiteSpace(m.Onderwerp) ? "(zonder onderwerp)" : m.Onderwerp,
            Voorvertoning = m.Voorvertoning ?? "", Ontvangen = DateTime.SpecifyKind(m.OntvangenOp, DateTimeKind.Utc).ToLocalTime(),
            // Enkel de projectnaam tonen als het project voor jou zichtbaar is.
            Project = m.ProjectId != null && c.Projecten.TryGetValue(m.ProjectId.Value, out var p) ? p.Naam : null,
            Klant = m.KlantId != null && klantNamen.TryGetValue(m.KlantId.Value, out var k) ? k : null,
            Leverancier = m.LeverancierId != null && leverancierNamen.TryGetValue(m.LeverancierId.Value, out var l) ? l : null,
            TeKoppelen = m.Status == OutlookMailStatus.TeKoppelen, VerwijderdInOutlook = m.VerwijderdInOutlook
        }).ToList();
    }

    /// <summary>Zet een eigen mail op afgehandeld: ze verdwijnt uit de Inbox maar blijft bij het project, de klant of de leverancier. Geeft false als de mail niet van jou is.</summary>
    public async Task<bool> MailAfgehandeld(MtContext c, int id)
    {
        var mail = await _db.OutlookMail.FirstOrDefaultAsync(m => m.Id == id && m.UserId == c.UserId);
        if (mail == null) return false;
        mail.Status = OutlookMailStatus.Afgehandeld;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> Leg(MtContext c, string titel)
    {
        titel = (titel ?? "").Trim();
        if (titel.Length == 0) return 0;
        var t = new ProjectTaak
        {
            Titel = Kort(titel), Status = StatusOpen, Prioriteit = (int)TaakPrioriteit.Normaal, ToegewezenAanUserId = c.UserStr, Herkomst = (int)TaakHerkomst.Handmatig,
            IsInbox = true, InboxBron = "Notitie", InboxMeta = "vastgelegd", CreatedByUserId = c.UserStr, CreatedDate = DateTime.UtcNow
        };
        _db.ProjectTaak.Add(t);
        await _db.SaveChangesAsync();
        return t.Id;
    }

    /// <param name="actie">nu, plan, next, del, ooit of weg.</param>
    public async Task<string?> Verwerk(MtContext c, int id, string actie, int? projectId, string? context, int? schatting, DateTime? wanneer, string? wachtOp, DateTime? wachtOpDatum)
    {
        var t = await EigenTaak(c, id);
        if (t == null) return "Taak niet gevonden.";
        if (projectId != null && !c.Projecten.ContainsKey(projectId.Value)) return "Dat project is niet beschikbaar.";
        t.ProjectId = projectId;
        t.Context = string.IsNullOrWhiteSpace(context) ? null : context.Trim()[..Math.Min(50, context.Trim().Length)];
        t.SchattingMinuten = schatting is > 0 and <= 960 ? schatting : null;
        t.IsInbox = false;
        t.ModifiedDate = DateTime.UtcNow; t.ModifiedByUserId = c.UserStr;
        switch (actie)
        {
            case "nu": t.Status = StatusAfgerond; t.AfgewerktOp = DateTime.UtcNow; break;
            case "plan":
                if (wanneer == null) return "Kies wanneer je het inplant.";
                t.GeplandStart = Snap(wanneer.Value); t.GeplandMinuten = t.SchattingMinuten ?? 30; break;
            case "next": break;
            case "del":
                if (string.IsNullOrWhiteSpace(wachtOp)) return "Zeg op wie of wat je wacht.";
                t.Status = StatusWachtend; t.WachtOp = wachtOp.Trim()[..Math.Min(200, wachtOp.Trim().Length)]; t.WachtOpDatum = wachtOpDatum?.Date; break;
            case "ooit": t.Ooit = true; break;
            case "weg": t.Status = StatusGeannuleerd; break;
            default: return "Onbekende keuze.";
        }
        await _db.SaveChangesAsync();
        return null;
    }

    // ── Plannen ─────────────────────────────────────────────────────────────────────────────────

    private static DateTime Snap(DateTime d) => new(d.Year, d.Month, d.Day, d.Hour, d.Minute / 15 * 15, 0);

    public async Task<string?> Plan(MtContext c, int id, DateTime start, int? minuten)
    {
        var t = await EigenTaak(c, id);
        if (t == null) return "Taak niet gevonden.";
        t.GeplandStart = Snap(start);
        t.GeplandMinuten = minuten is >= 15 and <= 960 ? minuten : (t.GeplandMinuten ?? t.SchattingMinuten ?? 30);
        t.ModifiedDate = DateTime.UtcNow; t.ModifiedByUserId = c.UserStr;
        await _db.SaveChangesAsync();
        return null;
    }

    /// <summary>Wijzigt een gepland blok zoals in Outlook: titel, begin en duur (exact, niet op 15 minuten afgerond). Een lege titel laat de titel ongemoeid.</summary>
    public async Task<string?> PlanWijzig(MtContext c, int id, string? titel, DateTime start, int minuten)
    {
        var t = await EigenTaak(c, id);
        if (t == null) return "Taak niet gevonden.";
        if (minuten < 5 || minuten > 960) return "Een blok duurt tussen 5 minuten en 16 uur.";
        if (!string.IsNullOrWhiteSpace(titel)) t.Titel = Kort(titel.Trim());
        t.GeplandStart = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, 0);
        t.GeplandMinuten = minuten;
        t.IsInbox = false;
        t.ModifiedDate = DateTime.UtcNow; t.ModifiedByUserId = c.UserStr;
        await _db.SaveChangesAsync();
        return null;
    }

    /// <summary>Maakt een nieuwe taak die meteen als blok in de kalender staat (slepen over een leeg stuk van de dag of week).</summary>
    public async Task<(int Id, string? Fout)> MaakBlok(MtContext c, string? titel, DateTime start, int minuten)
    {
        titel = (titel ?? "").Trim();
        if (titel.Length == 0) return (0, "Geef de taak een naam.");
        if (minuten < 5 || minuten > 960) return (0, "Een blok duurt tussen 5 minuten en 16 uur.");
        var t = new ProjectTaak
        {
            Titel = Kort(titel), Status = StatusOpen, Prioriteit = (int)TaakPrioriteit.Normaal, ToegewezenAanUserId = c.UserStr, Herkomst = (int)TaakHerkomst.Handmatig,
            GeplandStart = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, 0), GeplandMinuten = minuten, SchattingMinuten = minuten,
            CreatedByUserId = c.UserStr, CreatedDate = DateTime.UtcNow
        };
        _db.ProjectTaak.Add(t);
        await _db.SaveChangesAsync();
        return (t.Id, null);
    }

    /// <summary>Wijzigt een afspraak in de Outlook-agenda van de gebruiker (titel en tijd).</summary>
    public Task<string?> AgendaWijzig(MtContext c, string eventId, string? titel, DateTime start, int minuten) =>
        minuten < 5 || minuten > 960 ? Task.FromResult<string?>("Een afspraak duurt tussen 5 minuten en 16 uur.")
            : _graph.BijwerkenAfspraakAsync(c.AgendaGebruiker, eventId, titel, start, start.AddMinutes(minuten));

    public async Task<string?> Ontplan(MtContext c, int id)
    {
        var t = await EigenTaak(c, id);
        if (t == null) return "Taak niet gevonden.";
        t.GeplandStart = null; t.GeplandMinuten = null;
        await _db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> Afvink(MtContext c, int id, bool klaar)
    {
        var t = await EigenTaak(c, id);
        if (t == null) return "Taak niet gevonden.";
        t.Status = klaar ? StatusAfgerond : StatusOpen;
        t.AfgewerktOp = klaar ? DateTime.UtcNow : null;
        t.ModifiedDate = DateTime.UtcNow; t.ModifiedByUserId = c.UserStr;
        if (klaar) await StopLopende(c, c.Nu, t.Id);
        await _db.SaveChangesAsync();
        return null;
    }

    // ── Timer ───────────────────────────────────────────────────────────────────────────────────

    private async Task StopLopende(MtContext c, DateTime om, int? alleenTaakId = null)
    {
        var lopend = await _db.TaakTijd.Where(x => x.UserId == c.UserId && x.Einde == null).ToListAsync();
        foreach (var x in lopend.Where(x => alleenTaakId == null || x.TaakId == alleenTaakId))
            x.Einde = om < x.Start ? x.Start : om;
    }

    /// <summary>Start de timer op een bestaande taak (taakId) of op een nieuwe (titel). Een lopende timer stopt en wordt gelogd.
    /// <paramref name="terugMinuten"/>: "ik werk al 20 min", de timer begint met terugwerkende kracht.</summary>
    public async Task<(string? Fout, string? VorigeTekst)> Start(MtContext c, int? taakId, string? titel, int? projectId, string? context, int terugMinuten)
    {
        ProjectTaak? taak = null;
        if (taakId != null)
        {
            taak = await EigenTaak(c, taakId.Value);
            if (taak == null) return ("Taak niet gevonden.", null);
        }
        else
        {
            titel = (titel ?? "").Trim();
            if (titel.Length == 0) return ("Geef de taak een naam.", null);
            if (projectId != null && !c.Projecten.ContainsKey(projectId.Value)) return ("Dat project is niet beschikbaar.", null);
            taak = new ProjectTaak
            {
                Titel = Kort(titel), ProjectId = projectId, Context = string.IsNullOrWhiteSpace(context) ? null : context.Trim(), Status = StatusBezig,
                Prioriteit = (int)TaakPrioriteit.Normaal, ToegewezenAanUserId = c.UserStr, Herkomst = (int)TaakHerkomst.Handmatig,
                CreatedByUserId = c.UserStr, CreatedDate = DateTime.UtcNow
            };
            _db.ProjectTaak.Add(taak);
        }

        var nu = DateTime.Now;
        var vorige = await _db.TaakTijd.Where(x => x.UserId == c.UserId && x.Einde == null).OrderByDescending(x => x.Start).FirstOrDefaultAsync();
        string? vorigeTekst = null;
        if (vorige != null)
        {
            var minuten = (int)Math.Max(0, (nu - vorige.Start).TotalMinutes);
            vorigeTekst = $"{vorige.Titel} · {minuten} min gelogd";
        }
        await StopLopende(c, nu);

        if (taak.Status == StatusOpen) taak.Status = StatusBezig;
        taak.IsInbox = false;
        await _db.SaveChangesAsync();
        _db.TaakTijd.Add(new TaakTijd
        {
            UserId = c.UserId, TaakId = taak.Id, ProjectId = taak.ProjectId, Titel = taak.Titel, Context = taak.Context,
            Start = nu.AddMinutes(-Math.Clamp(terugMinuten, 0, 600))
        });
        await _db.SaveChangesAsync();
        return (null, vorigeTekst);
    }

    public async Task Pauze(MtContext c)
    {
        await StopLopende(c, DateTime.Now);
        await _db.SaveChangesAsync();
    }

    public async Task<string?> Klaar(MtContext c)
    {
        var laatste = await _db.TaakTijd.Where(x => x.UserId == c.UserId).OrderByDescending(x => x.Start).FirstOrDefaultAsync();
        if (laatste == null) return "Er loopt geen timer.";
        if (laatste.Einde == null) laatste.Einde = DateTime.Now;
        if (laatste.TaakId != null)
        {
            var t = await EigenTaak(c, laatste.TaakId.Value);
            if (t != null) { t.Status = StatusAfgerond; t.AfgewerktOp = DateTime.UtcNow; t.ModifiedDate = DateTime.UtcNow; t.ModifiedByUserId = c.UserStr; }
        }
        await _db.SaveChangesAsync();
        return null;
    }

    /// <summary>Hervat de laatst gestopte taak.</summary>
    public async Task<string?> Hervat(MtContext c)
    {
        var laatste = await _db.TaakTijd.AsNoTracking().Where(x => x.UserId == c.UserId).OrderByDescending(x => x.Start).FirstOrDefaultAsync();
        if (laatste == null) return "Er is niets om te hervatten.";
        if (laatste.Einde == null) return null;
        if (laatste.TaakId != null) return (await Start(c, laatste.TaakId, null, null, null, 0)).Fout;
        return (await Start(c, null, laatste.Titel, laatste.ProjectId, laatste.Context, 0)).Fout;
    }

    /// <summary>Zoekt open taken voor de snelstart: titel bevat alle woorden.</summary>
    public async Task<List<MtTaak>> Zoek(MtContext c, string woorden)
    {
        var delen = (woorden ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (delen.Length == 0) return new();
        var q = Mijn(c).Where(t => t.Status != StatusAfgerond && t.Status != StatusGeannuleerd);
        foreach (var d in delen) { var w = d; q = q.Where(t => t.Titel.Contains(w)); }
        return (await q.OrderByDescending(t => t.CreatedDate).Take(4).ToListAsync()).Select(t => Map(t, c)).ToList();
    }

    public async Task<List<MtTaak>> Recent(MtContext c)
    {
        var ids = await _db.TaakTijd.AsNoTracking().Where(x => x.UserId == c.UserId && x.TaakId != null).OrderByDescending(x => x.Start).Select(x => x.TaakId!.Value).Take(40).ToListAsync();
        var uniek = ids.Distinct().Take(5).ToList();
        var taken = await Mijn(c).Where(t => uniek.Contains(t.Id) && t.Status != StatusAfgerond && t.Status != StatusGeannuleerd).ToListAsync();
        return uniek.Select(i => taken.FirstOrDefault(t => t.Id == i)).Where(t => t != null).Select(t => Map(t!, c)).ToList();
    }

    // ── Overzicht ───────────────────────────────────────────────────────────────────────────────

    public async Task<MtOverzichtVm> Overzicht(MtContext c, DateOnly maandag)
    {
        var eind = maandag.AddDays(4);
        var vm = new MtOverzichtVm { Maandag = maandag, Weeknummer = Weeknummer(maandag) };
        vm.Shell = await Shell(c, "overzicht", $"Week {vm.Weeknummer} · {maandag.Day} – {eind.Day} {Nl.DateTimeFormat.GetMonthName(eind.Month)}");

        var van = maandag.ToDateTime(TimeOnly.MinValue);
        var tot = van.AddDays(7);
        var tijden = await _db.TaakTijd.AsNoTracking().Where(x => x.UserId == c.UserId && x.Start >= van && x.Start < tot).ToListAsync();
        double Min(TaakTijd x) => ((x.Einde ?? c.Nu) - x.Start).TotalMinutes;
        vm.GewerktMinuten = (int)tijden.Sum(Min);
        vm.FocusMinuten = (int)tijden.Where(x => Min(x) >= 25).Sum(Min);

        var gepland = await Mijn(c).Where(t => t.GeplandStart != null && t.GeplandStart >= van && t.GeplandStart < tot).ToListAsync();
        int Geplan(ProjectTaak t) => t.GeplandMinuten ?? t.SchattingMinuten ?? 30;
        vm.GeplandMinuten = gepland.Sum(Geplan);
        var vanUtc = van.ToUniversalTime(); var totUtc = tot.ToUniversalTime();
        vm.Afgerond = await Mijn(c).CountAsync(t => t.Status == StatusAfgerond && t.AfgewerktOp >= vanUtc && t.AfgewerktOp < totUtc);

        vm.PerProject = tijden.GroupBy(x => x.ProjectId).Select(g => (g.Key != null && c.Projecten.TryGetValue(g.Key.Value, out var p) ? p.Naam : "Zonder project", KleurVoor(g.Key), (int)g.Sum(Min)))
            .OrderByDescending(x => x.Item3).Take(6).ToList();
        vm.PerSoort = tijden.GroupBy(x => Soort(x.Context)).Select(g => (g.Key, (int)g.Sum(Min))).OrderByDescending(x => x.Item2).ToList();
        for (var i = 0; i < 5; i++)
        {
            var d = maandag.AddDays(i);
            vm.PerDag.Add((DagNaam(d), gepland.Where(t => DateOnly.FromDateTime(t.GeplandStart!.Value) == d).Sum(Geplan), (int)tijden.Where(x => DateOnly.FromDateTime(x.Start) == d).Sum(Min)));
        }

        var review = await _db.TaakWeekReview.AsNoTracking().FirstOrDefaultAsync(r => r.UserId == c.UserId && r.WeekStart == van);
        vm.Afgevinkt = review?.Afgevinkt ?? 0;

        var open = await Mijn(c).Where(t => t.Status != StatusAfgerond && t.Status != StatusGeannuleerd && !t.IsInbox && !t.Ooit && t.Status != StatusWachtend)
            .OrderByDescending(t => t.Prioriteit).ThenBy(t => t.Vervaldatum ?? DateOnly.MaxValue).Take(3).ToListAsync();
        vm.Focus = open.Select(t => Map(t, c)).ToList();

        var wachten = await Mijn(c).Where(t => t.Status == StatusWachtend && t.WachtOp != null).ToListAsync();
        vm.WachtenOpen = wachten.Count;
        vm.WachtenTeLaat = wachten.Count(t => t.WachtOpDatum != null && t.WachtOpDatum < c.Nu.Date);
        return vm;
    }

    public static string Soort(string? context) => context?.ToLowerInvariant() switch
    {
        "@werf" => "Werf en bezoeken",
        "@telefoon" => "Overleg en telefoon",
        "@bureau" => "Bureau en administratie",
        null or "" => "Zonder context",
        _ => "Andere contexten"
    };

    public async Task ZetReview(MtContext c, DateOnly week, int punt, bool aan)
    {
        if (punt < 0 || punt >= ReviewPunten.Length) return;
        var maandag = Maandag(week).ToDateTime(TimeOnly.MinValue);
        var r = await _db.TaakWeekReview.FirstOrDefaultAsync(x => x.UserId == c.UserId && x.WeekStart == maandag);
        if (r == null) { r = new TaakWeekReview { UserId = c.UserId, WeekStart = maandag }; _db.TaakWeekReview.Add(r); }
        r.Afgevinkt = aan ? r.Afgevinkt | (1 << punt) : r.Afgevinkt & ~(1 << punt);
        await _db.SaveChangesAsync();
    }
}

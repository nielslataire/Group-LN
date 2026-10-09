using System.Text.Json;
using BOCore;
using CPMCore.Configuration;
using CPMCore.Documents.GlV2;
using CPMCore.Helpers;
using CPMCore.Models.Issues;
using CPMCore.Models.Verslagen;
using CPMCore.Services.Security;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using SmartBreadcrumbs.Nodes;

namespace CPMCore.Controllers;

/// <summary>Verslagen bij Punten (design-handoff 40i/40l): werfverslagen en opleveringen bundelen de punten van één bezoek.
/// Lijst, hernemen van het vorige verslag (open punten overnemen, ter plaatse nakijken), nieuwe punten tijdens het bezoek,
/// afronden (punten goedkeuren/afsluiten) en versturen als PDF naar de aanwezigen.</summary>
[Authorize]
[Route("Projects/{projectId:int}/Verslagen")]
public class ProjectVerslagenController : BaseController
{
    private readonly cpmRunningContext _db;
    private readonly IConstructionIssueService _issues;
    private readonly IEmailSender _email;
    private readonly IWebHostEnvironment _env;
    private readonly IOptions<GlV2PdfCompanyOptions> _company;
    private readonly ILogger<ProjectVerslagenController> _logger;

    // Statussen van een punt dat "nog open" is bij een volgend bezoek (nieuwe reeks + legacy)
    private static readonly int[] OpenStatuses = { 10, 11, 6, 12, 0, 1, 2, 3, 7 };

    public ProjectVerslagenController(cpmRunningContext db, IConstructionIssueService issues, IEmailSender email, IWebHostEnvironment env,
        IOptions<GlV2PdfCompanyOptions> company, ILogger<ProjectVerslagenController> logger)
    {
        _db = db; _issues = issues; _email = email; _env = env; _company = company; _logger = logger;
    }

    private string? UserCode => User.FindFirst(CpmClaims.UserId)?.Value;
    private bool CanWrite => HttpContext.RequestServices.GetRequiredService<IPermissionService>().HasWrite(PermissionCodes.ProjectsIssues);

    private async Task<bool> IsAdmin()
    {
        var uid = User.GetCpmUserId();
        return uid.HasValue && await HttpContext.RequestServices.GetRequiredService<ISecurityService>().UserIsAdmin(uid.Value.ToString());
    }

    private async Task<string> ProjectName(int projectId) =>
        await _db.Project.AsNoTracking().Where(p => p.ProjectId == projectId).Select(p => p.ProjectName).FirstOrDefaultAsync() ?? $"Project {projectId}";

    private void Crumbs(int projectId, string projectName, string? leaf = null, string? action = null, object? route = null)
    {
        var home = new MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var projecten = new MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = home };
        var project = new MvcBreadcrumbNode("Detail", "Projecten", projectName) { Parent = projecten };
        var verslagen = new MvcBreadcrumbNode("Index", "ProjectVerslagen", "Verslagen") { Parent = project, RouteValues = new { projectId } };
        ViewData["BreadcrumbNode"] = leaf == null ? verslagen : new MvcBreadcrumbNode(action ?? "Index", "ProjectVerslagen", leaf) { Parent = verslagen, RouteValues = route ?? new { projectId } };
    }

    private static List<VerslagAanwezigeV2> ParseAanwezigen(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            return (JsonSerializer.Deserialize<List<VerslagAanwezigeV2>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new())
                .Where(a => !string.IsNullOrWhiteSpace(a.Name)).ToList();
        }
        catch { return new(); }
    }

    private static string AanwezigenTekst(string? json) => string.Join(", ", ParseAanwezigen(json).Select(a => a.Name));

    // ── Lijst (40i) ─────────────────────────────────────────────────────────────────────────────────

    [HttpGet("")]
    public async Task<IActionResult> Index(int projectId, int? id)
    {
        var name = await ProjectName(projectId);
        Crumbs(projectId, name);
        var verslagen = await _db.Werfverslag.AsNoTracking().Include(v => v.Punten).Where(v => v.ProjectId == projectId)
            .OrderByDescending(v => v.Datum).ThenByDescending(v => v.Id).ToListAsync();
        var vm = new VerslagIndexV2Vm
        {
            ProjectId = projectId, ProjectName = name, CanWrite = CanWrite, IsAdmin = await IsAdmin(),
            Rows = verslagen.Select(v => new VerslagRowV2
            {
                Id = v.Id, Naam = v.Naam, Datum = v.Datum, AanwezigenTekst = AanwezigenTekst(v.AanwezigenJson), Type = v.VerslagType,
                TypeLabel = v.VerslagType == 1 ? "Voorlopige oplevering" : "Werfverslag", Punten = v.Punten.Count, Status = v.Status,
            }).ToList(),
            VolgendNummer = (verslagen.Where(v => v.VerslagType == 0).Select(v => (int?)v.Nummer).Max() ?? 0) + 1,
            LopendConceptId = verslagen.FirstOrDefault(v => v.Status == 0 && v.VerslagType == 0)?.Id,
        };
        var sel = verslagen.FirstOrDefault(v => v.Id == id) ?? verslagen.FirstOrDefault();
        if (sel != null)
        {
            var issueIds = sel.Punten.Select(p => p.IssueId).ToList();
            var statuses = await _db.ConstructionIssue.AsNoTracking().Where(i => issueIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, i => i.Status);
            int St(WerfverslagPunt p) => statuses.TryGetValue(p.IssueId, out var s) ? PuntenWeergave.Canon(s) : 0;
            var nieuw = sel.Punten.Where(p => p.IsNieuw).ToList();
            var open = sel.Punten.Where(p => !p.IsNieuw).ToList();
            vm.Selected = new VerslagSamenvattingV2
            {
                Id = sel.Id, Naam = sel.Naam, Datum = sel.Datum, Status = sel.Status, AanwezigenTekst = AanwezigenTekst(sel.AanwezigenJson),
                Weer = sel.Weer, Opmerkingen = sel.Opmerkingen, VolgendBezoek = sel.VolgendBezoek,
                NieuweTotaal = nieuw.Count, NieuweTeKeuren = nieuw.Count(p => St(p) is (int)ConstructionIssueStatus.Concept or (int)ConstructionIssueStatus.PendingApproval),
                OpenTotaal = open.Count, OpenGemeld = open.Count(p => St(p) == (int)ConstructionIssueStatus.Reported),
            };
        }
        return View("IndexV2", vm);
    }

    // ── Starten / hernemen (40l) ────────────────────────────────────────────────────────────────────

    [HttpPost("Start")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int projectId, int type, string? naam)
    {
        type = type == 1 ? 1 : 0;
        // Eén lopend concept per type: liever dat openen dan een dubbel verslag maken.
        var lopend = await _db.Werfverslag.Where(v => v.ProjectId == projectId && v.VerslagType == type && v.Status == 0).OrderByDescending(v => v.Id).FirstOrDefaultAsync();
        if (lopend != null) return RedirectToAction(nameof(Edit), new { projectId, id = lopend.Id });

        var vorig = await _db.Werfverslag.Where(v => v.ProjectId == projectId && v.VerslagType == type).OrderByDescending(v => v.Datum).ThenByDescending(v => v.Id).FirstOrDefaultAsync();
        var nummer = (await _db.Werfverslag.Where(v => v.ProjectId == projectId && v.VerslagType == type).Select(v => (int?)v.Nummer).MaxAsync() ?? 0) + 1;
        var verslag = new Werfverslag
        {
            ProjectId = projectId, VerslagType = type, Nummer = nummer,
            Naam = type == 0 ? $"Werfbezoek nr. {nummer}" : (string.IsNullOrWhiteSpace(naam) ? "Oplevering" : naam.Trim()),
            Datum = DateOnly.FromDateTime(DateTime.Today), Uur = TimeOnly.FromDateTime(DateTime.Now),
            AanwezigenJson = vorig?.AanwezigenJson, Opmerkingen = vorig?.Opmerkingen, HernomenVanId = vorig?.Id,
            CreatedAt = DateTime.UtcNow, CreatedBy = UserCode,
        };
        _db.Werfverslag.Add(verslag);
        await _db.SaveChangesAsync();

        // Alle punten die nog open staan komen mee; afgesloten punten niet.
        var openIds = await _db.ConstructionIssue.AsNoTracking().Where(i => i.ProjectId == projectId && OpenStatuses.Contains(i.Status)).Select(i => i.Id).ToListAsync();
        foreach (var iid in openIds) _db.WerfverslagPunt.Add(new WerfverslagPunt { VerslagId = verslag.Id, IssueId = iid, IsNieuw = false });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Edit), new { projectId, id = verslag.Id });
    }

    [HttpPost("{id:int}/Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int projectId, int id)
    {
        var v = await _db.Werfverslag.Include(x => x.Punten).FirstOrDefaultAsync(x => x.Id == id && x.ProjectId == projectId);
        if (v == null) return NotFound();
        // Een verstuurd verslag verwijderen mag enkel een beheerder; de punten zelf blijven dan bestaan (ze zijn al goedgekeurd/doorgestuurd).
        if (v.Status != 0 && !await IsAdmin()) { AddMessage("error", "Enkel een beheerder kan een verstuurd verslag verwijderen.", "Fout"); return RedirectToAction(nameof(Index), new { projectId }); }
        if (v.Status == 0 && !CanWrite) { AddMessage("error", "Je hebt geen schrijfrecht om dit verslag te verwijderen.", "Fout"); return RedirectToAction(nameof(Index), new { projectId }); }
        // Bij een concept verdwijnen nieuwe punten die enkel voor dit verslag aangemaakt werden en nog concept zijn mee.
        if (v.Status == 0)
            foreach (var p in v.Punten.Where(p => p.IsNieuw).ToList())
            {
                var issue = await _db.ConstructionIssue.AsNoTracking().FirstOrDefaultAsync(i => i.Id == p.IssueId);
                _db.WerfverslagPunt.Remove(p);
                if (issue is { Status: (int)ConstructionIssueStatus.Concept }) { await _db.SaveChangesAsync(); await _issues.Delete(projectId, issue.Id, UserCode); }
            }
        _db.Werfverslag.Remove(v);
        await _db.SaveChangesAsync();
        AddMessage("success", "Verslag verwijderd.", "Geslaagd");
        return RedirectToAction(nameof(Index), new { projectId });
    }

    // ── Verslag bewerken (40l) ──────────────────────────────────────────────────────────────────────

    private async Task<VerslagPuntV2> ToPunt(WerfverslagPunt p, ConstructionIssue i, Dictionary<int, string> contractors, int projectId) => await Task.FromResult(new VerslagPuntV2
    {
        IssueId = i.Id, Nr = "P-" + (i.PuntNr ?? i.Id).ToString("000"), Title = PuntenWeergave.Titel(i.Title),
        Unit = i.Unit?.Name ?? "Algemeen",
        Contractor = PuntenWeergave.Aannemer(i.ResponsiblePartyId.HasValue && contractors.TryGetValue(i.ResponsiblePartyId.Value, out var n) ? n : i.ResponsibleOtherName),
        Status = i.Status, StatusLabel = PuntenWeergave.Label(i.Status), StatusTone = PuntenWeergave.Tone(i.Status),
        Sinds = i.CreatedDate.ToLocalTime().ToString("dd/MM"), TerPlaatse = p.TerPlaatse, Opmerking = p.Opmerking, IsNieuw = p.IsNieuw,
        DetailUrl = Url.Action("Details", "ProjectsIssues", new { projectId, id = i.Id }) ?? "#",
    });

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Edit(int projectId, int id)
    {
        var v = await _db.Werfverslag.AsNoTracking().Include(x => x.Punten).FirstOrDefaultAsync(x => x.Id == id && x.ProjectId == projectId);
        if (v == null) return NotFound();
        var name = await ProjectName(projectId);
        Crumbs(projectId, name, v.Naam, nameof(Edit), new { projectId, id });

        var issueIds = v.Punten.Select(p => p.IssueId).ToList();
        var issues = await _db.ConstructionIssue.AsNoTracking().Include(i => i.Unit).Where(i => issueIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id);
        var contractors = await _db.Contract.AsNoTracking().Where(c => c.ProjectId == projectId).Select(c => new { c.CompanyId, N = c.Company.BedrijfsNaam }).Distinct().ToDictionaryAsync(c => c.CompanyId, c => c.N);
        var open = new List<VerslagPuntV2>(); var nieuw = new List<VerslagPuntV2>();
        foreach (var p in v.Punten.OrderBy(p => issues.TryGetValue(p.IssueId, out var i) ? i.PuntNr ?? i.Id : 0))
        {
            if (!issues.TryGetValue(p.IssueId, out var issue)) continue;
            var row = await ToPunt(p, issue, contractors, projectId);
            (p.IsNieuw ? nieuw : open).Add(row);
        }
        var vorig = v.HernomenVanId.HasValue ? await _db.Werfverslag.AsNoTracking().FirstOrDefaultAsync(x => x.Id == v.HernomenVanId) : null;

        // Suggesties voor "aanwezig": de werfleiders van de aannemers van dit project + de gebruiker zelf
        var sugg = new List<VerslagSuggestieV2>();
        var me = User.GetCpmDisplayName();
        if (!string.IsNullOrWhiteSpace(me)) sugg.Add(new VerslagSuggestieV2 { Name = me!, Email = User.GetCpmEmail(), Org = "intern" });
        var contacts = await _db.Contract.AsNoTracking().Where(c => c.ProjectId == projectId && c.SiteManagerContactId != null)
            .Select(c => new { Org = c.Company.BedrijfsNaam, V = c.SiteManagerContact!.ContactVoornaam, N = c.SiteManagerContact.ContactNaam, E = c.SiteManagerContact.Email }).ToListAsync();
        foreach (var c in contacts)
        {
            var nm = $"{c.V} {c.N}".Trim();
            if (nm != "") sugg.Add(new VerslagSuggestieV2 { Name = nm, Email = c.E, Org = PuntenWeergave.Aannemer(c.Org) });
        }

        var vm = new VerslagEditV2Vm
        {
            ProjectId = projectId, ProjectName = name, CanWrite = CanWrite, IsAdmin = await IsAdmin(), Id = v.Id, Naam = v.Naam, Type = v.VerslagType, Status = v.Status,
            Datum = v.Datum, Uur = v.Uur?.ToString("HH:mm"), Weer = v.Weer, Opmerkingen = v.Opmerkingen,
            VolgendDatum = v.VolgendBezoek.HasValue ? DateOnly.FromDateTime(v.VolgendBezoek.Value) : null, VolgendUur = v.VolgendBezoek?.ToString("HH:mm"),
            Aanwezigen = ParseAanwezigen(v.AanwezigenJson), Suggesties = sugg.DistinctBy(s => s.Name.ToLowerInvariant()).ToList(),
            OpenPunten = open, NieuwePunten = nieuw, VorigId = vorig?.Id, VorigNaam = vorig?.Naam, VorigDatum = vorig?.Datum,
            Panel = await BuildPanelData(projectId, name),
        };
        return View("EditV2", vm);
    }

    /// <summary>Wat het gedeelde punt-zijpaneel nodig heeft (eenheden, aannemers, categorieën …) — zonder de volledige lijst.</summary>
    private async Task<PuntenIndexV2Vm> BuildPanelData(int projectId, string name)
    {
        var contractors = await _db.Contract.AsNoTracking().Where(c => c.ProjectId == projectId).Select(c => new { c.CompanyId, N = c.Company.BedrijfsNaam }).Distinct().OrderBy(c => c.N).ToListAsync();
        var zones = await _db.ConstructionIssue.AsNoTracking().Where(i => i.ProjectId == projectId && i.RoomOrZone != null).OrderByDescending(i => i.CreatedDate).Select(i => i.RoomOrZone!).Take(60).ToListAsync();
        return new PuntenIndexV2Vm
        {
            ProjectId = projectId, ProjectName = name, CanWrite = CanWrite,
            Units = await _db.Units.AsNoTracking().Where(u => u.ProjectId == projectId).OrderBy(u => u.Name).Select(u => new KeyValuePair<int, string>(u.Id, u.Name)).ToListAsync(),
            ProjectContractors = contractors.Select(c => new KeyValuePair<int, string>(c.CompanyId, PuntenWeergave.Aannemer(c.N))).ToList(),
            Categories = (await _issues.GetCategories()).Select(c => new KeyValuePair<int, string>(c.Id, c.Name)).ToList(),
            Types = Enum.GetValues<ConstructionIssueType>().Select(t => new KeyValuePair<int, string>((int)t, TypeLabel((int)t))).ToList(),
            Phases = Enum.GetValues<ConstructionIssuePhase>().Select(t => new KeyValuePair<int, string>((int)t, PhaseLabel((int)t))).ToList(),
            RecentZones = zones.Select(PuntenWeergave.Zone).Distinct().Take(12).ToList(),
        };
    }

    private static string EnumLabel<T>(int value) where T : Enum
    {
        var e = (T)Enum.ToObject(typeof(T), value);
        var member = typeof(T).GetMember(e.ToString()).FirstOrDefault();
        return member?.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), false).OfType<System.ComponentModel.DataAnnotations.DisplayAttribute>().FirstOrDefault()?.Name ?? e.ToString();
    }
    private static string TypeLabel(int v) => EnumLabel<ConstructionIssueType>(v);
    private static string PhaseLabel(int v) => EnumLabel<ConstructionIssuePhase>(v);

    /// <summary>Zoekveld "Aanwezig": enkel interne medewerkers en de aannemers die een contract hebben voor deze werf, met hun contactpersonen. Antwoord voor de gedeelde keuzelijst (gl-v2-combo): id = e-mailadres (of "n:naam" zonder e-mail) zodat het e-mailadres
    /// automatisch overgenomen wordt.</summary>
    [HttpPost("Aanwezigen")]
    public async Task<IActionResult> Aanwezigen(int projectId, string? term, int take = 20)
    {
        var t = (term ?? "").Trim().ToLowerInvariant();
        var items = new List<(string Name, string? Email, string Org)>();

        // interne medewerkers (geen aannemers-/portaalgebruikers)
        var portalUserIds = _db.UserCompanyAccess.Select(a => a.UserId);
        var users = await _db.Users.AsNoTracking().Where(u => u.Email != null && u.Email != "" && !portalUserIds.Contains(u.Id)).Select(u => new { u.Voornaam, u.Familienaam, u.Email }).ToListAsync();
        foreach (var u in users) items.Add(((u.Voornaam + " " + u.Familienaam).Trim(), u.Email, "intern"));

        // bedrijven van de werf + hun contactpersonen
        var companies = await _db.Contract.AsNoTracking().Where(c => c.ProjectId == projectId)
            .Select(c => new { c.CompanyId, c.Company.BedrijfsNaam, c.Company.Email, SmV = c.SiteManagerContact != null ? c.SiteManagerContact.ContactVoornaam : null, SmN = c.SiteManagerContact != null ? c.SiteManagerContact.ContactNaam : null, SmE = c.SiteManagerContact != null ? c.SiteManagerContact.Email : null })
            .ToListAsync();
        var ids = companies.Select(c => c.CompanyId).Distinct().ToList();
        var contacts = await _db.CompanyContacts.AsNoTracking().Where(c => ids.Contains(c.CompanyId)).Select(c => new { c.CompanyId, c.ContactVoornaam, c.ContactNaam, c.Email, c.Functie }).ToListAsync();
        var orgName = companies.GroupBy(c => c.CompanyId).ToDictionary(g => g.Key, g => PuntenWeergave.Aannemer(g.First().BedrijfsNaam));
        foreach (var c in contacts)
        {
            var nm = $"{c.ContactVoornaam} {c.ContactNaam}".Trim();
            if (nm != "") items.Add((nm, c.Email, orgName.GetValueOrDefault(c.CompanyId, "") + (string.IsNullOrWhiteSpace(c.Functie) ? "" : " · " + c.Functie)));
        }
        foreach (var c in companies.GroupBy(c => c.CompanyId).Select(g => g.First()))
        {
            var sm = $"{c.SmV} {c.SmN}".Trim();
            if (sm != "") items.Add((sm, c.SmE, orgName[c.CompanyId] + " · werfleider"));
            items.Add((orgName[c.CompanyId], c.Email, "bedrijf"));
        }

        var result = items
            .Where(i => !string.IsNullOrWhiteSpace(i.Name))
            .Where(i => t == "" || (i.Name + " " + i.Org + " " + i.Email).ToLowerInvariant().Contains(t))
            .GroupBy(i => (i.Name.ToLowerInvariant(), (i.Email ?? "").ToLowerInvariant())).Select(g => g.First())
            .Take(Math.Clamp(take, 1, 50))
            .Select(i => new { id = string.IsNullOrWhiteSpace(i.Email) ? "n:" + i.Name : i.Email!, text = i.Name, sub = i.Org + (string.IsNullOrWhiteSpace(i.Email) ? " · geen e-mailadres" : " · " + i.Email) })
            .ToList();
        return Json(result);
    }

    // ── JSON-acties vanuit de bewerkpagina ──────────────────────────────────────────────────────────

    private async Task<Werfverslag?> Concept(int projectId, int id) =>
        await _db.Werfverslag.FirstOrDefaultAsync(v => v.Id == id && v.ProjectId == projectId && v.Status == 0);

    [HttpPost("{id:int}/Save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int projectId, int id, DateOnly? datum, string? uur, string? weer, string? opmerkingen, DateOnly? volgendDatum, string? volgendUur, string? aanwezigenJson)
    {
        var v = await Concept(projectId, id);
        if (v == null) return Json(new { ok = false, error = "Verslag niet gevonden of al verstuurd." });
        if (datum.HasValue) v.Datum = datum.Value;
        v.Uur = TimeOnly.TryParse(uur, out var u) ? u : null;
        v.Weer = string.IsNullOrWhiteSpace(weer) ? null : weer.Trim();
        v.Opmerkingen = string.IsNullOrWhiteSpace(opmerkingen) ? null : opmerkingen.Trim();
        v.VolgendBezoek = volgendDatum.HasValue ? volgendDatum.Value.ToDateTime(TimeOnly.TryParse(volgendUur, out var vu) ? vu : new TimeOnly(9, 0)) : null;
        if (aanwezigenJson != null) v.AanwezigenJson = JsonSerializer.Serialize(ParseAanwezigen(aanwezigenJson));
        await _db.SaveChangesAsync();
        return Json(new { ok = true });
    }

    [HttpPost("{id:int}/Point")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Point(int projectId, int id, int issueId, int? terPlaatse, string? opmerking)
    {
        var v = await Concept(projectId, id);
        if (v == null) return Json(new { ok = false });
        var p = await _db.WerfverslagPunt.FirstOrDefaultAsync(x => x.VerslagId == id && x.IssueId == issueId);
        if (p == null) return Json(new { ok = false });
        p.TerPlaatse = terPlaatse is >= 0 and <= 2 ? terPlaatse : null;
        p.Opmerking = string.IsNullOrWhiteSpace(opmerking) ? null : opmerking.Trim();
        await _db.SaveChangesAsync();
        var gecontroleerd = await _db.WerfverslagPunt.CountAsync(x => x.VerslagId == id && !x.IsNieuw && x.TerPlaatse != null);
        return Json(new { ok = true, gecontroleerd });
    }

    /// <summary>"Typ een punt en druk Enter — eenheid en aannemer kies je erna": het punt wordt een concept en hoort bij dit verslag.</summary>
    [HttpPost("{id:int}/AddPoint")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPoint(int projectId, int id, string? title, int? contractorId)
    {
        var v = await Concept(projectId, id);
        if (v == null) return Json(new { ok = false, error = "Verslag niet gevonden of al verstuurd." });
        if (string.IsNullOrWhiteSpace(title)) return Json(new { ok = false, error = "Geef een titel in." });
        var cat = (await _issues.GetCategories()).FirstOrDefault();
        if (cat == null) return Json(new { ok = false, error = "Er is nog geen categorie aangemaakt (Instellingen)." });
        var dto = new ConstructionIssueUpsertBO
        {
            Title = PuntenWeergave.Titel(title), LocationText = "Algemeen", CategoryId = cat.Id, Status = (int)ConstructionIssueStatus.Concept,
            Priority = (int)ConstructionIssuePriority.Normal, ResponsiblePartyType = (int)ConstructionIssueResponsiblePartyType.Contractor,
            ResponsiblePartyId = contractorId is > 0 ? contractorId : null,
        };
        var issue = await _issues.Create(projectId, dto, UserCode);
        _db.WerfverslagPunt.Add(new WerfverslagPunt { VerslagId = id, IssueId = issue.Id, IsNieuw = true });
        await _db.SaveChangesAsync();
        var contractorName = "";
        if (contractorId is > 0)
            contractorName = PuntenWeergave.Aannemer(await _db.CompanyInfo.AsNoTracking().Where(c => c.CompanyId == contractorId).Select(c => c.BedrijfsNaam).FirstOrDefaultAsync());
        return Json(new { ok = true, issueId = issue.Id, nr = "P-" + (issue.PuntNr ?? issue.Id).ToString("000"), title = PuntenWeergave.Titel(title), contractor = contractorName });
    }

    // ── Afronden: punten goedkeuren/afsluiten + verslag versturen ───────────────────────────────────

    private async Task<WerfverslagPdfModel> BuildPdfModel(Werfverslag v, int projectId)
    {
        var projectName = await ProjectName(projectId);
        var punten = await _db.WerfverslagPunt.AsNoTracking().Where(p => p.VerslagId == v.Id).ToListAsync();
        var ids = punten.Select(p => p.IssueId).ToList();
        var issues = await _db.ConstructionIssue.AsNoTracking().Include(i => i.Unit).Where(i => ids.Contains(i.Id)).ToDictionaryAsync(i => i.Id);
        var contractors = await _db.Contract.AsNoTracking().Where(c => c.ProjectId == projectId).Select(c => new { c.CompanyId, N = c.Company.BedrijfsNaam }).Distinct().ToDictionaryAsync(c => c.CompanyId, c => c.N);
        string Ter(int? t) => t switch { 0 => "Blijft open", 1 => "Opgelost", 2 => "Afgesloten", _ => "Niet nagekeken" };
        WerfverslagPdfPunt Map(WerfverslagPunt p)
        {
            var i = issues[p.IssueId];
            var aannemer = PuntenWeergave.Aannemer(i.ResponsiblePartyId.HasValue && contractors.TryGetValue(i.ResponsiblePartyId.Value, out var n) ? n : i.ResponsibleOtherName);
            return new WerfverslagPdfPunt("P-" + (i.PuntNr ?? i.Id).ToString("000"), PuntenWeergave.Titel(i.Title), i.Unit?.Name ?? "Algemeen", aannemer == "" ? "—" : aannemer,
                PuntenWeergave.Label(i.Status), Ter(p.TerPlaatse), p.Opmerking ?? "");
        }
        var ordered = punten.Where(p => issues.ContainsKey(p.IssueId)).OrderBy(p => issues[p.IssueId].PuntNr ?? issues[p.IssueId].Id).ToList();
        return new WerfverslagPdfModel
        {
            Naam = v.Naam, TypeLabel = v.VerslagType == 1 ? "Voorlopige oplevering" : "Werfverslag", ProjectName = projectName, Datum = v.Datum,
            Uur = v.Uur?.ToString("HH:mm"), Aanwezigen = ParseAanwezigen(v.AanwezigenJson).Select(a => a.Name).ToList(), Weer = v.Weer, Opmerkingen = v.Opmerkingen,
            VolgendBezoek = v.VolgendBezoek, Nieuwe = ordered.Where(p => p.IsNieuw).Select(Map).ToList(), Open = ordered.Where(p => !p.IsNieuw).Select(Map).ToList(),
        };
    }

    private byte[] RenderPdf(WerfverslagPdfModel model)
    {
        var o = _company.Value;
        var logoPath = Path.Combine(_env.WebRootPath, "Img", "groupln-logo.png");
        var company = new GlV2PdfCompanyInfo
        {
            Name = o.Name, Tagline = o.Tagline, Street = o.Street, PostalCity = o.PostalCity, Phone = o.Phone, Email = o.Email, Website = o.Website,
            LegalForm = o.LegalForm, VatNumber = o.VatNumber, Iban = o.Iban, LogoBytes = System.IO.File.Exists(logoPath) ? System.IO.File.ReadAllBytes(logoPath) : null,
        };
        var fonts = GlV2PdfFonts.EnsureRegistered(_env, _logger);
        return new WerfverslagDocumentV2(model, company, fonts).GeneratePdf();
    }

    [HttpGet("{id:int}/Pdf")]
    public async Task<IActionResult> Pdf(int projectId, int id)
    {
        var v = await _db.Werfverslag.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.ProjectId == projectId);
        if (v == null) return NotFound();
        var bytes = RenderPdf(await BuildPdfModel(v, projectId));
        Response.Headers["Content-Disposition"] = $"inline; filename=\"{v.Naam.Replace(' ', '_')}.pdf\"";
        return File(bytes, "application/pdf");
    }

    [HttpPost("{id:int}/Finish")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Finish(int projectId, int id)
    {
        var v = await Concept(projectId, id);
        if (v == null) { AddMessage("error", "Verslag niet gevonden of al verstuurd.", "Fout"); return RedirectToAction(nameof(Index), new { projectId }); }
        var punten = await _db.WerfverslagPunt.Where(p => p.VerslagId == id).ToListAsync();
        var issues = await _db.ConstructionIssue.AsNoTracking().Where(i => punten.Select(p => p.IssueId).Contains(i.Id)).ToDictionaryAsync(i => i.Id);

        // 1. wat ter plaatse vastgesteld werd: opgelost → gemeld uitgevoerd (de werfleider heeft het gezien maar sluit nog niet af), afgesloten → afgesloten
        foreach (var p in punten.Where(p => !p.IsNieuw))
        {
            if (!issues.TryGetValue(p.IssueId, out var issue)) continue;
            if (p.TerPlaatse == 2 && !PuntenWeergave.IsClosed(issue.Status)) await _issues.ChangeStatus(projectId, issue.Id, (int)ConstructionIssueStatus.Closed, $"Afgesloten bij {v.Naam}", UserCode);
            else if (p.TerPlaatse == 1 && PuntenWeergave.Canon(issue.Status) != (int)ConstructionIssueStatus.Reported && !PuntenWeergave.IsClosed(issue.Status))
                await _issues.ChangeStatus(projectId, issue.Id, (int)ConstructionIssueStatus.Reported, $"Opgelost vastgesteld bij {v.Naam}", UserCode);
            if (!string.IsNullOrWhiteSpace(p.Opmerking)) await _issues.AddMessage(projectId, issue.Id, $"{v.Naam}: {p.Opmerking}", isInternal: true, UserCode);
        }
        // 2. nieuwe punten blijven concept: ze worden niet automatisch goedgekeurd, zodat je er nog foto's of details aan kan toevoegen.

        // 3. PDF + mail naar de aanwezigen met e-mailadres
        var model = await BuildPdfModel(v, projectId);
        var pdf = RenderPdf(model);
        var ontvangers = ParseAanwezigen(v.AanwezigenJson).Where(a => !string.IsNullOrWhiteSpace(a.Email)).ToList();
        var sent = 0;
        foreach (var a in ontvangers)
        {
            try
            {
                var html = $"<p>Beste {System.Net.WebUtility.HtmlEncode(a.Name)},</p><p>In bijlage het verslag <b>{System.Net.WebUtility.HtmlEncode(v.Naam)}</b> van {v.Datum:dd/MM/yyyy} voor {System.Net.WebUtility.HtmlEncode(model.ProjectName)}.</p>" +
                           $"<p>Nieuwe punten gaan na goedkeuring naar de betrokken aannemers.</p>";
                await _email.SendEmailAsync(a.Email!, $"{v.Naam} — {model.ProjectName}", html, new[] { new EmailAttachment($"{v.Naam.Replace(' ', '_')}.pdf", pdf, "application/pdf") });
                sent++;
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Verslag {VerslagId} niet verstuurd naar {Email}", v.Id, a.Email); }
        }
        v.Status = 1; v.VerzondenOp = DateTime.UtcNow; v.VerzondenDoor = UserCode;
        await _db.SaveChangesAsync();
        AddMessage("success", $"{v.Naam} afgerond." + (sent > 0 ? $" Het verslag is verstuurd naar {sent} aanwezige(n)." : " Geen aanwezige heeft een e-mailadres, dus er is niets verstuurd."), "Geslaagd");
        return RedirectToAction(nameof(Index), new { projectId, id });
    }
}

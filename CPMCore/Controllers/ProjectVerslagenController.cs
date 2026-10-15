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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
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
    private readonly FacadeCore.Signing.IAssetStorageClient _storage;
    private readonly CPMCore.Services.Signing.ChangeOrderPdfBuilder _pdfBuilder;

    // Statussen van een punt dat "nog open" is bij een volgend bezoek (nieuwe reeks + legacy) en dus
    // meegaat als "vorig punt" in het volgende verslag. Concept/PendingApproval staan hier bewust niet
    // bij (Niels, 2026-10-10): een punt dat nog niet is ingediend of nog wacht op goedkeuring is geen
    // al-eerder-gemeld punt, dat hoort pas mee zodra het effectief is goedgekeurd of actief is.
    // Approved stond hier aanvankelijk niet bij (zag er elders in de codebase wél als actief wordt
    // behandeld, bv. ProjectIssuesController.cs's activeStatuses) — toegevoegd.
    private static readonly int[] OpenStatuses =
    {
        (int)ConstructionIssueStatus.Forwarded, (int)ConstructionIssueStatus.Reported, (int)ConstructionIssueStatus.Rejected,
        (int)ConstructionIssueStatus.OnHold, (int)ConstructionIssueStatus.Open, (int)ConstructionIssueStatus.Assigned,
        (int)ConstructionIssueStatus.InProgress, (int)ConstructionIssueStatus.WaitingInspection, (int)ConstructionIssueStatus.Reopened,
        (int)ConstructionIssueStatus.Approved,
    };

    public ProjectVerslagenController(cpmRunningContext db, IConstructionIssueService issues, IEmailSender email, IWebHostEnvironment env,
        IOptions<GlV2PdfCompanyOptions> company, ILogger<ProjectVerslagenController> logger, FacadeCore.Signing.IAssetStorageClient storage, CPMCore.Services.Signing.ChangeOrderPdfBuilder pdfBuilder)
    {
        _db = db; _issues = issues; _email = email; _env = env; _company = company; _logger = logger; _storage = storage; _pdfBuilder = pdfBuilder;
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
        var openIssues = await _db.ConstructionIssue.AsNoTracking().Where(i => i.ProjectId == projectId && OpenStatuses.Contains(i.Status)).Select(i => new { i.Id, i.Status }).ToListAsync();
        foreach (var oi in openIssues) _db.WerfverslagPunt.Add(new WerfverslagPunt { VerslagId = verslag.Id, IssueId = oi.Id, IsNieuw = false, StatusBijStart = PuntenWeergave.Canon(oi.Status) });
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
            PlanPages = (await PlanPages(await LoadData(v, projectId), projectId)).Select(x => (object)new
            {
                key = x.Key, url = x.Url, kind = x.Kind, page = x.Page, naam = x.Naam,
                pins = x.Pins.Select(p => new { x = p.X, y = p.Y, label = p.Label, kritiek = p.Kritiek }).ToList()
            }).ToList(),
        };
        ViewData["HelpScreenId"] = "verslagen-edit";
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

    // ── Gegevens voor het verslag en de plannen-bijlage ─────────────────────────────────────────────

    private sealed record PlanPinDef(double X, double Y, string Label, bool Kritiek, string Nr, string Tekst);
    private sealed record PlanPageDef(string Key, int PlanId, int Page, string Naam, string Sub, string Url, string Kind, List<PlanPinDef> Pins);

    private sealed class VerslagData
    {
        public Werfverslag V = null!;
        public List<(WerfverslagPunt P, ConstructionIssue I)> Nieuwe = new();
        public List<(WerfverslagPunt P, ConstructionIssue I)> Open = new();
        public Dictionary<int, int> Num = new();
        public Dictionary<int, string> Contractors = new();
    }

    private async Task<VerslagData> LoadData(Werfverslag v, int projectId)
    {
        var punten = await _db.WerfverslagPunt.AsNoTracking().Where(p => p.VerslagId == v.Id).ToListAsync();
        var ids = punten.Select(p => p.IssueId).ToList();
        var issues = await _db.ConstructionIssue.AsNoTracking().Include(i => i.Unit).Where(i => ids.Contains(i.Id)).ToDictionaryAsync(i => i.Id);
        var d = new VerslagData { V = v };
        d.Contractors = await _db.Contract.AsNoTracking().Where(c => c.ProjectId == projectId).Select(c => new { c.CompanyId, N = c.Company.BedrijfsNaam }).Distinct().ToDictionaryAsync(c => c.CompanyId, c => c.N);
        foreach (var p in punten.Where(p => issues.ContainsKey(p.IssueId)).OrderBy(p => issues[p.IssueId].PuntNr ?? issues[p.IssueId].Id))
            (p.IsNieuw ? d.Nieuwe : d.Open).Add((p, issues[p.IssueId]));
        for (var n = 0; n < d.Nieuwe.Count; n++) d.Num[d.Nieuwe[n].I.Id] = n + 1;
        return d;
    }

    private static string NrOf(ConstructionIssue i) => "P-" + (i.PuntNr ?? i.Id).ToString("000");
    private static string AannemerOf(ConstructionIssue i, VerslagData d)
    {
        var naam = PuntenWeergave.Aannemer(i.ResponsiblePartyId.HasValue && d.Contractors.TryGetValue(i.ResponsiblePartyId.Value, out var n) ? n : i.ResponsibleOtherName);
        return naam == "" ? "—" : naam;
    }

    /// <summary>Per plan(pagina) de punten die erop staan, met hetzelfde nummer als in het verslag (nieuw: 1, 2, 3 …; open uit een vorig verslag: "O").</summary>
    private async Task<List<PlanPageDef>> PlanPages(VerslagData d, int projectId)
    {
        var all = d.Nieuwe.Concat(d.Open).Where(x => x.I.PlanDocumentId.HasValue && x.I.PlanXnormalized.HasValue && x.I.PlanYnormalized.HasValue).ToList();
        if (all.Count == 0) return new();
        var planIds = all.Select(x => x.I.PlanDocumentId!.Value).Distinct().ToList();
        var plans = await _db.UnitExecutionPlan.AsNoTracking().Include(p => p.Unit).Where(p => planIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        var result = new List<PlanPageDef>();
        foreach (var g in all.GroupBy(x => (Plan: x.I.PlanDocumentId!.Value, Page: x.I.PlanPageNumber ?? 1)).OrderBy(g => g.Key.Plan).ThenBy(g => g.Key.Page))
        {
            if (!plans.TryGetValue(g.Key.Plan, out var plan)) continue;
            var naam = plan.UnitId == null ? plan.Name : (plan.Unit?.Name ?? "") + " · " + plan.Name;
            var ext = Path.GetExtension(plan.FileId ?? "").ToLowerInvariant();
            var pins = g.Select(x => new PlanPinDef((double)x.I.PlanXnormalized!.Value, (double)x.I.PlanYnormalized!.Value,
                x.P.IsNieuw ? d.Num[x.I.Id].ToString() : "O", x.I.Priority == 3, NrOf(x.I), PuntenWeergave.Titel(x.I.Title) + " · " + (x.I.Unit?.Name ?? "Algemeen"))).ToList();
            result.Add(new PlanPageDef($"{plan.Id}:{g.Key.Page}", plan.Id, g.Key.Page, naam, (plan.Soort ?? "plan") + " · v" + plan.Versie,
                Url.Action("PlanContent", "ProjectsIssues", new { projectId, fileId = plan.FileId }) ?? "", ext is ".png" or ".jpg" or ".jpeg" ? "image" : "pdf", pins));
        }
        return result;
    }

    private static byte[]? Shrink(byte[]? src, int max = 900)
    {
        if (src == null || src.Length == 0) return null;
        try
        {
            using var img = SixLabors.ImageSharp.Image.Load(src);
            img.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(max, max), Mode = ResizeMode.Max }));
            using var ms = new MemoryStream();
            img.Save(ms, new JpegEncoder { Quality = 72 });
            return ms.ToArray();
        }
        catch { return null; }
    }

    private static byte[]? FromDataUrl(string? s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        var i = s.IndexOf("base64,", StringComparison.Ordinal);
        if (i < 0) return null;
        try { return Convert.FromBase64String(s[(i + 7)..]); } catch { return null; }
    }

    /// <summary>De door de browser gerasterde plannen (pdf.js + pins) uit het formulier: velden "planimg:{planId}:{pagina}" met een data-URL.</summary>
    private Dictionary<string, string> PlanImagesFromForm()
    {
        var map = new Dictionary<string, string>();
        if (!Request.HasFormContentType) return map;
        foreach (var k in Request.Form.Keys.Where(k => k.StartsWith("planimg:", StringComparison.Ordinal)))
            map[k.Substring(8)] = Request.Form[k].ToString();
        return map;
    }

    // ── Afronden: punten goedkeuren/afsluiten + verslag versturen ───────────────────────────────────

    private async Task<WerfverslagPdfModel> BuildPdfModel(Werfverslag v, int projectId, Dictionary<string, string>? planImages = null)
    {
        var d = await LoadData(v, projectId);
        var projectName = await ProjectName(projectId);
        var project = await _db.Project.AsNoTracking().Include(p => p.PostalCode).FirstOrDefaultAsync(p => p.ProjectId == projectId);
        string J(params string?[] parts) => string.Join(" ", parts.Where(s => !string.IsNullOrWhiteSpace(s)));
        var adres = string.Join(", ", new[] { J(project?.Street, project?.Number), J(project?.PostalCode?.Postcode, project?.PostalCode?.Gemeente) }.Where(s => s != ""));
        var vorigV = v.HernomenVanId.HasValue ? await _db.Werfverslag.AsNoTracking().FirstOrDefaultAsync(x => x.Id == v.HernomenVanId) : null;
        var aanwezigen = ParseAanwezigen(v.AanwezigenJson);

        // plannamen (ook voor punten zonder pin) + foto's van de nieuwe punten
        var planIds = d.Nieuwe.Concat(d.Open).Where(x => x.I.PlanDocumentId.HasValue).Select(x => x.I.PlanDocumentId!.Value).Distinct().ToList();
        var planNames = await _db.UnitExecutionPlan.AsNoTracking().Include(p => p.Unit).Where(p => planIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.UnitId == null ? p.Name : (p.Unit != null ? p.Unit.Name : "") + " · " + p.Name);
        var nieuweIds = d.Nieuwe.Select(x => x.I.Id).ToList();
        var media = await _db.ConstructionIssueMedia.AsNoTracking().Where(m => nieuweIds.Contains(m.IssueId) && m.MediaType == (int)ConstructionIssueMediaType.Photo).OrderBy(m => m.Id).ToListAsync();

        var nieuwe = new List<WerfverslagPdfNieuw>();
        foreach (var (p, i) in d.Nieuwe)
        {
            var fotos = new List<byte[]>();
            foreach (var m in media.Where(m => m.IssueId == i.Id).Take(3))
            {
                var bytes = Shrink(await _storage.DownloadAsync("pictures", m.FileId));
                if (bytes != null) fotos.Add(bytes);
            }
            nieuwe.Add(new WerfverslagPdfNieuw(d.Num[i.Id], NrOf(i), PuntenWeergave.Titel(i.Title), i.Unit?.Name ?? "Algemeen", AannemerOf(i, d), PuntenWeergave.Label(i.Status),
                i.Priority == 3, i.Description, i.DueDate?.ToString("dd/MM/yyyy"), i.PlanDocumentId.HasValue && planNames.TryGetValue(i.PlanDocumentId.Value, out var pn) ? pn : null, fotos));
        }

        // openstaand uit vorig verslag; "nu" houdt rekening met wat ter plaatse vastgesteld werd (ook in het voorbeeld vóór afronden)
        var open = new List<WerfverslagPdfOpen>(); var afgesloten = new List<WerfverslagPdfOpen>();
        foreach (var (p, i) in d.Open)
        {
            var dat = v.Datum.ToString("dd/MM");
            var nu = p.TerPlaatse switch { 2 => "Afgesloten", 1 => "Gemeld uitgevoerd", _ => PuntenWeergave.Label(i.Status) };
            var notitie = p.TerPlaatse switch
            {
                2 => $"Gecontroleerd op {dat} — in orde.",
                1 => $"Opgelost vastgesteld op {dat}.",
                _ => null,
            };
            if (!string.IsNullOrWhiteSpace(p.Opmerking)) notitie = (notitie == null ? "" : notitie + " ") + p.Opmerking.Trim();
            var vorig = p.StatusBijStart.HasValue ? PuntenWeergave.Label(p.StatusBijStart.Value) : "—";
            var row = new WerfverslagPdfOpen(NrOf(i), PuntenWeergave.Titel(i.Title), notitie, i.Unit?.Name ?? "Algemeen", AannemerOf(i, d), vorig, nu);
            (p.TerPlaatse == 2 || PuntenWeergave.IsClosed(i.Status) ? afgesloten : open).Add(row);
        }

        // plannen-bijlage: enkel plan(pagina)'s waarvoor de browser een afbeelding meestuurde
        var plannen = new List<WerfverslagPdfPlan>();
        if (planImages is { Count: > 0 })
            foreach (var def in await PlanPages(d, projectId))
            {
                var img = Shrink(FromDataUrl(planImages.GetValueOrDefault(def.Key)), 2000);
                if (img == null) continue;
                plannen.Add(new WerfverslagPdfPlan(def.Naam, def.Sub, img, def.Pins.Select(x => new WerfverslagPdfPlanPunt(x.Label, x.Nr, x.Tekst, x.Kritiek)).ToList()));
            }

        return new WerfverslagPdfModel
        {
            Naam = v.Naam, TypeLabel = v.VerslagType == 1 ? "Voorlopige oplevering" : "Werfverslag", ProjectName = projectName, ProjectAdres = adres == "" ? null : adres,
            Datum = v.Datum, Uur = v.Uur?.ToString("HH:mm"), VorigBezoek = vorigV == null ? null : vorigV.Naam + " · " + vorigV.Datum.ToString("dd/MM/yyyy"),
            Aanwezigen = aanwezigen.Select(a => a.Name).ToList(), Weer = v.Weer, Opmerkingen = v.Opmerkingen, VolgendBezoek = v.VolgendBezoek,
            Nieuwe = nieuwe, Open = open, Afgesloten = afgesloten,
            Kritiek = d.Nieuwe.Count(x => x.I.Priority == 3) + d.Open.Count(x => x.I.Priority == 3 && x.P.TerPlaatse != 2 && !PuntenWeergave.IsClosed(x.I.Status)),
            Verdeling = aanwezigen.Select(a => new WerfverslagPdfVerdeling(a.Name, string.IsNullOrWhiteSpace(a.Email) ? null : a.Email, "volledig verslag")).ToList(),
            Plannen = plannen,
        };
    }

    private async Task<byte[]> RenderPdfAsync(WerfverslagPdfModel model, int projectId)
    {
        // kop/voet/logo: het facturatiebedrijf van het project, zoals bij de wijzigingsopdracht (logo zonder tekst ernaast)
        var company = await _pdfBuilder.BuildGlV2CompanyForProjectAsync(projectId);
        var fonts = GlV2PdfFonts.EnsureRegistered(_env, _logger);
        var hoofd = new WerfverslagDocumentV2(model, company, fonts).GeneratePdf();
        if (model.Plannen.Count == 0) return hoofd;

        // bijlage: één pagina (A4 liggend) per plan, elk met de volledige kop; paginanummers lopen door vanaf het verslag
        int PaginaTelling(byte[] pdf) { using var m = new MemoryStream(pdf); using var d = PdfSharpCore.Pdf.IO.PdfReader.Open(m, PdfSharpCore.Pdf.IO.PdfDocumentOpenMode.Import); return d.PageCount; }
        var hoofdPaginas = PaginaTelling(hoofd);
        var totaal = hoofdPaginas + model.Plannen.Count;
        hoofd = new WerfverslagDocumentV2(model, company, fonts, totaal).GeneratePdf();
        var delen = new List<byte[]> { hoofd };
        for (var i = 0; i < model.Plannen.Count; i++)
            delen.Add(new WerfverslagBijlageDocumentV2(model, i, hoofdPaginas + i, totaal, company, fonts).GeneratePdf());
        using var output = new PdfSharpCore.Pdf.PdfDocument();
        foreach (var part in delen)
        {
            using var ms = new MemoryStream(part);
            using var input = PdfSharpCore.Pdf.IO.PdfReader.Open(ms, PdfSharpCore.Pdf.IO.PdfDocumentOpenMode.Import);
            foreach (PdfSharpCore.Pdf.PdfPage page in input.Pages) output.AddPage(page);
        }
        using var outMs = new MemoryStream();
        output.Save(outMs);
        return outMs.ToArray();
    }

    /// <summary>Voorbeeld zonder plannen-bijlage (geen browser-rendering mogelijk); het voorbeeld vanuit de bewerkpagina (PdfPreview) neemt de plannen wél mee.</summary>
    [HttpGet("{id:int}/Pdf")]
    public async Task<IActionResult> Pdf(int projectId, int id)
    {
        var v = await _db.Werfverslag.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.ProjectId == projectId);
        if (v == null) return NotFound();
        var bytes = await RenderPdfAsync(await BuildPdfModel(v, projectId), projectId);
        Response.Headers["Content-Disposition"] = $"inline; filename=\"{v.Naam.Replace(' ', '_')}.pdf\"";
        return File(bytes, "application/pdf");
    }

    /// <summary>Voorbeeld mét plannen: de bewerkpagina post de door pdf.js gerasterde plannen (met pins) mee.</summary>
    [HttpPost("{id:int}/PdfPreview")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PdfPreview(int projectId, int id)
    {
        var v = await _db.Werfverslag.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.ProjectId == projectId);
        if (v == null) return NotFound();
        var bytes = await RenderPdfAsync(await BuildPdfModel(v, projectId, PlanImagesFromForm()), projectId);
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
        // 2. afronden keurt de nieuwe punten goed (Concept / Ter goedkeuring → Goedgekeurd); ze gaan daarna via de avondmelding of "Doorsturen" naar de aannemer
        var goedgekeurd = 0;
        foreach (var p in punten.Where(p => p.IsNieuw))
            if (issues.TryGetValue(p.IssueId, out var nieuw) && PuntenWeergave.Canon(nieuw.Status) is (int)ConstructionIssueStatus.Concept or (int)ConstructionIssueStatus.PendingApproval)
            {
                await _issues.ChangeStatus(projectId, nieuw.Id, (int)ConstructionIssueStatus.Approved, $"Goedgekeurd bij afronden van {v.Naam}", UserCode);
                goedgekeurd++;
            }

        // 3. PDF (met foto's en plannen) + mail naar de aanwezigen met e-mailadres
        var model = await BuildPdfModel(v, projectId, PlanImagesFromForm());
        var pdf = await RenderPdfAsync(model, projectId);
        var ontvangers = ParseAanwezigen(v.AanwezigenJson).Where(a => !string.IsNullOrWhiteSpace(a.Email)).ToList();
        var sent = 0;
        foreach (var a in ontvangers)
        {
            try
            {
                var html = $"<p>Beste {System.Net.WebUtility.HtmlEncode(a.Name)},</p><p>In bijlage het verslag <b>{System.Net.WebUtility.HtmlEncode(v.Naam)}</b> van {v.Datum:dd/MM/yyyy} voor {System.Net.WebUtility.HtmlEncode(model.ProjectName)}.</p>" +
                           $"<p>De nieuwe punten zijn goedgekeurd en gaan naar de betrokken aannemers.</p>";
                await _email.SendEmailAsync(a.Email!, $"{v.Naam} — {model.ProjectName}", html, new[] { new EmailAttachment($"{v.Naam.Replace(' ', '_')}.pdf", pdf, "application/pdf") });
                sent++;
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Verslag {VerslagId} niet verstuurd naar {Email}", v.Id, a.Email); }
        }
        v.Status = 1; v.VerzondenOp = DateTime.UtcNow; v.VerzondenDoor = UserCode;
        await _db.SaveChangesAsync();
        AddMessage("success", $"{v.Naam} afgerond." + (goedgekeurd > 0 ? $" {goedgekeurd} nieuwe punt(en) goedgekeurd." : "") + (sent > 0 ? $" Het verslag is verstuurd naar {sent} aanwezige(n)." : " Geen aanwezige heeft een e-mailadres, dus er is niets verstuurd."), "Geslaagd");
        return RedirectToAction(nameof(Index), new { projectId, id });
    }
}

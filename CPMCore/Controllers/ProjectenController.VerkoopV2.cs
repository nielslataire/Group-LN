using BOCore;
using CPMCore.Helpers;
using CPMCore.Models.Projecten;
using CPMCore.Services.Verkoop;
using FacadeCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuestPDF.Fluent;

namespace CPMCore.Controllers;

// gl-v2 (design-handoff punt 45 "Verkoop"): Te koop (45a), Verkoopinstellingen (45b); Eenheid berekenen (45c), afdrukken (45d/45e) en de prijslijst (45f/45h) staan er; Excel (45g) volgt.
// De legacy schermen (DetailSales/SalesSettings) blijven bestaan voor ?classic=true.
public partial class ProjectenController
{
    private VerkoopService Verkoop => HttpContext.RequestServices.GetRequiredService<VerkoopService>();
    private bool CanWriteVerkoop => HttpContext.RequestServices.GetRequiredService<IPermissionService>().HasWrite(PermissionCodes.ProjectsForSale);

    private void SetVerkoopBreadcrumb(int projectid, string projectName, string? leaf = null, string? leafAction = null)
    {
        var home = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var projecten = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = home };
        var project = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", projectName) { Parent = projecten, RouteValues = new { projectid } };
        var verkoop = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailSales", "Projecten", "Te koop") { Parent = project, RouteValues = new { projectid } };
        ViewData["BreadcrumbNode"] = leaf == null ? verkoop : new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode(leafAction ?? "DetailSales", "Projecten", leaf) { Parent = verkoop, RouteValues = new { projectid } };
    }

    /// <summary>45a — Te koop.</summary>
    private async Task<IActionResult> VerkoopListV2(int projectid)
    {
        var vm = await Verkoop.GetIndexAsync(projectid);
        vm.CanWrite = CanWriteVerkoop;
        SetVerkoopBreadcrumb(projectid, vm.ProjectName);
        SetPageHeader("bx bx-building-house", $"{vm.ProjectName} - Te koop");
        ViewData["HelpScreenId"] = "projecten-te-koop";
        return View("DetailSalesV2", vm);
    }

    /// <summary>45b — Verkoopinstellingen.</summary>
    private async Task<IActionResult> VerkoopSettingsV2(int projectid)
    {
        var projectName = _projectService.GetProjectNameById(projectid);
        var vm = new VerkoopInstellingenVm
        {
            ProjectId = projectid, ProjectName = projectName, CanWrite = CanWriteVerkoop,
            Instellingen = await Verkoop.GetInstellingenAsync(projectid), Website = await Verkoop.GetWebsiteAsync(projectid),
        };
        var rijen = (await Verkoop.GetIndexAsync(projectid)).Rijen;
        vm.Voorbeeld = rijen.LastOrDefault(r => r.Status != "Verkocht") ?? rijen.LastOrDefault();
        await VulBankrekeningenV2(vm);
        SetVerkoopBreadcrumb(projectid, projectName, "Instellingen", "SalesSettings");
        SetPageHeader("bx bx-building-house", $"{projectName} - Verkoopinstellingen");
        ViewData["HelpScreenId"] = "projecten-verkoopinstellingen";
        return View("SalesSettingsV2", vm);
    }

    private async Task VulBankrekeningenV2(VerkoopInstellingenVm vm)
    {
        if (vm.Website.BuilderCompanyId == null) { vm.MissingBuilder = true; return; }
        using var scope = HttpContext.RequestServices.CreateScope();
        var bankService = scope.ServiceProvider.GetRequiredService<IIssuerBankAccountService>();
        var accounts = await bankService.ListByIssuerAsync(vm.Website.BuilderCompanyId.Value);
        IssuerBankAccountBO? gekozen = null;
        if (vm.Website.BankAccountId is int id) gekozen = accounts.FirstOrDefault(a => a.Id == id) ?? await bankService.GetAsync(id);
        vm.RekeningOntbrak = vm.Website.BankAccountId == null;
        vm.BankAccounts = BuildBankAccountSelectList(accounts, vm.Website.BankAccountId, gekozen);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsForSale)]
    public async Task<IActionResult> SalesSettingsSaveV2(int projectid, IFormCollection form)
    {
        string F(string k) => form[k].ToString();
        decimal D(string k) => VerkoopService.ParseBedrag(F(k));
        var i = new VerkoopInstellingen
        {
            VatPercentage = D("vat"), RegistrationPercentage = D("reg"), RegistrationType = int.TryParse(F("regtype"), out var rt) ? rt : 0,
            MortgageRegistrationCost = D("mortgage"), BaseCertificateCost = D("basecert"),
            ParcelCost = D("parcel"), ConnectionFees = D("connection"), SurveyorCost = D("surveyor"),
        };
        var names = form["extraname"].ToArray(); var amounts = form["extraamount"].ToArray(); var notes = form["extranote"].ToArray();
        for (var n = 0; n < names.Length; n++)
            i.Extra.Add(new VerkoopExtraKost { Naam = names[n] ?? "", Bedrag = VerkoopService.ParseBedrag(n < amounts.Length ? amounts[n] : null), Toelichting = n < notes.Length ? notes[n] : null });

        var w = new VerkoopWebsite
        {
            SaleVisible = F("salevisible") == "1", IsFeatured = F("featured") == "1",
            DisplayStatus = int.TryParse(F("displaystatus"), out var ds) && ds > 0 ? ds : null,
            Title = F("title"), Text = SchoonWebsiteTekst(F("text")), BankAccountId = int.TryParse(F("bankaccount"), out var bid) && bid > 0 ? bid : null,
        };

        // Nieuwe rekening via de lijst: zelfde aanmaak als het oude scherm (zoekt eerst op IBAN).
        string? iban = null;
        var nieuweIban = F("newiban").Trim();
        var project = _projectService.GetProjectByID(projectid);
        if (project.Success && project.Value?.IssuerCompanyIdBuilder != null)
        {
            var model = new ProjectSalesSettingsModel { ProjectId = projectid, Project = project.Value, Settings = new ProjectSalesSettingsBO { ProjectId = projectid, BankAccountId = w.BankAccountId }, NewBankAccountIban = nieuweIban };
            await EnsureBankAccountAsync(model);
            w.BankAccountId = model.Settings.BankAccountId;
            iban = model.Settings.BankAccountNumber;
        }

        try { await Verkoop.SaveAsync(projectid, i, w, iban); }
        catch (Exception ex)
        {
            return Json(new { ok = false, error = "Opslaan is mislukt: " + ex.Message });
        }
        AddMessage("success", "De verkoopinstellingen zijn opgeslagen.", "Geslaagd!");
        return Json(new { ok = true, redirect = Url.Action("DetailSales", "Projecten", new { projectid }) });
    }

    /// <summary>Websitetekst: enkel p, br, b, i, ul en li blijven over, zonder attributen (de tekst komt als HTML op de publieke site).</summary>
    private static string SchoonWebsiteTekst(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return "";
        var opt = System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline;
        html = System.Text.RegularExpressions.Regex.Replace(html, @"<(script|style)[^>]*>.*?</\1>", "", opt);
        // toegelaten tags zonder attributen terugschrijven, alle andere weg
        return System.Text.RegularExpressions.Regex.Replace(html, @"<(/?)([a-z0-9]+)[^>]*>", m =>
        {
            var t = m.Groups[2].Value.ToLowerInvariant();
            t = t == "strong" ? "b" : t == "em" ? "i" : t;
            return t is "p" or "br" or "b" or "i" or "ul" or "li" ? "<" + m.Groups[1].Value + t + ">" : "";
        }, opt).Trim();
    }

    /// <summary>45h — Prijslijst (PDF). <paramref name="ids"/> = enkel de gekozen eenheden (leeg = alle); de opties volgen 45f "Opnemen" en "Opmaak".</summary>
    [HttpGet]
    public async Task<IActionResult> SalesListPdfV2(int projectid, string? ids, bool verkochtZonderPrijs = true, bool totaalKoper = true, bool optieAlsTeKoop = false, bool liggend = true)
    {
        var vm = await Verkoop.GetIndexAsync(projectid);
        var gekozen = (ids ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => int.TryParse(s, out var n) ? n : 0).Where(n => n > 0).ToHashSet();
        var rijen = gekozen.Count == 0 ? vm.Rijen : vm.Rijen.Where(r => gekozen.Contains(r.UnitId)).ToList();
        if (rijen.Count == 0) { AddMessage("error", "Er zijn geen eenheden om in de prijslijst op te nemen.", "Fout"); return RedirectToAction("DetailSales", new { projectid }); }
        var company = await HttpContext.RequestServices.GetRequiredService<CPMCore.Services.Signing.ChangeOrderPdfBuilder>().BuildGlV2CompanyForProjectAsync(projectid);
        var fonts = CPMCore.Documents.GlV2.GlV2PdfFonts.EnsureRegistered(HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(), HttpContext.RequestServices.GetRequiredService<ILogger<ProjectenController>>());
        var opties = new CPMCore.Documents.GlV2.PrijslijstOpties(verkochtZonderPrijs, totaalKoper, optieAlsTeKoop, liggend);
        var pdf = new CPMCore.Documents.GlV2.PrijslijstDocumentV2(vm.ProjectName, rijen, opties, company, fonts).GeneratePdf();
        var naam = string.Concat(vm.ProjectName.Where(ch => char.IsLetterOrDigit(ch) || ch == ' ')).Trim().Replace(' ', '_');
        return File(pdf, "application/pdf", $"Prijslijst_{naam}_{DateTime.Now:yyyyMMdd}.pdf");
    }

    /// <summary>Acties op de selectie in Te koop: Te koop zetten, In optie zetten, Op website zetten of van de website halen. Verkochte eenheden worden overgeslagen.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalesBulkV2(int projectid, string op, string unitIds, string? until, string? holder, string? via)
    {
        if (!CanWriteVerkoop) return Json(new { ok = false, error = "Je hebt geen schrijfrecht op de verkoop." });
        var ids = (unitIds ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => int.TryParse(s, out var n) ? n : 0).Where(n => n > 0).ToList();
        if (ids.Count == 0) return Json(new { ok = false, error = "Geen eenheden geselecteerd." });
        int aantal;
        switch (op)
        {
            case "tekoop": aantal = await Verkoop.ZetTeKoopAsync(projectid, ids); break;
            case "optie":
                DateOnly? tot = DateOnly.TryParse(until, System.Globalization.CultureInfo.GetCultureInfo("nl-BE"), out var d) ? d : (DateOnly.TryParse(until, System.Globalization.CultureInfo.InvariantCulture, out var d2) ? d2 : null);
                aantal = await Verkoop.ZetOptieAsync(projectid, ids, tot, holder, via); break;
            case "online": aantal = await Verkoop.ZetOnlineAsync(projectid, ids, true); break;
            case "offline": aantal = await Verkoop.ZetOnlineAsync(projectid, ids, false); break;
            default: return Json(new { ok = false, error = "Onbekende actie." });
        }
        return Json(new { ok = true, aantal, overgeslagen = ids.Count - aantal });
    }
}

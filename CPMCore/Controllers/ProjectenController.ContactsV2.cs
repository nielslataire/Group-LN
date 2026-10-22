using BOCore;
using CPMCore.Helpers;
using CPMCore.Models.Contacten;
using CPMCore.Models.Projecten;
using CPMCore.Services.Contacten;
using FacadeCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuestPDF.Fluent;

namespace CPMCore.Controllers;

// gl-v2 (design-handoff punt 44 "Contacten"): lijst (44a), detail met opvolging (44b), toevoegen (44c) en lijsten & export (44d).
// Het legacy DetailContacts/ContactDetails blijft bestaan voor wie de gl-v2-preview uitzet (?classic=true) en toont enkel de oude gegevens.
public partial class ProjectenController
{
    private CrmContactService Crm => HttpContext.RequestServices.GetRequiredService<CrmContactService>();
    private bool CanWriteContacts => HttpContext.RequestServices.GetRequiredService<IPermissionService>().HasWrite(PermissionCodes.ProjectsContacts);
    private bool CanDeleteContacts => HttpContext.RequestServices.GetRequiredService<IPermissionService>().HasDelete(PermissionCodes.ProjectsContacts);
    private bool CanExportContacts => HttpContext.RequestServices.GetRequiredService<IPermissionService>().HasRead(PermissionCodes.ProjectsContactsExport);
    private string ContactUserName => User.GetCpmDisplayName() ?? User?.Identity?.Name ?? "Onbekende gebruiker";

    private void SetContactBreadcrumb(int projectid, string projectName, string? leaf = null, string? leafAction = null, object? leafRoute = null)
    {
        var home = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var projecten = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = home };
        var project = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", projectName) { Parent = projecten, RouteValues = new { projectid } };
        var contacten = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailContacts", "Projecten", "Contacten") { Parent = project, RouteValues = new { projectid } };
        ViewData["BreadcrumbNode"] = leaf == null ? contacten : new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode(leafAction ?? "DetailContacts", "Projecten", leaf) { Parent = contacten, RouteValues = leafRoute ?? new { projectid } };
    }

    /// <summary>44a — de lijst.</summary>
    private async Task<ActionResult> ContactsListV2(int projectid)
    {
        var projectName = _projectService.GetProjectNameById(projectid);
        await Crm.SyncLegacyAsync(projectid);
        var vm = new CrmContactsIndexVm
        {
            ProjectId = projectid, ProjectName = projectName, CanWrite = CanWriteContacts, CanDelete = CanDeleteContacts, Today = DateOnly.FromDateTime(DateTime.Today),
            Rows = await Crm.ListAsync(projectid), Units = await Crm.UnitsAsync(projectid), Users = await Crm.UsersAsync(), Assignees = await Crm.AssigneesAsync(projectid), CanExport = CanExportContacts, CurrentUserId = User.GetCpmUserId() ?? 0,
            Lists = (await Crm.ListsAsync(projectid)).Select(l => new CrmListVm(l.Id, l.Name, l.FiltersJson, l.CreatedBy)).ToList(),
        };
        SetContactBreadcrumb(projectid, projectName);
        SetPageHeader("bx bx-building-house", $"{projectName} - Contacten");
        ViewData["HelpScreenId"] = "projecten-contacten";
        return View("DetailContactsV2", vm);
    }

    /// <summary>44b — het contact met opvolging.</summary>
    [HttpGet]
    public async Task<ActionResult> ContactV2(int projectid, int id)
    {
        if (ViewData["UseGlV2Layout"] as bool? != true) return RedirectToAction("DetailContacts", new { projectid });
        var projectName = _projectService.GetProjectNameById(projectid);
        await Crm.SyncLegacyAsync(projectid);
        var c = await Crm.GetAsync(projectid, id);
        if (c == null) { AddMessage("error", "Contact niet gevonden.", "Fout"); return RedirectToAction("DetailContacts", new { projectid }); }
        var templates = (_emailTemplateService.GetAll(alleenActief: true).Values ?? new List<EmailTemplateBO>()).Select(t => new EmailTemplateItem(t.ID, t.Naam)).ToList();
        var name = ContactenWeergave.Name(c.FirstName, c.PartnerName, c.LastName);
        var vm = new CrmContactDetailVm
        {
            ProjectId = projectid, ProjectName = projectName, CanWrite = CanWriteContacts, CanDelete = CanDeleteContacts, Id = c.Id, Name = name, Initials = ContactenWeergave.Initials(name),
            FirstName = c.FirstName, LastName = c.LastName, PartnerName = c.PartnerName, Email = c.Email, Phone = c.Phone,
            Address = string.Join(", ", new[] { c.Street, string.Join(" ", new[] { c.PostalCode, c.City }.Where(s => !string.IsNullOrWhiteSpace(s))) }.Where(s => !string.IsNullOrWhiteSpace(s))),
            Source = c.Source, BrokerName = c.BrokerName, SeenVia = c.SeenVia, Status = c.Status, StatusReason = c.StatusReason, AssignedUserId = c.AssignedUserId, AssignedCompanyId = c.AssignedCompanyId, AssignedName = c.AssignedName,
            BrokerCompanyId = c.BrokerCompanyId, PostcodeId = c.PostcodeId, Street = c.Street, PostalCode = c.PostalCode, City = c.City, Assignees = await Crm.AssigneesAsync(projectid),
            UnitNames = c.Units.Select(u => u.Unit?.Name ?? "").Where(n => n != "").OrderBy(n => n).ToList(), UnitIds = c.Units.Select(u => u.UnitId).ToList(),
            Budget = c.Budget, Financing = c.Financing, OwnHome = c.OwnHome, Wish = c.Wish, ConsentContact = c.ConsentContact, ConsentNewsletter = c.ConsentNewsletter, ConsentAt = c.ConsentAt,
            NextText = c.NextActionText, NextDate = c.NextActionDate, NextName = c.NextActionName, NextUserId = c.NextActionUserId, VisitAt = c.VisitAt, VisitUnit = c.VisitUnit,
            OptionUntil = c.OptionUntil, OptionUnit = c.OptionUnit, CreatedAt = c.CreatedAt,
            Events = c.Events.OrderByDescending(e => e.OccurredAt).ThenByDescending(e => e.Id).Select(e => new CrmEventVm { Id = e.Id, Kind = e.Kind, OccurredAt = e.OccurredAt, UserName = e.UserName, Title = e.Title, Text = e.Text, DurationMin = e.DurationMin, IsAuto = e.IsAuto }).ToList(),
            Users = await Crm.UsersAsync(), Units = await Crm.UnitsAsync(projectid), CurrentUserId = User.GetCpmUserId() ?? 0, Today = DateOnly.FromDateTime(DateTime.Today), Templates = templates,
        };
        SetContactBreadcrumb(projectid, projectName, name, "ContactV2", new { projectid, id });
        SetPageHeader("bx bx-building-house", $"{projectName} - {name}");
        ViewData["HelpScreenId"] = "projecten-contact";
        return View("ContactV2", vm);
    }

    /// <summary>44c — nieuw contact (of een bestaand contact bewerken).</summary>
    [HttpGet]
    public async Task<ActionResult> ContactFormV2(int projectid, int? id)
    {
        if (ViewData["UseGlV2Layout"] as bool? != true) return RedirectToAction("AddContact", new { projectid });
        var projectName = _projectService.GetProjectNameById(projectid);
        var vm = new CrmContactFormVm { ProjectId = projectid, ProjectName = projectName, Users = await Crm.UsersAsync(), Assignees = await Crm.AssigneesAsync(projectid), Brokers = await Crm.BrokersAsync(projectid), BelgiumCountryId = 19, Units = await Crm.UnitsAsync(projectid), CurrentUserId = User.GetCpmUserId() ?? 0, Today = DateOnly.FromDateTime(DateTime.Today) };
        var existing = id.HasValue ? await Crm.GetAsync(projectid, id.Value) : null;
        ViewData["Existing"] = existing;
        vm.Phone = existing?.Phone; vm.Email = existing?.Email;
        SetContactBreadcrumb(projectid, projectName, id.HasValue ? "Contact bewerken" : "Nieuw contact", "ContactFormV2", new { projectid, id });
        SetPageHeader("bx bx-building-house", $"{projectName} - Contact toevoegen");
        ViewData["HelpScreenId"] = "projecten-contact-nieuw";
        return View("ContactFormV2", vm);
    }

    /// <summary>Dubbel-check (44c): bestaat er al een contact met dit e-mailadres of telefoonnummer?</summary>
    [HttpGet]
    public async Task<IActionResult> ContactDuplicatesV2(int projectid, string? email, string? phone, int? exclude)
    {
        var list = await Crm.FindDuplicatesAsync(projectid, email, phone, exclude);
        return Json(list.Select(c => new
        {
            id = c.Id, name = ContactenWeergave.Name(c.FirstName, c.PartnerName, c.LastName), source = c.Source.ToLowerInvariant(), status = ContactenWeergave.Label(c.Status), detail = c.OptionUnit ?? "",
            url = Url.Action("ContactV2", "Projecten", new { projectid, id = c.Id }),
            match = !string.IsNullOrWhiteSpace(email) && string.Equals((c.Email ?? "").Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase) ? "e-mailadres" : "telefoonnummer",
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ContactSaveV2(int projectid, int? id, CrmContactService.ContactInput input, string? unitIds, bool another = false)
    {
        if (!CanWriteContacts) return Json(new { ok = false, error = "Je hebt geen schrijfrecht op contacten." });
        input.UnitIds = (unitIds ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => int.TryParse(s, out var n) ? n : 0).Where(n => n > 0).ToList();
        if (string.IsNullOrWhiteSpace(input.LastName) && string.IsNullOrWhiteSpace(input.FirstName)) return Json(new { ok = false, error = "Geef minstens een naam in.", field = "lastname" });
        if (string.IsNullOrWhiteSpace(input.Phone) && string.IsNullOrWhiteSpace(input.Email)) return Json(new { ok = false, error = "Geef een telefoonnummer of een e-mailadres in.", field = "phone" });
        // server-kant van de telefoon-/e-mailcontrole ([GlV2Phone], [EmailAddress] op ContactInput)
        foreach (var key in new[] { "Phone", "Email" })
        {
            var bad = ModelState.TryGetValue("input." + key, out var e1) && e1.Errors.Count > 0 ? e1 : ModelState.TryGetValue(key, out var e2) && e2.Errors.Count > 0 ? e2 : null;
            if (bad != null) return Json(new { ok = false, error = bad.Errors[0].ErrorMessage, field = key.ToLowerInvariant() });
        }
        if (id.HasValue)
        {
            if (!await Crm.UpdateAsync(projectid, id.Value, input)) return Json(new { ok = false, error = "Contact niet gevonden." });
            return Json(new { ok = true, id = id.Value, url = Url.Action("ContactV2", "Projecten", new { projectid, id = id.Value }) });
        }
        var newId = await Crm.CreateAsync(projectid, input, ContactUserName);
        return Json(new { ok = true, id = newId, url = Url.Action("ContactV2", "Projecten", new { projectid, id = newId }) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ContactStatusV2(int projectid, string ids, int status, string? reason)
    {
        if (!CanWriteContacts) return Json(new { ok = false, error = "Je hebt geen schrijfrecht op contacten." });
        var list = (ids ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => int.TryParse(s, out var n) ? n : 0).Where(n => n > 0).ToList();
        if (list.Count == 0 || status < 0 || status > ContactenWeergave.Afgevallen) return Json(new { ok = false, error = "Niets om te wijzigen." });
        await Crm.SetStatusAsync(projectid, list, status, reason, ContactUserName);
        return Json(new { ok = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ContactAssignV2(int projectid, string ids, string? assignee)
    {
        if (!CanWriteContacts) return Json(new { ok = false, error = "Je hebt geen schrijfrecht op contacten." });
        var list = (ids ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => int.TryParse(s, out var n) ? n : 0).Where(n => n > 0).ToList();
        await Crm.AssignAsync(projectid, list, assignee);
        return Json(new { ok = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ContactEventV2(int projectid, int id, string kind, string? text, int? duration, DateOnly? visitDate, string? visitTime, DateOnly? optionUntil, string? unit, string? nextText, DateOnly? nextDate, int? nextUserId)
    {
        if (!CanWriteContacts) return Json(new { ok = false, error = "Je hebt geen schrijfrecht op contacten." });
        DateTime? when = null;
        if (kind == "visit")
        {
            if (!visitDate.HasValue) return Json(new { ok = false, error = "Kies de datum van de bezichtiging." });
            when = visitDate.Value.ToDateTime(TimeOnly.TryParse(visitTime, out var t) ? t : new TimeOnly(10, 0));
        }
        if (kind == "option" && !optionUntil.HasValue) return Json(new { ok = false, error = "Kies tot wanneer de optie loopt." });
        if (kind is "note" or "call" or "mail" && string.IsNullOrWhiteSpace(text)) return Json(new { ok = false, error = "Schrijf eerst wat er gebeurd of besproken is." });
        var ok = await Crm.AddEventAsync(projectid, id, new CrmContactService.EventInput { Kind = kind, Text = text, DurationMin = duration, WhenLocal = when, Until = optionUntil, Unit = unit, NextText = nextText, NextDate = nextDate, NextUserId = nextUserId }, ContactUserName);
        return Json(new { ok });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ContactNextV2(int projectid, int id, string? text, DateOnly? date, int? userId, bool? done)
    {
        if (!CanWriteContacts) return Json(new { ok = false, error = "Je hebt geen schrijfrecht op contacten." });
        if (done == true) return Json(new { ok = await Crm.SetNextActionAsync(projectid, id, null, null, null) });
        return Json(new { ok = await Crm.SetNextActionAsync(projectid, id, text, date, userId) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ContactRescheduleV2(int projectid, int id, DateOnly? visitDate, string? visitTime, DateOnly? optionUntil)
    {
        if (!CanWriteContacts) return Json(new { ok = false, error = "Je hebt geen schrijfrecht op contacten." });
        DateTime? when = visitDate.HasValue ? visitDate.Value.ToDateTime(TimeOnly.TryParse(visitTime, out var t) ? t : new TimeOnly(10, 0)) : null;
        return Json(new { ok = await Crm.RescheduleAsync(projectid, id, when, optionUntil, ContactUserName) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ContactDeleteV2(int projectid, string ids)
    {
        if (!CanDeleteContacts) return Json(new { ok = false, error = "Je hebt geen verwijderrecht op contacten." });
        var list = (ids ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => int.TryParse(s, out var n) ? n : 0).Where(n => n > 0).ToList();
        return Json(new { ok = true, count = await Crm.DeleteAsync(projectid, list) });
    }

    /// <summary>Mail met een template vanuit het contact (zelfde sjablonen en handtekening als de legacy mailknop).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ContactMailV2(int projectid, int id, int templateId, bool includeSignature)
    {
        if (!CanWriteContacts) return Json(new { ok = false, error = "Je hebt geen schrijfrecht op contacten." });
        var c = await Crm.GetAsync(projectid, id);
        if (c == null || string.IsNullOrWhiteSpace(c.Email)) return Json(new { ok = false, error = "Dit contact heeft geen e-mailadres." });
        var name = ContactenWeergave.Name(c.FirstName, c.PartnerName, c.LastName);
        var (ok, error, template) = SendContactEmailCore(new SendContactEmailInputModel { ProjectId = projectid, Email = c.Email, Fullname = name, TemplateId = templateId, IncludeSignature = includeSignature });
        if (!ok) return Json(new { ok = false, error });
        await Crm.LogMailAsync(projectid, c.Email, template ?? "sjabloon", ContactUserName);
        return Json(new { ok = true });
    }

    // ───────────────────────── opgeslagen lijsten en export (44d) ─────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ContactListSaveV2(int projectid, string name, string? filters)
    {
        if (!CanWriteContacts) return Json(new { ok = false, error = "Je hebt geen schrijfrecht op contacten." });
        if (string.IsNullOrWhiteSpace(name)) return Json(new { ok = false, error = "Geef de lijst een naam." });
        var id = await Crm.SaveListAsync(projectid, name, filters, ContactUserName);
        return Json(new { ok = true, id, name = name.Trim() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ContactListDeleteV2(int projectid, int id)
    {
        if (!CanWriteContacts) return Json(new { ok = false, error = "Je hebt geen schrijfrecht op contacten." });
        return Json(new { ok = await Crm.DeleteListAsync(projectid, id) });
    }

    /// <summary>Exporteert de gekozen contacten (Excel, CSV of PDF-lijst) met de gekozen kolommen. Contacten zonder toestemming blijven eruit; elke export wordt gelogd.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ContactExportV2(int projectid, string ids, string format, string columns, string? listName, bool newsletter = false)
    {
        if (!CanExportContacts) { AddMessage("error", "Je hebt geen recht om contacten te exporteren.", "Fout"); return RedirectToAction("DetailContacts", new { projectid }); }
        var idList = (ids ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => int.TryParse(x, out var n) ? n : 0).Where(n => n > 0).ToList();
        var cols = new HashSet<string>((columns ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries));
        var (contacts, skipped) = await Crm.ForExportAsync(projectid, idList, newsletter);
        if (contacts.Count == 0)
        {
            AddMessage("error", skipped > 0 ? (newsletter ? "Geen van de gekozen contacten heeft toestemming voor de nieuwsbrief." : "Alle gekozen contacten hebben geen toestemming om gecontacteerd te worden.") : "Er zijn geen contacten om te exporteren.", "Fout");
            return RedirectToAction("DetailContacts", new { projectid });
        }
        var projectName = _projectService.GetProjectNameById(projectid) ?? "Project";
        var fmt = (format ?? "xlsx").ToLowerInvariant();
        if (fmt is not ("xlsx" or "csv" or "pdf")) fmt = "xlsx";
        string F(DateTime utc) => ContactenWeergave.Local(utc).ToString("dd/MM/yyyy HH:mm");
        string Next(DALCore.Models.CrmContact c) => string.Join(" · ", new[] { c.NextActionDate?.ToString("dd/MM/yyyy"), c.NextActionText }.Where(x => !string.IsNullOrWhiteSpace(x)));
        var stamp = DateTime.Now.ToString("yyyyMMdd");
        var baseName = "Contacten_" + string.Concat(projectName.Where(ch => char.IsLetterOrDigit(ch) || ch == ' ')).Trim().Replace(' ', '_') + "_" + stamp;

        if (fmt == "pdf")
        {
            var rijen = contacts.Select(c => new CPMCore.Documents.GlV2.ContactenLijstRij(
                ContactenWeergave.Name(c.FirstName, c.PartnerName, c.LastName), string.Join(" · ", new[] { c.Phone, c.Email }.Where(x => !string.IsNullOrWhiteSpace(x))),
                c.Source + (string.IsNullOrWhiteSpace(c.BrokerName) ? "" : " · " + c.BrokerName), string.Join(" · ", c.Units.Select(u => u.Unit?.Name).Where(n => !string.IsNullOrEmpty(n)).OrderBy(n => n)),
                ContactenWeergave.Label(c.Status), Next(c), c.AssignedName ?? "")).ToList();
            var company = await HttpContext.RequestServices.GetRequiredService<CPMCore.Services.Signing.ChangeOrderPdfBuilder>().BuildGlV2CompanyForProjectAsync(projectid);
            var fonts = CPMCore.Documents.GlV2.GlV2PdfFonts.EnsureRegistered(HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(), HttpContext.RequestServices.GetRequiredService<ILogger<ProjectenController>>());
            var pdf = new CPMCore.Documents.GlV2.ContactenLijstDocumentV2(projectName, listName ?? "", rijen, company, fonts).GeneratePdf();
            await Crm.LogExportAsync(projectid, ContactUserName, newsletter ? ((listName ?? "").Trim() + " (nieuwsbrief)").Trim() : listName, "pdf", contacts.Count, skipped);
            return File(pdf, "application/pdf", baseName + ".pdf");
        }

        // kolommen kiezen
        var headers = new List<string> { "Contactdatum", "Voornaam", "Naam" };
        var getters = new List<Func<DALCore.Models.CrmContact, string>> { c => ContactenWeergave.Local(c.CreatedAt).ToString("dd/MM/yyyy"), c => c.FirstName ?? "", c => c.LastName ?? "" };
        void Add(string h, Func<DALCore.Models.CrmContact, string> g) { headers.Add(h); getters.Add(g); }
        if (cols.Contains("name")) Add("Partner", c => c.PartnerName ?? "");
        if (cols.Contains("contact")) { Add("Telefoon", c => c.Phone ?? ""); Add("E-mail", c => c.Email ?? ""); Add("Straat", c => c.Street ?? ""); Add("Postcode", c => c.PostalCode ?? ""); Add("Gemeente", c => c.City ?? ""); Add("Nieuwsbrief", c => c.ConsentNewsletter ? "ja" : "nee"); }
        if (cols.Contains("source")) { Add("Bron", c => c.Source); Add("Makelaar", c => c.BrokerName ?? ""); Add("Gezien via", c => c.SeenVia ?? ""); }
        if (cols.Contains("interest")) Add("Interesse", c => string.Join(", ", c.Units.Select(u => u.Unit?.Name).Where(n => !string.IsNullOrEmpty(n)).OrderBy(n => n)));
        if (cols.Contains("status")) { Add("Status", c => ContactenWeergave.Label(c.Status)); Add("Volgende actie", c => Next(c)); Add("Toegewezen aan", c => c.AssignedName ?? ""); }
        if (cols.Contains("budget")) { Add("Budget", c => c.Budget ?? ""); Add("Financiering", c => c.Financing ?? ""); Add("Eigen woning", c => c.OwnHome ?? ""); Add("Wens", c => c.Wish ?? ""); }
        if (cols.Contains("note")) Add("Laatste notitie", c => c.Events.Where(e => !string.IsNullOrWhiteSpace(e.Text)).OrderByDescending(e => e.OccurredAt).Select(e => e.Text!).FirstOrDefault() ?? "");
        var full = cols.Contains("full");

        await Crm.LogExportAsync(projectid, ContactUserName, newsletter ? ((listName ?? "").Trim() + " (nieuwsbrief)").Trim() : listName, fmt, contacts.Count, skipped);
        if (fmt == "csv")
        {
            string Q(string v) => "\"" + (v ?? "").Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";
            var sb = new System.Text.StringBuilder();
            var h2 = new List<string>(headers); if (full) h2.Add("Opvolging");
            sb.AppendLine(string.Join(";", h2.Select(Q)));
            foreach (var c in contacts)
            {
                var vals = getters.Select(g => g(c)).ToList();
                if (full) vals.Add(string.Join(" | ", c.Events.OrderBy(e => e.OccurredAt).Select(e => F(e.OccurredAt) + " " + (e.Title ?? ContactenWeergave.KindLabel(e.Kind)) + (string.IsNullOrWhiteSpace(e.Text) ? "" : ": " + e.Text))));
                sb.AppendLine(string.Join(";", vals.Select(Q)));
            }
            return File(new System.Text.UTF8Encoding(true).GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray(), "text/csv; charset=utf-8", baseName + ".csv");
        }

        using var wb = new ClosedXML.Excel.XLWorkbook();
        var ws = wb.AddWorksheet("Contacten");
        for (var i = 0; i < headers.Count; i++) { var cell = ws.Cell(1, i + 1); cell.Value = headers[i]; cell.Style.Font.Bold = true; cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#00532D"); cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White; }
        var rowNo = 2;
        foreach (var c in contacts) { for (var i = 0; i < getters.Count; i++) ws.Cell(rowNo, i + 1).Value = getters[i](c); rowNo++; }
        ws.Columns().AdjustToContents(); ws.SheetView.FreezeRows(1);
        if (full)
        {
            var w2 = wb.AddWorksheet("Opvolging");
            string[] k = { "Contact", "Datum", "Soort", "Omschrijving", "Tekst", "Door" };
            for (var i = 0; i < k.Length; i++) { var cell = w2.Cell(1, i + 1); cell.Value = k[i]; cell.Style.Font.Bold = true; cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#00532D"); cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White; }
            var r2 = 2;
            foreach (var c in contacts) foreach (var e in c.Events.OrderBy(e => e.OccurredAt))
            {
                w2.Cell(r2, 1).Value = ContactenWeergave.Name(c.FirstName, c.PartnerName, c.LastName); w2.Cell(r2, 2).Value = F(e.OccurredAt); w2.Cell(r2, 3).Value = ContactenWeergave.KindLabel(e.Kind);
                w2.Cell(r2, 4).Value = e.Title ?? ""; w2.Cell(r2, 5).Value = e.Text ?? ""; w2.Cell(r2, 6).Value = e.IsAuto ? "automatisch" : (e.UserName ?? ""); r2++;
            }
            w2.Columns().AdjustToContents(); w2.SheetView.FreezeRows(1);
        }
        using var ms = new MemoryStream(); wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", baseName + ".xlsx");
    }
}

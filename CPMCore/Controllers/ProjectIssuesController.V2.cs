using BOCore;
using CPMCore.Helpers;
using CPMCore.Models.Issues;
using FacadeCore;
using DALCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBreadcrumbs.Nodes;

namespace CPMCore.Controllers;

// gl-v2 (design-handoff punt 40 "Punten"): lijst (40a), selectie (40n), detail (40o). Het legacy Index/Details blijft
// bestaan voor wie de gl-v2-preview uitzet (?classic=true).
public partial class ProjectsIssuesController
{
    private bool UseGlV2 => ViewData["UseGlV2Layout"] as bool? == true;

    /// <summary>Kruimelpad Dashboard / Projecten / project / Punten [/ leaf] voor de gl-v2 puntenpagina's.</summary>
    private void SetPuntenBreadcrumb(int projectId, string projectName, string? leaf = null, string? leafAction = null)
    {
        var home = new MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var projecten = new MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = home };
        var projectNode = new MvcBreadcrumbNode("Detail", "Projecten", projectName) { Parent = projecten };
        var punten = new MvcBreadcrumbNode("Index", "ProjectsIssues", "Punten") { Parent = projectNode, RouteValues = new { projectId } };
        ViewData["BreadcrumbNode"] = leaf == null ? punten : new MvcBreadcrumbNode(leafAction ?? "Index", "ProjectsIssues", leaf) { Parent = punten, RouteValues = new { projectId } };
    }

    private async Task<PuntenIndexV2Vm> BuildIndexV2(int projectId, string projectName)
    {
        var ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        var issues = await _db.ConstructionIssue.AsNoTracking()
            .Include(x => x.Unit)
            .Where(x => x.ProjectId == projectId)
            .ToListAsync();
        var ids = issues.Select(x => x.Id).ToList();
        var sent = await _db.ConstructionIssueNotification.AsNoTracking().Where(n => ids.Contains(n.IssueId))
            .GroupBy(n => n.IssueId).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);
        var photos = await _db.ConstructionIssueMedia.AsNoTracking().Where(m => ids.Contains(m.IssueId))
            .OrderBy(m => m.Id).ToListAsync();
        var photoBy = photos.GroupBy(m => m.IssueId).ToDictionary(g => g.Key, g => g.ToList());
        var contractors = await _db.Contract.AsNoTracking().Where(c => c.ProjectId == projectId)
            .Select(c => new { c.CompanyId, Name = c.Company.BedrijfsNaam }).Distinct().OrderBy(c => c.Name).ToListAsync();
        var names = contractors.ToDictionary(c => c.CompanyId, c => c.Name);
        var missing = issues.Where(i => i.ResponsiblePartyId.HasValue && !names.ContainsKey(i.ResponsiblePartyId.Value)).Select(i => i.ResponsiblePartyId!.Value).Distinct().ToList();
        if (missing.Count > 0)
            foreach (var c in await _db.CompanyInfo.AsNoTracking().Where(c => missing.Contains(c.CompanyId)).Select(c => new { c.CompanyId, c.BedrijfsNaam }).ToListAsync())
                names[c.CompanyId] = c.BedrijfsNaam;

        var verslagLinks = await _db.WerfverslagPunt.AsNoTracking().Where(w => ids.Contains(w.IssueId)).Select(w => new { w.IssueId, w.VerslagId }).ToListAsync();
        var verslagBy = verslagLinks.GroupBy(w => w.IssueId).ToDictionary(g => g.Key, g => string.Join(",", g.Select(x => x.VerslagId)));
        var verslagen = await _db.Werfverslag.AsNoTracking().Where(v => v.ProjectId == projectId).OrderByDescending(v => v.Datum).ThenByDescending(v => v.Id).Select(v => new { v.Id, v.Naam, v.Status }).ToListAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var rows = issues.Select(i =>
        {
            var closed = PuntenWeergave.IsClosed(i.Status);
            photoBy.TryGetValue(i.Id, out var ph);
            return new PuntRowV2
            {
                Id = i.Id,
                Nr = i.PuntNr ?? i.Id,
                Title = PuntenWeergave.Titel(i.Title),
                Priority = i.Priority,
                HasPlan = i.PlanDocumentId.HasValue,
                UnitId = i.UnitId,
                UnitName = i.Unit?.Name ?? "Algemeen",
                Zone = PuntenWeergave.Zone(i.RoomOrZone),
                Phase = i.IssuePhase,
                PhaseLabel = GetEnumDisplayName<ConstructionIssuePhase>(i.IssuePhase),
                ContractorId = i.ResponsiblePartyId,
                Contractor = PuntenWeergave.Aannemer(i.ResponsiblePartyId.HasValue && names.TryGetValue(i.ResponsiblePartyId.Value, out var n) ? n : (i.ResponsibleOtherName ?? "")),
                Status = i.Status,
                StatusLabel = PuntenWeergave.Label(i.Status),
                StatusTone = PuntenWeergave.Tone(i.Status),
                IsClosed = closed,
                DueDate = i.DueDate,
                Overdue = !closed && i.Status != (int)ConstructionIssueStatus.OnHold && i.DueDate.HasValue && i.DueDate.Value < today,
                Sent = sent.GetValueOrDefault(i.Id),
                VerslagIds = verslagBy.GetValueOrDefault(i.Id) ?? "",
                PhotoCount = ph?.Count ?? 0,
                FirstPhotoUrl = ph is { Count: > 0 } ? GetSignedAssetUrlByFileName(ph[0].FileId, "pictures") : null,
            };
        }).OrderByDescending(r => r.Nr).ToList();

        var units = await _db.Units.AsNoTracking().Where(u => u.ProjectId == projectId).OrderBy(u => u.Name).Select(u => new KeyValuePair<int, string>(u.Id, u.Name)).ToListAsync();
        var openPer = rows.Where(r => !r.IsClosed && r.ContractorId.HasValue).GroupBy(r => r.ContractorId!.Value).ToDictionary(g => g.Key, g => g.Count());
        return new PuntenIndexV2Vm
        {
            ProjectId = projectId,
            ProjectName = projectName,
            CanWrite = ps.HasWrite(PermissionCodes.ProjectsIssues),
            CanDelete = ps.HasDelete(PermissionCodes.ProjectsIssues),
            Rows = rows,
            Units = units,
            Contractors = rows.Where(r => r.ContractorId.HasValue).Select(r => new KeyValuePair<int, string>(r.ContractorId!.Value, r.Contractor)).DistinctBy(k => k.Key).OrderBy(k => k.Value).ToList(),
            ProjectContractors = contractors.Select(c => new KeyValuePair<int, string>(c.CompanyId, PuntenWeergave.Aannemer(c.Name))).ToList(),
            OpenPerContractor = openPer,
            Phases = Enum.GetValues<ConstructionIssuePhase>().Select(p => new KeyValuePair<int, string>((int)p, GetEnumDisplayName<ConstructionIssuePhase>((int)p))).ToList(),
            Today = today,
            PlanCount = (await LoadPlans(projectId)).Count,
            Verslagen = verslagen.Select(v => (v.Id, v.Naam, v.Status == 0)).ToList(),
            Categories = (await _service.GetCategories()).Select(c => new KeyValuePair<int, string>(c.Id, c.Name)).ToList(),
            Types = Enum.GetValues<ConstructionIssueType>().Select(t => new KeyValuePair<int, string>((int)t, GetEnumDisplayName<ConstructionIssueType>((int)t))).ToList(),
            RecentZones = issues.Where(i => !string.IsNullOrWhiteSpace(i.RoomOrZone)).OrderByDescending(i => i.CreatedDate).Select(i => PuntenWeergave.Zone(i.RoomOrZone)).Distinct(StringComparer.OrdinalIgnoreCase).Take(12).ToList(),
        };
    }

    private async Task<PuntDetailV2Vm> BuildDetailV2(int projectId, ConstructionIssue issue)
    {
        var ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        var project = await _db.Project.AsNoTracking().Where(p => p.ProjectId == projectId).Select(p => p.ProjectName).FirstOrDefaultAsync() ?? $"Project {projectId}";
        var media = await _service.GetMedia(projectId, issue.Id);
        var notifications = await _service.GetNotifications(projectId, issue.Id);

        string contractor = issue.ResponsibleOtherName ?? "", contact = "", email = issue.ResponsibleOtherEmail ?? "", phone = "";
        if (issue.ResponsiblePartyId.HasValue)
        {
            var contract = await _db.Contract.AsNoTracking().Include(c => c.Company).Include(c => c.SiteManagerContact)
                .FirstOrDefaultAsync(c => c.ProjectId == projectId && c.CompanyId == issue.ResponsiblePartyId.Value);
            var company = contract?.Company ?? await _db.CompanyInfo.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyId == issue.ResponsiblePartyId.Value);
            contractor = company?.BedrijfsNaam ?? contractor;
            var sm = contract?.SiteManagerContact;
            contact = sm != null ? $"{sm.ContactVoornaam} {sm.ContactNaam}".Trim() : "";
            email = !string.IsNullOrWhiteSpace(sm?.Email) ? sm!.Email : (company?.Email ?? email);
            phone = sm?.Gsm ?? sm?.Telefoon ?? "";
        }

        // Auteurs van de historiek: intern of aannemer (portaalgebruiker; beide in Users)
        var userKeys = issue.ConstructionIssueHistory.Where(h => h.UserId != null).Select(h => h.UserId!).Distinct().ToList();
        var userIntKeys = userKeys.Where(k => int.TryParse(k, out _)).Select(int.Parse).ToList();
        var users = await _db.Users.AsNoTracking().Where(u => userKeys.Contains(u.UserId) || userIntKeys.Contains(u.Id)).Select(u => new { u.UserId, u.Id, Name = (u.Voornaam + " " + u.Familienaam).Trim() }).ToListAsync();
        var userIntIds = users.Select(u => u.Id).ToList();
        var contractorUserIds = (await _db.UserCompanyAccess.AsNoTracking().Where(a => userIntIds.Contains(a.UserId)).Select(a => a.UserId).Distinct().ToListAsync()).ToHashSet();
        static string Initials(string n) => string.Concat(n.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpperInvariant(p[0])));

        var history = new List<PuntHistoriekItemV2>();
        foreach (var h in issue.ConstructionIssueHistory.OrderBy(h => h.Timestamp))
        {
            var u = users.FirstOrDefault(x => x.UserId == h.UserId || (h.UserId != null && x.Id.ToString() == h.UserId));
            var author = !string.IsNullOrEmpty(u?.Name) ? u!.Name : "Systeem";
            var item = new PuntHistoriekItemV2 { Timestamp = h.Timestamp.ToLocalTime(), Author = author, Initials = Initials(author), IsInternal = h.IsInternal, FromContractor = u != null && contractorUserIds.Contains(u.Id) };
            switch ((ConstructionIssueHistoryAction)h.Action)
            {
                case ConstructionIssueHistoryAction.CommentAdded:
                    item.Kind = "message"; item.Text = h.Comment ?? ""; break;
                case ConstructionIssueHistoryAction.StatusChanged:
                    item.Kind = "status";
                    item.Text = h.Comment ?? "";
                    if (int.TryParse(h.NewValueJson, out var ns)) { item.StatusLabel = PuntenWeergave.Label(ns); item.StatusTone = PuntenWeergave.Tone(ns); }
                    break;
                case ConstructionIssueHistoryAction.Created: item.Text = "Punt aangemaakt"; break;
                case ConstructionIssueHistoryAction.SentToResponsible: item.Text = "Doorgestuurd naar " + contractor; break;
                case ConstructionIssueHistoryAction.ReminderSent: item.Text = "Herinnering verstuurd"; break;
                case ConstructionIssueHistoryAction.Assigned: item.Text = "Aannemer gewijzigd"; break;
                case ConstructionIssueHistoryAction.AttachmentAdded: item.Text = "Foto toegevoegd"; break;
                case ConstructionIssueHistoryAction.AttachmentRemoved: item.Text = "Foto verwijderd"; break;
                default: continue; // Updated / PriorityChanged: ruis, niet tonen
            }
            history.Add(item);
        }

        var nr = "P-" + (issue.PuntNr ?? issue.Id).ToString("000");
        PuntPlanV2? plan = null;
        if (issue.PlanDocumentId.HasValue && issue.PlanXnormalized.HasValue && issue.PlanYnormalized.HasValue)
        {
            var planKey = issue.UnitId.HasValue ? $"u{issue.UnitId}:{issue.PlanDocumentId}" : "a:" + issue.PlanDocumentId;
            plan = (await LoadPlans(projectId)).FirstOrDefault(p => p.Key == planKey);
        }
        var home = new MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var projecten = new MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = home };
        var projectNode = new MvcBreadcrumbNode("Detail", "Projecten", project) { Parent = projecten };
        var puntenNode = new MvcBreadcrumbNode("Index", "ProjectsIssues", "Punten") { Parent = projectNode, RouteValues = new { projectId } };
        ViewData["BreadcrumbNode"] = new MvcBreadcrumbNode("Details", "ProjectsIssues", nr) { Parent = puntenNode, RouteValues = new { projectId, id = issue.Id } };
        contractor = PuntenWeergave.Aannemer(contractor);
        var zone = PuntenWeergave.Zone(issue.RoomOrZone);
        var unit = issue.Unit?.Name ?? "Algemeen";
        var created = issue.CreatedDate.ToLocalTime();
        return new PuntDetailV2Vm
        {
            ProjectId = projectId,
            ProjectName = project,
            CanWrite = ps.HasWrite(PermissionCodes.ProjectsIssues),
            Id = issue.Id,
            NrLabel = nr,
            Title = PuntenWeergave.Titel(issue.Title),
            Description = issue.Description,
            Status = issue.Status,
            StatusLabel = PuntenWeergave.Label(issue.Status),
            StatusTone = PuntenWeergave.Tone(issue.Status),
            Priority = issue.Priority,
            PriorityLabel = GetEnumDisplayName<ConstructionIssuePriority>(issue.Priority),
            Subtitle = $"{unit}{(zone == "" ? "" : " · " + zone)} · ingegeven {created:dd/MM}",
            UnitName = unit,
            Zone = zone,
            PhaseLabel = GetEnumDisplayName<ConstructionIssuePhase>(issue.IssuePhase),
            CategoryName = issue.Category?.Name ?? "",
            TypeLabel = GetEnumDisplayName<ConstructionIssueType>(issue.IssueType),
            DueDate = issue.DueDate,
            DuePaused = issue.Status == (int)ConstructionIssueStatus.OnHold,
            OnHoldSince = issue.OnHoldSince?.ToLocalTime(),
            OnHoldReason = issue.OnHoldReason,
            FollowUpDate = issue.FollowUpDate,
            ContractorName = contractor,
            ContractorContact = contact,
            ContractorEmail = email,
            ContractorPhone = phone,
            Photos = media.Select(m => (m.Id, GetSignedAssetUrlByFileName(m.FileId, "pictures") ?? "")).Where(m => m.Item2.Length > 0).ToList(),
            HasPlan = plan != null,
            PlanName = plan == null ? null : (plan.UnitId == null ? plan.Name : plan.UnitName + " · " + plan.Name),
            PlanUrl = plan?.Url,
            PlanPage = issue.PlanPageNumber ?? 1,
            PlanX = (double)(issue.PlanXnormalized ?? 0),
            PlanY = (double)(issue.PlanYnormalized ?? 0),
            History = history,
            Reminders = notifications.Count,
            NextStatuses = PuntenWeergave.Next(issue.Status).Select(s => ((int)s, PuntenWeergave.Label((int)s))).ToList(),
        };
    }

    /// <summary>Snel ingeven (40c): enkel titel, eenheid, zone en aannemer zijn nodig; de rest staat onder "Meer". Het punt wordt Concept.</summary>
    [HttpPost("CreateV2")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateV2(int projectId, string? title, int? unitId, string? zone, int? contractorId, string? description,
        int? categoryId, int? issueType, int? phase, int? priority, DateOnly? dueDate, List<IFormFile>? mediaFiles,
        int? planId, int? planPage, decimal? planX, decimal? planY, bool approve = false)
    {
        if (string.IsNullOrWhiteSpace(title)) return Json(new { ok = false, error = "Geef een titel in." });
        var catId = categoryId ?? (await _service.GetCategories()).FirstOrDefault()?.Id ?? 0;
        if (catId == 0) return Json(new { ok = false, error = "Er is nog geen categorie aangemaakt (Instellingen)." });

        string unitName = "Algemeen";
        if (unitId is > 0)
        {
            unitName = await _db.Units.Where(u => u.Id == unitId && u.ProjectId == projectId).Select(u => u.Name).FirstOrDefaultAsync() ?? "";
            if (unitName == "") return Json(new { ok = false, error = "Eenheid niet gevonden." });
        }
        var dto = new ConstructionIssueUpsertBO
        {
            Title = PuntenWeergave.Titel(title),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            UnitId = unitId is > 0 ? unitId : null,
            RoomOrZone = string.IsNullOrWhiteSpace(zone) ? null : PuntenWeergave.Zone(zone),
            LocationText = unitName + (string.IsNullOrWhiteSpace(zone) ? "" : " · " + PuntenWeergave.Zone(zone)),
            CategoryId = catId,
            IssueType = issueType ?? 0,
            IssuePhase = phase ?? 0,
            Priority = priority ?? (int)ConstructionIssuePriority.Normal,
            Status = approve ? (int)ConstructionIssueStatus.Approved : (int)ConstructionIssueStatus.Concept,
            ResponsiblePartyType = (int)ConstructionIssueResponsiblePartyType.Contractor,
            ResponsiblePartyId = contractorId is > 0 ? contractorId : null,
            DueDate = dueDate,
        };
        if (planX is >= 0 and <= 1 && planY is >= 0 and <= 1 && planId.HasValue)
        {
            if (!await PlanBelongs(projectId, unitId is > 0 ? unitId : null, planId.Value)) return Json(new { ok = false, error = "Dat plan hoort niet bij deze eenheid." });
            dto.PlanDocumentId = planId; dto.PlanPageNumber = planPage ?? 1; dto.PlanXNormalized = planX; dto.PlanYNormalized = planY;
        }
        var created = await _service.Create(projectId, dto, User.FindFirst(CpmClaims.UserId)?.Value);
        await AddIssueMedia(projectId, created.Id, ResolveMediaFiles(mediaFiles));
        return Json(new { ok = true, id = created.Id, nr = "P-" + (created.PuntNr ?? created.Id).ToString("000") });
    }

    /// <summary>Bewerken (zelfde paneel als snel ingeven): enkel de velden uit het paneel veranderen, status/plan/historiek blijven.</summary>
    [HttpPost("EditV2/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditV2(int projectId, int id, string? title, int? unitId, string? zone, int? contractorId, string? description,
        int? categoryId, int? issueType, int? phase, int? priority, DateOnly? dueDate, List<IFormFile>? mediaFiles,
        int? planId, int? planPage, decimal? planX, decimal? planY, bool clearPlan = false)
    {
        if (string.IsNullOrWhiteSpace(title)) return Json(new { ok = false, error = "Geef een titel in." });
        var issue = await _service.GetById(projectId, id);
        if (issue == null) return Json(new { ok = false, error = "Punt niet gevonden." });

        string unitName = "Algemeen";
        if (unitId is > 0)
        {
            unitName = await _db.Units.Where(u => u.Id == unitId && u.ProjectId == projectId).Select(u => u.Name).FirstOrDefaultAsync() ?? "";
            if (unitName == "") return Json(new { ok = false, error = "Eenheid niet gevonden." });
        }
        var z = PuntenWeergave.Zone(zone);
        var dto = new ConstructionIssueUpsertBO
        {
            Title = PuntenWeergave.Titel(title),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            LocationText = unitName + (z == "" ? "" : " · " + z),
            CategoryId = categoryId ?? issue.CategoryId,
            BuildingPart = issue.BuildingPart,
            RoomOrZone = z == "" ? null : z,
            UnitId = unitId is > 0 ? unitId : null,
            IssueType = issueType ?? issue.IssueType,
            IssuePhase = phase ?? issue.IssuePhase,
            Priority = priority ?? issue.Priority,
            Status = issue.Status,
            ResponsiblePartyType = issue.ResponsiblePartyType,
            ResponsiblePartyId = contractorId is > 0 ? contractorId : null,
            ResponsibleOtherName = contractorId is > 0 ? null : issue.ResponsibleOtherName,
            ResponsibleOtherEmail = contractorId is > 0 ? null : issue.ResponsibleOtherEmail,
            DueDate = dueDate,
            PlannedDate = issue.PlannedDate,
            PlanDocumentId = issue.PlanDocumentId,
            PlanPageNumber = issue.PlanPageNumber,
            PlanXNormalized = issue.PlanXnormalized,
            PlanYNormalized = issue.PlanYnormalized,
        };
        if (clearPlan) { dto.PlanDocumentId = null; dto.PlanPageNumber = null; dto.PlanXNormalized = null; dto.PlanYNormalized = null; }
        else if (planX is >= 0 and <= 1 && planY is >= 0 and <= 1 && planId.HasValue)
        {
            if (!await PlanBelongs(projectId, unitId is > 0 ? unitId : null, planId.Value)) return Json(new { ok = false, error = "Dat plan hoort niet bij deze eenheid." });
            dto.PlanDocumentId = planId; dto.PlanPageNumber = planPage ?? 1; dto.PlanXNormalized = planX; dto.PlanYNormalized = planY;
        }
        await _service.Update(projectId, id, dto, User.FindFirst(CpmClaims.UserId)?.Value);
        await AddIssueMedia(projectId, id, ResolveMediaFiles(mediaFiles));
        return Json(new { ok = true, id });
    }

    // ── Plannen (40b): per eenheid + algemene plannen van het project ─────────────────────────────────────

    /// <summary>Enkel uitvoeringsplannen (UnitExecutionPlan); een algemeen plan heeft geen eenheid.</summary>
    private async Task<bool> PlanBelongs(int projectId, int? unitId, int planId)
    {
        if (unitId.HasValue)
        {
            var unit = await _db.Units.AsNoTracking().FirstOrDefaultAsync(u => u.Id == unitId && u.ProjectId == projectId);
            if (unit == null) return false;
            if (planId == 0) return false; // het verkoopplan (Units.Plan) is geen uitvoeringsplan
            return await _db.UnitExecutionPlan.AnyAsync(p => p.Id == planId && p.UnitId == unitId && p.DeletedDate == null);
        }
        return planId != 0 && await _db.UnitExecutionPlan.AnyAsync(p => p.Id == planId && p.UnitId == null && p.ProjectId == projectId && p.DeletedDate == null);
    }

    private async Task<List<PuntPlanV2>> LoadPlans(int projectId)
    {
        var units = await _db.Units.AsNoTracking().Where(u => u.ProjectId == projectId).OrderBy(u => u.Name).ToListAsync();
        var unitIds = units.Select(u => u.Id).ToList();
        var rows = await _db.UnitExecutionPlan.AsNoTracking()
            .Where(p => p.DeletedDate == null && ((p.UnitId != null && unitIds.Contains(p.UnitId.Value)) || (p.UnitId == null && p.ProjectId == projectId)))
            .OrderBy(p => p.Name).ToListAsync();
        string? Proxy(string fileId) => Url.Action(nameof(PlanContent), "ProjectsIssues", new { projectId, fileId });
        var list = new List<PuntPlanV2>();
        foreach (var r in rows.Where(r => r.UnitId == null))
            list.Add(new PuntPlanV2 { Key = "a:" + r.Id, UnitId = null, UnitName = "Algemeen", PlanId = r.Id, Name = r.Name, Url = Proxy(r.FileId) ?? "" });
        foreach (var u in units)
        {
            // Enkel uitvoeringsplannen (UnitExecutionPlan). Units.Plan is het verkoopplan en hoort hier niet bij.
            foreach (var r in rows.Where(r => r.UnitId == u.Id))
                list.Add(new PuntPlanV2 { Key = $"u{u.Id}:{r.Id}", UnitId = u.Id, UnitName = u.Name, PlanId = r.Id, Name = r.Name, Url = Proxy(r.FileId) ?? "" });
        }
        return list;
    }

    /// <summary>Alle plannen van het project (voor de plankiezer in het paneel).</summary>
    [HttpGet("PlansV2")]
    public async Task<IActionResult> PlansV2(int projectId) => Json((await LoadPlans(projectId)).Select(p => new { p.Key, p.UnitId, p.UnitName, p.PlanId, p.Name, p.Url }));

    [HttpPost("UploadPlanV2")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPlanV2(int projectId, int? unitId, string? planName, IFormFile? planFile)
    {
        if (planFile == null || planFile.Length == 0) return Json(new { ok = false, error = "Kies een PDF-bestand." });
        if (!string.Equals(Path.GetExtension(planFile.FileName), ".pdf", StringComparison.OrdinalIgnoreCase)) return Json(new { ok = false, error = "Enkel PDF-bestanden zijn toegelaten." });
        if (unitId is > 0 && !await _db.Units.AnyAsync(u => u.Id == unitId && u.ProjectId == projectId)) return Json(new { ok = false, error = "Eenheid niet gevonden." });
        if (!await _db.Project.AnyAsync(p => p.ProjectId == projectId)) return Json(new { ok = false, error = "Project niet gevonden." });

        var fileId = await UploadAssetToStorageAsync(planFile, "plans");
        if (string.IsNullOrWhiteSpace(fileId)) return Json(new { ok = false, error = "Uploaden van het plan is mislukt." });
        var plan = new UnitExecutionPlan
        {
            UnitId = unitId is > 0 ? unitId : null,
            ProjectId = unitId is > 0 ? null : projectId,
            Name = string.IsNullOrWhiteSpace(planName) ? Path.GetFileNameWithoutExtension(planFile.FileName) : planName.Trim(),
            FileId = fileId, CreatedDate = DateTime.UtcNow, CreatedByUserId = User.FindFirst(CpmClaims.UserId)?.Value
        };
        _db.UnitExecutionPlan.Add(plan);
        await _db.SaveChangesAsync();
        return Json(new { ok = true, key = unitId is > 0 ? $"u{unitId}:{plan.Id}" : "a:" + plan.Id });
    }

    /// <summary>Op plan (40b): plannen links, het plan met pins in statuskleur, detail van de gekozen pin rechts.</summary>
    [HttpGet("Plan")]
    public async Task<IActionResult> PlanV2(int projectId, string? key, int? issue)
    {
        var projectName = await _db.Project.Where(x => x.ProjectId == projectId).Select(x => x.ProjectName).FirstOrDefaultAsync() ?? $"Project {projectId}";
        SetPuntenBreadcrumb(projectId, projectName, "Op plan", nameof(PlanV2));
        var idx = await BuildIndexV2(projectId, projectName);
        var plans = await LoadPlans(projectId);
        var pins = await _db.ConstructionIssue.AsNoTracking().Include(i => i.Unit)
            .Where(i => i.ProjectId == projectId && i.PlanDocumentId != null && i.PlanXnormalized != null && i.PlanYnormalized != null).ToListAsync();
        var photos = await _db.ConstructionIssueMedia.AsNoTracking().Where(m => pins.Select(p => p.Id).Contains(m.IssueId)).OrderBy(m => m.Id).ToListAsync();
        var rowBy = idx.Rows.ToDictionary(r => r.Id);
        var vm = new PuntenPlanV2Vm { Index = idx, Plans = plans };
        foreach (var i in pins)
        {
            var planKey = i.UnitId.HasValue ? $"u{i.UnitId}:{i.PlanDocumentId}" : "a:" + i.PlanDocumentId;
            if (!plans.Any(p => p.Key == planKey) || !rowBy.TryGetValue(i.Id, out var row)) continue;
            var ph = photos.Where(m => m.IssueId == i.Id).Select(m => GetSignedAssetUrlByFileName(m.FileId, "pictures")).Where(u => u != null).ToList();
            vm.Pins.Add(new PuntPinV2
            {
                Id = i.Id, Nr = row.NrLabel, Title = row.Title, PlanKey = planKey, Page = i.PlanPageNumber ?? 1, X = (double)i.PlanXnormalized!.Value, Y = (double)i.PlanYnormalized!.Value,
                Status = PuntenWeergave.Canon(i.Status), StatusLabel = row.StatusLabel, StatusTone = row.StatusTone, Closed = row.IsClosed,
                Unit = row.UnitName, Zone = row.Zone, Contractor = row.Contractor, Due = i.DueDate?.ToString("dd/MM/yyyy"), Photos = ph!,
                Url = Url.Action(nameof(Details), "ProjectsIssues", new { projectId, id = i.Id })!,
            });
        }
        vm.SelectedKey = plans.Any(p => p.Key == key) ? key
            : issue.HasValue ? vm.Pins.FirstOrDefault(p => p.Id == issue)?.PlanKey
            : plans.OrderByDescending(p => vm.Pins.Count(x => x.PlanKey == p.Key)).FirstOrDefault()?.Key;
        vm.SelectedIssueId = issue;
        return View("PlanV2", vm);
    }

    // ── Goedkeuren & doorsturen (40d) ───────────────────────────────────────────────────────────────

    private static readonly int[] SendNewStatuses = { (int)ConstructionIssueStatus.Concept, (int)ConstructionIssueStatus.PendingApproval, (int)ConstructionIssueStatus.Approved };
    // wat de aannemer nog moet doen: doorgestuurd (ook legacy open), afgewezen. Gemeld uitgevoerd / in de wacht / afgesloten gaan niet mee.
    private static readonly int[] SendOpenStatuses = { (int)ConstructionIssueStatus.Forwarded, (int)ConstructionIssueStatus.Rejected, 0, 1, 2, 7 };

    private static string SendGroupKey(ConstructionIssue i) => i.ResponsiblePartyId.HasValue ? "c" + i.ResponsiblePartyId.Value : "e" + (i.ResponsibleOtherEmail ?? "").Trim().ToLowerInvariant();

    private async Task<Dictionary<int, (string Name, string? Email)>> ContractorContacts(int projectId, IEnumerable<int> companyIds)
    {
        var ids = companyIds.Distinct().ToList();
        var result = new Dictionary<int, (string, string?)>();
        if (ids.Count == 0) return result;
        var companies = await _db.CompanyInfo.AsNoTracking().Where(c => ids.Contains(c.CompanyId)).Select(c => new { c.CompanyId, c.BedrijfsNaam, c.Email }).ToListAsync();
        var contacts = await _db.Contract.AsNoTracking().Where(c => c.ProjectId == projectId && ids.Contains(c.CompanyId) && c.SiteManagerContactId != null)
            .Select(c => new { c.CompanyId, c.SiteManagerContact!.Email }).ToListAsync();
        var contactBy = contacts.GroupBy(c => c.CompanyId).ToDictionary(g => g.Key, g => g.First().Email);
        foreach (var c in companies)
        {
            contactBy.TryGetValue(c.CompanyId, out var ce);
            var email = !string.IsNullOrWhiteSpace(ce) ? ce : c.Email;
            result[c.CompanyId] = (string.IsNullOrWhiteSpace(c.BedrijfsNaam) ? "Bedrijf #" + c.CompanyId : c.BedrijfsNaam, email);
        }
        return result;
    }

    [HttpGet("Send")]
    public async Task<IActionResult> SendV2(int projectId, List<int>? ids)
    {
        var ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        var project = await _db.Project.AsNoTracking().Where(p => p.ProjectId == projectId).Select(p => p.ProjectName).FirstOrDefaultAsync() ?? $"Project {projectId}";
        SetPuntenBreadcrumb(projectId, project, "Goedkeuren & doorsturen", nameof(SendV2));
        var eligible = SendNewStatuses.Concat(SendOpenStatuses).ToArray();
        var issues = await _db.ConstructionIssue.AsNoTracking().Include(i => i.Unit)
            .Where(i => i.ProjectId == projectId && eligible.Contains(i.Status)).OrderBy(i => i.PuntNr).ToListAsync();
        var only = (ids ?? new List<int>()).Distinct().ToList();
        if (only.Count > 0) issues = issues.Where(i => only.Contains(i.Id)).ToList();
        var contacts = await ContractorContacts(projectId, issues.Where(i => i.ResponsiblePartyId.HasValue).Select(i => i.ResponsiblePartyId!.Value));

        var vm = new PuntenSendV2Vm
        {
            ProjectId = projectId, ProjectName = project, CanWrite = ps.HasWrite(PermissionCodes.ProjectsIssues), OnlyIds = only,
            PendingApproval = issues.Count(i => i.Status == (int)ConstructionIssueStatus.PendingApproval),
            OpenAtContractors = issues.Count(i => SendOpenStatuses.Contains(i.Status)),
            WithoutContractor = issues.Count(i => !i.ResponsiblePartyId.HasValue && string.IsNullOrWhiteSpace(i.ResponsibleOtherEmail)),
        };
        foreach (var g in issues.Where(i => i.ResponsiblePartyId.HasValue || !string.IsNullOrWhiteSpace(i.ResponsibleOtherEmail)).GroupBy(SendGroupKey))
        {
            var first = g.First();
            string name = first.ResponsibleOtherName ?? "Onbekend"; string? email = first.ResponsibleOtherEmail;
            if (first.ResponsiblePartyId.HasValue && contacts.TryGetValue(first.ResponsiblePartyId.Value, out var c)) { name = c.Name; email = c.Email; }
            vm.Groups.Add(new PuntSendGroupV2
            {
                Key = g.Key, Name = PuntenWeergave.Aannemer(name), Email = string.IsNullOrWhiteSpace(email) ? null : email,
                Issues = g.Select(i => new PuntSendIssueV2
                {
                    Id = i.Id, Nr = "P-" + (i.PuntNr ?? i.Id).ToString("000"), Title = PuntenWeergave.Titel(i.Title),
                    Where = (i.Unit?.Name ?? "Algemeen") + (string.IsNullOrWhiteSpace(i.RoomOrZone) ? "" : " · " + PuntenWeergave.Zone(i.RoomOrZone)),
                    Due = i.DueDate?.ToString("dd/MM"), IsNew = SendNewStatuses.Contains(i.Status)
                }).ToList()
            });
        }
        vm.Groups = vm.Groups.OrderBy(g => g.Name).ToList();
        return View("SendV2", vm);
    }

    [HttpPost("Send")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendV2Post(int projectId, string scope, List<string>? groups, string? message, List<int>? issueIds)
    {
        var userId = User.FindFirst(CpmClaims.UserId)?.Value;
        var keys = (groups ?? new List<string>()).ToHashSet();
        if (keys.Count == 0) { AddMessage("error", "Kies minstens één aannemer.", "Fout"); return RedirectToAction(nameof(SendV2), new { projectId }); }

        var statuses = scope switch
        {
            "new" => SendNewStatuses,
            "approved" => SendOpenStatuses,
            _ => SendNewStatuses.Concat(SendOpenStatuses).ToArray(),
        };
        var issues = await _db.ConstructionIssue.Where(i => i.ProjectId == projectId && statuses.Contains(i.Status)).ToListAsync();
        issues = issues.Where(i => keys.Contains(SendGroupKey(i))).ToList();
        if (issueIds is { Count: > 0 }) issues = issues.Where(i => issueIds.Contains(i.Id)).ToList();

        // Zonder e-mailadres kan niets verstuurd én dus ook niet goedgekeurd worden.
        var contacts = await ContractorContacts(projectId, issues.Where(i => i.ResponsiblePartyId.HasValue).Select(i => i.ResponsiblePartyId!.Value));
        bool HasEmail(ConstructionIssue i) => i.ResponsiblePartyId.HasValue
            ? contacts.TryGetValue(i.ResponsiblePartyId.Value, out var c) && !string.IsNullOrWhiteSpace(c.Email)
            : !string.IsNullOrWhiteSpace(i.ResponsibleOtherEmail);
        var skipped = issues.Count(i => !HasEmail(i));
        issues = issues.Where(HasEmail).ToList();
        if (issues.Count == 0) { AddMessage("error", "Geen punten verzonden: geen van de gekozen aannemers heeft een e-mailadres.", "Fout"); return RedirectToAction(nameof(SendV2), new { projectId }); }

        // Goedkeuren: Concept / Ter goedkeuring → Doorgestuurd, vóór het mailen zodat de statussen in de mail kloppen.
        foreach (var i in issues.Where(i => SendNewStatuses.Contains(i.Status)))
            await _service.ChangeStatus(projectId, i.Id, (int)ConstructionIssueStatus.Forwarded, "Goedgekeurd en doorgestuurd", userId);

        var ids = issues.Select(i => i.Id).ToList();
        var contractorCount = issues.Select(SendGroupKey).Distinct().Count();
        var sent = await _reportService.SendSelectedIssues(projectId, new ConstructionIssueSendRequestBO { IssueIds = ids, GroupByResponsible = true, ReportType = 0 }, userId, message);
        await InviteContractorPortalUsers(projectId, ids);

        AddMessage(sent > 0 ? "success" : "error",
            sent > 0 ? $"{sent} punt(en) goedgekeurd en doorgestuurd naar {contractorCount} aannemer(s)." + (skipped > 0 ? $" {skipped} punt(en) zonder e-mailadres zijn niet verstuurd." : "") : "Geen punten verzonden.",
            sent > 0 ? "Geslaagd" : "Fout");
        return RedirectToAction(nameof(Index), new { projectId });
    }

    /// <summary>Nodigt de portaalgebruikers van de aannemers uit als ze nog niet geregistreerd zijn (zoals de oude SendSelected).</summary>
    private async Task InviteContractorPortalUsers(int projectId, List<int> issueIds)
    {
        var appBaseUrl = $"{Request.Scheme}://{Request.Host}";
        var inviterUserId = User.GetCpmUserId();
        var companyIds = await _db.ConstructionIssue.Where(x => x.ProjectId == projectId && issueIds.Contains(x.Id) && x.ResponsiblePartyId != null).Select(x => x.ResponsiblePartyId!.Value).Distinct().ToListAsync();
        var portalUserIds = await _db.UserCompanyAccess.Where(a => companyIds.Contains(a.CompanyId)).Select(a => a.UserId).Distinct().ToListAsync();
        foreach (var portalUserId in portalUserIds)
        {
            try { await _inviteService.EnsureInvitedAsync(portalUserId, appBaseUrl, inviterUserId); }
            catch { /* een mislukte uitnodiging mag het versturen niet blokkeren */ }
        }
    }

    // ── Acties vanuit de gl-v2 pagina's ─────────────────────────────────────────────────────────────

    [HttpPost("HoldV2/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HoldV2(int projectId, int id, string? reason, DateOnly? followUp)
    {
        await _service.PutOnHold(projectId, id, reason, followUp, User.FindFirst(CpmClaims.UserId)?.Value);
        return RedirectToAction(nameof(Details), new { projectId, id });
    }

    [HttpPost("MessageV2/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MessageV2(int projectId, int id, string? text, bool internalOnly = true)
    {
        var userId = User.FindFirst(CpmClaims.UserId)?.Value;
        if (await _service.AddMessage(projectId, id, text ?? "", internalOnly, userId) && !internalOnly)
            AddMessage("info", "Bericht toegevoegd. De aannemer ziet het in zijn portaal.", "Bericht");
        return RedirectToAction(nameof(Details), new { projectId, id });
    }

    /// <summary>Selectiebalk (40n): meerdere punten in één keer naar een status, deadline of aannemer.</summary>
    [HttpPost("BulkV2")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkV2(int projectId, List<int> issueIds, string op, int? status, DateOnly? dueDate, int? contractorId, int? verslagId)
    {
        var userId = User.FindFirst(CpmClaims.UserId)?.Value;
        var dto = new ConstructionIssueBulkUpdateBO { IssueIds = issueIds ?? new List<int>() };
        switch (op)
        {
            case "status" when status.HasValue: dto.Status = status; break;
            case "approve": dto.Status = (int)ConstructionIssueStatus.Approved; break;
            case "deadline" when dueDate.HasValue: dto.DueDate = dueDate; break;
            case "contractor" when contractorId.HasValue: dto.ResponsiblePartyType = (int)ConstructionIssueResponsiblePartyType.Contractor; dto.ResponsiblePartyId = contractorId; break;
            case "verslag" when verslagId.HasValue:
                var verslag = await _db.Werfverslag.FirstOrDefaultAsync(v => v.Id == verslagId && v.ProjectId == projectId && v.Status == 0);
                if (verslag == null) { AddMessage("error", "Dat verslag is niet meer open.", "Fout"); return RedirectToAction(nameof(Index), new { projectId }); }
                var bestaand = await _db.WerfverslagPunt.Where(w => w.VerslagId == verslag.Id).Select(w => w.IssueId).ToListAsync();
                var toe = dto.IssueIds.Distinct().Where(i => !bestaand.Contains(i)).ToList();
                var geldig = await _db.ConstructionIssue.Where(i => i.ProjectId == projectId && toe.Contains(i.Id)).Select(i => i.Id).ToListAsync();
                foreach (var iid in geldig) _db.WerfverslagPunt.Add(new WerfverslagPunt { VerslagId = verslag.Id, IssueId = iid, IsNieuw = false });
                await _db.SaveChangesAsync();
                AddMessage("success", $"{geldig.Count} punt(en) toegevoegd aan {verslag.Naam}.", "Geslaagd");
                return RedirectToAction(nameof(Index), new { projectId });
            case "delete":
                var n = 0;
                foreach (var id in dto.IssueIds.Distinct()) if (await _service.Delete(projectId, id, userId)) n++;
                AddMessage("success", $"{n} punt(en) verwijderd.", "Geslaagd");
                return RedirectToAction(nameof(Index), new { projectId });
            default: return RedirectToAction(nameof(Index), new { projectId });
        }
        var updated = await _service.BulkUpdate(projectId, dto, userId);
        AddMessage("success", $"{updated} punt(en) bijgewerkt.", "Geslaagd");
        return RedirectToAction(nameof(Index), new { projectId });
    }
}

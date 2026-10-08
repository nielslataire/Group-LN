using BOCore;
using CPMCore.Helpers;
using CPMCore.Models.Issues;
using FacadeCore;
using DALCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Controllers;

// gl-v2 (design-handoff punt 40 "Punten"): lijst (40a), selectie (40n), detail (40o). Het legacy Index/Details blijft
// bestaan voor wie de gl-v2-preview uitzet (?classic=true).
public partial class ProjectsIssuesController
{
    private bool UseGlV2 => ViewData["UseGlV2Layout"] as bool? == true;

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

        var today = DateOnly.FromDateTime(DateTime.Today);
        var rows = issues.Select(i =>
        {
            var closed = PuntenWeergave.IsClosed(i.Status);
            photoBy.TryGetValue(i.Id, out var ph);
            return new PuntRowV2
            {
                Id = i.Id,
                Nr = i.PuntNr ?? i.Id,
                Title = i.Title ?? "",
                Priority = i.Priority,
                HasPlan = i.PlanDocumentId.HasValue,
                UnitId = i.UnitId,
                UnitName = i.Unit?.Name ?? "Algemeen",
                Zone = i.RoomOrZone ?? "",
                Phase = i.IssuePhase,
                PhaseLabel = GetEnumDisplayName<ConstructionIssuePhase>(i.IssuePhase),
                ContractorId = i.ResponsiblePartyId,
                Contractor = i.ResponsiblePartyId.HasValue && names.TryGetValue(i.ResponsiblePartyId.Value, out var n) ? n : (i.ResponsibleOtherName ?? ""),
                Status = i.Status,
                StatusLabel = PuntenWeergave.Label(i.Status),
                StatusTone = PuntenWeergave.Tone(i.Status),
                IsClosed = closed,
                DueDate = i.DueDate,
                Overdue = !closed && i.Status != (int)ConstructionIssueStatus.OnHold && i.DueDate.HasValue && i.DueDate.Value < today,
                Sent = sent.GetValueOrDefault(i.Id),
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
            ProjectContractors = contractors.Select(c => new KeyValuePair<int, string>(c.CompanyId, c.Name)).ToList(),
            OpenPerContractor = openPer,
            Phases = Enum.GetValues<ConstructionIssuePhase>().Select(p => new KeyValuePair<int, string>((int)p, GetEnumDisplayName<ConstructionIssuePhase>((int)p))).ToList(),
            Today = today,
            Categories = (await _service.GetCategories()).Select(c => new KeyValuePair<int, string>(c.Id, c.Name)).ToList(),
            Types = Enum.GetValues<ConstructionIssueType>().Select(t => new KeyValuePair<int, string>((int)t, GetEnumDisplayName<ConstructionIssueType>((int)t))).ToList(),
            RecentZones = issues.Where(i => !string.IsNullOrWhiteSpace(i.RoomOrZone)).OrderByDescending(i => i.CreatedDate).Select(i => i.RoomOrZone!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(12).ToList(),
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
        var users = await _db.Users.AsNoTracking().Where(u => userKeys.Contains(u.UserId)).Select(u => new { u.UserId, u.Id, Name = (u.Voornaam + " " + u.Familienaam).Trim() }).ToListAsync();
        var userIntIds = users.Select(u => u.Id).ToList();
        var contractorUserIds = (await _db.UserCompanyAccess.AsNoTracking().Where(a => userIntIds.Contains(a.UserId)).Select(a => a.UserId).Distinct().ToListAsync()).ToHashSet();
        static string Initials(string n) => string.Concat(n.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpperInvariant(p[0])));

        var history = new List<PuntHistoriekItemV2>();
        foreach (var h in issue.ConstructionIssueHistory.OrderBy(h => h.Timestamp))
        {
            var u = users.FirstOrDefault(x => x.UserId == h.UserId);
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
        var unit = issue.Unit?.Name ?? "Algemeen";
        var created = issue.CreatedDate.ToLocalTime();
        return new PuntDetailV2Vm
        {
            ProjectId = projectId,
            ProjectName = project,
            CanWrite = ps.HasWrite(PermissionCodes.ProjectsIssues),
            Id = issue.Id,
            NrLabel = nr,
            Title = issue.Title ?? "",
            Description = issue.Description,
            Status = issue.Status,
            StatusLabel = PuntenWeergave.Label(issue.Status),
            StatusTone = PuntenWeergave.Tone(issue.Status),
            Priority = issue.Priority,
            PriorityLabel = GetEnumDisplayName<ConstructionIssuePriority>(issue.Priority),
            Subtitle = $"{unit}{(string.IsNullOrWhiteSpace(issue.RoomOrZone) ? "" : " · " + issue.RoomOrZone)} · ingegeven {created:dd/MM}",
            UnitName = unit,
            Zone = issue.RoomOrZone ?? "",
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
            HasPlan = issue.PlanDocumentId.HasValue,
            History = history,
            Reminders = notifications.Count,
            NextStatuses = PuntenWeergave.Next(issue.Status).Select(s => ((int)s, PuntenWeergave.Label((int)s))).ToList(),
        };
    }

    /// <summary>Snel ingeven (40c): enkel titel, eenheid, zone en aannemer zijn nodig; de rest staat onder "Meer". Het punt wordt Concept.</summary>
    [HttpPost("CreateV2")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateV2(int projectId, string? title, int? unitId, string? zone, int? contractorId, string? description,
        int? categoryId, int? issueType, int? phase, int? priority, DateOnly? dueDate, List<IFormFile>? mediaFiles)
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
            Title = title.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            UnitId = unitId is > 0 ? unitId : null,
            RoomOrZone = string.IsNullOrWhiteSpace(zone) ? null : zone.Trim(),
            LocationText = unitName + (string.IsNullOrWhiteSpace(zone) ? "" : " · " + zone.Trim()),
            CategoryId = catId,
            IssueType = issueType ?? 0,
            IssuePhase = phase ?? 0,
            Priority = priority ?? (int)ConstructionIssuePriority.Normal,
            Status = (int)ConstructionIssueStatus.Concept,
            ResponsiblePartyType = (int)ConstructionIssueResponsiblePartyType.Contractor,
            ResponsiblePartyId = contractorId is > 0 ? contractorId : null,
            DueDate = dueDate,
        };
        var created = await _service.Create(projectId, dto, User.FindFirst(CpmClaims.UserId)?.Value);
        await AddIssueMedia(projectId, created.Id, ResolveMediaFiles(mediaFiles));
        return Json(new { ok = true, id = created.Id, nr = "P-" + (created.PuntNr ?? created.Id).ToString("000") });
    }

    // ── Goedkeuren & doorsturen (40d) ───────────────────────────────────────────────────────────────

    private static readonly int[] SendNewStatuses = { (int)ConstructionIssueStatus.Concept, (int)ConstructionIssueStatus.PendingApproval };
    // wat de aannemer nog moet doen: doorgestuurd (ook legacy open), afgewezen. Gemeld uitgevoerd / in de wacht / afgesloten gaan niet mee.
    private static readonly int[] SendOpenStatuses = { (int)ConstructionIssueStatus.Forwarded, (int)ConstructionIssueStatus.Rejected, 0, 1, 2, 7 };

    private static string SendGroupKey(ConstructionIssue i) => i.ResponsiblePartyId.HasValue ? "c" + i.ResponsiblePartyId.Value : "e" + (i.ResponsibleOtherEmail ?? "").Trim().ToLowerInvariant();

    private async Task<Dictionary<int, (string Name, string? Email)>> ContractorContacts(int projectId, IEnumerable<int> companyIds)
    {
        var ids = companyIds.Distinct().ToList();
        var result = new Dictionary<int, (string, string?)>();
        if (ids.Count == 0) return result;
        var companies = await _db.CompanyInfo.AsNoTracking().Where(c => ids.Contains(c.CompanyId)).Select(c => new { c.CompanyId, c.BedrijfsNaam, c.Email, c.InvoiceEmail }).ToListAsync();
        var contacts = await _db.Contract.AsNoTracking().Where(c => c.ProjectId == projectId && ids.Contains(c.CompanyId) && c.SiteManagerContactId != null)
            .Select(c => new { c.CompanyId, c.SiteManagerContact!.Email }).ToListAsync();
        var contactBy = contacts.GroupBy(c => c.CompanyId).ToDictionary(g => g.Key, g => g.First().Email);
        foreach (var c in companies)
        {
            contactBy.TryGetValue(c.CompanyId, out var ce);
            var email = !string.IsNullOrWhiteSpace(ce) ? ce : !string.IsNullOrWhiteSpace(c.InvoiceEmail) ? c.InvoiceEmail : c.Email;
            result[c.CompanyId] = (string.IsNullOrWhiteSpace(c.BedrijfsNaam) ? "Bedrijf #" + c.CompanyId : c.BedrijfsNaam, email);
        }
        return result;
    }

    [HttpGet("Send")]
    public async Task<IActionResult> SendV2(int projectId, List<int>? ids)
    {
        var ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        var project = await _db.Project.AsNoTracking().Where(p => p.ProjectId == projectId).Select(p => p.ProjectName).FirstOrDefaultAsync() ?? $"Project {projectId}";
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
                Key = g.Key, Name = name, Email = string.IsNullOrWhiteSpace(email) ? null : email,
                Issues = g.Select(i => new PuntSendIssueV2
                {
                    Id = i.Id, Nr = "P-" + (i.PuntNr ?? i.Id).ToString("000"), Title = i.Title ?? "",
                    Where = (i.Unit?.Name ?? "Algemeen") + (string.IsNullOrWhiteSpace(i.RoomOrZone) ? "" : " · " + i.RoomOrZone),
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
    public async Task<IActionResult> BulkV2(int projectId, List<int> issueIds, string op, int? status, DateOnly? dueDate, int? contractorId)
    {
        var userId = User.FindFirst(CpmClaims.UserId)?.Value;
        var dto = new ConstructionIssueBulkUpdateBO { IssueIds = issueIds ?? new List<int>() };
        switch (op)
        {
            case "status" when status.HasValue: dto.Status = status; break;
            case "approve": dto.Status = (int)ConstructionIssueStatus.Forwarded; break;
            case "deadline" when dueDate.HasValue: dto.DueDate = dueDate; break;
            case "contractor" when contractorId.HasValue: dto.ResponsiblePartyType = (int)ConstructionIssueResponsiblePartyType.Contractor; dto.ResponsiblePartyId = contractorId; break;
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

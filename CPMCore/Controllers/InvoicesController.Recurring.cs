using BOCore;
using CPMCore.Models.Invoicing;
using DALCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Invoicing;
using SmartBreadcrumbs.Nodes;
using System.Globalization;

namespace CPMCore.Controllers
{
    /// <summary>
    /// Terugkerende facturen (design-handoff punt 49 en 50, migratie 133): het overzicht (Invoices?view=recurring), de instellingen, het schemadetail en het
    /// bewaren/pauzeren/hervatten/stopzetten van een schema. De rekenregels staan in ServiceCore (RecurringScheduleCalculator, RecurringInvoiceService);
    /// het formulier zelf is de gewone Nieuwe factuur (Create, CreateV2) met "Terugkerend" gekozen. Views in Views/Invoices/Recurring/.
    /// </summary>
    public partial class InvoicesController
    {
        private static readonly CultureInfo Nl = CultureInfo.GetCultureInfo("nl-BE");

        // ───────────────────────── overzicht (50b) ─────────────────────────

        private sealed record BookyearScope(string Selected, DateOnly? From, DateOnly? To, List<string> Labels);

        /// <summary>Het gekozen boekjaar van de facturenlijst: "" = alle, anders het label ("2026" of "2025-2026") met zijn datumbereik. Zonder keuze het nieuwste boekjaar.</summary>
        private async Task<BookyearScope> ResolveBookyearScopeAsync(int issuerId, string? requested, CancellationToken ct)
        {
            var bookyears = await _db.OctopusBookyears.AsNoTracking().Where(b => b.IssuerCompanyId == issuerId).OrderByDescending(b => b.EndDate).ToListAsync(ct);
            var labels = bookyears.Select(b => FormatBookyearLabel(b.StartDate, b.EndDate)).ToList();
            var years = await _db.Invoices.AsNoTracking().Where(i => i.IssuerCompanyId == issuerId).Select(i => i.Date.Year).Distinct().ToListAsync(ct);
            // facturen buiten elk boekjaar vallen terug op hun kalenderjaar
            foreach (var y in years)
                if (!bookyears.Any(b => b.StartDate.Year <= y && y <= b.EndDate.Year) && !labels.Contains(y.ToString())) labels.Add(y.ToString());
            labels = labels.Distinct().OrderByDescending(l => l, StringComparer.Ordinal).ToList();

            string selected;
            if (string.Equals(requested, "alle", StringComparison.OrdinalIgnoreCase)) selected = "";
            else if (!string.IsNullOrWhiteSpace(requested) && labels.Contains(requested)) selected = requested;
            else selected = bookyears.Count > 0 ? FormatBookyearLabel(bookyears[0].StartDate, bookyears[0].EndDate) : (labels.FirstOrDefault() ?? DateTime.Today.Year.ToString());

            DateOnly? from = null, to = null;
            if (selected != "")
            {
                var match = bookyears.FirstOrDefault(b => FormatBookyearLabel(b.StartDate, b.EndDate) == selected);
                if (match != null) { from = DateOnly.FromDateTime(match.StartDate); to = DateOnly.FromDateTime(match.EndDate); }
                else if (int.TryParse(selected, out var y)) { from = new DateOnly(y, 1, 1); to = new DateOnly(y, 12, 31); }
            }
            return new BookyearScope(selected, from, to, labels);
        }

        private async Task<IActionResult> RecurringIndexAsync(int issuerCompanyId, bool canWrite, string? bookyear, CancellationToken ct)
        {
            var yearScope = await ResolveBookyearScopeAsync(issuerCompanyId, bookyear, ct);
            var today = DateOnly.FromDateTime(DateTime.Today);
            var healthIndex = await _health.GetAsync(ct);
            var schedules = await _db.RecurringInvoiceSchedule.AsNoTracking().Include(s => s.Lines)
                .Where(s => s.IssuerCompanyId == issuerCompanyId).ToListAsync(ct);
            var names = await ResolvePartyNamesAsync(schedules.Select(s => (s.PartyType, s.PartyId)), ct);
            var lastNumbers = (await _db.Invoices.AsNoTracking()
                    .Where(i => i.IssuerCompanyId == issuerCompanyId && i.RecurringScheduleId != null && i.PublicId != null && i.StatusId != InvoiceStatusIds.Draft)
                    .Select(i => new { i.RecurringScheduleId, i.Id, i.PublicId, i.Date })
                    .ToListAsync(ct))
                .GroupBy(i => i.RecurringScheduleId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(i => i.Date).ThenByDescending(i => i.Id).First());

            var rows = schedules.Select(s =>
            {
                names.TryGetValue((s.PartyType, s.PartyId), out var party);
                var first = s.Lines.OrderBy(l => l.SortOrder).FirstOrDefault();
                var pending = s.Status == RecurringInvoiceSchedule.StatusActive && s.CreatedCount == 0 && s.NextOccurrence.HasValue && s.NextOccurrence.Value.AddDays(-s.DraftLeadDays) > today;
                var key = s.Status switch
                {
                    RecurringInvoiceSchedule.StatusConcept => "concept",
                    RecurringInvoiceSchedule.StatusPaused => "paused",
                    RecurringInvoiceSchedule.StatusStopped => "stopped",
                    RecurringInvoiceSchedule.StatusFinished => "finished",
                    _ => pending ? "pending" : "active"
                };
                lastNumbers.TryGetValue(s.Id, out var last);
                return new RecurringRowVM
                {
                    Id = s.Id,
                    ClientName = party.Name ?? "(onbekend)",
                    Text = first?.Text ?? "",
                    Rhythm = RecurringScheduleCalculator.RhythmText(s),
                    Next = key is "active" or "pending" ? s.NextOccurrence : null,
                    Net = RecurringScheduleCalculator.InvoiceNet(s, s.NextOccurrence ?? s.FirstDate, s.CreatedCount == 0, healthIndex),
                    OnCreateText = "Ter controle · " + (party.Digital ? "Peppol" : "E-mail"),
                    LastInvoiceId = last?.Id,
                    LastInvoiceNumber = last?.PublicId,
                    Status = s.Status,
                    StatusKey = key,
                    StatusLabel = key switch
                    {
                        "concept" => "Concept",
                        "paused" => "Gepauzeerd",
                        "stopped" => "Stopgezet",
                        "finished" => "Afgelopen",
                        "pending" => "Start " + (s.NextOccurrence ?? s.FirstDate).ToString("dd/MM/yyyy", Nl),
                        _ => "Actief"
                    },
                    FirstYear = s.FirstDate.Year,
                    LastYear = s.EndMode == 2 ? s.EndDate?.Year : s.StoppedAt?.Year
                };
            })
            .OrderBy(r => r.Status == RecurringInvoiceSchedule.StatusStopped || r.Status == RecurringInvoiceSchedule.StatusFinished ? 1 : 0)
            .ThenBy(r => r.Next ?? DateOnly.MaxValue).ThenBy(r => r.ClientName).ToList();

            var drafts = await _db.Invoices.AsNoTracking()
                .Where(i => i.IssuerCompanyId == issuerCompanyId && i.RecurringScheduleId != null && i.StatusId == InvoiceStatusIds.Draft)
                .Select(i => i.Date).ToListAsync(ct);

            var firstYear = Math.Min(rows.Count == 0 ? today.Year : rows.Min(r => r.FirstYear), today.Year);
            var years = Enumerable.Range(firstYear, today.Year - firstYear + 2).Select(y => y.ToString()).OrderByDescending(y => y).ToList();

            var vm = new RecurringIndexVM
            {
                IssuerCompanyId = issuerCompanyId,
                Rows = rows,
                // de teller bij Facturen telt enkel het gekozen boekjaar, net als de lijst zelf
                InvoiceCount = await _db.Invoices.CountAsync(i => i.IssuerCompanyId == issuerCompanyId && (yearScope.From == null || i.Date >= yearScope.From) && (yearScope.To == null || i.Date <= yearScope.To), ct),
                ScheduleCount = schedules.Count(s => s.Status != RecurringInvoiceSchedule.StatusStopped && s.Status != RecurringInvoiceSchedule.StatusFinished),
                ReviewCount = drafts.Count,
                ReviewDate = drafts.Count == 0 ? null : drafts.Min(),
                MonthlyAverage = schedules.Where(s => s.Status == RecurringInvoiceSchedule.StatusActive).Sum(RecurringScheduleCalculator.MonthlyAverage),
                CanWrite = canWrite,
                Years = years,
                DefaultYear = today.Year.ToString()
            };

            var settings = await _recurring.GetSettingsAsync(issuerCompanyId, ct);
            var companyName = await _companies.GetIssuerNameAsync(issuerCompanyId, ct);
            var (hiLatest, hiFetched) = await _health.StatusAsync(ct);
            ViewBag.HealthIndexStatus = HealthIndexStatusText(hiLatest, hiFetched);
            ViewBag.RecurringSettings = new RecurringSettingsVM
            {
                IssuerCompanyId = issuerCompanyId,
                CompanyName = companyName,
                OnCreateMode = settings.OnCreateMode,
                DraftLeadDays = settings.DraftLeadDays,
                WeekendRule = settings.WeekendRule,
                NotifyUsers = settings.NotifyUsers,
                IndexationType = settings.IndexationType,
                IndexationWhen = settings.IndexationWhen,
                IndexationConfirm = settings.IndexationConfirm,
                ResumeMode = settings.ResumeMode,
                WarnUnpaid = settings.WarnUnpaid
            };
            ViewBag.CompanyName = companyName;
            ViewBag.CompanyId = issuerCompanyId;
            ViewBag.CanWriteInvoices = canWrite;
            ViewBag.InvoiceCount = vm.InvoiceCount;
            ViewBag.BookyearParam = string.IsNullOrWhiteSpace(bookyear) ? null : (yearScope.Selected is { Length: > 0 } ? yearScope.Selected : "alle");
            ViewBag.RecurringCount = vm.ScheduleCount;
            ViewBag.HideRecurring = false;
            ViewData["BreadcrumbNode"] = CreateRecurringNode(issuerCompanyId, ViewBag.CompanyName as string);
            SetPageHeader("bx bx-receipt", $"Facturen - {ViewBag.CompanyName}");
            ViewData["HelpScreenId"] = "invoices-recurring";
            return View("RecurringIndexV2", vm);
        }

        private MvcBreadcrumbNode CreateRecurringNode(int issuerId, string? companyName)
            => new MvcBreadcrumbNode(nameof(Index), ControllerName, "Terugkerend")
            {
                Parent = CreateIndexNode(issuerId, companyName),
                RouteValues = new { issuerCompanyId = issuerId, view = "recurring" }
            };

        /// <summary>Naam en "digitale factuur vereist" van klanten (1), contacten (2) en leveranciers (3), in drie queries.</summary>
        private async Task<Dictionary<(byte Type, int Id), (string Name, bool Digital, string? Email)>> ResolvePartyNamesAsync(IEnumerable<(byte Type, int Id)> parties, CancellationToken ct)
        {
            var list = parties.Distinct().ToList();
            var result = new Dictionary<(byte, int), (string, bool, string?)>();
            var ca = list.Where(p => p.Type == 1).Select(p => p.Id).ToList();
            if (ca.Count > 0)
                foreach (var x in await _db.ClientAccount.AsNoTracking().Where(x => ca.Contains(x.Id))
                    .Select(x => new { x.Id, Name = !string.IsNullOrWhiteSpace(x.CompanyName) ? x.CompanyName : x.Name, x.RequiresDigitalInvoice, Email = x.InvoiceEmail != null && x.InvoiceEmail != "" ? x.InvoiceEmail : x.Email }).ToListAsync(ct))
                    result[(1, x.Id)] = (x.Name ?? "", x.RequiresDigitalInvoice, x.Email);
            var cc = list.Where(p => p.Type == 2).Select(p => p.Id).ToList();
            if (cc.Count > 0)
                foreach (var x in await _db.ClientContacts.AsNoTracking().Where(x => cc.Contains(x.Id))
                    .Select(x => new { x.Id, x.CompanyName, x.Forename, x.Name, x.RequiresDigitalInvoice, Email = x.InvoiceEmail != null && x.InvoiceEmail != "" ? x.InvoiceEmail : x.Email }).ToListAsync(ct))
                    result[(2, x.Id)] = (!string.IsNullOrWhiteSpace(x.CompanyName) ? x.CompanyName : ((x.Forename ?? "") + " " + (x.Name ?? "")).Trim(), x.RequiresDigitalInvoice, x.Email);
            var su = list.Where(p => p.Type == 3).Select(p => p.Id).ToList();
            if (su.Count > 0)
                foreach (var x in await _db.CompanyInfo.AsNoTracking().Where(x => su.Contains(x.CompanyId))
                    .Select(x => new { x.CompanyId, x.BedrijfsNaam, x.RequiresDigitalInvoice, Email = x.InvoiceEmail != null && x.InvoiceEmail != "" ? x.InvoiceEmail : x.Email }).ToListAsync(ct))
                    result[(3, x.CompanyId)] = (x.BedrijfsNaam ?? "", x.RequiresDigitalInvoice, x.Email);
            return result;
        }

        // ───────────────────────── instellingen (50c) ─────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Invoicing)]
        public async Task<IActionResult> RecurringSettingsSave(RecurringSettingsVM vm, CancellationToken ct)
        {
            var writeScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!writeScope.HasAllIssuers && !writeScope.AllowedIssuerIds.Contains(vm.IssuerCompanyId))
                return FlowError("Je hebt geen rechten om de instellingen van dit facturatiebedrijf te wijzigen.");
            await _recurring.SaveSettingsAsync(new RecurringInvoiceSettings
            {
                IssuerCompanyId = vm.IssuerCompanyId,
                OnCreateMode = vm.OnCreateMode,
                DraftLeadDays = vm.DraftLeadDays,
                WeekendRule = vm.WeekendRule,
                NotifyUsers = vm.NotifyUsers,
                IndexationType = vm.IndexationType,
                IndexationWhen = vm.IndexationWhen,
                IndexationConfirm = vm.IndexationConfirm,
                ResumeMode = vm.ResumeMode,
                WarnUnpaid = vm.WarnUnpaid
            }, ct);
            return Json(new { ok = true });
        }

        // ───────────────────────── schemadetail (50d) ─────────────────────────

        [HttpGet]
        public async Task<IActionResult> RecurringDetail(int id, CancellationToken ct = default)
        {
            var s = await _db.RecurringInvoiceSchedule.AsNoTracking().Include(x => x.Lines).Include(x => x.Events).FirstOrDefaultAsync(x => x.Id == id, ct);
            if (s == null)
            {
                AddMessage("error", "Schema niet gevonden.", "Terugkerende facturen");
                return RedirectToAction(nameof(Index));
            }
            var readScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Read, ct);
            if (!readScope.HasAccess || (!readScope.HasAllIssuers && !readScope.AllowedIssuerIds.Contains(s.IssuerCompanyId)))
            {
                AddMessage("error", "Je hebt geen rechten om facturen voor dit facturatiebedrijf te bekijken.", "Geen toegang");
                return RedirectToAction("AccessDenied", "Account");
            }
            var writeScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Write, ct);
            var canWrite = writeScope.HasAccess && (writeScope.HasAllIssuers || writeScope.AllowedIssuerIds.Contains(s.IssuerCompanyId));
            var deleteScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Delete, ct);
            var canDelete = deleteScope.HasAccess && (deleteScope.HasAllIssuers || deleteScope.AllowedIssuerIds.Contains(s.IssuerCompanyId));
            var today = DateOnly.FromDateTime(DateTime.Today);
            var healthIndex = await _health.GetAsync(ct);

            var party = (await ResolvePartyNamesAsync(new[] { (s.PartyType, s.PartyId) }, ct)).GetValueOrDefault((s.PartyType, s.PartyId));
            string? projectName = s.ProjectId.HasValue ? await _db.Project.AsNoTracking().Where(p => p.ProjectId == s.ProjectId).Select(p => p.ProjectName).FirstOrDefaultAsync(ct) : null;
            string? termText = null;
            if (s.PaymentTermId.HasValue)
                termText = await _db.PaymentTerms.AsNoTracking().Where(t => t.Id == s.PaymentTermId).Select(t => t.Name).FirstOrDefaultAsync(ct);

            var invoices = await _db.Invoices.AsNoTracking().Where(i => i.RecurringScheduleId == s.Id)
                .Select(i => new { i.Id, i.PublicId, i.Date, i.RecurringPeriodText, i.StatusId, i.RecurringOccurrence, Net = i.InvoicesDetails.Sum(d => (decimal?)d.Price) ?? 0m })
                .OrderByDescending(i => i.RecurringOccurrence).ToListAsync(ct);
            var draft = invoices.FirstOrDefault(i => i.StatusId == InvoiceStatusIds.Draft);

            var pending = s.Status == RecurringInvoiceSchedule.StatusActive && s.CreatedCount == 0 && s.NextOccurrence.HasValue && s.NextOccurrence.Value.AddDays(-s.DraftLeadDays) > today;
            var key = s.Status switch
            {
                RecurringInvoiceSchedule.StatusConcept => "concept",
                RecurringInvoiceSchedule.StatusPaused => "paused",
                RecurringInvoiceSchedule.StatusStopped => "stopped",
                RecurringInvoiceSchedule.StatusFinished => "finished",
                _ => pending ? "pending" : "active"
            };

            var vm = new RecurringDetailVM
            {
                Schedule = s,
                IssuerCompanyId = s.IssuerCompanyId,
                ClientName = party.Name ?? "(onbekend)",
                ClientEmail = party.Email,
                ClientPeppolText = party.Digital ? "Peppol" : "E-mail",
                ProjectName = projectName,
                PaymentTermText = termText,
                StatusKey = key,
                StatusLabel = key switch { "concept" => "Concept", "paused" => "Gepauzeerd", "stopped" => "Stopgezet", "finished" => "Afgelopen", "pending" => "Nog niet gestart", _ => "Actief" },
                Rhythm = RecurringScheduleCalculator.RhythmText(s),
                RhythmLong = RhythmLong(s),
                Lines = s.Lines.OrderBy(l => l.SortOrder).Select(l => new RecurringLineVM { Text = l.Text, Quantity = l.Quantity, UnitPrice = l.UnitPrice, VatTypeId = l.VatTypeId, VatPercentage = l.VatPercentage, FirstOnly = l.FirstOnly, Indexable = l.Indexable }).ToList(),
                CanWrite = canWrite,
                CanDelete = canDelete,
                InvoiceTotal = invoices.Count(i => i.StatusId != InvoiceStatusIds.Cancelled),
                InvoiceTotalNet = invoices.Sum(i => i.Net),
                Events = s.Events.OrderByDescending(e => e.At).Take(12).Select(e => new RecurringEventVM { At = e.At.ToLocalTime(), Text = e.Text, Detail = e.Detail }).ToList(),
                DraftInvoiceId = draft?.Id,
                DraftOccurrence = draft?.RecurringOccurrence,
                DraftPeriod = draft?.RecurringPeriodText,
                MissedCount = s.Status == RecurringInvoiceSchedule.StatusPaused ? await _recurring.CountMissedAsync(s.Id, today, ct) : 0,
                IndexationText = s.IndexationEnabled
                    ? (s.IndexationType switch { 2 => "Consumptieprijzen", 3 => "Vast percentage", _ => "Gezondheidsindex (Statbel)" }) + (s.IndexationType != 1 && s.IndexationPercent.HasValue ? " · " + s.IndexationPercent.Value.ToString("0.##", Nl) + " %" : "")
                    : "Geen",
                StopText = s.EndMode switch { 1 => $"Na {s.EndCount} facturen", 2 => "Op " + s.EndDate?.ToString("dd/MM/yyyy", Nl), _ => "Nooit" }
            };
            // het voorbeeld onder de lijnen: de omschrijving van de eerste lijn zoals ze op de eerstvolgende factuur komt
            var example = s.NextOccurrence ?? s.FirstDate;
            var firstLine = vm.Lines.FirstOrDefault();
            if (firstLine != null && firstLine.Text.Contains('{'))
                vm.ExampleSentence = $"Op de factuur van {RecurringScheduleCalculator.MonthName(example.Month)} wordt dit “{RecurringScheduleCalculator.Interpolate(firstLine.Text, s, example, s.CreatedCount + 1)}”.";
            vm.LinesNet = s.Lines.Where(l => !l.FirstOnly).Sum(l => Math.Round(l.Quantity * l.UnitPrice, 2, MidpointRounding.AwayFromZero));
            vm.LinesVat = s.Lines.Where(l => !l.FirstOnly).Sum(l => Math.Round(l.Quantity * l.UnitPrice * l.VatPercentage / 100m, 2, MidpointRounding.AwayFromZero));

            foreach (var i in invoices.Take(5))
            {
                var isDraft = i.StatusId == InvoiceStatusIds.Draft;
                vm.Invoices.Add(new RecurringCreatedInvoiceVM
                {
                    Id = i.Id, PublicId = i.PublicId, Date = i.Date, Period = i.RecurringPeriodText ?? "", Net = i.Net, IsDraft = isDraft,
                    StatusKey = isDraft ? "review" : i.StatusId == InvoiceStatusIds.Paid ? "paid" : i.StatusId == InvoiceStatusIds.Cancelled ? "other" : "open",
                    StatusLabel = isDraft ? "Ter controle" : i.StatusId == InvoiceStatusIds.Paid ? "Betaald" : i.StatusId == InvoiceStatusIds.Cancelled ? "Geannuleerd" : "Open"
                });
            }

            if (s.NextOccurrence.HasValue && s.Status == RecurringInvoiceSchedule.StatusActive)
            {
                var first = s.CreatedCount == 0;
                var created = s.CreatedCount;
                var upcoming = RecurringScheduleCalculator.Upcoming(s, s.OccurrenceIndex, 4, created);
                for (var n = 0; n < upcoming.Count; n++)
                {
                    var (_, date) = upcoming[n];
                    vm.Upcoming.Add(new RecurringUpcomingVM
                    {
                        Date = date,
                        Net = RecurringScheduleCalculator.InvoiceNet(s, date, first && n == 0, healthIndex),
                        IsDue = date.AddDays(-s.DraftLeadDays) <= today,
                        Note = date.AddDays(-s.DraftLeadDays) <= today
                            ? $"concept klaar sinds {date.AddDays(-s.DraftLeadDays):dd/MM}"
                            : $"concept op {date.AddDays(-s.DraftLeadDays):dd/MM}"
                    });
                }
                var info = RecurringScheduleCalculator.IndexationAt(s, s.NextOccurrence.Value, healthIndex);
                vm.IndexationIsHealth = s.IndexationType == 1;
                if (s.IndexationEnabled && info.Steps > 0 && !info.Ready)
                {
                    // de gezondheidsindex is nog niet bekend: het concept wacht vanzelf, er valt niets te bevestigen
                    vm.IndexationWaiting = true;
                    vm.IndexationExplain = "Het concept wacht tot " + info.Missing + ". Het CRM haalt de index op de laatste dag van de maand op en zet het concept daarna zelf klaar.";
                }
                else if (s.IndexationEnabled && s.IndexationConfirm && info.Steps > s.IndexationAckCount)
                {
                    vm.IndexationPending = true;
                    vm.IndexationOldNet = RecurringScheduleCalculator.InvoiceNet(new RecurringInvoiceSchedule { IndexationEnabled = false, Lines = s.Lines }, s.NextOccurrence.Value, first);
                    vm.IndexationNewNet = RecurringScheduleCalculator.InvoiceNet(s, s.NextOccurrence.Value, first, healthIndex);
                    if (s.IndexationType == 1 && info.NewIndex.HasValue && info.BaseIndex.HasValue && info.NewMonth.HasValue && info.BaseMonth.HasValue)
                        vm.IndexationExplain = $"Gezondheidsindex {RecurringScheduleCalculator.MonthName(info.NewMonth.Value.Month)} {info.NewMonth.Value.Year}: {info.NewIndex.Value.ToString("0.00", Nl)} ÷ basisindex {RecurringScheduleCalculator.MonthName(info.BaseMonth.Value.Month)} {info.BaseMonth.Value.Year}: {info.BaseIndex.Value.ToString("0.00", Nl)} (basis 2013 = 100).";
                }
            }

            ViewBag.CompanyName = await _companies.GetIssuerNameAsync(s.IssuerCompanyId, ct);
            ViewBag.CompanyId = s.IssuerCompanyId;
            ViewBag.CanWriteInvoices = canWrite;
            var crumb = CreateRecurringNode(s.IssuerCompanyId, ViewBag.CompanyName as string);
            ViewData["BreadcrumbNode"] = new MvcBreadcrumbNode(nameof(RecurringDetail), ControllerName, vm.ClientName) { Parent = crumb, RouteValues = new { id = s.Id } };
            SetPageHeader("ph ph-arrows-clockwise", $"{vm.ClientName} · {s.Lines.OrderBy(l => l.SortOrder).First().Text.Split('{')[0].Trim().TrimEnd('—', '-', ' ')}");
            ViewData["HelpScreenId"] = "invoices-recurring-detail";
            return View("RecurringDetailV2", vm);
        }

        private static string RhythmLong(RecurringInvoiceSchedule s)
        {
            if (s.Frequency == 1) return "Elke week op " + new[] { "maandag", "dinsdag", "woensdag", "donderdag", "vrijdag" }[Math.Clamp(s.DayOfMonth - 1, 0, 4)];
            var day = s.DayMode == 2 ? "op de laatste dag" : "op de " + RecurringScheduleCalculator.Ordinal(s.DayOfMonth);
            return s.Frequency switch
            {
                3 => "Elk kwartaal, " + day,
                4 => $"Elk jaar op {s.FirstDate.Day} {RecurringScheduleCalculator.MonthName(s.FirstDate.Month)}",
                _ => "Elke maand, " + day
            };
        }

        // ───────────────────────── acties op een schema ─────────────────────────

        private async Task<(bool Ok, string? Error)> CheckScheduleWriteAsync(int id, CancellationToken ct)
        {
            var issuerId = await _db.RecurringInvoiceSchedule.AsNoTracking().Where(s => s.Id == id).Select(s => (int?)s.IssuerCompanyId).FirstOrDefaultAsync(ct);
            if (issuerId == null) return (false, "Schema niet gevonden.");
            var writeScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!writeScope.HasAllIssuers && !writeScope.AllowedIssuerIds.Contains(issuerId.Value))
                return (false, "Je hebt geen rechten om schema's van dit facturatiebedrijf te wijzigen.");
            return (true, null);
        }

        private string RecurringUserName() => User?.Identity?.Name ?? "";

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Invoicing)]
        public async Task<IActionResult> RecurringAction(int id, string op, bool backfill = false, string? percent = null, CancellationToken ct = default)
        {
            var (ok, error) = await CheckScheduleWriteAsync(id, ct);
            if (!ok) return FlowError(error!);
            var today = DateOnly.FromDateTime(DateTime.Today);
            try
            {
                switch (op)
                {
                    case "pause": await _recurring.PauseAsync(id, RecurringUserName(), ct); break;
                    case "resume": await _recurring.ResumeAsync(id, backfill, RecurringUserName(), today, ct); break;
                    case "stop": await _recurring.StopAsync(id, RecurringUserName(), ct); break;
                    case "confirm-index":
                        await _recurring.ConfirmIndexationAsync(id, ParseDecimalOrNull(percent), RecurringUserName(), ct); break;
                    case "generate":
                        var r = await _recurring.GenerateNextAsync(id, RecurringUserName(), today, ct);
                        if (!r.Ok) return FlowError(r.Error ?? "Het concept kon niet klaargezet worden.");
                        if (r.Held) return FlowError("De indexatie moet eerst bevestigd worden voor je een factuur maakt.");
                        return Json(new { ok = true, invoiceId = r.InvoiceId });
                    case "reactivate": await _recurring.ReactivateAsync(id, RecurringUserName(), today, ct); break;
                    case "delete":
                        var issuerOfSchedule = await _db.RecurringInvoiceSchedule.AsNoTracking().Where(s => s.Id == id).Select(s => s.IssuerCompanyId).FirstAsync(ct);
                        var deleteScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Delete, ct);
                        if (!deleteScope.HasAccess || (!deleteScope.HasAllIssuers && !deleteScope.AllowedIssuerIds.Contains(issuerOfSchedule)))
                            return FlowError("Je hebt geen rechten om schema's te verwijderen.");
                        await _recurring.DeleteAsync(id, ct);
                        return Json(new { ok = true, deleted = true });
                    default: return FlowError("Onbekende actie.");
                }
                return Json(new { ok = true });
            }
            catch (InvalidOperationException ex)
            {
                return FlowError(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Terugkerende factuur: actie {Op} op schema {Id} mislukt", op, id);
                return FlowError("De actie kon niet uitgevoerd worden.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> RecurringMissed(int id, CancellationToken ct = default)
        {
            var issuerId = await _db.RecurringInvoiceSchedule.AsNoTracking().Where(s => s.Id == id).Select(s => (int?)s.IssuerCompanyId).FirstOrDefaultAsync(ct);
            if (issuerId == null) return Json(new { count = 0 });
            var readScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Read, ct);
            if (!readScope.HasAllIssuers && !readScope.AllowedIssuerIds.Contains(issuerId.Value)) return Json(new { count = 0 });
            return Json(new { count = await _recurring.CountMissedAsync(id, DateOnly.FromDateTime(DateTime.Today), ct) });
        }

        /// <summary>Haalt de gezondheidsindex nu op bij Statbel (anders gebeurt dat automatisch op de laatste dag van de maand).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Invoicing)]
        public async Task<IActionResult> HealthIndexRefresh(CancellationToken ct = default)
        {
            var r = await _health.RefreshAsync(ct);
            if (!r.Ok) return FlowError(r.Error ?? "Ophalen mislukt.");
            var (latest, fetched) = await _health.StatusAsync(ct);
            return Json(new { ok = true, rows = r.Rows, text = HealthIndexStatusText(latest, fetched) });
        }

        private static string HealthIndexStatusText(DateOnly? latest, DateTime? fetched)
            => latest is null ? "Nog geen gezondheidsindex opgehaald."
                : $"Laatste maand: {RecurringScheduleCalculator.MonthName(latest.Value.Month)} {latest.Value.Year} · opgehaald op {fetched?.ToLocalTime():dd/MM/yyyy}.";

        /// <summary>Zoeker voor "Melding voor concepten naar": enkel interne medewerkers: actieve CPM-gebruikers zonder bedrijfstoegang (dus geen aannemers van het werfportaal), geen klanten of contacten.</summary>
        [HttpPost]
        public async Task<IActionResult> RecurringUsersLookup(string? term, CancellationToken ct = default)
        {
            var t = (term ?? "").Trim().ToLowerInvariant();
            var users = await _db.Users.AsNoTracking().Where(u => u.IsActive && u.Email != null && u.Email != "" && !u.UserCompanyAccess.Any())
                .Select(u => new { u.Voornaam, u.Familienaam, u.Email }).ToListAsync(ct);
            var list = users.Select(u => new { Naam = $"{u.Voornaam} {u.Familienaam}".Trim(), u.Email })
                .Where(u => t.Length == 0 || u.Naam.ToLowerInvariant().Contains(t) || u.Email!.ToLowerInvariant().Contains(t))
                .OrderBy(u => u.Naam, StringComparer.CurrentCultureIgnoreCase).Take(30)
                .Select(u => new { id = u.Email, text = string.IsNullOrWhiteSpace(u.Naam) ? u.Email : u.Naam, sub = u.Email, group = "Medewerkers" });
            return Json(list);
        }

        // ───────────────────────── schema bewaren (49) ─────────────────────────

        private static decimal? ParseDecimalOrNull(string? s)
            => decimal.TryParse((s ?? "").Replace(" ", "").Replace("%", ""), NumberStyles.Number, Nl, out var d) ? d : null;

        private static DateOnly? ParseIsoOrNull(string? s)
            => DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

        /// <summary>Het formulier naar een schema met lijnen. Gooit InvalidOperationException als iets ontbreekt.</summary>
        private static (RecurringInvoiceSchedule Schedule, List<RecurringInvoiceScheduleLine> Lines) MapRecurringForm(RecurringScheduleFormVM f)
        {
            if (f.IssuerCompanyId <= 0 || f.PartyId is null || f.PartyType is null)
                throw new InvalidOperationException("Kies een facturatiebedrijf en een klant of leverancier.");
            var first = ParseIsoOrNull(f.FirstDate) ?? throw new InvalidOperationException("Vul de datum van de eerste factuur in.");
            var s = new RecurringInvoiceSchedule
            {
                Id = f.ScheduleId,
                IssuerCompanyId = f.IssuerCompanyId,
                Status = f.Status == 1 ? RecurringInvoiceSchedule.StatusActive : RecurringInvoiceSchedule.StatusConcept,
                PartyType = (byte)(int)f.PartyType.Value,
                PartyId = f.PartyId.Value,
                ProjectId = f.ProjectId,
                SupplierContractId = f.SupplierContractId,
                IssuerBankAccountId = f.IssuerBankAccountId,
                PaymentTermId = f.PaymentTermId,
                VatTypeId = f.VatTypeId,
                IsPrepaid = f.IsPrepaid,
                HeaderDescription = f.HeaderDescription,
                DetailDescription = f.DetailDescription,
                FooterDescription = f.FooterDescription,
                Frequency = f.Frequency,
                DayMode = f.DayMode,
                DayOfMonth = f.DayOfMonth,
                FirstDate = first,
                PeriodMode = f.PeriodMode,
                EndMode = f.EndMode,
                EndCount = f.EndCount,
                EndDate = ParseIsoOrNull(f.EndDate),
                DraftLeadDays = f.DraftLeadDays,
                WeekendRule = f.WeekendRule,
                ResumeMode = f.ResumeMode,
                WarnUnpaid = f.WarnUnpaid,
                IndexationEnabled = f.IndexationEnabled,
                IndexationType = f.IndexationType,
                IndexationWhen = f.IndexationWhen,
                IndexationBaseDate = ParseIsoOrNull(f.IndexationBaseDate),
                IndexationPercent = ParseDecimalOrNull(f.IndexationPercent),
                IndexationBaseIndex = ParseDecimalOrNull(f.IndexationBaseIndex),
                IndexationConfirm = f.IndexationConfirm
            };
            if (f.IndexationEnabled && f.IndexationWhen == 3)
            {
                var parts = (f.IndexationDate ?? "").Split('/');
                if (parts.Length < 2 || !int.TryParse(parts[0], out var d) || !int.TryParse(parts[1], out var m) || d is < 1 or > 31 || m is < 1 or > 12)
                    throw new InvalidOperationException("Vul de datum van de indexering in als dd/mm.");
                s.IndexationDay = (byte)d;
                s.IndexationMonth = (byte)m;
            }
            var lines = (f.Lines ?? new()).Select((l, i) => new RecurringInvoiceScheduleLine
            {
                SortOrder = i,
                Text = l.Text ?? "",
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                VatTypeId = l.VatTypeId,
                VatPercentage = l.VatPercentage,
                FirstOnly = l.FirstOnly,
                Indexable = l.Indexable
            }).ToList();
            foreach (var l in lines) s.Lines.Add(l);
            return (s, lines);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Invoicing)]
        public async Task<IActionResult> SaveRecurringV2(RecurringScheduleFormVM vm, CancellationToken ct)
        {
            var writeScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!writeScope.HasAllIssuers && !writeScope.AllowedIssuerIds.Contains(vm.IssuerCompanyId))
                return FlowError("Je hebt geen rechten om facturen voor dit facturatiebedrijf aan te maken.");
            try
            {
                if (vm.ScheduleId > 0)
                {
                    var (ok, error) = await CheckScheduleWriteAsync(vm.ScheduleId, ct);
                    if (!ok) return FlowError(error!);
                }
                var (schedule, lines) = MapRecurringForm(vm);
                // het btw-tarief van elke lijn komt uit de btw-tabel van het facturatiebedrijf
                var vatTypes = await _db.Vattype.AsNoTracking().Where(v => v.IssuerCompanyId == vm.IssuerCompanyId).ToListAsync(ct);
                foreach (var l in lines)
                {
                    var vat = vatTypes.FirstOrDefault(v => v.Id == (l.VatTypeId ?? vm.VatTypeId));
                    if (vat != null) { l.VatTypeId = vat.Id; l.VatPercentage = vat.BasePercentage; }
                }
                var id = await _recurring.SaveScheduleAsync(schedule, lines, RecurringUserName(), DateOnly.FromDateTime(DateTime.Today), ct);
                return Json(new { ok = true, scheduleId = id, redirect = Url.Action(nameof(RecurringDetail), ControllerName, new { id }) });
            }
            catch (InvalidOperationException ex)
            {
                return FlowError(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SaveRecurringV2 failed for issuer {IssuerCompanyId}", vm.IssuerCompanyId);
                return FlowError("Het schema kon niet bewaard worden.");
            }
        }

        // ───────────────────────── planning en voorbeeld (49a/49c) ─────────────────────────

        /// <summary>De planning rechts in stap 2 en het voorbeeld van de komende facturen in stap 3: dezelfde rekenregels als bij het echt klaarzetten.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecurringPlanV2(RecurringScheduleFormVM vm, string? partyName, CancellationToken ct)
        {
            var healthIndex = await _health.GetAsync(ct);
            RecurringInvoiceSchedule s;
            try { (s, _) = MapRecurringForm(vm); }
            catch (InvalidOperationException) { return Json(new { ok = false }); }
            s.Lines = s.Lines.Where(l => !string.IsNullOrWhiteSpace(l.Text)).ToList();
            var settingsAck = 0; // in het formulier is er nog niets bevestigd
            _ = settingsAck;

            var total = RecurringScheduleCalculator.TotalPlanned(s);
            var occurrences = RecurringScheduleCalculator.Upcoming(s, 0, Math.Min(total ?? 6, 6), 0);
            var firstText = s.Lines.OrderBy(l => l.SortOrder).FirstOrDefault()?.Text ?? "Factuur";
            var idxDates = RecurringScheduleCalculator.IndexationDates(s, occurrences.Count == 0 ? s.FirstDate : occurrences.Last().Date.AddYears(30));
            var firstIndexation = idxDates.Count > 0 ? idxDates[0] : (DateOnly?)null;
            var taggedIndexation = false;
            var plan = new List<object>();
            for (var n = 0; n < occurrences.Count; n++)
            {
                var (_, date) = occurrences[n];
                var idx = RecurringScheduleCalculator.IndexationAt(s, date, healthIndex);
                var tag = n == 0 ? "eerste" : null;
                // een indexatie waarvan de gezondheidsindex nog niet bestaat toont het oude bedrag met het label "index volgt"
                if (idx.Steps > 0 && !taggedIndexation) { tag ??= idx.Ready ? "geïndexeerd" : "index volgt"; taggedIndexation = true; }
                plan.Add(new
                {
                    date = date.ToString("dd/MM/yyyy", Nl),
                    period = RecurringScheduleCalculator.Interpolate(firstText, s, date, n + 1),
                    amount = RecurringScheduleCalculator.InvoiceNet(s, date, n == 0, healthIndex),
                    tag
                });
            }
            var perPeriod = occurrences.Count > 1 ? RecurringScheduleCalculator.InvoiceNet(s, occurrences[1].Date, false, healthIndex) : RecurringScheduleCalculator.InvoiceNet(s, s.FirstDate, false, healthIndex);
            var adverb = s.Frequency switch { 1 => "wekelijks", 3 => "per kwartaal", 4 => "jaarlijks", _ => "maandelijks" };

            var previews = new List<object>();
            var previewDates = RecurringScheduleCalculator.Upcoming(s, 0, 3, 0);
            for (var n = 0; n < previewDates.Count; n++)
            {
                var (_, date) = previewDates[n];
                var lines = s.Lines.Where(l => n == 0 || !l.FirstOnly).OrderBy(l => l.SortOrder).Select(l =>
                {
                    var unit = RecurringScheduleCalculator.LineUnitPrice(s, l, date, healthIndex);
                    return new { text = RecurringScheduleCalculator.Interpolate(l.Text, s, date, n + 1), amount = Math.Round(l.Quantity * unit, 2, MidpointRounding.AwayFromZero), vat = l.VatPercentage };
                }).ToList();
                var excl = lines.Sum(l => l.amount);
                var vat = lines.Sum(l => Math.Round(l.amount * l.vat / 100m, 2, MidpointRounding.AwayFromZero));
                previews.Add(new
                {
                    title = n == 0 ? "Eerste factuur" : RecurringScheduleCalculator.PeriodText(s, date),
                    date = date.ToString("dd/MM/yyyy", Nl),
                    lines,
                    excl,
                    vat,
                    total = excl + vat,
                    first = n == 0
                });
            }

            var lead = s.DraftLeadDays;
            var note = (total.HasValue && total > 6 ? $"+ {total - 6} volgende facturen · " : "")
                + $"Elke factuur verschijnt {lead} dag{(lead == 1 ? "" : "en")} vooraf als concept ter controle."
                + (firstIndexation.HasValue ? $" Eerste indexatie op {firstIndexation.Value:dd/MM/yyyy}." : "");
            var baseMonth = (s.IndexationBaseDate ?? s.FirstDate).AddMonths(-1);
            return Json(new
            {
                ok = true,
                summary = perPeriod.ToString("C", Nl) + " " + adverb,
                summary2 = (string.IsNullOrWhiteSpace(partyName) ? "" : partyName + " · ") + (total.HasValue ? $"{total} facturen, samen {(perPeriod * total.Value).ToString("C", Nl)}" : "zonder einddatum"),
                plan,
                note,
                previews,
                indexation = firstIndexation.HasValue ? $"Eerste indexatie {firstIndexation.Value:dd/MM/yyyy} · basisindex: {RecurringScheduleCalculator.MonthName(baseMonth.Month)} {baseMonth.Year} (maand vóór de start)" : null,
                firstDate = occurrences.Count > 0 ? occurrences[0].Date.ToString("yyyy-MM-dd") : null,
                firstDateText = occurrences.Count > 0 ? occurrences[0].Date.ToString("dd/MM/yyyy", Nl) : null,
                perPeriod
            });
        }

        /// <summary>Het schema in het formulier: gewone Create met de velden van een bestaand schema (Bewerken in 50d).</summary>
        private async Task<object?> BuildRecurringCreateConfigAsync(int issuerId, int? scheduleId, bool recurring, InvoiceComposeVM vm, CancellationToken ct)
        {
            var settings = await _recurring.GetSettingsAsync(issuerId, ct);
            var today = DateOnly.FromDateTime(DateTime.Today);
            RecurringInvoiceSchedule? s = null;
            if (scheduleId.HasValue)
            {
                s = await _db.RecurringInvoiceSchedule.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == scheduleId.Value && x.IssuerCompanyId == issuerId, ct);
                if (s != null)
                {
                    vm.PartyType = (InvoicePartyType)s.PartyType;
                    vm.PartyId = s.PartyId;
                    vm.ProjectId = s.ProjectId;
                    vm.SupplierContractId = s.SupplierContractId;
                    vm.IssuerBankAccountId = s.IssuerBankAccountId ?? vm.IssuerBankAccountId;
                    vm.PaymentTermId = s.PaymentTermId ?? vm.PaymentTermId;
                    vm.VatTypeId = s.VatTypeId ?? vm.VatTypeId;
                    vm.IsPrepaid = s.IsPrepaid;
                    vm.HeaderDescription = s.HeaderDescription;
                    vm.DetailDescription = s.DetailDescription;
                    vm.FooterDescription = s.FooterDescription;
                    vm.Lines = s.Lines.OrderBy(l => l.SortOrder).Select(l => new InvoiceLineVM
                    {
                        Text = l.Text, Quantity = l.Quantity, UnitPrice = l.UnitPrice, Price = Math.Round(l.Quantity * l.UnitPrice, 2),
                        VatTypeId = l.VatTypeId, VatPercentage = l.VatPercentage
                    }).ToList();
                }
            }
            var tomorrow = today.AddDays(1);
            return new
            {
                active = recurring || scheduleId.HasValue,
                scheduleId = s?.Id ?? 0,
                status = s?.Status ?? (byte)0,
                locked = s != null && s.Status is RecurringInvoiceSchedule.StatusStopped or RecurringInvoiceSchedule.StatusFinished,
                today = today.ToString("yyyy-MM-dd"),
                saveUrl = Url.Action(nameof(SaveRecurringV2), ControllerName),
                planUrl = Url.Action(nameof(RecurringPlanV2), ControllerName),
                detailUrl = Url.Action(nameof(RecurringDetail), ControllerName),
                listUrl = Url.Action(nameof(Index), ControllerName, new { issuerCompanyId = issuerId, view = "recurring" }),
                created = s?.CreatedCount ?? 0,
                schedule = new
                {
                    frequency = s?.Frequency ?? 2,
                    dayMode = s?.DayMode ?? 1,
                    dayOfMonth = s?.DayOfMonth ?? tomorrow.Day,
                    firstDate = (s?.FirstDate ?? tomorrow).ToString("yyyy-MM-dd"),
                    periodMode = s?.PeriodMode ?? 1,
                    endMode = s?.EndMode ?? 0,
                    endCount = s?.EndCount ?? 12,
                    endDate = s?.EndDate?.ToString("yyyy-MM-dd"),
                    leadDays = s?.DraftLeadDays ?? settings.DraftLeadDays,
                    weekendRule = s?.WeekendRule ?? settings.WeekendRule,
                    resumeMode = s?.ResumeMode ?? settings.ResumeMode,
                    warnUnpaid = s?.WarnUnpaid ?? settings.WarnUnpaid,
                    idxEnabled = s?.IndexationEnabled ?? false,
                    idxType = s?.IndexationType ?? settings.IndexationType,
                    idxWhen = s?.IndexationWhen ?? settings.IndexationWhen,
                    idxBase = s?.IndexationBaseDate?.ToString("yyyy-MM-dd"),
                    idxDate = s?.IndexationDay != null ? $"{s.IndexationDay:00}/{s.IndexationMonth:00}" : "",
                    idxPercent = s?.IndexationPercent?.ToString("0.####", Nl) ?? "",
                    idxBaseIndex = s?.IndexationBaseIndex?.ToString("0.00", Nl) ?? "",
                    idxConfirm = s?.IndexationConfirm ?? settings.IndexationConfirm
                },
                lineFlags = s?.Lines.OrderBy(l => l.SortOrder).Select(l => new { firstOnly = l.FirstOnly, indexable = l.Indexable }).ToList()
            };
        }
    }
}

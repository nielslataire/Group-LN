using BOCore;
using DALCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceCore.Invoicing
{
    /// <summary>
    /// Terugkerende facturen (design-handoff 49/50). Een schema zet bij elke factuurdatum een concept klaar (Proforma, "Ter controle") via dezelfde
    /// <see cref="IInvoiceCommandService.CreateWithLinesAsync"/> als een gewone nieuwe factuur: nummeren en verzenden gebeurt daarna in de lijst met facturen.
    /// Het automatisch nummeren en versturen zonder tussenkomst bestaat nog niet: ook dan blijft het bij een concept ter controle.
    /// </summary>
    public class RecurringInvoiceService : IRecurringInvoiceService
    {
        private const int MaxPerScheduleRun = 12;
        private readonly UnitOfWorkCore _uow;
        private readonly cpmRunningContext _db;
        private readonly IInvoiceCommandService _cmd;
        private readonly IEmailSender _email;
        private readonly IHealthIndexService _health;
        private readonly ILogger<RecurringInvoiceService> _logger;

        public RecurringInvoiceService(UnitOfWorkCore uow, IInvoiceCommandService cmd, IEmailSender email, IHealthIndexService health, ILogger<RecurringInvoiceService> logger)
        {
            _uow = uow;
            _db = (cpmRunningContext)uow.Context;
            _cmd = cmd;
            _email = email;
            _health = health;
            _logger = logger;
        }

        // ───────────────────────── instellingen ─────────────────────────

        public async Task<RecurringInvoiceSettings> GetSettingsAsync(int issuerCompanyId, CancellationToken ct = default)
        {
            var row = await _db.RecurringInvoiceSettings.AsNoTracking().FirstOrDefaultAsync(x => x.IssuerCompanyId == issuerCompanyId, ct);
            return row ?? new RecurringInvoiceSettings { IssuerCompanyId = issuerCompanyId };
        }

        public async Task SaveSettingsAsync(RecurringInvoiceSettings s, CancellationToken ct = default)
        {
            var row = await _db.RecurringInvoiceSettings.FirstOrDefaultAsync(x => x.IssuerCompanyId == s.IssuerCompanyId, ct);
            if (row == null)
            {
                row = new RecurringInvoiceSettings { IssuerCompanyId = s.IssuerCompanyId };
                _db.RecurringInvoiceSettings.Add(row);
            }
            row.OnCreateMode = 1; // automatisch nummeren en versturen bestaat nog niet
            row.DraftLeadDays = (byte)Math.Clamp((int)s.DraftLeadDays, 0, 30);
            row.WeekendRule = (byte)(s.WeekendRule is >= 1 and <= 3 ? s.WeekendRule : 1);
            row.NotifyUsers = NormalizeEmails(s.NotifyUsers);
            row.IndexationType = (byte)(s.IndexationType is >= 1 and <= 3 ? s.IndexationType : 1);
            row.IndexationWhen = (byte)(s.IndexationWhen is >= 1 and <= 3 ? s.IndexationWhen : 1);
            row.IndexationConfirm = s.IndexationConfirm;
            row.ResumeMode = (byte)(s.ResumeMode is 1 or 2 ? s.ResumeMode : 1);
            row.WarnUnpaid = s.WarnUnpaid;
            row.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        private static string? NormalizeEmails(string? csv)
        {
            var list = (csv ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(e => e.Contains('@')).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return list.Count == 0 ? null : string.Join(",", list);
        }

        // ───────────────────────── schema bewaren ─────────────────────────

        public async Task<int> SaveScheduleAsync(RecurringInvoiceSchedule input, IList<RecurringInvoiceScheduleLine> lines, string? user, DateOnly today, CancellationToken ct = default)
        {
            if (input.PartyId <= 0 || input.PartyType is < 1 or > 3)
                throw new InvalidOperationException("Kies een klant of leverancier.");
            lines = (lines ?? new List<RecurringInvoiceScheduleLine>()).Where(l => !string.IsNullOrWhiteSpace(l.Text)).ToList();
            if (lines.Count == 0)
                throw new InvalidOperationException("Voeg minstens één lijn met een omschrijving toe.");
            if (lines.All(l => l.FirstOnly))
                throw new InvalidOperationException("Minstens één lijn moet op elke factuur komen.");
            if (input.EndMode == 1 && (input.EndCount ?? 0) < 1)
                throw new InvalidOperationException("Vul in na hoeveel facturen het schema stopt.");
            if (input.EndMode == 2 && !input.EndDate.HasValue)
                throw new InvalidOperationException("Vul de einddatum van het schema in.");
            if (input.IndexationEnabled && input.IndexationWhen == 3 && (input.IndexationDay is null || input.IndexationMonth is null))
                throw new InvalidOperationException("Vul de datum van de indexering in (dag en maand).");
            if (input.Status != RecurringInvoiceSchedule.StatusConcept && input.Status != RecurringInvoiceSchedule.StatusActive)
                throw new InvalidOperationException("Een schema bewaar je als concept of actief.");

            RecurringInvoiceSchedule row;
            var isNew = input.Id == 0;
            byte previousStatus = 255;
            if (isNew)
            {
                if (input.Status == RecurringInvoiceSchedule.StatusActive && input.FirstDate < today)
                    throw new InvalidOperationException("De eerste factuur moet vandaag of later zijn.");
                row = new RecurringInvoiceSchedule { CreatedAt = DateTime.UtcNow, CreatedBy = user };
                _db.RecurringInvoiceSchedule.Add(row);
            }
            else
            {
                row = await _db.RecurringInvoiceSchedule.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == input.Id, ct)
                    ?? throw new InvalidOperationException("Schema niet gevonden.");
                if (row.Status is RecurringInvoiceSchedule.StatusStopped or RecurringInvoiceSchedule.StatusFinished)
                    throw new InvalidOperationException("Een stopgezet of afgelopen schema kan je niet meer wijzigen.");
                // een schema dat al loopt (of gepauzeerd is) blijft in zijn status; enkel een concept kan geactiveerd worden
                previousStatus = row.Status;
                if (row.Status != RecurringInvoiceSchedule.StatusConcept)
                    input.Status = row.Status;
                _db.RecurringInvoiceScheduleLine.RemoveRange(row.Lines);
                row.Lines.Clear();
            }

            row.IssuerCompanyId = input.IssuerCompanyId;
            row.Status = input.Status;
            row.PartyType = input.PartyType;
            row.PartyId = input.PartyId;
            row.ProjectId = input.ProjectId;
            row.SupplierContractId = input.SupplierContractId;
            row.IssuerBankAccountId = input.IssuerBankAccountId;
            row.PaymentTermId = input.PaymentTermId;
            row.VatTypeId = input.VatTypeId;
            row.IsPrepaid = input.IsPrepaid;
            row.HeaderDescription = Clean(input.HeaderDescription);
            row.DetailDescription = Clean(input.DetailDescription);
            row.FooterDescription = Clean(input.FooterDescription);
            row.Frequency = (byte)(input.Frequency is >= 1 and <= 4 ? input.Frequency : 2);
            row.DayMode = (byte)(input.DayMode == 2 ? 2 : 1);
            row.FirstDate = input.FirstDate;
            row.DayOfMonth = row.Frequency == 4 ? (byte)input.FirstDate.Day : (byte)Math.Clamp((int)input.DayOfMonth, 1, 31);
            row.PeriodMode = (byte)(input.PeriodMode == 2 ? 2 : 1);
            row.EndMode = (byte)(input.EndMode is >= 0 and <= 2 ? input.EndMode : 0);
            row.EndCount = row.EndMode == 1 ? input.EndCount : null;
            row.EndDate = row.EndMode == 2 ? input.EndDate : null;
            row.OnCreateMode = 1; // automatisch nummeren en versturen bestaat nog niet
            row.DraftLeadDays = (byte)Math.Clamp((int)input.DraftLeadDays, 0, 30);
            row.WeekendRule = (byte)(input.WeekendRule is >= 1 and <= 3 ? input.WeekendRule : 1);
            row.ResumeMode = (byte)(input.ResumeMode is 1 or 2 ? input.ResumeMode : 1);
            row.WarnUnpaid = input.WarnUnpaid;
            row.IndexationEnabled = input.IndexationEnabled;
            row.IndexationType = (byte)(input.IndexationType is >= 1 and <= 3 ? input.IndexationType : 1);
            row.IndexationWhen = (byte)(input.IndexationWhen is >= 1 and <= 3 ? input.IndexationWhen : 1);
            row.IndexationBaseDate = input.IndexationBaseDate;
            row.IndexationDay = input.IndexationWhen == 3 ? input.IndexationDay : null;
            row.IndexationMonth = input.IndexationWhen == 3 ? input.IndexationMonth : null;
            row.IndexationPercent = input.IndexationPercent;
            row.IndexationBaseIndex = input.IndexationBaseIndex;
            row.IndexationConfirm = input.IndexationConfirm;
            row.UpdatedAt = DateTime.UtcNow;

            var order = 0;
            foreach (var l in lines)
            {
                row.Lines.Add(new RecurringInvoiceScheduleLine
                {
                    SortOrder = order++,
                    Text = l.Text.Trim().Length > 200 ? l.Text.Trim().Substring(0, 200) : l.Text.Trim(),
                    Quantity = l.Quantity <= 0 ? 1m : l.Quantity,
                    UnitPrice = l.UnitPrice,
                    VatTypeId = l.VatTypeId,
                    VatPercentage = l.VatPercentage,
                    FirstOnly = l.FirstOnly,
                    Indexable = row.IndexationEnabled && l.Indexable
                });
            }

            RecomputeNext(row, today);
            if (isNew)
                row.Events.Add(new RecurringInvoiceEvent { At = DateTime.UtcNow, Text = "Schema aangemaakt", Detail = user });
            else
                row.Events.Add(new RecurringInvoiceEvent { At = DateTime.UtcNow, Text = row.Status == RecurringInvoiceSchedule.StatusConcept ? "Schema bewaard als concept" : "Schema gewijzigd", Detail = user });
            if (row.Status == RecurringInvoiceSchedule.StatusActive && previousStatus is 255 or RecurringInvoiceSchedule.StatusConcept)
                row.Events.Add(new RecurringInvoiceEvent { At = DateTime.UtcNow.AddMilliseconds(1), Text = "Schema geactiveerd", Detail = user });

            await _db.SaveChangesAsync(ct);
            return row.Id;
        }

        /// <summary>Bepaalt de volgende factuurdatum uit het patroon: na de laatst gemaakte factuur, of de eerste datum van een nieuw schema.</summary>
        private static void RecomputeNext(RecurringInvoiceSchedule s, DateOnly today)
        {
            if (s.Status == RecurringInvoiceSchedule.StatusConcept)
            {
                s.NextOccurrence = null;
                return;
            }
            if (s.Status is RecurringInvoiceSchedule.StatusStopped or RecurringInvoiceSchedule.StatusFinished)
            {
                s.NextOccurrence = null;
                return;
            }
            (int idx, DateOnly? date) next = s.LastOccurrence.HasValue
                ? RecurringScheduleCalculator.FirstAfter(s, s.LastOccurrence.Value)
                : (0, RecurringScheduleCalculator.WithinEnd(s, RecurringScheduleCalculator.OccurrenceDate(s, 0)) ? RecurringScheduleCalculator.OccurrenceDate(s, 0) : null);
            if (RecurringScheduleCalculator.CountReached(s) || next.date is null)
            {
                s.NextOccurrence = null;
                s.OccurrenceIndex = next.idx;
                s.Status = RecurringInvoiceSchedule.StatusFinished;
                return;
            }
            s.OccurrenceIndex = next.idx;
            s.NextOccurrence = next.date;
        }

        // ───────────────────────── concepten klaarzetten ─────────────────────────

        public async Task<int> GenerateDueAsync(DateOnly today, CancellationToken ct = default)
        {
            var candidates = await _db.RecurringInvoiceSchedule.AsNoTracking()
                .Where(s => s.Status == RecurringInvoiceSchedule.StatusActive && s.NextOccurrence != null)
                .Select(s => new { s.Id, s.IssuerCompanyId, s.NextOccurrence, s.DraftLeadDays })
                .ToListAsync(ct);
            var due = candidates.Where(c => c.NextOccurrence!.Value.AddDays(-c.DraftLeadDays) <= today).ToList();
            var created = new Dictionary<int, List<(string Client, DateOnly Date)>>();
            var total = 0;
            foreach (var c in due)
            {
                for (var n = 0; n < MaxPerScheduleRun; n++)
                {
                    RecurringGenerateResult r;
                    try
                    {
                        _db.ChangeTracker.Clear();
                        r = await GenerateOneAsync(c.Id, null, today, force: false, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Terugkerende factuur: schema {ScheduleId} mislukt", c.Id);
                        break;
                    }
                    if (!r.Ok || r.Held || r.InvoiceId == null) break;
                    total++;
                    if (!created.TryGetValue(c.IssuerCompanyId, out var list)) created[c.IssuerCompanyId] = list = new();
                    list.Add((await ClientNameAsync(r.InvoiceId.Value, ct), r.Occurrence ?? today));
                    var next = await _db.RecurringInvoiceSchedule.AsNoTracking().Where(s => s.Id == c.Id)
                        .Select(s => new { s.Status, s.NextOccurrence, s.DraftLeadDays }).FirstOrDefaultAsync(ct);
                    if (next == null || next.Status != RecurringInvoiceSchedule.StatusActive || next.NextOccurrence == null || next.NextOccurrence.Value.AddDays(-next.DraftLeadDays) > today) break;
                }
            }
            foreach (var (issuerId, list) in created)
                await NotifyAsync(issuerId, list, ct);
            return total;
        }

        public async Task<RecurringGenerateResult> GenerateNextAsync(int scheduleId, string? user, DateOnly today, CancellationToken ct = default)
        {
            try
            {
                return await GenerateOneAsync(scheduleId, user, today, force: true, ct);
            }
            catch (InvalidOperationException ex)
            {
                return new RecurringGenerateResult(false, ex.Message, null, null);
            }
        }

        private async Task<string> ClientNameAsync(int invoiceId, CancellationToken ct)
            => await _db.Invoices.AsNoTracking().Where(i => i.Id == invoiceId).Select(i => i.ClientName).FirstOrDefaultAsync(ct) ?? "";

        private async Task<RecurringGenerateResult> GenerateOneAsync(int scheduleId, string? user, DateOnly today, bool force, CancellationToken ct)
        {
            var s = await _db.RecurringInvoiceSchedule.Include(x => x.Lines).Include(x => x.Events).FirstOrDefaultAsync(x => x.Id == scheduleId, ct)
                ?? throw new InvalidOperationException("Schema niet gevonden.");
            if (s.Status != RecurringInvoiceSchedule.StatusActive)
                throw new InvalidOperationException("Enkel een actief schema kan facturen maken.");
            if (s.NextOccurrence is null)
                throw new InvalidOperationException("Dit schema heeft geen volgende factuur meer.");

            var occ = s.NextOccurrence.Value;
            var isFirst = s.CreatedCount == 0;

            var healthIndex = await _health.GetAsync(ct);
            var indexation = RecurringScheduleCalculator.IndexationAt(s, occ, healthIndex);

            // Gezondheidsindex nog niet gepubliceerd (of basisindex onbekend): het concept wacht, anders zou het met het oude bedrag vertrekken.
            if (s.IndexationEnabled && indexation.Steps > 0 && !indexation.Ready)
            {
                var waiting = "Indexatie wacht: " + indexation.Missing;
                if (!s.Events.OrderByDescending(e => e.At).Take(1).Any(e => e.Text == waiting))
                {
                    s.Events.Add(new RecurringInvoiceEvent { At = DateTime.UtcNow, Text = waiting, Detail = "Het concept wordt klaargezet zodra de index bekend is" });
                    await _db.SaveChangesAsync(ct);
                }
                return new RecurringGenerateResult(true, null, null, occ, Held: true);
            }

            // Nieuw bedrag eerst laten bevestigen: het concept wacht tot de indexatie bevestigd is.
            if (s.IndexationEnabled && s.IndexationConfirm && indexation.Steps > s.IndexationAckCount)
            {
                const string held = "Indexatie wacht op bevestiging";
                if (!s.Events.OrderByDescending(e => e.At).Take(1).Any(e => e.Text == held))
                {
                    s.Events.Add(new RecurringInvoiceEvent { At = DateTime.UtcNow, Text = held, Detail = "Het concept wordt klaargezet zodra je het nieuwe bedrag bevestigt" });
                    await _db.SaveChangesAsync(ct);
                }
                return new RecurringGenerateResult(true, null, null, occ, Held: true);
            }

            var sequence = s.CreatedCount + 1;
            var bo = new InvoiceDraftBO
            {
                IssuerCompanyId = s.IssuerCompanyId,
                InvoiceDate = occ,
                Mode = InvoiceMode.Free,
                ProjectId = s.ProjectId,
                SupplierContractId = s.SupplierContractId,
                IssuerBankAccountId = s.IssuerBankAccountId,
                PaymentTermId = s.PaymentTermId,
                SelectedVatTypeId = s.VatTypeId,
                IsPrepaid = s.IsPrepaid,
                HeaderDescription = RecurringScheduleCalculator.Interpolate(s.HeaderDescription ?? "", s, occ, sequence),
                DetailDescription = RecurringScheduleCalculator.Interpolate(s.DetailDescription ?? "", s, occ, sequence),
                FooterDescription = RecurringScheduleCalculator.Interpolate(s.FooterDescription ?? "", s, occ, sequence)
            };
            if (s.PartyType == 3)
            {
                bo.CompanyId = s.PartyId;
                bo.ClientType = null;
                bo.ClientId = null;
            }
            else
            {
                bo.CompanyId = null;
                bo.ClientType = s.PartyType;
                bo.ClientId = s.PartyId;
            }
            foreach (var l in s.Lines.Where(l => isFirst || !l.FirstOnly).OrderBy(l => l.SortOrder))
            {
                var unit = RecurringScheduleCalculator.LineUnitPrice(s, l, occ, healthIndex);
                bo.Lines.Add(new InvoiceLineBO
                {
                    Text = RecurringScheduleCalculator.Interpolate(l.Text, s, occ, sequence),
                    Quantity = l.Quantity,
                    UnitPrice = unit,
                    Price = Math.Round(l.Quantity * unit, 2, MidpointRounding.AwayFromZero),
                    VatPercentage = l.VatPercentage,
                    VatTypeId = l.VatTypeId ?? s.VatTypeId,
                    LineType = "Free",
                    GroupName = "Vrije lijnen"
                });
            }
            if (bo.Lines.Count == 0)
                throw new InvalidOperationException("Dit schema heeft geen lijnen voor deze factuur.");

            // Dubbel aanmaken voorkomen (twee runs tegelijk, of een eerdere run die halverwege stopte).
            if (await _db.Invoices.AnyAsync(i => i.RecurringScheduleId == s.Id && i.RecurringOccurrence == occ, ct))
                throw new InvalidOperationException("Voor deze factuurdatum bestaat al een factuur.");

            var firstLine = s.Lines.OrderBy(l => l.SortOrder).First().Text;
            var periodText = RecurringScheduleCalculator.PeriodText(s, occ);
            var (invoiceId, _) = await _cmd.CreateWithLinesAsync(bo, issueNow: false, ct);
            try
            {
                var invoice = await _db.Invoices.FirstAsync(i => i.Id == invoiceId, ct);
                invoice.RecurringScheduleId = s.Id;
                invoice.RecurringOccurrence = occ;
                invoice.RecurringLabel = RecurringScheduleCalculator.ListLabel(s, firstLine, occ);
                invoice.RecurringPeriodText = periodText;

                var previousUnpaid = s.WarnUnpaid && s.LastInvoiceId.HasValue && await PreviousUnpaidAsync(s.LastInvoiceId.Value, today, ct);

                s.CreatedCount++;
                s.LastOccurrence = occ;
                s.LastInvoiceId = invoiceId;
                s.UpdatedAt = DateTime.UtcNow;
                RecomputeNext(s, today);
                s.Events.Add(new RecurringInvoiceEvent { At = DateTime.UtcNow, Text = $"Concept {periodText.ToLowerInvariant()} aangemaakt", Detail = user ?? "CRM" });
                if (previousUnpaid)
                    s.Events.Add(new RecurringInvoiceEvent { At = DateTime.UtcNow.AddMilliseconds(1), Text = "De vorige factuur staat nog open", Detail = "Controleer of de klant betaald heeft voor je dit concept nummert" });
                if (s.Status == RecurringInvoiceSchedule.StatusFinished)
                    s.Events.Add(new RecurringInvoiceEvent { At = DateTime.UtcNow.AddMilliseconds(2), Text = "Schema afgelopen", Detail = "Laatste factuur klaargezet" });
                await _db.SaveChangesAsync(ct);
            }
            catch
            {
                // de factuur mag niet los van het schema blijven staan: anders maakt de volgende run een tweede concept voor dezelfde datum
                _db.ChangeTracker.Clear();
                try { await _cmd.DeleteAsync(invoiceId, ct); } catch (Exception ex) { _logger.LogError(ex, "Terugkerende factuur: concept {InvoiceId} kon niet opgeruimd worden", invoiceId); }
                throw;
            }
            return new RecurringGenerateResult(true, null, invoiceId, occ);
        }

        private async Task<bool> PreviousUnpaidAsync(int invoiceId, DateOnly today, CancellationToken ct)
        {
            var prev = await _db.Invoices.AsNoTracking().Where(i => i.Id == invoiceId).Select(i => new { i.StatusId, i.ExpirationDate }).FirstOrDefaultAsync(ct);
            if (prev == null) return false;
            var open = prev.StatusId != InvoiceStatusIds.Paid && prev.StatusId != InvoiceStatusIds.Cancelled && prev.StatusId != InvoiceStatusIds.Draft;
            return open && (!prev.ExpirationDate.HasValue || prev.ExpirationDate.Value < today);
        }

        private async Task NotifyAsync(int issuerCompanyId, List<(string Client, DateOnly Date)> created, CancellationToken ct)
        {
            try
            {
                var settings = await _db.RecurringInvoiceSettings.AsNoTracking().FirstOrDefaultAsync(x => x.IssuerCompanyId == issuerCompanyId, ct);
                var to = NormalizeEmails(settings?.NotifyUsers);
                if (to == null) return;
                var rows = string.Join("", created.Select(c => $"<li>{WebUtility.HtmlEncode(c.Client)} · factuurdatum {c.Date:dd/MM/yyyy}</li>"));
                var html = $"<p>{created.Count} concept{(created.Count == 1 ? "" : "en")} uit terugkerende facturen {(created.Count == 1 ? "staat" : "staan")} klaar ter controle. Je vindt {(created.Count == 1 ? "het" : "ze")} als Proforma in de lijst met facturen.</p><ul>{rows}</ul>";
                var recipients = to.Split(',');
                await _email.SendEmailAsync(recipients[0], created.Count == 1 ? "Een concept terugkerende factuur staat klaar" : $"{created.Count} concepten terugkerende facturen staan klaar", html, cc: recipients.Length > 1 ? string.Join(",", recipients.Skip(1)) : null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Terugkerende facturen: melding voor facturatiebedrijf {IssuerCompanyId} kon niet verstuurd worden", issuerCompanyId);
            }
        }

        // ───────────────────────── pauzeren, hervatten, stoppen ─────────────────────────

        public async Task PauseAsync(int scheduleId, string? user, CancellationToken ct = default)
        {
            var s = await LoadAsync(scheduleId, ct);
            if (s.Status != RecurringInvoiceSchedule.StatusActive)
                throw new InvalidOperationException("Enkel een actief schema kan je pauzeren.");
            s.Status = RecurringInvoiceSchedule.StatusPaused;
            s.PausedAt = DateTime.UtcNow;
            s.UpdatedAt = DateTime.UtcNow;
            s.Events.Add(new RecurringInvoiceEvent { At = DateTime.UtcNow, Text = "Schema gepauzeerd", Detail = user });
            await _db.SaveChangesAsync(ct);
        }

        public async Task<int> CountMissedAsync(int scheduleId, DateOnly today, CancellationToken ct = default)
        {
            var s = await LoadAsync(scheduleId, ct);
            return MissedDates(s, today).Count;
        }

        private static List<DateOnly> MissedDates(RecurringInvoiceSchedule s, DateOnly today)
        {
            var list = new List<DateOnly>();
            if (s.NextOccurrence is null) return list;
            var created = s.CreatedCount;
            for (var k = s.OccurrenceIndex; k < s.OccurrenceIndex + 1200; k++)
            {
                var d = RecurringScheduleCalculator.OccurrenceDate(s, k);
                if (d >= today || !RecurringScheduleCalculator.WithinEnd(s, d)) break;
                if (s.EndMode == 1 && s.EndCount.HasValue && created + list.Count >= s.EndCount.Value) break;
                list.Add(d);
            }
            return list;
        }

        public async Task ResumeAsync(int scheduleId, bool backfillMissed, string? user, DateOnly today, CancellationToken ct = default)
        {
            var s = await LoadAsync(scheduleId, ct);
            if (s.Status != RecurringInvoiceSchedule.StatusPaused)
                throw new InvalidOperationException("Enkel een gepauzeerd schema kan je hervatten.");
            var missed = MissedDates(s, today);
            s.Status = RecurringInvoiceSchedule.StatusActive;
            s.PausedAt = null;
            s.UpdatedAt = DateTime.UtcNow;
            if (missed.Count == 0)
            {
                s.Events.Add(new RecurringInvoiceEvent { At = DateTime.UtcNow, Text = "Schema hervat", Detail = user });
            }
            else if (backfillMissed)
            {
                s.Events.Add(new RecurringInvoiceEvent { At = DateTime.UtcNow, Text = "Schema hervat", Detail = $"{missed.Count} gemiste periode{(missed.Count == 1 ? "" : "s")} wordt alsnog gefactureerd" });
            }
            else
            {
                // gemiste periodes overslaan: de volgende factuur is de eerste datum vanaf vandaag
                var next = RecurringScheduleCalculator.FirstOnOrAfter(s, today, s.OccurrenceIndex);
                s.OccurrenceIndex = next.Index;
                s.NextOccurrence = next.Date;
                if (next.Date is null) s.Status = RecurringInvoiceSchedule.StatusFinished;
                s.Events.Add(new RecurringInvoiceEvent { At = DateTime.UtcNow, Text = "Schema hervat", Detail = $"{missed.Count} gemiste periode{(missed.Count == 1 ? "" : "s")} overgeslagen" });
            }
            await _db.SaveChangesAsync(ct);
        }

        public async Task StopAsync(int scheduleId, string? user, CancellationToken ct = default)
        {
            var s = await LoadAsync(scheduleId, ct);
            if (s.Status is RecurringInvoiceSchedule.StatusStopped or RecurringInvoiceSchedule.StatusFinished)
                return;
            s.Status = RecurringInvoiceSchedule.StatusStopped;
            s.StoppedAt = DateTime.UtcNow;
            s.NextOccurrence = null;
            s.UpdatedAt = DateTime.UtcNow;
            s.Events.Add(new RecurringInvoiceEvent { At = DateTime.UtcNow, Text = "Schema stopgezet", Detail = user });
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(int scheduleId, CancellationToken ct = default)
        {
            var s = await LoadAsync(scheduleId, ct);
            // facturen en concepten blijven bestaan als gewone facturen: enkel de koppeling met het schema verdwijnt
            await _db.Invoices.Where(i => i.RecurringScheduleId == scheduleId)
                .ExecuteUpdateAsync(u => u.SetProperty(i => i.RecurringScheduleId, (int?)null).SetProperty(i => i.RecurringOccurrence, (DateOnly?)null)
                    .SetProperty(i => i.RecurringLabel, (string?)null).SetProperty(i => i.RecurringPeriodText, (string?)null), ct);
            _db.RecurringInvoiceSchedule.Remove(s);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<bool> CanRestoreAsync(int scheduleId, DateOnly occurrence, CancellationToken ct = default)
        {
            var s = await _db.RecurringInvoiceSchedule.AsNoTracking().Where(x => x.Id == scheduleId).Select(x => new { x.Status, x.LastOccurrence, x.CreatedCount }).FirstOrDefaultAsync(ct);
            return s != null && s.CreatedCount > 0 && s.LastOccurrence == occurrence
                && s.Status is RecurringInvoiceSchedule.StatusActive or RecurringInvoiceSchedule.StatusPaused or RecurringInvoiceSchedule.StatusFinished;
        }

        public async Task<bool> HandleDeletedConceptAsync(int scheduleId, DateOnly occurrence, string? periodText, bool restore, string? user, CancellationToken ct = default)
        {
            var s = await _db.RecurringInvoiceSchedule.Include(x => x.Lines).Include(x => x.Events).FirstOrDefaultAsync(x => x.Id == scheduleId, ct);
            if (s == null) return false;
            var label = string.IsNullOrWhiteSpace(periodText) ? occurrence.ToString("dd/MM/yyyy") : periodText.ToLowerInvariant();
            var canRestore = restore && s.CreatedCount > 0 && s.LastOccurrence == occurrence
                && s.Status is RecurringInvoiceSchedule.StatusActive or RecurringInvoiceSchedule.StatusPaused or RecurringInvoiceSchedule.StatusFinished;
            if (!canRestore)
            {
                s.Events.Add(new RecurringInvoiceEvent { At = DateTime.UtcNow, Text = $"Concept {label} verwijderd", Detail = (restore ? "Terugzetten in het schema kon niet" : "Deze periode wordt niet opnieuw aangemaakt") + (string.IsNullOrWhiteSpace(user) ? "" : " · " + user) });
                await _db.SaveChangesAsync(ct);
                return false;
            }
            // de plaats van deze factuurdatum in het patroon zoeken en het schema daar laten hervatten
            var k = -1;
            for (var i = 0; i <= s.OccurrenceIndex; i++)
                if (RecurringScheduleCalculator.OccurrenceDate(s, i) == occurrence) { k = i; break; }
            if (k < 0) return false;
            var prev = await _db.Invoices.AsNoTracking().Where(i => i.RecurringScheduleId == scheduleId && i.RecurringOccurrence < occurrence)
                .OrderByDescending(i => i.RecurringOccurrence).Select(i => new { i.Id, i.RecurringOccurrence }).FirstOrDefaultAsync(ct);
            s.CreatedCount--;
            s.LastOccurrence = prev?.RecurringOccurrence;
            s.LastInvoiceId = prev?.Id;
            s.OccurrenceIndex = k;
            s.NextOccurrence = occurrence;
            if (s.Status == RecurringInvoiceSchedule.StatusFinished) s.Status = RecurringInvoiceSchedule.StatusActive;
            s.UpdatedAt = DateTime.UtcNow;
            s.Events.Add(new RecurringInvoiceEvent { At = DateTime.UtcNow, Text = $"Concept {label} verwijderd", Detail = "Teruggezet in het schema" + (string.IsNullOrWhiteSpace(user) ? "" : " · " + user) });
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task ReactivateAsync(int scheduleId, string? user, DateOnly today, CancellationToken ct = default)
        {
            var s = await LoadAsync(scheduleId, ct);
            if (s.Status != RecurringInvoiceSchedule.StatusStopped)
                throw new InvalidOperationException("Enkel een stopgezet schema kan je opnieuw activeren.");
            if (RecurringScheduleCalculator.CountReached(s))
                throw new InvalidOperationException("Dit schema heeft het aantal facturen bereikt. Pas bij Bewerken het einde aan en probeer opnieuw.");
            var next = RecurringScheduleCalculator.FirstOnOrAfter(s, today, s.OccurrenceIndex);
            if (next.Date is null)
                throw new InvalidOperationException("De einddatum van dit schema is voorbij. Pas bij Bewerken het einde aan en probeer opnieuw.");
            s.Status = RecurringInvoiceSchedule.StatusActive;
            s.StoppedAt = null;
            s.OccurrenceIndex = next.Index;
            s.NextOccurrence = next.Date;
            s.UpdatedAt = DateTime.UtcNow;
            s.Events.Add(new RecurringInvoiceEvent { At = DateTime.UtcNow, Text = "Schema opnieuw geactiveerd", Detail = user });
            await _db.SaveChangesAsync(ct);
        }

        public async Task ConfirmIndexationAsync(int scheduleId, decimal? percent, string? user, CancellationToken ct = default)
        {
            var s = await LoadAsync(scheduleId, ct);
            if (s.NextOccurrence is null || !s.IndexationEnabled)
                throw new InvalidOperationException("Dit schema heeft geen indexatie om te bevestigen.");
            var nl = CultureInfo.GetCultureInfo("nl-BE");
            string eventText;
            if (s.IndexationType == 1)
            {
                // gezondheidsindex: het nieuwe bedrag volgt uit de indexen van Statbel; er is geen percentage in te vullen
                var info = RecurringScheduleCalculator.IndexationAt(s, s.NextOccurrence.Value, await _health.GetAsync(ct));
                if (!info.Ready)
                    throw new InvalidOperationException("Je kan nog niet bevestigen: " + info.Missing + ".");
                eventText = $"Indexatie bevestigd (index {info.NewIndex?.ToString("0.00", nl)} ÷ {info.BaseIndex?.ToString("0.00", nl)})";
            }
            else
            {
                if (percent.HasValue)
                {
                    if (percent.Value < -50m || percent.Value > 100m)
                        throw new InvalidOperationException("Vul een percentage tussen -50 en 100 in.");
                    s.IndexationPercent = percent.Value;
                }
                if (!s.IndexationPercent.HasValue)
                    throw new InvalidOperationException("Vul eerst het percentage van de indexering in.");
                eventText = $"Indexatie bevestigd ({s.IndexationPercent.Value.ToString("0.##", nl)} %)";
            }
            s.IndexationAckCount = Math.Max(s.IndexationAckCount, RecurringScheduleCalculator.IndexationSteps(s, s.NextOccurrence.Value));
            s.UpdatedAt = DateTime.UtcNow;
            s.Events.Add(new RecurringInvoiceEvent { At = DateTime.UtcNow, Text = eventText, Detail = user });
            await _db.SaveChangesAsync(ct);
        }

        private async Task<RecurringInvoiceSchedule> LoadAsync(int id, CancellationToken ct)
            => await _db.RecurringInvoiceSchedule.Include(x => x.Lines).Include(x => x.Events).FirstOrDefaultAsync(x => x.Id == id, ct)
               ?? throw new InvalidOperationException("Schema niet gevonden.");

        private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }
}

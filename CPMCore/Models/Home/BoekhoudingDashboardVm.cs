using BOCore;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Models.Home;

/// <summary>Tint van een statuspil in de tabel "Inkomende facturen" (vertaald naar <c>.gl-v2-badge.is-*</c>).</summary>
public enum BkTone { Positive, Neutral, Attention, Blocked, Info }

/// <summary>Eén inkomende factuur in de dashboardtabel. Geen URL's hier: de view bouwt die uit <see cref="Id"/>.</summary>
public class BkIncomingRowVm
{
    public int Id { get; set; }
    public int? IssuerCompanyId { get; set; }
    public string IssuerName { get; set; } = "";
    public string Supplier { get; set; } = "";
    public string Reference { get; set; } = "";
    public string? ProjectName { get; set; }
    public decimal Amount { get; set; }
    public DateOnly? DueDate { get; set; }
    public string StatusLabel { get; set; } = "";
    public BkTone Tone { get; set; } = BkTone.Neutral;
    public string ActionLabel { get; set; } = "Bekijk";
}

/// <summary>Een regel in het meldingenpaneel van het boekhoudingsdashboard (<c>Group</c>: 0 actie vereist, 1 op te lossen, 2 ter info).</summary>
public class BkMeldingVm
{
    public int Group { get; set; }
    public string Text { get; set; } = "";
    public string Meta { get; set; } = "";
    public string IconClass { get; set; } = "ph-info";
    /// <summary>"Incoming" (documentencentrum, optioneel met <see cref="IncomingId"/>), "IncomingWarnings", "Invoice" (klantfactuur, <see cref="InvoiceId"/>), "Settings" (facturatiebedrijf bewerken) of "None".</summary>
    public string Target { get; set; } = "None";
    public int? IncomingId { get; set; }
    public int? InvoiceId { get; set; }
    public int? IssuerCompanyId { get; set; }
}

/// <summary>Een regel in "Deze maand".</summary>
public class BkAgendaItemVm
{
    public DateOnly Date { get; set; }
    public string Title { get; set; } = "";
    public string Sub { get; set; } = "";
}

/// <summary>Btw-aangifte van één facturatiebedrijf. <see cref="Deadline"/> is null zolang het bedrijf zijn aangiftefrequentie niet instelde.</summary>
public class BkVatReturnVm
{
    public int IssuerCompanyId { get; set; }
    public string IssuerName { get; set; } = "";
    public string? Frequency { get; set; }
    public DateOnly? Deadline { get; set; }
    public string PeriodLabel { get; set; } = "";
    public int DaysLeft { get; set; }
}

/// <summary>Een proforma (concept)factuur die nog definitief gemaakt moet worden.</summary>
public class BkProformaVm
{
    public int InvoiceId { get; set; }
    public int? IssuerCompanyId { get; set; }
    public string IssuerName { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string? ProjectName { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
}

/// <summary>Per klantenaccount: wat er te factureren valt (bereikte schijven waarvan de akte verleden is, nog niet gefactureerd).</summary>
public class BkToBillAccountVm
{
    public int ClientAccountId { get; set; }
    public string ClientName { get; set; } = "";
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public int? IssuerCompanyId { get; set; }
    public string IssuerName { get; set; } = "";
    public DateOnly? DeedDate { get; set; }
    public decimal Amount { get; set; }
    public List<string> Units { get; set; } = new();
    public List<string> Stages { get; set; } = new();
    /// <summary>Ondertekende wijzigingsopdrachten met een nog niet gefactureerd restbedrag ("WO-07 · extra stopcontacten").</summary>
    public List<string> ChangeOrders { get; set; } = new();
}

/// <summary>Een pil in de bedrijfsfilter bovenaan: een facturatiebedrijf waartoe de gebruiker toegang heeft.</summary>
public class BkCompanyOptionVm
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool Selected { get; set; }
    /// <summary>Waarde voor de querystring <c>bedrijven</c> na een klik op deze pil (aan- of uitzetten); null = geen filter.</summary>
    public string? ToggleValue { get; set; }
}

/// <summary>Onderdelen van het boekhoudingsdashboard die nog niet op punt staan en daarom verborgen zijn. Zet een vlag op true om het onderdeel
/// (kaart en bijhorende meldingen) weer te tonen.</summary>
public static class BoekhoudingDashboardFeatures
{
    /// <summary>Kaart "Inkomende facturen" en de meldingen over inkomende facturen.</summary>
    public const bool ShowIncomingInvoices = false;
    /// <summary>Kaart "Openstaand bij klanten" en de meldingen over vervallen klantfacturen.</summary>
    public const bool ShowOpenCustomers = false;
}

/// <summary>Een facturatiebedrijf waartoe de gebruiker toegang heeft, met zijn cijfers uit het uitgaande overzicht.</summary>
public class BkCompanyVm
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int OutstandingCount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public int OverdueCount { get; set; }
    public decimal OverdueAmount { get; set; }
}

/// <summary>Alle gegevens voor het gl-v2 boekhoudingsdashboard (design-handoff Dashboards per rol, punt 1a). Alles is beperkt tot de
/// facturatiebedrijven waartoe de gebruiker toegang heeft (<see cref="Companies"/>) en draagt zijn bedrijf mee. Wat nog niet beschikbaar
/// is (geblokkeerde betalingen) blijft leeg en toont in de view een lege staat.</summary>
public class BoekhoudingDashboardVm
{
    /// <summary>De bedrijven die in de cijfers meetellen (de gekozen pillen, of alle toegelaten bedrijven als er niets gekozen is).</summary>
    public List<BkCompanyVm> Companies { get; set; } = new();
    /// <summary>Alle toegelaten bedrijven als filterpillen; <see cref="BkCompanyOptionVm.Selected"/> volgt de actieve filter.</summary>
    public List<BkCompanyOptionVm> CompanyFilter { get; set; } = new();
    public bool IsFiltered => CompanyFilter.Any(c => c.Selected) && CompanyFilter.Any(c => !c.Selected);
    public bool NoAccess => Companies.Count == 0;

    public int ToApproveCount { get; set; }
    public decimal ToApproveAmount { get; set; }
    public int ToApproveDueThisWeek { get; set; }
    public List<BkIncomingRowVm> ToApprove { get; set; } = new();

    public int ReadyToPayCount { get; set; }
    public decimal ReadyToPayAmount { get; set; }
    public List<BkIncomingRowVm> ReadyToPay { get; set; } = new();

    // Geblokkeerd (inhoudingsplicht, ontbrekend attest tienjarige aansprakelijkheid): nog geen gegevens, dus altijd leeg.
    public int BlockedCount { get; set; }
    public List<BkIncomingRowVm> Blocked { get; set; } = new();

    public List<BkVatReturnVm> VatReturns { get; set; } = new();
    /// <summary>Eerstvolgende aangifte over alle bedrijven met een ingestelde frequentie; null als geen enkel bedrijf er een heeft.</summary>
    public BkVatReturnVm? NextVatReturn { get; set; }

    public List<BkProformaVm> Proformas { get; set; } = new();
    public decimal ProformaAmount { get; set; }
    public List<BkToBillAccountVm> ToBill { get; set; } = new();
    public int ToBillAccountCount { get; set; }
    public decimal ToBillAmount { get; set; }

    public List<BkMeldingVm> Meldingen { get; set; } = new();
    public List<BkAgendaItemVm> Agenda { get; set; } = new();
}

/// <summary>Bouwt <see cref="BoekhoudingDashboardVm"/>; de uitgaande cijfers komen uit de al berekende <see cref="InvoiceDashboardSummaryBO"/>.</summary>
public static class BoekhoudingDashboardBuilder
{
    private const int RowsPerTab = 6;
    private const int MaxToBillAccounts = 30;

    private sealed class Raw
    {
        public int Id { get; set; }
        public int? IssuerCompanyId { get; set; }
        public byte StatusId { get; set; }
        public string? SupplierName { get; set; }
        public string? Reference { get; set; }
        public decimal Price { get; set; }
        public DateOnly? DueDate { get; set; }
        public int? ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public long ReasonFlags { get; set; }
        public bool HasWarnings { get; set; }
    }

    private static readonly System.Globalization.CultureInfo Nl = System.Globalization.CultureInfo.GetCultureInfo("nl-BE");

    /// <summary>De uiterste datum van de volgende btw-aangifte: de 20e van de maand na de periode. Maandelijks: periode is de vorige maand;
    /// per kwartaal: de aangifte van een kwartaal valt op 20 januari, april, juli en oktober.</summary>
    public static (DateOnly Deadline, string PeriodLabel) NextVatReturn(string frequency, DateOnly today)
    {
        if (string.Equals(frequency, "Quarterly", StringComparison.OrdinalIgnoreCase))
        {
            var months = new[] { 1, 4, 7, 10 };
            var year = today.Year;
            var deadline = months.Select(m => new DateOnly(year, m, 20)).FirstOrDefault(d => d >= today);
            if (deadline == default) deadline = new DateOnly(year + 1, 1, 20);
            var quarter = deadline.Month switch { 1 => 4, 4 => 1, 7 => 2, _ => 3 };
            return (deadline, $"{quarter}e kwartaal");
        }
        var monthly = new DateOnly(today.Year, today.Month, 20);
        if (today > monthly) monthly = monthly.AddMonths(1);
        return (monthly, monthly.AddMonths(-1).ToString("MMMM", Nl));
    }

    public static async Task<BoekhoudingDashboardVm> BuildAsync(
        cpmRunningContext db, IReadOnlyList<IssuerCompanyBO> companies, InvoiceDashboardSummaryBO? summary, DateOnly today, CancellationToken ct = default)
    {
        var vm = new BoekhoudingDashboardVm();
        var allowed = companies.Select(c => c.Id).ToList();
        var names = companies.ToDictionary(c => c.Id, c => c.Name ?? "");
        string NameOf(int? id) => id.HasValue && names.TryGetValue(id.Value, out var n) ? n : "";

        vm.Companies = companies.Select(c =>
        {
            var line = summary?.ByCompany.FirstOrDefault(l => l.IssuerCompanyId == c.Id);
            return new BkCompanyVm
            {
                Id = c.Id,
                Name = c.Name ?? "",
                OutstandingCount = line?.OutstandingCount ?? 0,
                OutstandingAmount = line?.OutstandingAmount ?? 0m,
                OverdueCount = line?.OverdueCount ?? 0,
                OverdueAmount = line?.OverdueAmount ?? 0m
            };
        }).ToList();
        if (allowed.Count == 0) return vm;

        // ── Btw-aangifte per facturatiebedrijf ──
        foreach (var c in companies)
        {
            var item = new BkVatReturnVm { IssuerCompanyId = c.Id, IssuerName = c.Name ?? "", Frequency = c.VatReturnFrequency };
            if (!string.IsNullOrWhiteSpace(c.VatReturnFrequency))
            {
                var (deadline, label) = NextVatReturn(c.VatReturnFrequency, today);
                item.Deadline = deadline;
                item.PeriodLabel = label;
                item.DaysLeft = deadline.DayNumber - today.DayNumber;
            }
            vm.VatReturns.Add(item);
        }
        vm.NextVatReturn = vm.VatReturns.Where(v => v.Deadline.HasValue).OrderBy(v => v.Deadline).FirstOrDefault();

        // ── Inkomende facturen: enkel documentencentrum-records van de toegelaten bedrijven, niet-betaalde stadia ──
        var toApproveStatuses = new byte[] { IncomingInvoiceStatus.New, IncomingInvoiceStatus.PendingApproval, IncomingInvoiceStatus.Enriched, IncomingInvoiceStatus.NeedsReview };
        var readyStatuses = new byte[] { IncomingInvoiceStatus.Approved, IncomingInvoiceStatus.Booked };
        var open = toApproveStatuses.Concat(readyStatuses).ToArray();

        var rows = await db.IncommingInvoices.AsNoTracking()
            .Where(i => i.IssuerCompanyId != null && allowed.Contains(i.IssuerCompanyId.Value) && open.Contains(i.StatusId))
            .Select(i => new Raw
            {
                Id = i.Id,
                IssuerCompanyId = i.IssuerCompanyId,
                StatusId = i.StatusId,
                SupplierName = i.SupplierName,
                Reference = i.ExternalId,
                Price = i.Price,
                DueDate = i.DueDate,
                ProjectId = i.ProjectId,
                ProjectName = i.Project != null ? i.Project.ProjectName : null,
                ReasonFlags = i.Enrichment != null ? i.Enrichment.ReasonFlags : 0L,
                HasWarnings = i.Warnings.Any(w => !w.IsResolved)
            })
            .ToListAsync(ct);

        BkIncomingRowVm Map(Raw r)
        {
            var status = r.StatusId;
            var duplicate = ((InvoiceReasonFlags)r.ReasonFlags & InvoiceReasonFlags.DuplicateSuspected) != 0;
            string label = IncomingInvoiceStatus.Label(status);
            var tone = BkTone.Neutral;
            var action = "Bekijk";
            if (duplicate) { label = "Mogelijk dubbel"; tone = BkTone.Blocked; action = "Vergelijk"; }
            else if (r.ProjectId == null && toApproveStatuses.Contains(status)) { label = "Geen project"; tone = BkTone.Attention; action = "Koppelen"; }
            else if (status == IncomingInvoiceStatus.Enriched) { label = "Klaar om te boeken"; tone = BkTone.Positive; action = "Goedkeuren"; }
            else if (status == IncomingInvoiceStatus.PendingApproval) { label = "Te keuren"; tone = BkTone.Attention; action = "Goedkeuren"; }
            else if (status == IncomingInvoiceStatus.NeedsReview) { label = "Vraagt aandacht"; tone = BkTone.Attention; }
            else if (status == IncomingInvoiceStatus.New) { label = "Nieuw"; tone = BkTone.Info; }
            else if (status == IncomingInvoiceStatus.Approved) { label = "Goedgekeurd"; tone = BkTone.Positive; }
            else if (status == IncomingInvoiceStatus.Booked) { label = "Geboekt"; tone = BkTone.Positive; }
            return new BkIncomingRowVm
            {
                Id = r.Id,
                IssuerCompanyId = r.IssuerCompanyId,
                IssuerName = NameOf(r.IssuerCompanyId),
                Supplier = r.SupplierName ?? "",
                Reference = r.Reference ?? "",
                ProjectName = r.ProjectName,
                Amount = r.Price,
                DueDate = r.DueDate,
                StatusLabel = label,
                Tone = tone,
                ActionLabel = action
            };
        }

        var toApprove = rows.Where(r => toApproveStatuses.Contains(r.StatusId)).OrderBy(r => r.DueDate ?? DateOnly.MaxValue).ToList();
        var ready = rows.Where(r => readyStatuses.Contains(r.StatusId)).OrderBy(r => r.DueDate ?? DateOnly.MaxValue).ToList();

        vm.ToApproveCount = toApprove.Count;
        vm.ToApproveAmount = toApprove.Sum(r => r.Price);
        vm.ToApproveDueThisWeek = toApprove.Count(r => r.DueDate.HasValue && r.DueDate.Value <= today.AddDays(7));
        vm.ToApprove = toApprove.Take(RowsPerTab).Select(Map).ToList();
        vm.ReadyToPayCount = ready.Count;
        vm.ReadyToPayAmount = ready.Sum(r => r.Price);
        vm.ReadyToPay = ready.Take(RowsPerTab).Select(Map).ToList();

        // ── Klaar om te factureren, deel 1: proforma's (conceptfacturen aan klanten) die nog definitief gemaakt moeten worden ──
        // CompanyId == null = factuur aan een klant (een waarde = leveranciersfactuur, zie InvoiceListItemBO.IsSupplier).
        var proformas = await (
            from i in db.Invoices.AsNoTracking()
            join bal in db.VwInvoiceBalance.AsNoTracking() on i.Id equals bal.Id
            where i.StatusId == (byte)ServiceCore.Helpers.InvoiceStatusId.Draft
                  && i.CompanyId == null
                  && i.IssuerCompanyId != null && allowed.Contains(i.IssuerCompanyId.Value)
            orderby i.Date
            select new { i.Id, i.IssuerCompanyId, i.ClientName, i.ProjectId, i.Date, Gross = bal.GrossTotal ?? 0m })
            .ToListAsync(ct);
        var proformaProjectIds = proformas.Where(p => p.ProjectId.HasValue).Select(p => p.ProjectId!.Value).Distinct().ToList();
        var proformaProjects = proformaProjectIds.Count == 0
            ? new Dictionary<int, string>()
            : await db.Project.AsNoTracking().Where(p => proformaProjectIds.Contains(p.ProjectId)).ToDictionaryAsync(p => p.ProjectId, p => p.ProjectName ?? "", ct);
        vm.ProformaAmount = proformas.Sum(p => p.Gross);
        vm.Proformas = proformas.Select(p => new BkProformaVm
        {
            InvoiceId = p.Id,
            IssuerCompanyId = p.IssuerCompanyId,
            IssuerName = NameOf(p.IssuerCompanyId),
            ClientName = p.ClientName ?? "",
            ProjectName = p.ProjectId.HasValue && proformaProjects.TryGetValue(p.ProjectId.Value, out var pn) ? pn : null,
            Amount = p.Gross,
            Date = p.Date
        }).ToList();

        // ── Klaar om te factureren, deel 2: per klantenaccount de bereikte schijven waarvan de akte verleden is ──
        // Zelfde regels als ProjectenController.InvoicingV2: akte t.e.m. morgen, schijf bereikt (groep-brede vlag of per eenheid),
        // nog niet op een factuur (ook niet op een proforma: die staat hierboven). Het facturatiebedrijf is dat van de bouwheer (aanneming).
        var cutoff = today.AddDays(1);
        var units = await db.Units.AsNoTracking()
            .Where(u => u.ClientAccountId > 0
                        && u.ClientAccount.DateDeedOfSale != null && u.ClientAccount.DateDeedOfSale.Value <= cutoff
                        && u.Project.IssuerCompanyIdBuilder != null && allowed.Contains(u.Project.IssuerCompanyIdBuilder.Value))
            .Select(u => new
            {
                u.Id,
                u.Name,
                TypeName = u.Type != null ? u.Type.Name : null,
                u.ProjectId,
                ProjectName = u.Project.ProjectName,
                Issuer = u.Project.IssuerCompanyIdBuilder,
                AccountId = u.ClientAccountId!.Value,
                ClientName = u.ClientAccount.Name,
                u.ClientAccount.CompanyName,
                Deed = u.ClientAccount.DateDeedOfSale
            })
            .ToListAsync(ct);

        var accounts = new Dictionary<int, BkToBillAccountVm>();
        if (units.Count > 0)
        {
            var unitIds = units.Select(u => u.Id).ToList();
            var values = await db.UnitConstructionValue.AsNoTracking()
                .Where(v => unitIds.Contains(v.UnitId) && v.ValueSold > 0 && v.PaymentGroupId != null)
                .Select(v => new { v.UnitId, v.ValueSold, GroupId = v.PaymentGroupId!.Value })
                .ToListAsync(ct);
            var groupIds = values.Select(v => v.GroupId).Distinct().ToList();
            var stages = await db.InvoicingPaymentStages.AsNoTracking().Where(s => groupIds.Contains(s.GroupId)).OrderBy(s => s.Id)
                .Select(s => new { s.Id, s.GroupId, s.Name, s.Percentage, s.Invoicable })
                .ToListAsync(ct);
            var invoiced = (await db.InvoicesDetails.AsNoTracking()
                    .Where(d => d.LineType == "Stages" && d.PaymentStageId != null && d.UnitId != null && unitIds.Contains(d.UnitId.Value))
                    .Select(d => new { StageId = d.PaymentStageId!.Value, UnitId = d.UnitId!.Value })
                    .ToListAsync(ct))
                .Select(x => (x.StageId, x.UnitId)).ToHashSet();
            var reached = (await db.UnitPaymentStageReached.AsNoTracking()
                    .Where(r => unitIds.Contains(r.UnitId))
                    .Select(r => new { r.PaymentStageId, r.UnitId })
                    .ToListAsync(ct))
                .Select(x => (x.PaymentStageId, x.UnitId)).ToHashSet();

            var unitsById = units.ToDictionary(u => u.Id);
            foreach (var v in values)
            {
                if (!unitsById.TryGetValue(v.UnitId, out var u)) continue;
                foreach (var stage in stages.Where(s => s.GroupId == v.GroupId))
                {
                    if (invoiced.Contains((stage.Id, u.Id))) continue;
                    if (!stage.Invoicable && !reached.Contains((stage.Id, u.Id))) continue;
                    var amount = Math.Round(v.ValueSold!.Value * stage.Percentage / 100m, 2, MidpointRounding.AwayFromZero);
                    if (amount <= 0m) continue;

                    if (!accounts.TryGetValue(u.AccountId, out var acc))
                    {
                        acc = new BkToBillAccountVm
                        {
                            ClientAccountId = u.AccountId,
                            ClientName = !string.IsNullOrWhiteSpace(u.ClientName) ? u.ClientName! : u.CompanyName ?? "",
                            ProjectId = u.ProjectId,
                            ProjectName = u.ProjectName ?? "",
                            IssuerCompanyId = u.Issuer,
                            IssuerName = NameOf(u.Issuer),
                            DeedDate = u.Deed
                        };
                        accounts[u.AccountId] = acc;
                    }
                    acc.Amount += amount;
                    var unitName = $"{u.TypeName} {u.Name}".Trim();
                    if (unitName.Length > 0 && !acc.Units.Contains(unitName)) acc.Units.Add(unitName);
                    acc.Stages.Add(stage.Name ?? "Schijf");
                }
            }
        }

        // ── Klaar om te factureren, deel 3: ondertekende wijzigingsopdrachten met een openstaand restbedrag ──
        // Zelfde regels als InvoicingV2 (GetProjectInvoicableChangeOrders): Invoiceable, ondertekend (DateAgreement), klantenaccount, minstens
        // één detailregel die factureerbaar en nog niet gefactureerd is. Restbedrag per regel = totaal min wat al op een factuur (of proforma) staat.
        var allowedProjects = await db.Project.AsNoTracking()
            .Where(p => p.IssuerCompanyIdBuilder != null && allowed.Contains(p.IssuerCompanyIdBuilder.Value))
            .Select(p => new { p.ProjectId, p.ProjectName, Issuer = p.IssuerCompanyIdBuilder })
            .ToDictionaryAsync(p => p.ProjectId, ct);
        if (allowedProjects.Count > 0)
        {
            var projectIds = allowedProjects.Keys.ToList();
            var orders = await db.ChangeOrder.AsNoTracking()
                .Where(m => m.Invoiceable && m.DateAgreement != null && m.ClientAccountId > 0
                            && projectIds.Contains(m.ContractActivity.Contract.ProjectId)
                            && m.ChangeOrderDetail.Any(d => d.Invoicable != false && d.Invoiced != true))
                .Select(m => new
                {
                    m.Id,
                    m.PublicNumber,
                    m.Subject,
                    m.Description,
                    ProjectId = m.ContractActivity.Contract.ProjectId,
                    m.ClientAccountId,
                    ClientName = m.ClientAccount.Name,
                    m.ClientAccount.CompanyName,
                    Deed = m.ClientAccount.DateDeedOfSale,
                    Details = m.ChangeOrderDetail
                        .Where(d => d.Invoicable != false && d.Invoiced != true)
                        .Select(d => new { d.Id, d.Number, d.Price, d.Commission })
                        .ToList()
                })
                .ToListAsync(ct);

            var detailIds = orders.SelectMany(o => o.Details.Select(d => d.Id)).ToList();
            var invoicedByDetail = detailIds.Count == 0
                ? new Dictionary<int, decimal>()
                : (await db.InvoicesDetails.AsNoTracking()
                        .Where(d => d.LineType == "ChangeOrders" && d.ChangeOrderDetailId != null && detailIds.Contains(d.ChangeOrderDetailId.Value))
                        .GroupBy(d => d.ChangeOrderDetailId!.Value)
                        .Select(g => new { DetailId = g.Key, Amount = g.Sum(x => x.Price ?? 0m) })
                        .ToListAsync(ct))
                    .ToDictionary(x => x.DetailId, x => x.Amount);

            foreach (var o in orders)
            {
                var remaining = 0m;
                foreach (var d in o.Details)
                {
                    var total = Math.Round(d.Number * d.Price * (1m + d.Commission / 100m), 2, MidpointRounding.AwayFromZero);
                    invoicedByDetail.TryGetValue(d.Id, out var already);
                    var rest = Math.Round(total - already, 2, MidpointRounding.AwayFromZero);
                    if (rest > 0m) remaining += rest;
                }
                if (remaining <= 0m) continue;

                var project = allowedProjects[o.ProjectId];
                if (!accounts.TryGetValue(o.ClientAccountId, out var acc))
                {
                    acc = new BkToBillAccountVm
                    {
                        ClientAccountId = o.ClientAccountId,
                        ClientName = !string.IsNullOrWhiteSpace(o.ClientName) ? o.ClientName! : o.CompanyName ?? "",
                        ProjectId = o.ProjectId,
                        ProjectName = project.ProjectName ?? "",
                        IssuerCompanyId = project.Issuer,
                        IssuerName = NameOf(project.Issuer),
                        DeedDate = o.Deed
                    };
                    accounts[o.ClientAccountId] = acc;
                }
                acc.Amount += remaining;
                var title = !string.IsNullOrWhiteSpace(o.Subject) ? o.Subject : o.Description;
                acc.ChangeOrders.Add(string.IsNullOrWhiteSpace(title) ? o.PublicNumber ?? "Wijzigingsopdracht" : $"{o.PublicNumber} · {title}");
            }
        }

        var ordered = accounts.Values.OrderByDescending(a => a.Amount).ToList();
        vm.ToBillAccountCount = ordered.Count;
        vm.ToBillAmount = ordered.Sum(a => a.Amount);
        vm.ToBill = ordered.Take(MaxToBillAccounts).ToList();

        // ── Meldingen ──
        if (BoekhoudingDashboardFeatures.ShowIncomingInvoices)
        foreach (var r in toApprove.Concat(ready).Where(r => ((InvoiceReasonFlags)r.ReasonFlags & InvoiceReasonFlags.DuplicateSuspected) != 0).OrderBy(r => r.DueDate ?? DateOnly.MaxValue).Take(3))
            vm.Meldingen.Add(new BkMeldingVm
            {
                Group = 0,
                Text = $"Mogelijk dubbele factuur: {r.SupplierName} {r.Reference} (€ {r.Price.ToString("N2", Nl)}).",
                Meta = Join("Inkomende facturen", NameOf(r.IssuerCompanyId)),
                IconClass = "ph-copy",
                Target = "Incoming",
                IncomingId = r.Id
            });

        foreach (var g in BoekhoudingDashboardFeatures.ShowIncomingInvoices
                     ? toApprove.Where(r => r.ProjectId == null).GroupBy(r => r.IssuerCompanyId).OrderBy(g => NameOf(g.Key))
                     : Enumerable.Empty<IGrouping<int?, Raw>>())
        {
            var n = g.Count();
            vm.Meldingen.Add(new BkMeldingVm
            {
                Group = 1,
                Text = $"{n} inkomende {(n == 1 ? "factuur" : "facturen")} zonder project. Koppel ze voor de nacalculatie klopt.",
                Meta = Join("Inkomende facturen", NameOf(g.Key)),
                IconClass = "ph-link-simple-break",
                Target = "Incoming"
            });
        }

        foreach (var g in BoekhoudingDashboardFeatures.ShowIncomingInvoices
                     ? toApprove.Concat(ready).Where(r => r.HasWarnings).GroupBy(r => r.IssuerCompanyId).OrderBy(g => NameOf(g.Key))
                     : Enumerable.Empty<IGrouping<int?, Raw>>())
        {
            var n = g.Count();
            vm.Meldingen.Add(new BkMeldingVm
            {
                Group = 1,
                Text = $"{n} inkomende {(n == 1 ? "factuur heeft" : "facturen hebben")} een onopgeloste waarschuwing.",
                Meta = Join("Inkomende facturen", NameOf(g.Key)),
                IconClass = "ph-warning-circle",
                Target = "IncomingWarnings"
            });
        }

        if (summary != null && BoekhoudingDashboardFeatures.ShowOpenCustomers)
            foreach (var line in summary.TopOverdue.Where(l => l.DaysOverdue > 30).Take(3))
                vm.Meldingen.Add(new BkMeldingVm
                {
                    Group = 1,
                    Text = $"{line.ClientName}: € {line.Balance.ToString("N2", Nl)} is al {line.DaysOverdue} dagen vervallen.",
                    Meta = Join("Klantfacturen", NameOf(line.IssuerCompanyId), line.ProjectName),
                    IconClass = "ph-clock-countdown",
                    Target = "Invoice",
                    InvoiceId = line.InvoiceId
                });

        if (vm.Proformas.Count > 0)
            foreach (var g in vm.Proformas.GroupBy(p => p.IssuerCompanyId).OrderBy(g => NameOf(g.Key)))
            {
                var n = g.Count();
                vm.Meldingen.Add(new BkMeldingVm
                {
                    Group = 1,
                    Text = $"{n} {(n == 1 ? "proforma wacht" : "proforma's wachten")} op een definitieve factuur.",
                    Meta = Join("Klaar om te factureren", NameOf(g.Key)),
                    IconClass = "ph-file-dashed",
                    Target = "None"
                });
            }

        foreach (var v in vm.VatReturns)
        {
            if (v.Deadline is null)
                vm.Meldingen.Add(new BkMeldingVm
                {
                    Group = 2,
                    Text = "Stel in hoe vaak je de btw-aangifte indient, dan toont het dashboard de uiterste datum.",
                    Meta = Join("Btw-aangifte", v.IssuerName),
                    IconClass = "ph-calendar-x",
                    Target = "Settings",
                    IssuerCompanyId = v.IssuerCompanyId
                });
            else if (v.DaysLeft <= 21)
                vm.Meldingen.Add(new BkMeldingVm
                {
                    Group = 2,
                    Text = $"Btw-aangifte {v.PeriodLabel} moet uiterlijk {v.Deadline:dd/MM} ingediend zijn (nog {v.DaysLeft} {(v.DaysLeft == 1 ? "dag" : "dagen")}).",
                    Meta = Join("Btw-aangifte", v.IssuerName),
                    IconClass = "ph-calendar-check"
                });
        }

        // ── Deze maand ──
        var monthEnd = new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
        var agenda = new List<BkAgendaItemVm>();
        foreach (var v in vm.VatReturns.Where(v => v.Deadline.HasValue && v.Deadline.Value >= today && v.Deadline.Value <= monthEnd))
            agenda.Add(new BkAgendaItemVm { Date = v.Deadline!.Value, Title = $"Btw-aangifte {v.PeriodLabel}", Sub = Join("Indienen via Intervat", v.IssuerName) });
        // Afsluiting volgt het ritme van het bedrijf (zelfde instelling als de btw-aangifte): maandelijks elke maand, per kwartaal enkel op het
        // einde van een kwartaal. Zonder ingesteld ritme weten we het niet, dus geen regel. Elke regel toont zijn bedrijf.
        foreach (var c in companies)
        {
            if (string.Equals(c.VatReturnFrequency, "Monthly", StringComparison.OrdinalIgnoreCase))
                agenda.Add(new BkAgendaItemVm { Date = monthEnd, Title = "Maandafsluiting", Sub = Join("Export naar boekhouder", c.Name) });
            else if (string.Equals(c.VatReturnFrequency, "Quarterly", StringComparison.OrdinalIgnoreCase) && monthEnd.Month % 3 == 0)
                agenda.Add(new BkAgendaItemVm { Date = monthEnd, Title = "Kwartaalafsluiting", Sub = Join("Export naar boekhouder", c.Name) });
        }
        vm.Agenda = agenda.OrderBy(a => a.Date).ToList();

        return vm;
    }

    private static string Join(params string?[] parts) => string.Join(" · ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
}

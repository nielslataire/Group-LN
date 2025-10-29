using BOCore;
using DALCore;
using DALCore.Models;
using DALCore.Query;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Helpers;
using ServiceCore.Translators;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceCore
{
    public class InvoiceCommandService : IInvoiceCommandService
    {
        private readonly UnitOfWorkCore _uow;
        private readonly cpmRunningContext _db;
        private readonly IInvoiceNumberingService _num;

        public InvoiceCommandService(UnitOfWorkCore uow, IInvoiceNumberingService num)
        {
            _uow = uow;
            _db = (cpmRunningContext)uow.Context;
            _num = num;
        }

        public async Task<(int invoiceId, string? publicId)> CreateWithLinesAsync(InvoiceDraftBO bo, bool issueNow, CancellationToken ct = default)
        {
            // 🔹 0) Schijven → lijnen genereren indien UI geen lijnen heeft gepost
            if (bo.Mode == InvoiceMode.Stages && (bo.Lines == null || bo.Lines.Count == 0))
                await BuildLinesForStagesAsync(bo, ct);

            // issuer + default term
            var issuer = await _db.IssuerCompany.FirstAsync(x => x.Id == bo.IssuerCompanyId && x.IsActive, ct);
            int? defaultDays = null;
            if (issuer.DefaultPaymentTermId.HasValue)
                defaultDays = await _db.PaymentTerms
                    .Where(t => t.Id == issuer.DefaultPaymentTermId.Value)
                    .Select(t => (int?)t.Days)
                    .FirstOrDefaultAsync(ct);

            var due = bo.ExpirationDate ?? (defaultDays.HasValue ? bo.InvoiceDate.AddDays(defaultDays.Value) : (DateOnly?)null);

            string? issuerBankAccountIban = null;
            if (bo.IssuerBankAccountId is int bankAccountId)
            {
                issuerBankAccountIban = await _db.IssuerBankAccount
                    .Where(x => x.Id == bankAccountId && x.IssuerCompanyId == bo.IssuerCompanyId)
                    .Select(x => x.Iban)
                    .FirstOrDefaultAsync(ct);

                if (issuerBankAccountIban is null)
                    throw new InvalidOperationException("Geselecteerd rekeningnummer hoort niet bij dit facturatiebedrijf.");
            }
            else
            {
                issuerBankAccountIban = await _db.IssuerBankAccount
                    .Where(x => x.IssuerCompanyId == bo.IssuerCompanyId && x.IsDefault)
                    .Select(x => x.Iban)
                    .FirstOrDefaultAsync(ct);
            }

            var party = await ResolvePartySnapshotAsync(bo, ct);

            var headerText = Clean(bo.HeaderDescription);
            var detailText = Clean(bo.DetailDescription);
            var bankAccount = Clean(issuerBankAccountIban);
            var invoiceText = headerText ?? detailText;
            var footerText = Clean(bo.FooterDescription);

            // status ids
            var draftId = await _db.InvoiceStatusLookup.Where(s => s.Name == "Draft").Select(s => (byte?)s.Id).FirstOrDefaultAsync(ct) ?? (byte)1;
            var issuedId = await _db.InvoiceStatusLookup.Where(s => s.Name == "Issued").Select(s => (byte?)s.Id).FirstOrDefaultAsync(ct) ?? (byte)2;

            await using var tx = await _uow.BeginTransactionAsync(ct);
            try
            {
                var inv = new Invoices
                {
                    IssuerCompanyId = bo.IssuerCompanyId,
                    Date = bo.InvoiceDate,
                    ExpirationDate = due,
                    StatusId = draftId,
                    PublicId = null,
                    ClientType = bo.CompanyId.HasValue ? null : bo.ClientType,
                    ClientId = bo.CompanyId.HasValue ? null : bo.ClientId,
                    CompanyId = bo.CompanyId,
                    InvoiceMode = (byte)bo.Mode,
                    ProjectId = bo.ProjectId,
                    SupplierContractId = bo.SupplierContractId,
                    HeaderDescription = headerText,
                    Text = invoiceText,
                    DetailText = detailText,
                    ClientName = party.ClientName,
                    Adress = party.Address,
                    PostalCodeId = party.PostalCodeId,
                    VatNumber = party.VatNumber,
                    ExtraInfo = footerText ?? party.ExtraInfo,
                    BankAccount = bankAccount,
                    PaymentTermId = bo.PaymentTermId
                };
                _uow.Invoices.Add(inv);
                await _uow.SaveChangesAsync(ct); // Id is nu bekend

                // lines
                foreach (var l in bo.Lines)
                {
                    // normaliseer korting
                    decimal? discAmt = l.DiscountAmount;
                    decimal? discPct = l.DiscountPercent;

                    if (discPct.HasValue && !discAmt.HasValue)
                        discAmt = Math.Round(l.Price * (discPct.Value / 100m), 4, MidpointRounding.AwayFromZero);
                    else if (discAmt.HasValue && !discPct.HasValue && l.Price != 0)
                        discPct = Math.Round((discAmt.Value / l.Price) * 100m, 4, MidpointRounding.AwayFromZero);

                    _uow.InvoiceDetails.Add(new InvoicesDetails
                    {
                        InvoiceId = inv.Id,
                        Text = (l.Text ?? "").Trim().Length > 200 ? (l.Text ?? "").Trim().Substring(0, 200) : (l.Text ?? "").Trim(),
                        Price = l.Price,
                        VatPercentage = l.VatPercentage,
                        DiscountPercent = discPct,
                        DiscountAmount = discAmt,
                        UnitId = l.UnitId,
                        PaymentStageId = l.PaymentStageId,   // ← bij schijven vullen we dit in met Stage.Id
                        LineType = string.IsNullOrWhiteSpace(l.LineType) ? null : l.LineType.Trim(),
                        GroupName = string.IsNullOrWhiteSpace(l.GroupName) ? null : l.GroupName.Trim(),
                        UtilityCost = l.UtilityCost,
                        // ConstructionValued / ChangeOrderDetailId blijven null
                    });
                }
                await _uow.SaveChangesAsync(ct);

                string? publicId = null;
                if (issueNow)
                {
                    // default reeks kiezen (verbeteren we later met expliciete reeks-keuze)
                    var seriesId = await _db.InvoiceSeries
                        .Where(s => s.IssuerCompanyId == bo.IssuerCompanyId && s.IsActive)
                        .OrderBy(s => s.Id)
                        .Select(s => (int?)s.Id)
                        .FirstOrDefaultAsync(ct)
                        ?? throw new InvalidOperationException("Geen actieve nummerreeks voor dit bedrijf.");

                    var fiscalYear = bo.InvoiceDate.Year;
                    var issue = await _num.IssueAsync(
                        inv.Id,
                        seriesId,
                        new DateTime(bo.InvoiceDate.Year, bo.InvoiceDate.Month, bo.InvoiceDate.Day),
                        fiscalYear,
                        ct);

                    publicId = issue.publicId;

                    inv.StatusId = issuedId;
                    inv.PublicId = publicId;
                    inv.SeriesId = seriesId;
                    inv.FiscalYear = fiscalYear;
                    await _uow.SaveChangesAsync(ct);
                }

                await _uow.CommitTransactionAsync(_uow.CurrentTransaction!, ct);
                return (inv.Id, publicId);
            }
            catch
            {
                await _uow.RollbackTransactionAsync(_uow.CurrentTransaction!, ct);
                throw;
            }
        }


        public async Task<int> CreateDraftAsync(InvoiceDraftBO bo, CancellationToken ct = default)
        {
            var (id, _) = await CreateWithLinesAsync(bo, issueNow: false, ct);
            return id;
        }

        public async Task DeleteAsync(int invoiceId, CancellationToken ct = default)
        {
            await using var tx = await _uow.BeginTransactionAsync(ct);
            try
            {
                var invoice = await _db.Invoices
                    .Include(i => i.IssuerCompany)
                    .FirstOrDefaultAsync(i => i.Id == invoiceId, ct)
                    ?? throw new InvalidOperationException("Factuur niet gevonden.");

                InvoiceSequence? sequence = null;
                int? invoiceNumber = null;
                string pattern = invoice.IssuerCompany?.InvoiceNumberPattern ?? "{num:0000}/{date:yyyy}";

                if (invoice.SeriesId is int seriesId && invoice.FiscalYear is int fiscalYear && !string.IsNullOrWhiteSpace(invoice.PublicId))
                {
                    sequence = await _db.InvoiceSequence
                        .FirstOrDefaultAsync(s => s.SeriesId == seriesId && s.FiscalYear == fiscalYear, ct);

                    invoiceNumber = ExtractSequenceNumber(pattern, invoice.PublicId, invoice.Date);

                    if (sequence != null)
                    {
                        if (!invoiceNumber.HasValue)
                            throw new InvalidOperationException("Factuurnummer kon niet bepaald worden; verwijderen is niet mogelijk.");

                        if (sequence.CurrentNumber > invoiceNumber.Value)
                            throw new InvalidOperationException("Factuur is niet de laatste in de reeks en kan niet verwijderd worden.");

                        var higherNumbers = await _db.Invoices
                            .Where(i => i.SeriesId == seriesId && i.FiscalYear == fiscalYear && i.Id != invoiceId && i.PublicId != null)
                            .Select(i => new { i.PublicId, i.Date })
                            .ToListAsync(ct);

                        if (higherNumbers.Any(o =>
                        {
                            var otherNumber = ExtractSequenceNumber(pattern, o.PublicId!, o.Date);
                            return otherNumber.HasValue && otherNumber.Value > invoiceNumber.Value;
                        }))
                            throw new InvalidOperationException("Factuur is niet de laatste in de reeks en kan niet verwijderd worden.");
                    }
                }

                var hasPayments = await _db.PaymentAllocations.AnyAsync(p => p.InvoiceId == invoiceId, ct);
                if (hasPayments)
                    throw new InvalidOperationException("Factuur heeft betalingen en kan niet verwijderd worden.");

                var hasReplacements = await _db.Invoices.AnyAsync(i => i.ReplacementOfId == invoiceId, ct);
                if (hasReplacements)
                    throw new InvalidOperationException("Factuur heeft opvolgers en kan niet verwijderd worden.");

                var detailRows = await _db.InvoicesDetails
                    .Where(d => d.InvoiceId == invoiceId)
                    .ToListAsync(ct);
                if (detailRows.Count > 0)
                    _db.InvoicesDetails.RemoveRange(detailRows);

                var relations = await _db.InvoiceRelations
                    .Where(r => r.ParentInvoiceId == invoiceId || r.ChildInvoiceId == invoiceId)
                    .ToListAsync(ct);
                if (relations.Count > 0)
                    _db.InvoiceRelations.RemoveRange(relations);

                var emailLogs = await _db.InvoiceEmailLog
                    .Where(e => e.InvoiceId == invoiceId)
                    .ToListAsync(ct);
                if (emailLogs.Count > 0)
                    _db.InvoiceEmailLog.RemoveRange(emailLogs);

                var dunnings = await _db.InvoiceDunning
                    .Where(d => d.InvoiceId == invoiceId)
                    .ToListAsync(ct);
                if (dunnings.Count > 0)
                    _db.InvoiceDunning.RemoveRange(dunnings);

                var attachments = await _db.InvoiceAttachments
                    .Where(a => a.InvoiceId == invoiceId)
                    .ToListAsync(ct);
                if (attachments.Count > 0)
                    _db.InvoiceAttachments.RemoveRange(attachments);

                var pdfArchives = await _db.InvoicePdfArchive
                    .Where(p => p.InvoiceId == invoiceId)
                    .ToListAsync(ct);
                if (pdfArchives.Count > 0)
                    _db.InvoicePdfArchive.RemoveRange(pdfArchives);

                var ublDocs = await _db.InvoiceUbl
                    .Where(u => u.InvoiceId == invoiceId)
                    .ToListAsync(ct);
                if (ublDocs.Count > 0)
                    _db.InvoiceUbl.RemoveRange(ublDocs);

                _uow.Invoices.Remove(invoice);

                if (sequence != null && invoiceNumber.HasValue && sequence.CurrentNumber == invoiceNumber.Value)
                {
                    sequence.CurrentNumber = Math.Max(0, invoiceNumber.Value - 1);
                }

                await _uow.SaveChangesAsync(ct);
                await _uow.CommitTransactionAsync(tx, ct);
            }
            catch
            {
                await _uow.RollbackTransactionAsync(tx, ct);
                throw;
            }
        }


        // ---------- alleen voor modus SCHIJVEN ----------
        // Bouwt lijnen voor modus "Schijven" op basis van gekozen PaymentGroup + StageIds.
        // - Price = 0m (rekenbasis volgt later: contract/units * percentage)
        // - VatPercentage: Stage.VatPercentage of fallback Group.VatPercentage of 21
        // - PaymentStageId wordt gezet naar Stage.Id (handig voor rapportering/trace)
        private async Task BuildLinesForStagesAsync(InvoiceDraftBO bo, CancellationToken ct)
        {
            if (bo.StageIds == null || bo.StageIds.Count == 0)
                return;

            var stages = await (
                from s in _db.InvoicingPaymentStages.AsNoTracking()
                join g in _db.InvoicingPaymentGroup.AsNoTracking() on s.GroupId equals g.Id
                where bo.StageIds.Contains(s.Id)
                select new { s.Id, s.Name, s.Percentage, s.VatPercentage, GroupVat = g.VatPercentage, g.ProjectId }
            ).ToListAsync(ct);

            foreach (var s in stages)
            {
                var vat = s.VatPercentage != 0 ? s.VatPercentage : (s.GroupVat ?? 21m);
                bo.Lines.Add(new InvoiceLineBO
                {
                    Text = s.Name,
                    Price = 0m,
                    VatPercentage = vat,
                    PaymentStageId = s.Id
                });
            }

            // header (eerste project als er meerdere zijn)
            if (string.IsNullOrWhiteSpace(bo.HeaderDescription))
            {
                var projectId = stages.Select(x => x.ProjectId).FirstOrDefault();
                string? projName = null;
                if (projectId > 0)
                    projName = await _db.Project.AsNoTracking()
                        .Where(p => p.ProjectId == projectId)
                        .Select(p => p.ProjectName)
                        .FirstOrDefaultAsync(ct);

                var names = string.Join(", ", stages.Select(x => x.Name));
                bo.HeaderDescription = projName != null
                    ? $"Voorschotfactuur – {names} – project {projName}"
                    : $"Voorschotfactuur – {names}";
            }
        }

        private async Task<PartySnapshot> ResolvePartySnapshotAsync(InvoiceDraftBO bo, CancellationToken ct)
        {
            if (bo.CompanyId.HasValue)
            {
                var supplier = await _db.CompanyInfo
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.CompanyId == bo.CompanyId.Value, ct)
                    ?? throw new InvalidOperationException("Leverancier niet gevonden.");

                return new PartySnapshot(
                    Clean(supplier.BedrijfsNaam),
                    Clean(ComposeAddress(supplier.Straat, supplier.Huisnummer, supplier.Toevoeging, supplier.Busnummer)),
                    NormalizePostalCodeId(supplier.PostCodeId),
                    Clean(supplier.Ondernemingsnummer),
                    Clean(supplier.Opmerkingen)
                );
            }

            if (bo.ClientType is (int)InvoicePartyType.ClientAccount)
            {
                var client = await _db.ClientAccount
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == bo.ClientId, ct)
                    ?? throw new InvalidOperationException("Klant niet gevonden.");

                var useInvoiceAddress = client.InvoiceAddress == true;
                var street = useInvoiceAddress && !string.IsNullOrWhiteSpace(client.InvoiceStreet)
                    ? client.InvoiceStreet
                    : client.Street;
                var house = useInvoiceAddress && !string.IsNullOrWhiteSpace(client.InvoiceHousenumber)
                    ? client.InvoiceHousenumber
                    : client.Housenumber;
                var box = useInvoiceAddress && !string.IsNullOrWhiteSpace(client.InvoiceBusnumber)
                    ? client.InvoiceBusnumber
                    : client.Busnumber;
                var postalCodeId = useInvoiceAddress
                    ? (client.InvoicePostalCodeId ?? client.PostalCodeId)
                    : client.PostalCodeId;

                var nameParts = new List<string>();
                var salutation = FormatSalutation(client.Salutation);
                if (!string.IsNullOrWhiteSpace(salutation)) nameParts.Add(salutation);
                if (!string.IsNullOrWhiteSpace(client.Name))
                    nameParts.Add(client.Name.Trim());
                else if (!string.IsNullOrWhiteSpace(client.CompanyName))
                    nameParts.Add(client.CompanyName.Trim());

                var name = nameParts.Count > 0
                    ? string.Join(" ", nameParts)
                    : client.CompanyName;

                return new PartySnapshot(
                    Clean(name),
                    Clean(ComposeAddress(street, house, null, box)),
                    NormalizePostalCodeId(postalCodeId),
                    Clean(client.Vatnumber),
                    Clean(client.InvoiceExtra)
                );
            }

            if (bo.ClientType is (int)InvoicePartyType.ClientContact)
            {
                var contact = await _db.ClientContacts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == bo.ClientId, ct)
                    ?? throw new InvalidOperationException("Contact niet gevonden.");

                var nameParts = new List<string>();
                var salutation = FormatSalutation(contact.Salutation);
                if (!string.IsNullOrWhiteSpace(salutation)) nameParts.Add(salutation);
                if (!string.IsNullOrWhiteSpace(contact.Name)) nameParts.Add(contact.Name.Trim());
                if (!string.IsNullOrWhiteSpace(contact.Forename)) nameParts.Add(contact.Forename.Trim());
                var displayName = nameParts.Count > 0
                    ? string.Join(" ", nameParts)
                    : contact.CompanyName;

                var useInvoiceAddress = contact.InvoiceAddress == true;
                var street = useInvoiceAddress && !string.IsNullOrWhiteSpace(contact.InvoiceStreet)
                    ? contact.InvoiceStreet
                    : contact.Street;
                var house = useInvoiceAddress && !string.IsNullOrWhiteSpace(contact.InvoiceHousenumber)
                    ? contact.InvoiceHousenumber
                    : contact.Housenumber;
                var box = useInvoiceAddress && !string.IsNullOrWhiteSpace(contact.InvoiceBusnumber)
                    ? contact.InvoiceBusnumber
                    : contact.Busnumber;
                var postalCodeId = useInvoiceAddress
                    ? (contact.InvoicePostalCodeId ?? contact.PostalCodeId)
                    : contact.PostalCodeId;

                return new PartySnapshot(
                    Clean(displayName),
                    Clean(ComposeAddress(street, house, null, box)),
                    NormalizePostalCodeId(postalCodeId),
                    Clean(contact.Vatnumber),
                    null
                );
            }

            throw new InvalidOperationException("Kies een klant (type 1/2) of leverancier.");
        }

        private static string? ComposeAddress(string? street, string? houseNumber, string? addition, string? box)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(street))
                parts.Add(street.Trim());

            var houseParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(houseNumber))
                houseParts.Add(houseNumber.Trim());
            if (!string.IsNullOrWhiteSpace(addition))
                houseParts.Add(addition.Trim());
            if (houseParts.Count > 0)
                parts.Add(string.Join("", houseParts));

            if (!string.IsNullOrWhiteSpace(box))
                parts.Add($"bus {box.Trim()}");

            if (parts.Count == 0)
                return null;

            return string.Join(" ", parts);
        }

        private static string? Clean(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static int? NormalizePostalCodeId(int? value)
            => value.HasValue && value.Value > 0 ? value : null;

        private static string? FormatSalutation(string? value)
      => SalutationDisplayHelper.ToDisplayString(value);


        private sealed record PartySnapshot(
            string? ClientName,
            string? Address,
            int? PostalCodeId,
            string? VatNumber,
            string? ExtraInfo);

        private static readonly Regex PatternTokenRegex = new(@"\{(num|date):([^}]+)\}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly IReadOnlyDictionary<string, Func<DateTime, CultureInfo, string>> DateTokenResolvers =
            new Dictionary<string, Func<DateTime, CultureInfo, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["MM"] = static (d, c) => d.ToString("MM", c),
                ["MMM"] = static (d, c) => d.ToString("MMM", c),
                ["MMMM"] = static (d, c) => d.ToString("MMMM", c),
                ["yy"] = static (d, c) => d.ToString("yy", c),
                ["yyyy"] = static (d, c) => d.ToString("yyyy", c),
                ["MM-yyyy"] = static (d, c) => d.ToString("MM-yyyy", c),
            };

        private static int? ExtractSequenceNumber(Invoices invoice)
        {
            if (invoice == null) return null;

            var pattern = invoice.IssuerCompany?.InvoiceNumberPattern ?? "{num:0000}/{date:yyyy}";
            return ExtractSequenceNumber(pattern, invoice.PublicId, invoice.Date);
        }

        private static int? ExtractSequenceNumber(string? pattern, string? publicId, DateOnly invoiceDate)
        {
            if (string.IsNullOrWhiteSpace(publicId))
                return null;

            if (string.IsNullOrWhiteSpace(pattern))
                pattern = "{num:0000}/{date:yyyy}";

            var expression = new StringBuilder();
            var cursor = 0;
            var dateTime = invoiceDate.ToDateTime(TimeOnly.MinValue);
            var culture = CultureInfo.CurrentCulture;

            foreach (Match token in PatternTokenRegex.Matches(pattern))
            {
                expression.Append(Regex.Escape(pattern.Substring(cursor, token.Index - cursor)));

                var type = token.Groups[1].Value;
                var format = token.Groups[2].Value?.Trim();

                if (type.Equals("num", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(format) && format.All(c => c == '0'))
                    {
                        expression.Append($@"(?<num>\d{{{format.Length}}})");
                    }
                    else
                    {
                        expression.Append(@"(?<num>\d+)");
                    }
                }
                else
                {
                    try
                    {
                        string formattedDate;
                        if (!string.IsNullOrEmpty(format) && DateTokenResolvers.TryGetValue(format, out var resolver))
                        {
                            formattedDate = resolver(dateTime, culture);
                        }
                        else if (!string.IsNullOrWhiteSpace(format))
                        {
                            formattedDate = dateTime.ToString(format, culture);
                        }
                        else
                        {
                            formattedDate = dateTime.ToString(culture);
                        }
                        expression.Append(Regex.Escape(formattedDate));
                    }
                    catch (FormatException)
                    {
                        expression.Append(Regex.Escape(dateTime.ToString("yyyy", culture)));
                    }
                }

                cursor = token.Index + token.Length;
            }

            if (cursor < pattern.Length)
                expression.Append(Regex.Escape(pattern.Substring(cursor)));

            var match = Regex.Match(publicId, "^" + expression + "$", RegexOptions.CultureInvariant);
            if (!match.Success)
            {
                var fallback = Regex.Match(publicId, @"(\d+)(?!.*\d)");
                if (fallback.Success && int.TryParse(fallback.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var alt))
                    return alt;

                return null;
            }

            if (int.TryParse(match.Groups["num"].Value, NumberStyles.Integer, CultureInfo.CurrentCulture, out var number))
                return number;

            return null;
        }





    }

}

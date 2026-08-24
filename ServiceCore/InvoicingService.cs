using BOCore;
using FacadeCore;
using DALCore;
using DALCore.Models;
using ServiceCore.Invoicing;
using ServiceCore.Translators;
using DALCore.Query;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Helpers;

namespace ServiceCore
{
    public class InvoicingService : IInvoicingService
    {
        private readonly UnitOfWorkCore _uow;
        private readonly cpmRunningContext _db;

        public InvoicingService(UnitOfWorkCore uow)
        {
            _uow = uow;
            _db = (cpmRunningContext)uow.Context;
        }

        public GetResponse<InvoiceBO> GetInvoices()
        {
            var response = new GetResponse<InvoiceBO>();

            var entities = _uow.Invoices.GetNoTracking();
            foreach (var e in entities)
            {
                var bo = new InvoiceBO();
                var err = InvoiceTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }

            return response;
        }

        public GetResponse<InvoiceBO> GetClientInvoices(int id, int itype = 1)
        {
            var response = new GetResponse<InvoiceBO>();

            var entities = _uow.Invoices
                .GetNoTracking()
                .Where(m => m.ClientId == id && m.ClientType == itype);

            foreach (var e in entities)
            {
                var bo = new InvoiceBO();
                var err = InvoiceTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }

            return response;
        }

        public GetResponse<InvoiceBO> GetInvoiceById(int id)
        {
            var response = new GetResponse<InvoiceBO>();

            var entity = _uow.Invoices.GetById(id);
            if (entity == null)
            {
                response.AddError("invoice not found");
                return response;
            }

            var bo = new InvoiceBO();
            var err = InvoiceTranslator.TranslateEntityToBO(entity, bo);
            if (err == ErrorCode.Success) response.Value = bo;
            else response.AddError(err.ToString());

            return response;
        }

        public GetResponse<InvoiceFileBO> GetInvoiceFileByFilename(string name)
        {
            var response = new GetResponse<InvoiceFileBO>();

            var entity = _uow.Invoices
                .GetNoTracking()
                .FirstOrDefault(m => m.Filename == name);

            if (entity is null)
            {
                response.AddError("no invoice found");
                return response;
            }

            response.Value = new InvoiceFileBO
            {
                Filename = entity.Filename,
                DbId = entity.Id,
                ClientId = (int)entity.ClientId,
                InvoiceDate = entity.Date
            };

            return response;
        }

    }
    public class InvoiceQueryService : IInvoiceQueryService
    {
        private readonly UnitOfWorkCore _uow;
        private readonly cpmRunningContext _db;

        public InvoiceQueryService(UnitOfWorkCore uow)
        {
            _uow = uow;
            _db = (cpmRunningContext)_uow.Context;
        }

        public async Task<IReadOnlyList<InvoiceListItemBO>> GetAllAsync(CancellationToken ct = default)
        {
            var query =
                from i in _db.Invoices.AsNoTracking()
                let contactEmail = i.ClientIdClientContactsNavigation != null
                                  ? (i.ClientIdClientContactsNavigation.InvoiceEmail ?? i.ClientIdClientContactsNavigation.Email)
                                  : null
                let accountEmail = i.ClientIdClientAccountNavigation != null
                    ? (i.ClientIdClientAccountNavigation.InvoiceEmail ?? i.ClientIdClientAccountNavigation.Email)
                    : null
                join p in _db.Project.AsNoTracking() on i.ProjectId equals p.ProjectId into pj
                from project in pj.DefaultIfEmpty()
                from bal in _db.VwInvoiceBalance
                             .AsNoTracking()
                             .Where(v => v.Id == i.Id)
                             .DefaultIfEmpty()
                from tot in _db.VwInvoiceTotals
                             .AsNoTracking()
                             .Where(v => v.Id == i.Id)
                             .DefaultIfEmpty()
                join seriesLookup in _db.InvoiceSeries.AsNoTracking()
                on i.SeriesId equals seriesLookup.Id into seriesJoin
                from series in seriesJoin.DefaultIfEmpty()
                orderby i.Date descending
                select new InvoiceListItemBO
                {
                    Id = i.Id,
                    PublicId = i.PublicId,
                    ClientName = i.ClientName,
                    InvoiceDate = i.Date,
                    StatusId = i.StatusId,
                    StatusName = null,
                    GrossTotal = (decimal?)bal.GrossTotal ?? 0m,
                    NetTotal = (decimal?)tot.LinesNet,
                    Balance = (decimal?)bal.Balance ?? 0m,
                    IsCreditNote = series != null && series.IsCreditNote,
                    ProjectName = project != null ? project.ProjectName : null,
                    OctopusDeliveryState = i.OctopusDeliveryState,
                    OctopusBookyearId = i.OctopusBookyearId,
                    OctopusJournalKey = i.OctopusJournalKey,
                    OctopusDocumentSequenceNr = i.OctopusDocumentSequenceNr,
                    OctopusBookedAt = i.OctopusBookedAt,
                    HasEmailLog = _db.InvoiceEmailLog.AsNoTracking().Any(l => l.InvoiceId == i.Id),
                    RequiresDigitalInvoice = i.CompanyId.HasValue
                        ? true
                        : (i.ClientIdClientContactsNavigation != null
                            ? i.ClientIdClientContactsNavigation.RequiresDigitalInvoice
                            : i.ClientIdClientAccountNavigation != null && i.ClientIdClientAccountNavigation.RequiresDigitalInvoice),
                    HasEmail = !string.IsNullOrWhiteSpace(contactEmail ?? accountEmail),
                    ClientType = i.ClientType,
                    IsSupplier = i.CompanyId.HasValue,
                    HasCompanyName = !string.IsNullOrWhiteSpace(i.ClientIdClientAccountNavigation != null
                        ? i.ClientIdClientAccountNavigation.CompanyName
                        : i.ClientIdClientContactsNavigation != null
                            ? i.ClientIdClientContactsNavigation.CompanyName
                            : null),
                    OctopusWorkflowState = i.OctopusWorkflowState
                };

            var items = await query.ToListAsync(ct);
            foreach (var item in items)
            {
                item.StatusName = InvoiceStatusExtensions.GetDisplayName(item.StatusId);
            }

            return items;
        }
        public async Task<IReadOnlyList<InvoiceListItemBO>> GetByCompanyAsync(int issuerCompanyId, CancellationToken ct = default)
        {
            // legacy mapping ophalen (CompanyInfo.Id dat bij deze issuer hoort)
            var legacyId = await _db.Set<IssuerCompany>()
                                    .AsNoTracking()
                                    .Where(ic => ic.Id == issuerCompanyId)
                                    .Select(ic => (int?)ic.LegacyCompanyInfoId)
                                    .FirstOrDefaultAsync(ct);

            var q = _db.Invoices.AsNoTracking()
                     .Where(i => i.IssuerCompanyId == issuerCompanyId);

            var result =
                from i in q
                let contactEmail = i.ClientIdClientContactsNavigation != null
                   ? (i.ClientIdClientContactsNavigation.InvoiceEmail ?? i.ClientIdClientContactsNavigation.Email)
                   : null
                let accountEmail = i.ClientIdClientAccountNavigation != null
                    ? (i.ClientIdClientAccountNavigation.InvoiceEmail ?? i.ClientIdClientAccountNavigation.Email)
                    : null
                join p in _db.Project.AsNoTracking() on i.ProjectId equals p.ProjectId into pj
                from project in pj.DefaultIfEmpty()
                from bal in _db.VwInvoiceBalance.AsNoTracking().Where(v => v.Id == i.Id).DefaultIfEmpty()
                from tot in _db.VwInvoiceTotals.AsNoTracking().Where(v => v.Id == i.Id).DefaultIfEmpty()
                join seriesLookup in _db.InvoiceSeries.AsNoTracking()
                    on i.SeriesId equals seriesLookup.Id into seriesJoin
                from series in seriesJoin.DefaultIfEmpty()
                orderby i.Date descending
                select new InvoiceListItemBO
                {
                    Id = i.Id,
                    PublicId = i.PublicId,
                    ClientName = i.ClientName,
                    InvoiceDate = i.Date,
                    StatusId = i.StatusId,
                    StatusName = null,
                    IsCreditNote = series != null && series.IsCreditNote,
                    ProjectName = project != null ? project.ProjectName : null,
                    GrossTotal = (decimal?)bal.GrossTotal ?? 0m,
                    NetTotal = (decimal?)tot.LinesNet,
                    Balance = (decimal?)bal.Balance ?? 0m,
                    OctopusDeliveryState = i.OctopusDeliveryState,
                    OctopusBookyearId = i.OctopusBookyearId,
                    OctopusJournalKey = i.OctopusJournalKey,
                    OctopusDocumentSequenceNr = i.OctopusDocumentSequenceNr,
                    OctopusBookedAt = i.OctopusBookedAt,
                    HasEmailLog = _db.InvoiceEmailLog.AsNoTracking().Any(l => l.InvoiceId == i.Id),
                    RequiresDigitalInvoice = i.CompanyId.HasValue
                        ? true
                        : (i.ClientIdClientContactsNavigation != null
                            ? i.ClientIdClientContactsNavigation.RequiresDigitalInvoice
                            : i.ClientIdClientAccountNavigation != null && i.ClientIdClientAccountNavigation.RequiresDigitalInvoice),
                    HasEmail = !string.IsNullOrWhiteSpace(contactEmail ?? accountEmail),
                    ClientType = i.ClientType,
                    IsSupplier = i.CompanyId.HasValue,
                    HasCompanyName = !string.IsNullOrWhiteSpace(i.ClientIdClientAccountNavigation != null
                        ? i.ClientIdClientAccountNavigation.CompanyName
                        : i.ClientIdClientContactsNavigation != null
                            ? i.ClientIdClientContactsNavigation.CompanyName
                            : null),
                    OctopusWorkflowState = i.OctopusWorkflowState
                };

            var items = await result.ToListAsync(ct);
            foreach (var item in items)
            {
                item.StatusName = InvoiceStatusExtensions.GetDisplayName(item.StatusId);
            }

            return items;
        }

        public async Task<InvoiceDetailBO> GetDetailAsync(int invoiceId, CancellationToken ct = default)
        {
            var invoice = await _db.Invoices
                           .AsNoTracking()
                           .Include(i => i.InvoicesDetails)
                           .Include(i => i.PostalCode)
                               .ThenInclude(pc => pc.Country)
                           .Include(i => i.PostalCode)
                               .ThenInclude(pc => pc.Provincie)
                           .Include(i => i.IssuerCompany)
                               .ThenInclude(ic => ic.IssuerBankAccount)
                           .Include(i => i.IssuerCompany)
                               .ThenInclude(ic => ic.CompanyLegalForm)
                           .Include(i => i.ClientIdClientContactsNavigation)
                                   .ThenInclude(cc => cc.PostalCode)
                                   .ThenInclude(pc => pc.Country)
                           .Include(i => i.ClientIdClientContactsNavigation)
                               .ThenInclude(cc => cc.PostalCode)
                                   .ThenInclude(pc => pc.Provincie)
                           .Include(i => i.ClientIdClientContactsNavigation)
                               .ThenInclude(cc => cc.InvoicePostalCode)
                                   .ThenInclude(pc => pc.Country)
                           .Include(i => i.ClientIdClientContactsNavigation)
                               .ThenInclude(cc => cc.InvoicePostalCode)
                                   .ThenInclude(pc => pc.Provincie)
                           .Include(i => i.ClientIdClientAccountNavigation)
                               .ThenInclude(ca => ca.PostalCode)
                                   .ThenInclude(pc => pc.Country)
                           .Include(i => i.ClientIdClientAccountNavigation)
                               .ThenInclude(ca => ca.PostalCode)
                                   .ThenInclude(pc => pc.Provincie)
                           .Include(i => i.ClientIdClientAccountNavigation)
                               .ThenInclude(ca => ca.InvoicePostalCode)
                                   .ThenInclude(pc => pc.Country)
                           .Include(i => i.ClientIdClientAccountNavigation)
                               .ThenInclude(ca => ca.InvoicePostalCode)
                                   .ThenInclude(pc => pc.Provincie)
                           .Include(i => i.ClientIdClientAccountNavigation)
                               .ThenInclude(ca => ca.ClientContacts)
                           .Include(i => i.Series)
                           .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);

            if (invoice == null)
                return null;

            CompanyInfo? company = null;
            if (invoice.CompanyId.HasValue)
            {
                company = await _db.CompanyInfo
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CompanyId == invoice.CompanyId.Value, ct);
            }

            var statusName = InvoiceStatusExtensions.GetDisplayName(invoice.StatusId);

            string? defaultIban = null;
            string? defaultBic = null;

            if (invoice.IssuerCompanyId.HasValue)
            {
                var issuerDefaults = await _db.IssuerBankAccount
                    .AsNoTracking()
                    .Where(a => a.IssuerCompanyId == invoice.IssuerCompanyId && a.IsDefault)
                    .Select(a => new { a.Iban, a.Bic })
                    .FirstOrDefaultAsync(ct);

                if (issuerDefaults != null)
                {
                    defaultIban = issuerDefaults.Iban;
                    defaultBic = issuerDefaults.Bic;
                }
            }

            var detail = NewDetailBo(invoice, statusName, defaultIban, defaultBic, company);

            if (invoice.PaymentTermId.HasValue)
            {
                var term = await _db.PaymentTerms
                    .AsNoTracking()
                    .Where(t => t.Id == invoice.PaymentTermId.Value)
                    .Select(t => new
                    {
                        t.DisplayText,
                        t.DisplayMode
                    })
                    .FirstOrDefaultAsync(ct);

                if (term != null)
                {
                    detail.PaymentTermDisplayText = term.DisplayText;
                    detail.PaymentTermDisplayMode = (PaymentTermDisplayMode)term.DisplayMode;
                }
            }


            foreach (var detailRow in invoice.InvoicesDetails)
            {
                var line = new InvoiceLineBO();
                InvoiceDetailTranslator.TranslateEntityToBO(detailRow, line);
                detail.Lines.Add(line);
            }

            await PopulateTotalsAsync(detail, invoiceId, ct);

            return detail;
        }

        private static InvoiceDetailBO NewDetailBo(Invoices invoice, string statusName, string? defaultIban, string? defaultBic, CompanyInfo? company)
        {
            var contact = invoice.ClientIdClientContactsNavigation;
            var account = invoice.ClientIdClientAccountNavigation;
            var postal = invoice.PostalCode
                ?? contact?.InvoicePostalCode
                ?? account?.InvoicePostalCode
                ?? contact?.PostalCode
                ?? account?.PostalCode;
            var issuer = invoice.IssuerCompany;
            var issuerIban = defaultIban ?? issuer?.IssuerBankAccount?.Iban;
            var issuerBic = defaultBic ?? issuer?.IssuerBankAccount?.Bic;
            var isSupplier = invoice.CompanyId.HasValue;

            string? invoiceEmail = contact?.InvoiceEmail;
            if (string.IsNullOrWhiteSpace(invoiceEmail))
                invoiceEmail = account?.InvoiceEmail;
            if (string.IsNullOrWhiteSpace(invoiceEmail))
                invoiceEmail = company?.InvoiceEmail;
            if (string.IsNullOrWhiteSpace(invoiceEmail))
                invoiceEmail = contact?.Email ?? account?.Email ?? company?.Email;
            if (string.IsNullOrWhiteSpace(invoiceEmail))
            {
                var accountContacts = account?.ClientContacts;
                if (accountContacts != null && accountContacts.Count > 0)
                {
                    invoiceEmail = accountContacts
                        .Select(c => c.InvoiceEmail)
                        .FirstOrDefault(e => !string.IsNullOrWhiteSpace(e));

                    if (string.IsNullOrWhiteSpace(invoiceEmail))
                    {
                        invoiceEmail = accountContacts
                            .Select(c => c.Email)
                            .FirstOrDefault(e => !string.IsNullOrWhiteSpace(e));
                    }
                }
            }

            invoiceEmail = string.IsNullOrWhiteSpace(invoiceEmail)
                ? null
                : invoiceEmail.Trim();

            var requiresDigital = contact?.RequiresDigitalInvoice
                ?? account?.RequiresDigitalInvoice
                ?? company?.RequiresDigitalInvoice
                ?? false;
            if (isSupplier)
                requiresDigital = true;

            var attachUbl = contact?.AttachUblByDefault
                ?? company?.AttachUblByDefault
                ?? requiresDigital;
            if (isSupplier)
                attachUbl = true;

            return new InvoiceDetailBO
            {
                Id = invoice.Id,
                PublicId = invoice.PublicId,
                InvoiceDate = invoice.Date,
                ExpirationDate = invoice.ExpirationDate,
                StatusName = statusName,
                IssuerCompanyId = issuer?.Id ?? 0,
                IssuerName = issuer?.Name,
                IssuerLegalName = issuer?.LegalName,
                IssuerVatNumber = BuildInvoiceVatNumber(issuer?.VatNumber, issuer?.EnterpriseNumber, issuer?.CountryCode),
                IssuerAddressLine1 = issuer?.AddressLine1,
                IssuerAddressLine2 = issuer?.AddressLine2,
                IssuerPostalCode = issuer?.PostalCode,
                IssuerCity = issuer?.City,
                IssuerCountryCode = issuer?.CountryCode,
                IssuerEmail = issuer?.Email,
                IssuerPhone = issuer?.Phone,
                IssuerDefaultIban = issuerIban,
                IssuerDefaultBic = issuerBic,
                IssuerLegalFormAbbreviation = issuer?.CompanyLegalForm?.Abbreviation,
                StructuredMessage = invoice.StructuredCommOgm,
                QrPayLoad = invoice.QrEpcPayload,
                ClientName = invoice.ClientName,
                ClientAddress = invoice.Adress,
                ClientPostalCode = postal?.Postcode,
                ClientCity = postal?.Gemeente,
                ClientCountryName = postal?.Country?.LandNaam,
                ClientVatNumber = BuildInvoiceVatNumber(invoice.VatNumber, company?.Ondernemingsnummer, postal?.Country?.LandIsocode ?? issuer?.CountryCode),
                ClientEnterpriseNumber = company?.Ondernemingsnummer,
                ClientEmail = invoiceEmail,
                RequiresDigitalInvoice = requiresDigital,
                AttachUblByDefault = attachUbl,
                IsSupplier = isSupplier,
                BankAccount = invoice.BankAccount,
                PdfAppendixFileName = invoice.PdfAppendixFileName,
                ExtraInfo = invoice.ExtraInfo,
                HeaderText = invoice.HeaderDescription,
                DetailText = invoice.DetailText,
                IsPrepaid = invoice.Prepaid,
                IsCreditNote = invoice.Series?.IsCreditNote ?? false,
                ClientId = invoice.ClientId,
                ClientType = invoice.ClientType,
                CompanyId = invoice.CompanyId,
                InvoiceMode = invoice.InvoiceMode.HasValue ? (InvoiceMode?)invoice.InvoiceMode.Value : null,
                ProjectId = invoice.ProjectId,
                SupplierContractId = invoice.SupplierContractId,
                PaymentTermId = invoice.PaymentTermId,
                OctopusBookyearId = invoice.OctopusBookyearId,
                OctopusJournalKey = invoice.OctopusJournalKey,
                OctopusDocumentSequenceNr = invoice.OctopusDocumentSequenceNr,
                OctopusWorkflowState = invoice.OctopusWorkflowState,
                OctopusWorkflowUpdatedAt = invoice.OctopusWorkflowUpdatedAt,
                OctopusDeliveryState = invoice.OctopusDeliveryState,
                OctopusDeliveryComment = invoice.OctopusDeliveryComment,
                OctopusDeliveryDateTime = invoice.OctopusDeliveryDateTime,
                OctopusDeliveryUpdatedAt = invoice.OctopusDeliveryUpdatedAt,
                OctopusBookedAt = invoice.OctopusBookedAt,
                OctopusBookedBy = invoice.OctopusBookedBy,
            };
        }

        private static string? BuildInvoiceVatNumber(string? vatNumber, string? enterpriseNumber, string? countryCode)
        {
            var candidate = string.IsNullOrWhiteSpace(vatNumber) ? enterpriseNumber : vatNumber;
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return null;
            }

            var trimmed = candidate.Trim();
            var normalizedCountry = NormalizeCountryCode(countryCode);
            if (string.Equals(normalizedCountry, "BE", StringComparison.OrdinalIgnoreCase))
            {
                var digits = new string(trimmed.Where(char.IsDigit).ToArray());
                if (string.IsNullOrWhiteSpace(digits))
                {
                    return null;
                }

                if (digits.Length == 9)
                {
                    digits = "0" + digits;
                }

                return $"BE{digits}";
            }

            return trimmed;
        }

        private static string? NormalizeCountryCode(string? countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
            {
                return null;
            }

            var trimmed = countryCode.Trim();
            return trimmed.Length > 2 ? trimmed.Substring(0, 2).ToUpperInvariant() : trimmed.ToUpperInvariant();
        }

        private async Task PopulateTotalsAsync(InvoiceDetailBO detail, int invoiceId, CancellationToken ct)
        {
            decimal totalExcl = 0m;
            var netPerRate = new List<(decimal Net, decimal Rate)>();

            foreach (var line in detail.Lines)
            {
                var discount = line.DiscountAmount
                    ?? (line.DiscountPercent.HasValue
                        ? Math.Round(line.Price * (line.DiscountPercent.Value / 100m), 2, MidpointRounding.AwayFromZero)
                        : 0m);

                var net = line.Price - discount;
                totalExcl += net;
                netPerRate.Add((net, line.VatPercentage));
            }

            detail.TotalExclVat = Math.Round(totalExcl, 2, MidpointRounding.AwayFromZero);
            detail.TotalVat = InvoiceVatCalculator.CalculateTotalVat(netPerRate);
            detail.TotalInclVat = Math.Round(detail.TotalExclVat + detail.TotalVat, 2, MidpointRounding.AwayFromZero);

            var balance = await _db.VwInvoiceBalance
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == invoiceId, ct);

            if (balance != null)
            {
                detail.PaidAmount = balance.Paid;
                detail.Balance = balance.Balance;
                if (balance.GrossTotal.HasValue)
                    detail.TotalInclVat = Math.Round(balance.GrossTotal.Value, 2, MidpointRounding.AwayFromZero);
            }
        }

    }

}

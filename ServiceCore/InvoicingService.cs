using BOCore;
using FacadeCore;
using DALCore;
using DALCore.Models;
using ServiceCore.Translators;
using DALCore.Query;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

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
                from bal in _db.VwInvoiceBalance
                             .AsNoTracking()
                             .Where(v => v.Id == i.Id)
                             .DefaultIfEmpty()                   // <-- levert NULL als er geen row is
                                                                 // LEFT JOIN op status lookup
                join s in _db.Set<InvoiceStatusLookup>().AsNoTracking()
                    on i.StatusId equals s.Id into sj
                from st in sj.DefaultIfEmpty()
                orderby i.Date descending
                select new InvoiceListItemBO
                {
                    Id = i.Id,
                    PublicId = i.PublicId,
                    ClientName = i.ClientName,
                    InvoiceDate = i.Date,
                    StatusId = i.StatusId,
                    StatusName = st != null ? st.Name : null,
                    GrossTotal = (decimal?)bal.GrossTotal ?? 0m,
                    Balance = (decimal?)bal.Balance ?? 0m
                };

            return await query.ToListAsync(ct);
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
                from bal in _db.VwInvoiceBalance.AsNoTracking().Where(v => v.Id == i.Id).DefaultIfEmpty()
                join s in _db.Set<InvoiceStatusLookup>().AsNoTracking() on i.StatusId equals s.Id into sj
                from st in sj.DefaultIfEmpty()
                orderby i.Date descending
                select new InvoiceListItemBO
                {
                    Id = i.Id,
                    PublicId = i.PublicId,
                    ClientName = i.ClientName,
                    InvoiceDate = i.Date,
                    StatusId = i.StatusId,
                    StatusName = st != null ? st.Name : null,
                    GrossTotal = (decimal?)bal.GrossTotal ?? 0m,
                    Balance = (decimal?)bal.Balance ?? 0m
                };

            return await result.ToListAsync(ct);
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
                .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);

            if (invoice == null)
                return null;

            var statusName = await _db.InvoiceStatusLookup
                .AsNoTracking()
                .Where(s => s.Id == invoice.StatusId)
                .Select(s => s.Name)
                .FirstOrDefaultAsync(ct);

            var detail = NewDetailBo(invoice, statusName);

            foreach (var detailRow in invoice.InvoicesDetails)
            {
                var line = new InvoiceLineBO();
                InvoiceDetailTranslator.TranslateEntityToBO(detailRow, line);
                detail.Lines.Add(line);
            }

            await PopulateTotalsAsync(detail, invoiceId, ct);

            return detail;
        }

        private static InvoiceDetailBO NewDetailBo(Invoices invoice, string statusName)
        {
            var postal = invoice.PostalCode;
            var issuer = invoice.IssuerCompany;

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
                IssuerVatNumber = issuer?.VatNumber,
                IssuerAddressLine1 = issuer?.AddressLine1,
                IssuerAddressLine2 = issuer?.AddressLine2,
                IssuerPostalCode = issuer?.PostalCode,
                IssuerCity = issuer?.City,
                IssuerCountryCode = issuer?.CountryCode,
                IssuerEmail = issuer?.Email,
                IssuerPhone = issuer?.Phone,
                StructuredMessage = invoice.StructuredCommOgm,
                QrPayLoad = invoice.QrEpcPayload,
                ClientName = invoice.ClientName,
                ClientAddress = invoice.Adress,
                ClientPostalCode = postal?.Postcode,
                ClientCity = postal?.Gemeente,
                ClientCountryName = postal?.Country?.LandNaam,
                ClientVatNumber = invoice.VatNumber,
                BankAccount = invoice.BankAccount,
                ExtraInfo = invoice.ExtraInfo,
                HeaderText = invoice.HeaderDescription,
                DetailText = invoice.DetailText,
            };
        }

        private async Task PopulateTotalsAsync(InvoiceDetailBO detail, int invoiceId, CancellationToken ct)
        {
            decimal totalExcl = 0m;
            decimal totalVat = 0m;

            foreach (var line in detail.Lines)
            {
                var discount = line.DiscountAmount
                    ?? (line.DiscountPercent.HasValue
                        ? Math.Round(line.Price * (line.DiscountPercent.Value / 100m), 2, MidpointRounding.AwayFromZero)
                        : 0m);

                var net = line.Price - discount;
                var vat = Math.Round(net * (line.VatPercentage / 100m), 2, MidpointRounding.AwayFromZero);

                totalExcl += net;
                totalVat += vat;
            }

            detail.TotalExclVat = Math.Round(totalExcl, 2, MidpointRounding.AwayFromZero);
            detail.TotalVat = Math.Round(totalVat, 2, MidpointRounding.AwayFromZero);
            detail.TotalInclVat = Math.Round(detail.TotalExclVat + detail.TotalVat, 2, MidpointRounding.AwayFromZero);

            var balance = await _db.VwInvoiceBalance
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == invoiceId, ct);

            if (balance != null)
            {
                detail.PaidAmount = balance.Paid;
                detail.Balance = balance.Balance;
                if (balance.GrossTotal.HasValue)
                    detail.TotalInclVat = balance.GrossTotal.Value;
            }
        }

    }

}

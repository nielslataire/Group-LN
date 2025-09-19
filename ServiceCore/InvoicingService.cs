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

    }

}

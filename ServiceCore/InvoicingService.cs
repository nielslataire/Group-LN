using BOCore;
using FacadeCore;
using DALCore;
using DALCore.Models;
using ServiceCore.Translators;
using DALCore.Query;
using System.Linq;

namespace ServiceCore
{
    public class InvoicingService : IInvoicingService
    {
        private readonly UnitOfWorkCore _uow;

        public InvoicingService(UnitOfWorkCore uow)
        {
            _uow = uow;
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
                ClientId = entity.ClientId,
                InvoiceDate = entity.Date
            };

            return response;
        }
    }
}

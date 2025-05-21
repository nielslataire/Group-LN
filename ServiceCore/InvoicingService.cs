using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using FacadeCore;
using DALCore;
using DALCore.Models;
using ServiceCore.Translators;
using DALCore.Query;

namespace ServiceCore
{
    public class InvoicingService : IInvoicingService
    {
        public GetResponse<InvoiceBO> GetInvoices()
        {
            GetResponse<InvoiceBO> response = new GetResponse<InvoiceBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetInvoicesDAO();

            var entities = dao.GetNoTracking();
            foreach (var _entity in entities)
            {
                InvoiceBO invoice = new InvoiceBO();

                var err = InvoiceTranslator.TranslateEntityToBO(_entity, invoice);
                if (err == ErrorCode.Success)
                    response.AddValue(invoice);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<InvoiceBO> GetClientInvoices(int id, int itype = 1)
        {
            GetResponse<InvoiceBO> response = new GetResponse<InvoiceBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetInvoicesDAO();

            var entities = dao.GetNoTracking().Where(m => m.ClientId == id && m.ClientType == itype);
            foreach (var _entity in entities)
            {
                InvoiceBO invoice = new InvoiceBO();

                var err = InvoiceTranslator.TranslateEntityToBO(_entity, invoice);
                if (err == ErrorCode.Success)
                    response.AddValue(invoice);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<InvoiceBO> GetInvoiceById(int id)
        {
            GetResponse<InvoiceBO> response = new GetResponse<InvoiceBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetInvoicesDAO();

            var _entity = dao.GetById(id);
            InvoiceBO invoice = new InvoiceBO();

            var err = InvoiceTranslator.TranslateEntityToBO(_entity, invoice);
            if (err == ErrorCode.Success)
                response.Value = invoice;
            else
                response.AddError(err.ToString());
            return response;
        }
        public GetResponse<InvoiceFileBO> GetInvoiceFileByFilename(string name)
        {
            GetResponse<InvoiceFileBO> response = new GetResponse<InvoiceFileBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetInvoicesDAO();

            var _entity = dao.GetNoTracking().Where(m => m.Filename == name).FirstOrDefault();
            InvoiceFileBO invoice = new InvoiceFileBO();
            if (_entity is not null)
            {
                invoice.Filename = _entity.Filename;
                invoice.DbId = _entity.Id;
                invoice.ClientId = _entity.ClientId;
                invoice.InvoiceDate = _entity.Date;
                response.Value = invoice;
            }
            else
                response.AddError("no invoice found");

            // Dim err = InvoiceTranslator.TranslateEntityToBO(_entity, invoice)
            // If err = ErrorCode.Success Then
            // response.Value = invoice
            // Else
            // response.AddError(err.ToString())
            // End If
            return response;
        }
    }
}

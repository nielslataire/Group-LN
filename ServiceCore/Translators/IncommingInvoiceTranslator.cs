using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using DALCore;
using DALCore.Models;

namespace ServiceCore.Translators
{
    public class IncommingInvoiceTranslator
    {
        internal static ErrorCode TranslateEntityToBO(IncommingInvoices _entity, IncommingInvoiceBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Id = _entity.Id;
            bo.ProjectId = _entity.Project.ProjectId;
            if (_entity.Contract is not null)
                bo.ContractID = _entity.ContractId;
            if (_entity.Company is not null)
                bo.CompanyId = _entity.CompanyId;
            bo.IncommingInvoiceDate = _entity.Date;
            if (_entity.ExternalId is not null)
                bo.InvoiceExternalId = _entity.ExternalId;
            bo.InvoicePrice = _entity.Price;
            foreach (var x in _entity.IncommingInvoiceDetail)
            {
                IncommingInvoiceDetailBO bou = new IncommingInvoiceDetailBO();
                var err = IncommingInvoiceDetailTranslator.TranslateEntityToBO(x, bou);
                bo.Details.Add(bou);
            }
            return ErrorCode.Success;
        }
        internal static ErrorCode TranslateBOToEntity(IncommingInvoices _entity, IncommingInvoiceBO bo, UnitOfWork uow)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            if (bo.ProjectId != 0)
                _entity.ProjectId = bo.ProjectId;
            if (bo.ContractID != 0)
                _entity.ContractId = bo.ContractID;
            if (bo.CompanyId != 0)
                _entity.CompanyId = bo.CompanyId;
            _entity.Date = bo.IncommingInvoiceDate;
            _entity.ExternalId = bo.InvoiceExternalId;
            _entity.Price = bo.InvoicePrice;

            var err = HandleRows(_entity, bo.Details);
            if ((err != ErrorCode.Success))
                return err;
            return ErrorCode.Success;
        }

        private static ErrorCode HandleRows(IncommingInvoices _entity, List<IncommingInvoiceDetailBO> rows)
        {
            if ((rows.Count == 0))
                return ErrorCode.Success;
            foreach (var x in rows)
            {
                ErrorCode err;
                if ((x.Id == 0))
                {
                    // insert
                    IncommingInvoiceDetail row = new IncommingInvoiceDetail();
                    err = IncommingInvoiceDetailTranslator.TranslateBOToEntity(row, x);
                    _entity.IncommingInvoiceDetail.Add(row);
                }
                else
                {
                    // update
                    var row = _entity.IncommingInvoiceDetail.FirstOrDefault(f => f.Id == x.Id);
                    if ((row != null))
                        err = IncommingInvoiceDetailTranslator.TranslateBOToEntity(row, x);
                }
            }
            // delete
            List<IncommingInvoiceDetail> delList = new List<IncommingInvoiceDetail>();
            foreach (var x in _entity.IncommingInvoiceDetail)
            {
                if ((!rows.Any(f => f.Id == x.Id)))
                    delList.Add(x);
            }
            foreach (var x in delList)
                _entity.IncommingInvoiceDetail.Remove(x);
            return ErrorCode.Success;
        }
    }
}

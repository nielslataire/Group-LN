using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using Castle.Core.Smtp;
using DALCore;
using DALCore.Models;

namespace ServiceCore.Translators
{
    public class IncommingInvoiceActivityTranslator
    {
        public static ErrorCode TranslateEntityToBO(IncommingInvoiceDetail _entity, IncommingInvoiceActivityBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            var invoice = _entity.IncommingInvoice;
            bo.InvoiceId = _entity.IncommingInvoiceId;
            bo.InvoiceDetailId = _entity.Id;
            bo.Price = _entity.Price ?? 0m;
            bo.ExternalInvoiceId = invoice?.ExternalId;
            bo.Invoicedate = invoice?.Date ?? default;
            bo.Description = _entity.Description;
            bo.IncommingInvoiceType = (IncommingInvoiceType)_entity.Type;
            if (invoice?.ContractId is int contractId && contractId != 0)
                bo.ContractId = contractId;
            if (_entity.ContractAct is not null)
            {
                ActivityBO act = new ActivityBO();
                var err2 = ActivityTranslator.TranslateEntityToBO(_entity.ContractAct.Activity, act);
                if (err2 != ErrorCode.Success)
                    return err2;
                bo.Activity = act;
                if (_entity.ContractAct.Contract?.Company != null)
                {
                    IdNameBO comp = new IdNameBO();
                    comp.ID = _entity.ContractAct.Contract.Company.CompanyId;
                    comp.Display = _entity.ContractAct.Contract.Company.BedrijfsNaam;
                    bo.Company = comp;
                }
            }
            if (_entity.Act is not null)
            {
                ActivityBO act = new ActivityBO();
                var err2 = ActivityTranslator.TranslateEntityToBO(_entity.Act, act);
                if (err2 != ErrorCode.Success)
                    return err2;
                bo.Activity = act;
                if (invoice?.Company is null)
                {
                    IdNameBO comp = new IdNameBO();
                    if (invoice?.Contract?.Company != null)
                    {
                        comp.ID = invoice.Contract.Company.CompanyId;
                        comp.Display = invoice.Contract.Company.BedrijfsNaam;
                    }
                    bo.Company = comp;
                }
                else
                {
                    IdNameBO comp = new IdNameBO();
                    comp.ID = invoice.Company.CompanyId;
                    comp.Display = invoice.Company.BedrijfsNaam;
                    bo.Company = comp;
                }

            }
            return ErrorCode.Success;
        }
    }
}

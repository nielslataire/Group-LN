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
            bo.InvoiceId = _entity.IncommingInvoiceId;
            bo.InvoiceDetailId = _entity.Id;
            bo.Price = (decimal)_entity.Price;
            bo.ExternalInvoiceId = _entity.IncommingInvoice.ExternalId;
            bo.Invoicedate = _entity.IncommingInvoice.Date;
            bo.Description = _entity.Description;
            bo.IncommingInvoiceType = (IncommingInvoiceType)_entity.Type;
            if (_entity.IncommingInvoice.ContractId is int contractId && contractId != 0)
                bo.ContractId = contractId;
            if (_entity.ContractAct is not null)
            {
                ActivityBO act = new ActivityBO();
                var err2 = ActivityTranslator.TranslateEntityToBO(_entity.ContractAct.Activity, act);
                if (err2 != ErrorCode.Success)
                    return err2;
                bo.Activity = act;
                IdNameBO comp = new IdNameBO();
                comp.ID = _entity.ContractAct.Contract.Company.CompanyId;
                comp.Display = _entity.ContractAct.Contract.Company.BedrijfsNaam;
                bo.Company = comp;
            }
            if (_entity.Act is not null)
            {
                ActivityBO act = new ActivityBO();
                var err2 = ActivityTranslator.TranslateEntityToBO(_entity.Act, act);
                if (err2 != ErrorCode.Success)
                    return err2;
                bo.Activity = act;
                if(_entity.IncommingInvoice.Company is null)
                {
                    IdNameBO comp = new IdNameBO();
                    comp.ID = _entity.IncommingInvoice.Contract.Company.CompanyId;
                    comp.Display = _entity.IncommingInvoice.Contract.Company.BedrijfsNaam;
                    bo.Company = comp;
                }
                else
                {
                    IdNameBO comp = new IdNameBO();
                    comp.ID = _entity.IncommingInvoice.Company.CompanyId;
                    comp.Display = _entity.IncommingInvoice.Company.BedrijfsNaam;
                    bo.Company = comp;
                }

            }
            return ErrorCode.Success;
        }
    }
}

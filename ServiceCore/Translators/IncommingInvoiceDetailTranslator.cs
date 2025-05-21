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
   public class IncommingInvoiceDetailTranslator
    {
        internal static ErrorCode TranslateEntityToBO(IncommingInvoiceDetail _entity, IncommingInvoiceDetailBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Id = _entity.Id;
            bo.IncommingInvoiceType = (IncommingInvoiceType)_entity.Type;
            if (_entity.IncommingInvoice is not null)
                bo.IncommingInvoiceID = _entity.IncommingInvoice.Id;
            if (_entity.ContractAct is not null)
            {
                bo.ContractActivityID = (int)_entity.ContractActId;
                bo.ContractActivityText = _entity.ContractAct.Activity.Omschrijving;
            }
            if (_entity.Act is not null)
            {
                bo.ActivityID = _entity.Act.ActivityId;
                bo.ContractActivityText = _entity.Act.Omschrijving;
            }
            if (_entity.ChangeOrder is not null)
                bo.ChangeOrderId = _entity.ChangeOrderId;
            if (_entity.Description is not null)
                bo.Description = _entity.Description;
            if (_entity.Price is not null)
                bo.Price = (decimal)_entity.Price;

            return ErrorCode.Success;
        }
        internal static ErrorCode TranslateBOToEntity(IncommingInvoiceDetail _entity, IncommingInvoiceDetailBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;

            if (bo.ContractActivityID != 0)
                _entity.ContractActId = bo.ContractActivityID;
            if (bo.ChangeOrderId != 0)
                _entity.ChangeOrderId = bo.ChangeOrderId;
            if (bo.IncommingInvoiceID != 0)
                _entity.IncommingInvoiceId = bo.IncommingInvoiceID;
            if (bo.ActivityID != 0)
                _entity.ActId = bo.ActivityID;
            _entity.Type = (int)bo.IncommingInvoiceType;
            _entity.Price = bo.Price;
            _entity.Description = bo.Description;


            return ErrorCode.Success;
        }
    }
}

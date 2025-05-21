using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using DALCore.Models;
using DALCore;

namespace ServiceCore.Translators
{
    public class InvoiceDetailTranslator
    {
        internal static ErrorCode TranslateEntityToBO(InvoicesDetails _entity, InvoiceRowBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;

            bo.Id = _entity.Id;
            if (_entity.Invoice is not null)
                bo.InvoiceId = _entity.InvoiceId;
            if (_entity.PaymentStage is not null)
                bo.StageId = _entity.PaymentStageId;
            if (_entity.Unit is not null)
                bo.UnitId = _entity.UnitId;
            if (_entity.ChangeOrderDetailId is not null)
                bo.ChangeOrderDetailId = _entity.ChangeOrderDetailId;
            if (_entity.ConstructionValueId is not null)
                bo.ConstructionValueId = _entity.ConstructionValueId;
            bo.Text = _entity.Text;
            if (_entity.VatPercentage is not null)
                bo.VatPercentage = _entity.VatPercentage;
            if (_entity.Price is not null)
                bo.Price = _entity.Price;
            if (_entity.GroupName is not null)
                bo.GroupName = _entity.GroupName;
            if (!_entity.UtilityCost == null)
                bo.UtilityCost = _entity.UtilityCost;
            return ErrorCode.Success;
        }
        internal static ErrorCode TranslateBOToEntity(InvoicesDetails _entity, InvoiceRowBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;

            if (bo.StageId != 0)
                _entity.PaymentStageId = bo.StageId;
            if (bo.UnitId != 0)
                _entity.UnitId = bo.UnitId;
            if (bo.InvoiceId != 0)
                _entity.InvoiceId = bo.InvoiceId;
            if (bo.ChangeOrderDetailId != 0)
                _entity.ChangeOrderDetailId = bo.ChangeOrderDetailId;
            if (bo.ConstructionValueId != 0)
                _entity.ConstructionValueId = bo.ConstructionValueId;
            _entity.Text = bo.Text;
            _entity.VatPercentage = bo.VatPercentage;
            _entity.Price = bo.Price;
            _entity.GroupName = bo.GroupName;
            _entity.UtilityCost = bo.UtilityCost;

            return ErrorCode.Success;
        }
    }
}

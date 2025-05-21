using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using DALCore.Models;   

namespace ServiceCore.Translators
{
    public class UnitConstructionValueTranslator
    {
        public static ErrorCode TranslateEntityToBO(UnitConstructionValue _entity, UnitConstructionValueBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Id = _entity.Id;
            bo.Description = _entity.Description;
            if (_entity.ValueSold is not null)
                bo.ValueSold = _entity.ValueSold;
            if (_entity.Value is not null)
                bo.Value = _entity.Value;
            if (_entity.PaymentGroup is not null)
                bo.PaymentGroupId = _entity.PaymentGroup.Id;
            if (_entity.Unit is not null)
                bo.UnitId = _entity.Unit.Id;
            return ErrorCode.Success;
        }

        internal static ErrorCode TranslateBOToEntity(UnitConstructionValue _entity, UnitConstructionValueBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.Description = bo.Description;
            _entity.Value = bo.Value;
            _entity.ValueSold = bo.ValueSold;
            if (bo.PaymentGroupId == 0)
                _entity.PaymentGroupId = null;
            _entity.PaymentGroupId = bo.PaymentGroupId;
            if ((bo.UnitId != 0))
                _entity.UnitId = bo.UnitId;
            return ErrorCode.Success;
        }
    }
}

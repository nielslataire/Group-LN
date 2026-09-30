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
            // Rechtstreeks de FK-kolom lezen i.p.v. de PaymentGroup/Unit-navigatie: die hoeft niet overal
            // ge-Include't te zijn waar deze translator draait (bv. GetProjectInvoicableUnits, dat enkel
            // UnitConstructionValue zelf include't) — via de navigatie bleef PaymentGroupId/UnitId dan
            // stil op 0 staan, waardoor elke schijf onterecht als "geen match" gold (gevonden via het
            // Facturatie-testproject, 2026-09-29).
            bo.PaymentGroupId = _entity.PaymentGroupId;
            bo.UnitId = _entity.UnitId;
            bo.FinishingOptionId = _entity.FinishingOptionId;
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
            _entity.FinishingOptionId = bo.FinishingOptionId;
            return ErrorCode.Success;
        }
    }
}

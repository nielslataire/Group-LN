using BOCore;
using DALCore.Models;


namespace ServiceCore.Translators
{
    public static class InvoiceDetailTranslator
    {
        // Entity -> BO (InvoiceLineBO)
        internal static ErrorCode TranslateEntityToBO(InvoicesDetails entity, InvoiceLineBO bo)
        {
            if (entity == null) return ErrorCode.EntityNull;
            if (bo == null) return ErrorCode.BoNull;

            bo.Id = entity.Id;

            // FK’s: 0 = niet gezet in BO-conventie
            if (entity.PaymentStageId != null) bo.PaymentStageId = entity.PaymentStageId.Value;
            if (entity.UnitId != null) bo.UnitId = entity.UnitId.Value;
            if (entity.ChangeOrderDetailId != null) bo.ChangeOrderDetailId = entity.ChangeOrderDetailId.Value;
            //if (entity.ConstructionValueId != null) bo.ConstructionValueId = entity.ConstructionValueId.Value;

            bo.Text = entity.Text ?? string.Empty;
            bo.VatPercentage = entity.VatPercentage ?? 21m;
            bo.Price = entity.Price ?? 0m;
            bo.DiscountPercent = entity.DiscountPercent;
            bo.DiscountAmount = entity.DiscountAmount;
            bo.LineType = entity.LineType;
            bo.GroupName = entity.GroupName;

            if (entity.UtilityCost != null)
                bo.UtilityCost = entity.UtilityCost.Value;

            if (entity.UtilityIsAdvance != null)
                bo.UtilityIsAdvance = entity.UtilityIsAdvance;


            return ErrorCode.Success;
        }

        // BO (InvoiceLineBO) -> Entity
        internal static ErrorCode TranslateBOToEntity(InvoicesDetails entity, InvoiceLineBO bo)
        {
            if (entity == null) return ErrorCode.EntityNull;
            if (bo == null) return ErrorCode.BoNull;

            if (bo.PaymentStageId is int psId && psId != 0) entity.PaymentStageId = psId;
            if (bo.UnitId is int unitId && unitId != 0) entity.UnitId = unitId;
            if (bo.ChangeOrderDetailId is int codId && codId != 0) entity.ChangeOrderDetailId = codId;
            //if (bo.ConstructionValueId is int cvId && cvId != 0) entity.ConstructionValueId = cvId;

            entity.Text = bo.Text;
            entity.VatPercentage = bo.VatPercentage;
            entity.Price = bo.Price;
            entity.DiscountPercent = bo.DiscountPercent;
            entity.DiscountAmount = bo.DiscountAmount;
            entity.LineType = bo.LineType;
            entity.GroupName = bo.GroupName;
            entity.UtilityCost = bo.UtilityCost;
            entity.UtilityIsAdvance = bo.UtilityIsAdvance;


            return ErrorCode.Success;
        }
    }
}

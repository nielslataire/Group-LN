using BOCore;
using DALCore.Models;

namespace ServiceCore.Translators
{
    public class UnitFinishingOptionTranslator
    {
        public static ErrorCode TranslateEntityToBO(UnitFinishingOption entity, UnitFinishingOptionBO bo)
        {
            if (entity == null) return ErrorCode.EntityNull;
            if (bo == null) return ErrorCode.BoNull;
            bo.Id = entity.Id;
            bo.UnitId = entity.UnitId;
            bo.Name = entity.Name;
            bo.SortOrder = entity.SortOrder;
            bo.IsDefault = entity.IsDefault;
            return ErrorCode.Success;
        }

        public static ErrorCode TranslateBOToEntity(UnitFinishingOption entity, UnitFinishingOptionBO bo)
        {
            if (entity == null) return ErrorCode.EntityNull;
            if (bo == null) return ErrorCode.BoNull;
            entity.Name = bo.Name;
            entity.SortOrder = bo.SortOrder;
            entity.IsDefault = bo.IsDefault;
            if (bo.UnitId != 0) entity.UnitId = bo.UnitId;
            return ErrorCode.Success;
        }
    }
}

using BOCore;
using DALCore;
using DALCore.Models;

namespace ServiceCore.Translators
{
    public class ContractAdditionalOrderTranslator
    {
        public static ErrorCode TranslateEntityToBO(ContractAdditionalOrder _entity, ContractAdditionalOrderBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Id = _entity.Id;
            bo.ContractActivityId = _entity.ContractActivityId;
            bo.Description = _entity.Description;
            bo.Price = _entity.Price;
            return ErrorCode.Success;
        }

        internal static ErrorCode TranslateBOToEntity(ContractAdditionalOrder _entity, ContractAdditionalOrderBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.Description = string.IsNullOrWhiteSpace(bo.Description) ? null : bo.Description.Trim();
            _entity.Price = bo.Price;
            return ErrorCode.Success;
        }
    }
}

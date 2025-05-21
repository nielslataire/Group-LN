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
    class ClientOwnerTypeTranslator
    {
        internal static ErrorCode TranslateEntityToBO(ClientOwnerType _entity, ClientOwnerTypeBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Id = _entity.Id;
            bo.Name = _entity.Name;
            return ErrorCode.Success;
        }

        internal static ErrorCode TranslateBOToEntity(ClientOwnerType _entity, ClientOwnerTypeBO bo, UnitOfWork uow)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.Name = bo.Name;
            return ErrorCode.Success;
        }
    }
}

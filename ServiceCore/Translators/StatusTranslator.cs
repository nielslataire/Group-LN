using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using DALCore.Models;

namespace ServiceCore.Translators
{
    public class StatusTranslator
    {
        public static ErrorCode TranslateEntityToBO(ProjectStatus _entity, ProjectStatusBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Id = _entity.StatusId;
            bo.Name = _entity.StatusName;
            return ErrorCode.Success;
        }

        internal static ErrorCode TranslateBOToEntity(ProjectStatus _entity, ProjectStatusBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.StatusName = bo.Name;
            return ErrorCode.Success;
        }
    }
}

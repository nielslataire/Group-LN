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
    public class ProjectLevelTranslator
    {
        public static ErrorCode TranslateEntityToBO(ProjectLevels _entity, ProjectLevelBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Id = _entity.Id;
            bo.Name = _entity.Level;
            bo.ProjectId = _entity.ProjectId;
            return ErrorCode.Success;
        }

        internal static ErrorCode TranslateBOToEntity(ProjectLevels _entity, ProjectLevelBO bo, UnitOfWork uow)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.Level = bo.Name;
            _entity.ProjectId = bo.ProjectId;
            return ErrorCode.Success;
        }
    }
}

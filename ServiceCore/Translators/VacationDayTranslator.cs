using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using DALCore.Models;   

namespace ServiceCore.Translators
{
    public class VacationDayTranslator
    {
        public static ErrorCode TranslateEntityToBO(VacationDays _entity, VacationDayBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Id = _entity.Id;
            bo.VacationDay = _entity.VacationDay;
            if (_entity.ProjectId is not null)
                bo.ProjectId = _entity.ProjectId;
            return ErrorCode.Success;
        }

        internal static ErrorCode TranslateBOToEntity(VacationDays _entity, VacationDayBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.VacationDay = bo.VacationDay;
            if (bo.ProjectId != 0)
                _entity.ProjectId = bo.ProjectId;
            return ErrorCode.Success;
        }
    }
}

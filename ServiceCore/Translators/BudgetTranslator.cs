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
    public class BudgetTranslator
    {
        public static ErrorCode TranslateEntityToBO(ProjectBudget _entity, BudgetActivityBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Id = _entity.Id;
            bo.ProjectId = _entity.ProjectId;
            bo.Price = _entity.Price;
            ActivityBO act = new ActivityBO();
            bo.Activity = act;
            var err = ActivityTranslator.TranslateEntityToBO(_entity.Activity, bo.Activity);
            if ((err != ErrorCode.Success))
                return err;
            return ErrorCode.Success;
        }

        internal static ErrorCode TranslateBOToEntity(ProjectBudget _entity, BudgetActivityBO bo, UnitOfWork uow)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.ProjectId = bo.ProjectId;
            _entity.ActivityId = bo.Activity.ID;
            _entity.Price = bo.Price;
            return ErrorCode.Success;
        }
    }
}

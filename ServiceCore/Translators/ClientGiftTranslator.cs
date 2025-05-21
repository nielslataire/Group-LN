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
    public class ClientGiftTranslator
    {
        internal static ErrorCode TranslateEntityToBO(ClientGift _entity, ClientGiftBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Id = _entity.Id;
            bo.AccountId = _entity.ClientAccountId;
            bo.Description = _entity.Description;
            if (_entity.Activity != null)
            {
                foreach (var item in _entity.Activity)
                {
                    ActivityBO Activity = new ActivityBO();
                    var err = ActivityTranslator.TranslateEntityToBO(item, Activity);
                    if (err != ErrorCode.Success)
                        return err;
                    bo.Activities.Add(Activity);
                }
            }
            return ErrorCode.Success;
        }
        internal static ErrorCode TranslateBOToEntity(ClientGift _entity, ClientGiftBO bo, UnitOfWork uow)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.ClientAccountId = bo.AccountId;
            _entity.Description = bo.Description;

            var err = HandleActivities(_entity, bo.Activities, uow);
            if ((err != ErrorCode.Success))
                return err;
            return ErrorCode.Success;
        }

        private static ErrorCode HandleActivities(ClientGift _entity, List<ActivityBO> activities, UnitOfWork uow)
        {
            if ((activities.Count == 0))
                return ErrorCode.Success;
            foreach (var x in activities)
            {
                if ((x.ID == 0))
                {
                }
                else
                   // add the activity to the clientgift
                   if ((!_entity.Activity.Any(m => m.ActivityId == x.ID)))
                {
                    var act = uow.GetActivityDAO().GetById(x.ID);
                    _entity.Activity.Add(act);
                }
            }
            // delete
            List<Activity> delList = new List<Activity>();
            foreach (var x in _entity.Activity)
            {
                if ((!activities.Any(f => f.ID == x.ActivityId)))
                    delList.Add(x);
            }
            foreach (var x in delList)
                _entity.Activity.Remove(x);
            return ErrorCode.Success;
        }
    }
}

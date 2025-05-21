using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using DALCore.Models;

namespace ServiceCore.Translators
{
    public class ActivityGroupTranslator
    {
        public static ErrorCode TranslateEntityToBO(ActivityGroup _entity, ActivityGroupBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.ID = _entity.GroupId;
            bo.Name = _entity.Name;
            bo.Lot = System.Convert.ToInt16(_entity.Lot);
            foreach (var x in _entity.Activities)
            {
                ActivityBO activity = new ActivityBO();
                activity.ID = x.ActivityId;
                activity.Name = x.Omschrijving;

                bo.Activities.Add(activity);
            }
            return ErrorCode.Success;
        }
        // NAZIEN AUB
        internal static ErrorCode TranslateBOToEntity(ActivityGroup _entity, ActivityGroupBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.Lot = bo.Lot;
            _entity.Name = bo.Name;
            var err = HandleActivities(_entity, bo.Activities);
            if ((err != ErrorCode.Success))
                return err;
            return ErrorCode.Success;
        }
        private static ErrorCode HandleActivities(ActivityGroup _entity, List<ActivityBO> activities)
        {
            if ((activities.Count == 0))
                return ErrorCode.Success;
            foreach (var x in activities)
            {
                if ((x.ID == 0))
                {
                    // insert
                    Activity activity = new Activity();
                    var err = ActivityTranslator.TranslateBOToEntity(activity, x);
                    if ((err != ErrorCode.Success))
                        return err;
                    _entity.Activities.Add(activity);
                }
                else
                {
                    // update
                    var activity = _entity.Activities.FirstOrDefault(f => f.ActivityId == x.ID);
                    if ((activity != null))
                    {
                        var err = ActivityTranslator.TranslateBOToEntity(activity, x);
                        if ((err != ErrorCode.Success))
                            return err;
                    }
                }
            }
            // delete
            List<Activity> delList = new List<Activity>();
            foreach (var x in _entity.Activities)
            {
                if ((!activities.Any(f => f.ID == x.ActivityId)))
                    delList.Add(x);
            }
            foreach (var x in delList)
                _entity.Activities.Remove(x);
            return ErrorCode.Success;
        }
    }
}

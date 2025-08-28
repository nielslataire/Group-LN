using System;
using System.Collections.Generic;
using System.Linq;
using BOCore;
using DALCore.Models;

namespace ServiceCore.Translators
{
    public class ActivityGroupTranslator
    {
        // Vertaal een ActivityGroup entity naar een BO
        public static ErrorCode TranslateEntityToBO(ActivityGroup _entity, ActivityGroupBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;

            bo.ID = _entity.GroupId;
            bo.Name = _entity.Name;
            bo.Lot = Convert.ToInt16(_entity.Lot);

            foreach (var activityEntity in _entity.Activity)
            {
                var activityBO = new ActivityBO
                {
                    ID = activityEntity.ActivityId,
                    Name = activityEntity.Omschrijving
                };

                bo.Activities.Add(activityBO);
            }

            return ErrorCode.Success;
        }

        // Vertaal een BO naar een ActivityGroup entity
        internal static ErrorCode TranslateBOToEntity(ActivityGroup _entity, ActivityGroupBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;

            // Update de basisgegevens van de groep
            _entity.Lot = bo.Lot;
            _entity.Name = bo.Name;

            // Verwerk de activiteiten
            var err = HandleActivities(_entity, bo.Activities);
            if (err != ErrorCode.Success)
                return err;

            return ErrorCode.Success;
        }

        // Verwerk de activiteiten binnen een ActivityGroup
        private static ErrorCode HandleActivities(ActivityGroup _entity, List<ActivityBO> activities)
        {
            // Als er geen activiteiten zijn, is er niets te doen
            if (activities == null || activities.Count == 0)
                return ErrorCode.Success;

            foreach (var activityBO in activities)
            {
                if (activityBO.ID == 0)
                {
                    // Nieuwe activiteit toevoegen
                    var activityEntity = new Activity();
                    var err = ActivityTranslator.TranslateBOToEntity(activityEntity, activityBO);
                    if (err != ErrorCode.Success)
                        return err;

                    _entity.Activity.Add(activityEntity);
                }
                else
                {
                    // Activiteit bijwerken
                    var activityEntity = _entity.Activity.FirstOrDefault(f => f.ActivityId == activityBO.ID);
                    if (activityEntity != null)
                    {
                        var err = ActivityTranslator.TranslateBOToEntity(activityEntity, activityBO);
                        if (err != ErrorCode.Success)
                            return err;
                    }
                }
            }

            // Verwijder de activiteiten die niet meer in de nieuwe lijst zitten
            var activitiesToDelete = _entity.Activity
                .Where(x => !activities.Any(f => f.ID == x.ActivityId))
                .ToList();

            foreach (var activity in activitiesToDelete)
                _entity.Activity.Remove(activity);

            return ErrorCode.Success;
        }
    }
}

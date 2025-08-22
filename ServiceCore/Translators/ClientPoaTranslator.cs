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
   public class ClientPoaTranslator
    {
        internal static ErrorCode TranslateEntityToBO(ClientPoa _entity, ClientPoaBO bo)
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
        internal static ErrorCode TranslateBOToEntity(ClientPoa _entity, ClientPoaBO bo, UnitOfWorkCore uow)
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

        private static ErrorCode HandleActivities(ClientPoa _entity, List<ActivityBO> activities, UnitOfWorkCore uow)
        {
            if (_entity == null) return ErrorCode.EntityNull;
            if (uow == null) return ErrorCode.UowNull;

            // Null-safe
            activities ??= new List<ActivityBO>();
            _entity.Activity ??= new List<Activity>();

            // Gewenste ids uit BO (alleen > 0)
            var desiredIds = new HashSet<int>(activities.Where(a => a.ID > 0)
                                                        .Select(a => a.ID));

            // Bestaande ids op entity
            var existingIds = _entity.Activity.Select(a => a.ActivityId).ToHashSet();

            // ADD: wat nog niet aanwezig is
            foreach (var id in desiredIds)
            {
                if (!existingIds.Contains(id))
                {
                    var act = uow.Activities.GetById(id);
                    if (act != null)
                        _entity.Activity.Add(act);
                    existingIds.Add(id); // guard tegen dubbel toevoegen in één run
                }
            }

            // DELETE: wat niet meer gewenst is
            var delList = _entity.Activity
                                 .Where(a => !desiredIds.Contains(a.ActivityId))
                                 .ToList();

            foreach (var a in delList)
                _entity.Activity.Remove(a);

            return ErrorCode.Success;
        }

    }
}

using BOCore;
using FacadeCore;
using DALCore;
using DALCore.Models;
using ServiceCore.Translators;
//using System.Data.Entity;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;


namespace ServiceCore
{
    public class ActivityService : IActivityService
    {
        public GetResponse<IdNameBO> GetActivitiesForSelect()
        {
            GetResponse<IdNameBO> response = new GetResponse<IdNameBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetActivityDAO();
            var entities = dao.GetNoTracking()
                .Include(m => m.Group)
                .OrderBy(m => m.Omschrijving);
            foreach (var _entity in entities)
                response.AddValue(_entity.GetIdName());
            response.Values.OrderBy(m => m.Display);
            return response;
        }

        public GetResponse<ActivityBO> GetActivities()
        {
            GetResponse<ActivityBO> response = new GetResponse<ActivityBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetActivityDAO();
            var entities = dao.GetNoTracking().OrderBy(m => m.Omschrijving);
            foreach (var _entity in entities)
            {
                ActivityBO bo = new ActivityBO();
                bo.ID = _entity.ActivityId;
                bo.Name = _entity.Omschrijving;
                if ((_entity.Group != null))
                {
                    bo.Group.Name = _entity.Group.Name;
                    bo.Group.ID = _entity.Group.GroupId;
                    bo.Group.Lot = System.Convert.ToInt16(_entity.Group.Lot);
                }
                // bo.GroupName = _entity.ActivityGroup.Name

                response.AddValue(bo);
            }
            return response;
        }
        public GetResponse<ActivityBO> GetActivitiesbyId(List<int> IdList)
        {
            GetResponse<ActivityBO> response = new GetResponse<ActivityBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetActivityDAO();

            var entities = dao.GetNoTracking().Where(m => IdList.Contains(m.ActivityId));
            foreach (var _entity in entities)
            {
                ActivityBO bo = new ActivityBO();
                bo.ID = _entity.ActivityId;
                bo.Name = _entity.Omschrijving;
                if ((_entity.Group != null))
                {
                    bo.Group.Name = _entity.Group.Name;
                    bo.Group.ID = _entity.Group.GroupId;
                    bo.Group.Lot = System.Convert.ToInt16(_entity.Group.Lot);
                }
                response.AddValue(bo);
            }
            return response;
        }
        public GetResponse<ActivityBO> GetActivitybyId(int id)
        {
            var response = new GetResponse<ActivityBO>();

            using (var uow = new UnitOfWork())
            {
                var dao = uow.GetActivityDAO();
                var entity = dao.GetNoTracking()
                    .Where(m => m.ActivityId == id)
                    .Include(m => m.Group)
                    .SingleOrDefault();

                if (entity != null)
                {
                    var bo = new ActivityBO
                    {
                        ID = entity.ActivityId,
                        Name = entity.Omschrijving,
                        Group = entity.Group != null ? new ActivityGroupBO
                        {
                            ID = entity.Group.GroupId,
                            Name = entity.Group.Name,
                            Lot = Convert.ToInt16(entity.Group.Lot)
                        } : null
                    };

                    response.AddValue(bo);
                }
            }

            return response;
        }
        public GetResponse<ActivityGroupBO> GetActivityGroups()

        {
            GetResponse<ActivityGroupBO> response = new GetResponse<ActivityGroupBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetActivityGroupDAO();

            var entities = dao.GetNoTracking()
                .Include(m => m.Activities);
            foreach (var _entity in entities)
            {
                ActivityGroupBO bo = new ActivityGroupBO();
                var err = ActivityGroupTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<IdNameBO> GetActivityGroupsForSelect()
        {
            GetResponse<IdNameBO> response = new GetResponse<IdNameBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetActivityGroupDAO();
            var entities = dao.GetNoTracking();
            entities = entities.OrderBy(m => m.Lot);
            foreach (var _entity in entities)
                response.AddValue(_entity.GetIdName());
            return response;
        }
        public GetResponse<ActivityGroupBO> GetActivityGroupbyId(int id)
        {
            GetResponse<ActivityGroupBO> response = new GetResponse<ActivityGroupBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetActivityGroupDAO();

            var _entity = dao.GetById(id);
            // For Each _entity In entities
            ActivityGroupBO bo = new ActivityGroupBO();
            bo.ID = _entity.GroupId;
            bo.Name = _entity.Name;
            bo.Lot = System.Convert.ToInt16(_entity.Lot);
            foreach (var Activity in _entity.Activities)
            {
                ActivityBO act = new ActivityBO();
                act.Name = Activity.Omschrijving;
                act.ID = Activity.ActivityId;
                bo.Activities.Add(act);
            }
            response.AddValue(bo);
            // Next
            return response;
        }


        public Response InsertUpdate(ActivityBO bo)
        {
            Response response = new Response();
            if ((string.IsNullOrWhiteSpace(bo.Name)))
                response.AddError("name is mandatory");
            if ((!response.Success))
                return response;

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetActivityDAO();
            Activity _entity = null/* TODO Change to default(_) if this is not a reference type */;

            if ((bo.ID == 0))
                _entity = dao.GetNew();
            else
                _entity = dao.GetById(bo.ID);

            if ((_entity != null))
            {
                var err = ActivityTranslator.TranslateBOToEntity(_entity, bo);

                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("activity not found");
            response.AddError(uow.SaveChanges());

            return response;
        }
        public Response InsertUpdateGroup(ActivityGroupBO bo)
        {
            Response response = new Response();
            if ((string.IsNullOrWhiteSpace(bo.Name)))
                response.AddError("name is mandatory");
            if ((!response.Success))
                return response;

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetActivityGroupDAO();
            ActivityGroup _entity = null/* TODO Change to default(_) if this is not a reference type */;

            if ((bo.ID == 0))
                _entity = dao.GetNew();
            else
                _entity = dao.GetById(bo.ID);

            if ((_entity != null))
            {
                var err = ActivityGroupTranslator.TranslateBOToEntity(_entity, bo);

                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("activity not found");
            response.AddError(uow.SaveChanges());

            return response;
        }

        public Response Delete(List<int> ids)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();

            foreach (var id in ids)
                uow.GetActivityDAO().DeleteObject(id);
            response.Messages.AddRange(uow.SaveChanges());

            return response;
        }

        public Response Delete(List<ActivityBO> bos)
        {
            return Delete(bos.Select(s => s.ID).ToList());
        }
        public Response DeleteGroup(List<int> ids)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();

            foreach (var id in ids)
                uow.GetActivityGroupDAO().DeleteObject(id);
            response.Messages.AddRange(uow.SaveChanges());

            return response;
        }

        public Response DeleteGroup(List<ActivityBO> bos)
        {
            return Delete(bos.Select(s => s.ID).ToList());
        }
    }

}

using BOCore;
using FacadeCore;
using DALCore;
using DALCore.Models;
using ServiceCore.Translators;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceCore
{
    public class ActivityService : IActivityService
    {
        private readonly UnitOfWorkCore _uow;

        public ActivityService(UnitOfWorkCore uow)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        }

        public GetResponse<IdNameBO> GetActivitiesForSelect()
        {
            var response = new GetResponse<IdNameBO>();

            var entities = _uow.Activities
                .GetNoTracking()
                .Include(m => m.Group)
                .OrderBy(m => m.Omschrijving)
                .ToList();

            foreach (var e in entities)
                response.AddValue(e.GetIdName());

            // Sorteren op Display (de OrderBy bovenaan sorteert op Omschrijving in DB)
            // Hier nog eens voor de zekerheid op de uiteindelijke Display.
            if (response.Values != null && response.Values.Count > 1)
                response.Values = response.Values.OrderBy(v => v.Display).ToList();

            return response;
        }

        public GetResponse<ActivityBO> GetActivities()
        {
            var response = new GetResponse<ActivityBO>();

            var entities = _uow.Activities
                .GetNoTracking()
                .Include(m => m.Group)
                .OrderBy(m => m.Omschrijving)
                .ToList();

            foreach (var e in entities)
            {
                var bo = new ActivityBO
                {
                    ID = e.ActivityId,
                    Name = e.Omschrijving
                };

                if (e.Group != null)
                {
                    bo.Group.Name = e.Group.Name;
                    bo.Group.ID = e.Group.GroupId;
                    bo.Group.Lot = Convert.ToInt16(e.Group.Lot);
                }

                response.AddValue(bo);
            }

            return response;
        }

        public GetResponse<ActivityBO> GetActivitiesbyId(List<int> idList)
        {
            var response = new GetResponse<ActivityBO>();
            if (idList == null || idList.Count == 0) return response;

            var entities = _uow.Activities
                .GetNoTracking()
                .Include(m => m.Group)
                .Where(m => idList.Contains(m.ActivityId))
                .ToList();

            foreach (var e in entities)
            {
                var bo = new ActivityBO
                {
                    ID = e.ActivityId,
                    Name = e.Omschrijving
                };

                if (e.Group != null)
                {
                    bo.Group.Name = e.Group.Name;
                    bo.Group.ID = e.Group.GroupId;
                    bo.Group.Lot = Convert.ToInt16(e.Group.Lot);
                }

                response.AddValue(bo);
            }

            return response;
        }

        public GetResponse<ActivityBO> GetActivitybyId(int id)
        {
            var response = new GetResponse<ActivityBO>();

            var entity = _uow.Activities
                .GetNoTracking()
                .Include(m => m.Group)
                .SingleOrDefault(m => m.ActivityId == id);

            if (entity != null)
            {
                var bo = new ActivityBO
                {
                    ID = entity.ActivityId,
                    Name = entity.Omschrijving,
                    Group = entity.Group != null
                        ? new ActivityGroupBO
                        {
                            ID = entity.Group.GroupId,
                            Name = entity.Group.Name,
                            Lot = Convert.ToInt16(entity.Group.Lot)
                        }
                        : null
                };

                response.AddValue(bo);
            }

            return response;
        }
        public GetResponse<ActivityGroupBO> GetActivityGroups()
        {
            var response = new GetResponse<ActivityGroupBO>();

            var entities = _uow.ActivityGroups.GetNoTracking()
                .Include(g => g.Activity)               // we hebben de activiteiten vaak nodig
                .OrderBy(g => g.Lot).ThenBy(g => g.Name);

            foreach (var entity in entities)
            {
                var bo = new ActivityGroupBO();
                var err = ActivityGroupTranslator.TranslateEntityToBO(entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }

            return response;
        }

        public GetResponse<IdNameBO> GetActivityGroupsForSelect()
        {
            var response = new GetResponse<IdNameBO>();

            var entities = _uow.ActivityGroups.GetNoTracking()
                .OrderBy(g => g.Lot).ThenBy(g => g.Name)
                .ToList();

            foreach (var entity in entities)
                response.AddValue(entity.GetIdName());

            return response;
        }

        public GetResponse<ActivityGroupBO> GetActivityGroupbyId(int id)
        {
            var response = new GetResponse<ActivityGroupBO>();

            // Belangrijk: GetById() gebruikt Find() en laadt navigaties niet.
            var entity = _uow.ActivityGroups.GetNoTracking()
                .Include(g => g.Activity)
                .SingleOrDefault(g => g.GroupId == id);

            if (entity == null)
            {
                response.AddError("activity group not found");
                return response;
            }

            var bo = new ActivityGroupBO();
            var err = ActivityGroupTranslator.TranslateEntityToBO(entity, bo);
            if (err == ErrorCode.Success)
                response.AddValue(bo);
            else
                response.AddError(err.ToString());

            return response;
        }
        public Response InsertUpdate(ActivityBO bo)
        {
            var response = new Response();

            if (string.IsNullOrWhiteSpace(bo?.Name))
            {
                response.AddError("name is mandatory");
                return response;
            }

            Activity entity = null;

            if (bo.ID == 0)
                entity = _uow.Activities.GetNew();
            else
                entity = _uow.Activities.GetById(bo.ID);

            if (entity == null)
            {
                response.AddError("activity not found");
                return response;
            }

            var err = ActivityTranslator.TranslateBOToEntity(entity, bo);
            if (err != ErrorCode.Success)
            {
                response.AddError(err.ToString());
                return response;
            }

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Activiteit aangepast of toegevoegd", "Activiteit niet aangepast of toegevoegd");

            response.InsertedId = entity.ActivityId;
            return response;
        }

        public Response InsertUpdateGroup(ActivityGroupBO bo)
        {
            var response = new Response();

            if (string.IsNullOrWhiteSpace(bo?.Name))
            {
                response.AddError("name is mandatory");
                return response;
            }

            ActivityGroup entity = null;

            if (bo.ID == 0)
                entity = _uow.ActivityGroups.GetNew();
            else
                entity = _uow.ActivityGroups.GetById(bo.ID);

            if (entity == null)
            {
                response.AddError("activitygroup not found");
                return response;
            }

            var err = ActivityGroupTranslator.TranslateBOToEntity(entity, bo);
            if (err != ErrorCode.Success)
            {
                response.AddError(err.ToString());
                return response;
            }

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Activiteitgroep aangepast of toegevoegd", "Activiteitgroep niet aangepast of toegevoegd");

            return response;
        }

        public Response Delete(List<int> ids)
        {
            var response = new Response();
            if (ids == null || ids.Count == 0) return response;

            foreach (var id in ids)
                _uow.Activities.DeleteObject(id);

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Record(s) verwijderd", "Geen records verwijderd");

            return response;
        }

        public Response Delete(List<ActivityBO> bos)
        {
            return Delete(bos?.Select(s => s.ID).ToList() ?? new List<int>());
        }

        public Response DeleteGroup(List<int> ids)
        {
            var response = new Response();
            if (ids == null || ids.Count == 0) return response;

            foreach (var id in ids)
                _uow.ActivityGroups.DeleteObject(id);

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Record(s) verwijderd", "Geen records verwijderd");

            return response;
        }

        public Response DeleteGroup(List<ActivityBO> bos)
        {
            // Let op: jouw vorige code riep hier Delete(bos...) aan (die Activities delete),
            // dat lijkt me niet de bedoeling voor groups. Dus we mappen naar group-ids als je die hebt.
            // Als bos eigenlijk groups bevat met ID = groupId, is dit oké:
            return DeleteGroup(bos?.Select(b => b.ID).ToList() ?? new List<int>());
        }
    }
}
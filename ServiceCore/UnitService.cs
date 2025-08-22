using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BOCore;
using FacadeCore;
using DALCore;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Helpers;
using ServiceCore.Translators;

namespace ServiceCore
{
    public class UnitService : IUnitService
    {
        private readonly UnitOfWorkCore _uow;

        public UnitService(UnitOfWorkCore uow)
        {
            _uow = uow;
        }

        public GetResponse<UnitBO> GetUnitById(int id)
        {
            var response = new GetResponse<UnitBO>();

            var entity = _uow.Units.GetNoTracking()
                .Where(m => m.Id == id)
                .Include(m => m.AttachedUnit)
                .Include(m => m.LinkedUnit)
                .Include(m => m.Project)
                .Include(m => m.Type).ThenInclude(t => t.Group)
                .Include(m => m.InverseLinkedUnit)
                .Include(m => m.InverseAttachedUnit)
                .Include(m => m.LevelNavigation)
                .Include(m => m.UnitConstructionValue)
                .FirstOrDefault();

            var bo = new UnitBO();
            var err = UnitTranslator.TranslateEntityToBO(entity, bo);
            if (err == ErrorCode.Success) response.Value = bo;
            else response.AddError(err.ToString());

            return response;
        }

        public GetResponse<UnitBO> GetUnitsByProjectId(int projectId)
        {
            var response = new GetResponse<UnitBO>();

            var query = _uow.Units.GetNoTracking()
                .Where(m => m.ProjectId == projectId && m.TypeId != 0)
                .Include(m => m.AttachedUnit)
                .Include(m => m.LinkedUnit)
                .Include(m => m.Project)
                .Include(m => m.Type).ThenInclude(t => t.Group)
                .Include(m => m.InverseLinkedUnit)
                .Include(m => m.LevelNavigation)
                .OrderBy(x => x.Name);

            foreach (var e in query)
            {
                var bo = new UnitBO();
                var err = UnitTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<UnitBO> GetUnitsByProjectId(int projectId, int unitTypeId)
        {
            var response = new GetResponse<UnitBO>();

            var query = _uow.Units.GetNoTracking()
                .Where(m => m.ProjectId == projectId && m.TypeId == unitTypeId);

            foreach (var e in query)
            {
                var bo = new UnitBO();
                var err = UnitTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<UnitWithDetailsBO> GetUnitsWithDetailsByProjectId(int projectId)
        {
            var response = new GetResponse<UnitWithDetailsBO>();

            var query = _uow.Units.GetNoTracking()
                .Where(m => m.ProjectId == projectId)
                .Include(m => m.UnitRooms);

            foreach (var e in query)
            {
                var bo = new UnitWithDetailsBO();
                var err = UnitTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success)
                {
                    foreach (var room in e.UnitRooms)
                    {
                        var roomBo = new RoomBO();
                        var rerr = UnitRoomTranslator.TranslateEntityToBO(room, roomBo);
                        if (rerr == ErrorCode.Success) bo.Rooms.Add(roomBo);
                        else response.AddError(rerr.ToString());
                    }
                    response.AddValue(bo);
                }
                else response.AddError(err.ToString());
            }

            response.Values = response.Values
                .OrderBy(m => m.Name, new AlphanumComparator())
                .ThenBy(m => m.Level)
                .ToList();

            return response;
        }

        public GetResponse<UnitBO> GetUnitsByAccountId(int accountId)
        {
            var response = new GetResponse<UnitBO>();

            var list = _uow.Units.GetNoTracking()
                .Where(m => m.ClientAccountId == accountId)
                .Include(m => m.Type)
                .Include(m => m.UnitConstructionValue)
                .ToList();

            foreach (var e in list)
            {
                var bo = new UnitBO();
                var err = UnitTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<GroupUnitsBO> GetGroupedUnitsByProjectId(int projectId)
        {
            var response = new GetResponse<GroupUnitsBO>();

            var groups = _uow.Units.GetNoTracking()
                .Where(m => m.ProjectId == projectId && m.LinkedUnit == null)
                .Include(m => m.AttachedUnit)
                .Include(m => m.LinkedUnit)
                .Include(m => m.Project)
                .Include(m => m.Type).ThenInclude(t => t.Group)
                .Include(m => m.InverseLinkedUnit)
                .OrderBy(x => x.Name)
                .GroupBy(x => x.TypeId);

            foreach (var g in groups)
            {
                var bo = new GroupUnitsBO { Id = g.Key ?? 0 };
                foreach (var item in g)
                {
                    var ubo = new UnitBO();
                    var err = UnitTranslator.TranslateEntityToBO(item, ubo);
                    if (err == ErrorCode.Success) bo.Units.Add(ubo);
                    else response.AddError(err.ToString());
                }
                bo.Units = bo.Units
                    .OrderBy(m => m.Level)
                    .ThenBy(m => m.Name, new AlphanumComparator())
                    .ToList();
                response.AddValue(bo);
            }
            return response;
        }

        public GetResponse<GroupUnitsWithAttachedUnitsBO> GetGroupedUnitsForSaleByProjectId(int projectId)
        {
            var response = new GetResponse<GroupUnitsWithAttachedUnitsBO>();

            var groups = _uow.Units.GetNoTracking()
                .Where(m => m.ProjectId == projectId && m.ClientAccountId == null && m.AttachedUnit == null && m.LinkedUnit == null)
                .Include(m => m.InverseAttachedUnit).ThenInclude(a => a.InverseAttachedUnit).ThenInclude(a2 => a2.InverseAttachedUnit)
                .OrderBy(x => x.Name)
                .GroupBy(x => x.TypeId);

            foreach (var g in groups)
            {
                var go = new GroupUnitsWithAttachedUnitsBO { Id = g.Key ?? 0 };

                foreach (var item in g)
                {
                    var u = new UnitWithAttachedUnitsBO();
                    var err = UnitTranslator.TranslateEntityToBO(item, u.Unit);
                    if (err != ErrorCode.Success) { response.AddError(err.ToString()); continue; }

                    // nested attached up to depth 3 (zoals origineel)
                    foreach (var a1 in item.InverseAttachedUnit)
                    {
                        var au1 = new UnitBO();
                        var e1 = UnitTranslator.TranslateEntityToBO(a1, au1);
                        if (e1 == ErrorCode.Success) u.AttachedUnits.Add(au1);
                        else response.AddError(e1.ToString());

                        foreach (var a2 in a1.InverseAttachedUnit)
                        {
                            var au2 = new UnitBO();
                            var e2 = UnitTranslator.TranslateEntityToBO(a2, au2);
                            if (e2 == ErrorCode.Success) u.AttachedUnits.Add(au2);
                            else response.AddError(e2.ToString());

                            foreach (var a3 in a2.InverseAttachedUnit)
                            {
                                var au3 = new UnitBO();
                                var e3 = UnitTranslator.TranslateEntityToBO(a3, au3);
                                if (e3 == ErrorCode.Success) u.AttachedUnits.Add(au3);
                                else response.AddError(e3.ToString());
                            }
                        }
                    }

                    go.Units.Add(u);
                }

                go.Units = go.Units
                    .OrderBy(m => m.Unit.Level)
                    .ThenBy(m => m.Unit.Name, new AlphanumComparator())
                    .ToList();

                response.AddValue(go);
            }

            return response;
        }

        public GetResponse<GroupUnitsWithAttachedUnitsWithDetailsBO> GetGroupedUnitsForSaleWithDetailsByProjectId(int projectId)
        {
            var response = new GetResponse<GroupUnitsWithAttachedUnitsWithDetailsBO>();

            var groups = _uow.Units.GetNoTracking()
                .Where(m => m.ProjectId == projectId && m.ClientAccountId == null && m.AttachedUnit == null && m.LinkedUnit == null)
                .Include(m => m.UnitRooms)
                .Include(m => m.InverseAttachedUnit).ThenInclude(a => a.UnitRooms)
                .Include(m => m.InverseAttachedUnit).ThenInclude(a => a.InverseAttachedUnit).ThenInclude(a2 => a2.UnitRooms)
                .OrderBy(x => x.Name)
                .GroupBy(x => x.TypeId);

            foreach (var g in groups)
            {
                var go = new GroupUnitsWithAttachedUnitsWithDetailsBO { Id = g.Key ?? 0 };

                foreach (var item in g)
                {
                    var u = new UnitWithAttachedUnitsWithDetailsBO();

                    var err = UnitTranslator.TranslateEntityToBO(item, u.Unit);
                    if (err != ErrorCode.Success) { response.AddError(err.ToString()); continue; }

                    foreach (var room in item.UnitRooms)
                    {
                        var rbo = new RoomBO();
                        var rerr = UnitRoomTranslator.TranslateEntityToBO(room, rbo);
                        if (rerr == ErrorCode.Success) u.Unit.Rooms.Add(rbo);
                        else response.AddError(rerr.ToString());
                    }

                    foreach (var a1 in item.InverseAttachedUnit)
                    {
                        var au1 = new UnitWithDetailsBO();
                        var e1 = UnitTranslator.TranslateEntityToBO(a1, au1);
                        if (e1 == ErrorCode.Success) u.AttachedUnits.Add(au1);
                        else response.AddError(e1.ToString());

                        foreach (var room in a1.UnitRooms)
                        {
                            var rbo = new RoomBO();
                            var rerr = UnitRoomTranslator.TranslateEntityToBO(room, rbo);
                            if (rerr == ErrorCode.Success) au1.Rooms.Add(rbo);
                            else response.AddError(rerr.ToString());
                        }

                        foreach (var a2 in a1.InverseAttachedUnit)
                        {
                            var au2 = new UnitWithDetailsBO();
                            var e2 = UnitTranslator.TranslateEntityToBO(a2, au2);
                            if (e2 == ErrorCode.Success) u.AttachedUnits.Add(au2);
                            else response.AddError(e2.ToString());

                            foreach (var room in a2.UnitRooms)
                            {
                                var rbo = new RoomBO();
                                var rerr = UnitRoomTranslator.TranslateEntityToBO(room, rbo);
                                if (rerr == ErrorCode.Success) au2.Rooms.Add(rbo);
                                else response.AddError(rerr.ToString());
                            }

                            foreach (var a3 in a2.InverseAttachedUnit)
                            {
                                var au3 = new UnitWithDetailsBO();
                                var e3 = UnitTranslator.TranslateEntityToBO(a3, au3);
                                if (e3 == ErrorCode.Success) u.AttachedUnits.Add(au3);
                                else response.AddError(e3.ToString());

                                foreach (var room in a3.UnitRooms)
                                {
                                    var rbo3 = new RoomBO();
                                    var rerr3 = UnitRoomTranslator.TranslateEntityToBO(room, rbo3);
                                    if (rerr3 == ErrorCode.Success) au3.Rooms.Add(rbo3);
                                    else response.AddError(rerr3.ToString());
                                }
                            }
                        }
                    }

                    go.Units.Add(u);
                }

                go.Units = go.Units
                    .OrderBy(m => m.Unit.Level)
                    .ThenBy(m => m.Unit.Name, new AlphanumComparator())
                    .ToList();

                response.AddValue(go);
            }

            return response;
        }

        public GetResponse<GroupUnitsBO> GetGroupedUnitsByAccountId(int accountId)
        {
            var response = new GetResponse<GroupUnitsBO>();

            var groups = _uow.Units.GetNoTracking()
                .Where(m => m.ClientAccountId == accountId)
                .OrderBy(x => x.Name)
                .GroupBy(x => x.TypeId);

            foreach (var g in groups)
            {
                var bo = new GroupUnitsBO { Id = g.Key ?? 0 };
                foreach (var item in g)
                {
                    var ubo = new UnitBO();
                    var err = UnitTranslator.TranslateEntityToBO(item, ubo);
                    if (err == ErrorCode.Success) bo.Units.Add(ubo);
                    else response.AddError(err.ToString());
                }
                bo.Units = bo.Units.OrderBy(m => m.Name, new AlphanumComparator()).ToList();
                response.AddValue(bo);
            }
            return response;
        }

        public GetResponse<IdNameBO> GetAvailableUnitsByProjectId(int projectId)
        {
            var response = new GetResponse<IdNameBO>();

            var list = _uow.Units.GetNoTracking()
                .Where(m => m.ProjectId == projectId && m.ClientAccountId == null && m.LinkedUnit == null)
                .Include(m => m.Type).ThenInclude(t => t.Group)
                .ToList();

            foreach (var e in list) response.AddValue(e.GetIdName());
            return response;
        }

        public GetResponse<IdNameBO> GetUnitsByProjectIdForSelect(int projectId, bool withLinked)
        {
            var response = new GetResponse<IdNameBO>();

            var query = _uow.Units.GetNoTracking()
                .Where(m => m.ProjectId == projectId && (withLinked || m.Type.Id != 11))
                .Include(m => m.Type).ThenInclude(t => t.Group);

            foreach (var e in query) response.AddValue(e.GetIdName());
            return response;
        }

        public GetResponse<IdNameBO> GetUnitsByProjectIdForSelect(int projectId, int unitTypeId)
        {
            var response = new GetResponse<IdNameBO>();

            var query = _uow.Units.GetNoTracking()
                .Where(m => m.ProjectId == projectId && m.TypeId == unitTypeId)
                .Include(m => m.Type).ThenInclude(t => t.Group);

            foreach (var e in query) response.AddValue(e.GetIdName());
            return response;
        }

        public GetResponse<IdNameBO> GetUnitsByProjectIdForSelectAttachedUnit(int projectId)
        {
            var response = new GetResponse<IdNameBO>();

            var query = _uow.Units.GetNoTracking()
                .Where(m => m.ProjectId == projectId && m.LinkedUnit == null)
                .Include(m => m.Type).ThenInclude(t => t.Group);

            foreach (var e in query) response.AddValue(e.GetIdName());
            return response;
        }

        public GetResponse<IdNameBO> GetUnitsByProjectIdForSelectAttachedUnit(int projectId, int unitId)
        {
            var response = new GetResponse<IdNameBO>();

            var query = _uow.Units.GetNoTracking()
                .Where(m => m.ProjectId == projectId && m.LinkedUnit == null && m.Id != unitId)
                .Include(m => m.Type).ThenInclude(t => t.Group);

            foreach (var e in query) response.AddValue(e.GetIdName());
            return response;
        }

        public GetResponse<UnitTypeBO> GetUniqueUnitTypesInProjectByProjectId(int projectId)
        {
            var response = new GetResponse<UnitTypeBO>();

            var types = _uow.Units.GetNoTracking()
                .Where(m => m.ProjectId == projectId)
                .Select(m => m.Type)
                .Distinct();

            foreach (var t in types)
            {
                var bo = new UnitTypeBO();
                var err = UnitTypeTranslator.TranslateEntityToBO(t, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<UnitWithStagesBO> GetClientUnitsWithStages(int clientAcccountId)
        {
            var response = new GetResponse<UnitWithStagesBO>();

            var query = _uow.Units.GetNoTracking()
                .Where(m => m.ClientAccountId == clientAcccountId)
                .Include(m => m.Type).ThenInclude(t => t.Group)
                .Include(m => m.Project)
                .Include(m => m.UnitConstructionValue)
                .Include(m => m.PaymentGroup).ThenInclude(pg => pg.InvoicingPaymentStages);

            foreach (var e in query)
            {
                var bo = new UnitWithStagesBO();
                var ubo = new UnitBO();
                var err = UnitTranslator.TranslateEntityToBO(e, ubo);
                if (err != ErrorCode.Success) { response.AddError(err.ToString()); continue; }

                bo.Unit = ubo;

                if (e.PaymentGroup?.InvoicingPaymentStages != null)
                {
                    foreach (var st in e.PaymentGroup.InvoicingPaymentStages)
                    {
                        var stbo = new ProjectPaymentStageBO();
                        var serr = ProjectPaymentStageTranslator.TranslateEntityToBO(st, stbo);
                        if (serr == ErrorCode.Success) bo.PaymentStages.Add(stbo);
                        else response.AddError(serr.ToString());
                    }
                }

                response.AddValue(bo);
            }

            return response;
        }

        public Response InsertUpdateUnit(UnitBO bo)
        {
            var response = new Response();
            if (string.IsNullOrWhiteSpace(bo.Name))
            {
                response.AddError("name is mandatory");
                return response;
            }

            var entity = bo.Id == 0 ? _uow.Units.GetNew()
                                    : _uow.Units.GetById(bo.Id);

            if (entity == null)
            {
                response.AddError("unit not found");
                return response;
            }

            var err = UnitTranslator.TranslateBOToEntity(entity, bo, _uow);
            if (err != ErrorCode.Success)
            {
                response.AddError(err.ToString());
                return response;
            }

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Unit opgeslagen", "Geen wijzigingen");
            response.InsertedId = entity.Id;
            return response;
        }

        public Response InsertUpdateUnitToClientAccount(UnitBO bo)
        {
            var response = new Response();

            if (bo.ClientAccountId == 0) response.AddError("No ClientAccount selected");
            else if (bo.Id == 0) response.AddError("No Unit selected");
            if (!response.Success) return response;

            var entity = _uow.Units.GetById(bo.Id);
            if (entity == null)
            {
                response.AddError("unit not found");
                return response;
            }

            entity.ClientAccountId = bo.ClientAccountId;
            entity.ConstructionValueSold = bo.ConstructionValueSold;
            entity.LandValueSold = bo.LandValueSold;

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Unit gekoppeld", "Geen wijzigingen");
            return response;
        }

        public Response DeleteUnit(List<int> ids)
        {
            var response = new Response();

            foreach (var id in ids)
            {
                // unlink children
                var attached = _uow.Units.GetNormal().Where(m => m.AttachedUnitId == id);
                foreach (var e in attached) e.AttachedUnitId = null;

                var linked = _uow.Units.GetNormal().Where(m => m.LinkedUnitId == id);
                foreach (var e in linked) e.LinkedUnitId = null;

                _uow.Units.DeleteObject(id);
            }

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Unit(s) verwijderd", "Geen records verwijderd");
            return response;
        }

        // FIXED: correcte Response retour + éénmalig saven
        public Response DeleteUnitFromClientAccountByUnitId(List<int> ids)
        {
            var response = new Response();

            foreach (var id in ids)
            {
                var entity = _uow.Units.GetById(id);
                if (entity == null)
                {
                    response.AddError($"Unit {id} niet gevonden");
                    continue;
                }
                entity.ConstructionValueSold = null;
                entity.LandValueSold = null;
                entity.ClientAccountId = null;
            }

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Eenheden losgekoppeld", "Geen wijzigingen opgeslagen");
            return response;
        }

        public Response DeleteUnitFromClientAccountByAccountId(List<int> accountIds)
        {
            var response = new Response();

            foreach (var accountId in accountIds)
            {
                var unitIds = _uow.Units.GetNoTracking()
                    .Where(m => m.ClientAccountId == accountId)
                    .Select(m => m.Id)
                    .ToList();

                var r = DeleteUnitFromClientAccountByUnitId(unitIds);
                response.Messages.AddRange(r.Messages);
            }

            return response;
        }

        // UNIT GROUP TYPES
        public GetResponse<UnitGroupTypeBO> GetUnitGroupTypes()
        {
            var response = new GetResponse<UnitGroupTypeBO>();

            var query = _uow.UnitGroupTypes.GetNoTracking()
                .Where(m => m.Selectable == true);

            foreach (var e in query)
            {
                var bo = new UnitGroupTypeBO();
                var err = UnitGroupTypeTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }

            return response;
        }

        public Response InsertUpdateUnitGroupType(UnitGroupTypeBO bo)
        {
            var response = new Response();
            if (string.IsNullOrWhiteSpace(bo.Name))
            {
                response.AddError("name is mandatory");
                return response;
            }

            var entity = bo.Id == 0 ? _uow.UnitGroupTypes.GetNew()
                                    : _uow.UnitGroupTypes.GetById(bo.Id);

            if (entity == null)
            {
                response.AddError("unitgrouptype not found");
                return response;
            }

            var err = UnitGroupTypeTranslator.TranslateBOToEntity(entity, bo);
            if (err != ErrorCode.Success)
            {
                response.AddError(err.ToString());
                return response;
            }

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "UnitGroupType opgeslagen", "Geen wijzigingen");
            return response;
        }

        public Response DeleteUnitGroupType(List<int> ids)
        {
            var response = new Response();
            foreach (var id in ids) _uow.UnitGroupTypes.DeleteObject(id);
            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Record(s) verwijderd", "Geen records verwijderd");
            return response;
        }

        // UNIT TYPES
        public GetResponse<UnitTypeBO> GetUnitTypesByGroupId(int groupId)
        {
            var response = new GetResponse<UnitTypeBO>();

            var query = _uow.UnitTypes.GetNoTracking()
                .Where(m => m.GroupId == groupId && m.Selectable == true);

            foreach (var e in query)
            {
                var bo = new UnitTypeBO();
                var err = UnitTypeTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public Response InsertUpdateUnitType(UnitTypeBO bo)
        {
            var response = new Response();
            if (string.IsNullOrWhiteSpace(bo.Name))
            {
                response.AddError("name is mandatory");
                return response;
            }

            var entity = bo.Id == 0 ? _uow.UnitTypes.GetNew()
                                    : _uow.UnitTypes.GetById(bo.Id);

            if (entity == null)
            {
                response.AddError("UnitType not found");
                return response;
            }

            var err = UnitTypeTranslator.TranslateBOToEntity(entity, bo);
            if (err != ErrorCode.Success)
            {
                response.AddError(err.ToString());
                return response;
            }

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "UnitType opgeslagen", "Geen wijzigingen");
            return response;
        }

        public Response DeleteUnitType(List<int> ids)
        {
            var response = new Response();
            foreach (var id in ids) _uow.UnitTypes.DeleteObject(id);
            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Record(s) verwijderd", "Geen records verwijderd");
            return response;
        }

        // UNIT ROOMS
        public GetResponse<RoomBO> GetRooms(int unitId)
        {
            var response = new GetResponse<RoomBO>();

            var query = _uow.UnitRooms.GetNoTracking()
                .Where(m => m.UnitId == unitId);

            foreach (var e in query)
            {
                var bo = new RoomBO();
                var err = UnitRoomTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<RoomType> GetUniqueRoomTypesInProjectByProjectId(int projectId)
        {
            var response = new GetResponse<RoomType>();

            var types = _uow.UnitRooms.GetNoTracking()
                .Where(m => m.Unit.ProjectId == projectId && m.Unit.ClientAccountId == null)
                .Where(i => i.Type == (int)RoomType.Terras
                         || i.Type == (int)RoomType.Slaapkamer
                         || i.Type == (int)RoomType.Dakterras
                         || i.Type == (int)RoomType.Tuin
                         || i.Type == (int)RoomType.Zolder)
                .Select(m => m.Type)
                .Distinct();

            foreach (var t in types) response.AddValue((RoomType)t);
            return response;
        }

        public Response InsertUpdateRoom(RoomBO bo)
        {
            var response = new Response();

            if (bo.UnitId == 0) response.AddError("Er moet een eenheid geselecteerd zijn");
            else if (bo.Type == 0) response.AddError("Er moet een kamertype geselecteerd zijn");
            else if (bo.Number < 1) response.AddError("Er moet een aantal vermeld zijn");
            if (!response.Success) return response;

            var entity = bo.Id == 0 ? _uow.UnitRooms.GetNew()
                                    : _uow.UnitRooms.GetById(bo.Id);

            if (entity == null)
            {
                response.AddError("UnitRoom not found");
                return response;
            }

            var err = UnitRoomTranslator.TranslateBOToEntity(entity, bo);
            if (err != ErrorCode.Success)
            {
                response.AddError(err.ToString());
                return response;
            }

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Kamer opgeslagen", "Geen wijzigingen");
            return response;
        }

        public Response DeleteRooms(List<int> ids)
        {
            var response = new Response();
            foreach (var id in ids) _uow.UnitRooms.DeleteObject(id);
            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Record(s) verwijderd", "Geen records verwijderd");
            return response;
        }

        // UNIT CONSTRUCTION VALUES
        public GetResponse<UnitConstructionValueBO> GetConstructionValues(int unitId)
        {
            var response = new GetResponse<UnitConstructionValueBO>();

            var query = _uow.UnitConstructionValues.GetNoTracking()
                .Where(m => m.UnitId == unitId)
                .Include(m => m.PaymentGroup)
                .Include(m => m.Unit);

            foreach (var e in query)
            {
                var bo = new UnitConstructionValueBO();
                var err = UnitConstructionValueTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<UnitConstructionValueBO> GetConstructionValue(int id)
        {
            var response = new GetResponse<UnitConstructionValueBO>();

            var entity = _uow.UnitConstructionValues.GetById(id);

            var bo = new UnitConstructionValueBO();
            var err = UnitConstructionValueTranslator.TranslateEntityToBO(entity, bo);
            if (err == ErrorCode.Success) response.Value = bo;
            else response.AddError(err.ToString());

            return response;
        }

        // Let op: logica is overgenomen zoals in je oorspronkelijke service (ook al voelt de Id-check wat onnatuurlijk bij "Insert")
        public Response InsertUpdateConstructionValue(UnitConstructionValueBO bo)
        {
            var response = new Response();
            if (bo.Id == 0) response.AddError("Er moet een constructiewaarde geselecteerd zijn");
            if (!response.Success) return response;

            var entity = bo.Id == 0 ? _uow.UnitConstructionValues.GetNew()
                                    : _uow.UnitConstructionValues.GetById(bo.Id);

            if (entity == null)
            {
                response.AddError("Unitconstructionvalue not found");
                return response;
            }

            var err = UnitConstructionValueTranslator.TranslateBOToEntity(entity, bo);
            if (err != ErrorCode.Success)
            {
                response.AddError(err.ToString());
                return response;
            }

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Constructiewaarde opgeslagen", "Geen wijzigingen");
            response.InsertedId = entity.Id;
            return response;
        }
        public Response UpdateConstructionValueSold(UnitConstructionValueBO bo)
        {
            var response = new Response();
            if (bo.Id == 0)
            {
                response.AddError("Er moet een constructiewaarde geselecteerd zijn");
                return response;
            }

            // 1) Huidige waarde no-tracking ophalen
            var current = _uow.Context.Set<UnitConstructionValue>()
                .AsNoTracking()
                .Where(x => x.Id == bo.Id)
                .Select(x => new { x.Id, x.ValueSold })
                .FirstOrDefault();

            if (current == null)
            {
                response.AddError("Unitconstructionvalue not found");
                return response;
            }

            // 2) Geen wijziging? -> geen UPDATE, wel succes
            var newVal = bo.ValueSold;
            var oldVal = current.ValueSold;

            // Exacte decimal-vergelijking (financieel)
            var isEqual = newVal == oldVal;
            if (isEqual)
            {
                response.InsertedId = bo.Id;
                response.AddSaveChangesResult(1, "Geen wijziging nodig", ""); // 1 = behandel als succes
                return response;
            }

            // 3) Wijziging doorvoeren via stub + IsModified
            var entity = new UnitConstructionValue { Id = bo.Id };
            _uow.Context.Attach(entity);
            entity.ValueSold = newVal;
            _uow.Context.Entry(entity).Property(e => e.ValueSold).IsModified = true;

            // 4) Saven enkel als er geen expliciete transactie is
            var hasTx = _uow.HasActiveTransaction;
            var saved = _uow.SaveIfNoActiveTransaction(); // 0 als hasTx==true, anders echte savecount
            var result = hasTx ? 1 : saved;

            response.InsertedId = bo.Id;

            if (_uow.HasActiveTransaction)
                response.AddSaveChangesResult(1, "Constructiewaarde gemarkeerd voor update", "Geen wijzigingen");
            else
                response.AddSaveChangesResult(result, "Constructiewaarde opgeslagen", "Geen wijzigingen");

            return response;
        }

        public Response UpdateLandValueSold(UnitBO bo)
        {
            var response = new Response();
            if (bo.Id == 0)
            {
                response.AddError("Er moet een unit geselecteerd zijn");
                return response;
            }

            // 1) Huidige waarde no-tracking ophalen
            var current = _uow.Context.Set<Units>()
                .AsNoTracking()
                .Where(x => x.Id == bo.Id)
                .Select(x => new { x.Id, x.LandValueSold })
                .FirstOrDefault();

            if (current == null)
            {
                response.AddError("Unit not found");
                return response;
            }

            // 2) Geen wijziging? -> geen UPDATE, wel succes
            var newVal = bo.LandValueSold;
            var oldVal = current.LandValueSold;

            var isEqual = newVal == oldVal;
            if (isEqual)
            {
                response.InsertedId = bo.Id;
                response.AddSaveChangesResult(1, "Geen wijziging nodig", ""); // 1 = behandel als succes
                return response;
            }

            // 3) Wijziging doorvoeren via stub + IsModified
            var entity = new Units { Id = bo.Id };
            _uow.Context.Attach(entity);
            entity.LandValueSold = newVal;
            _uow.Context.Entry(entity).Property(e => e.LandValueSold).IsModified = true;

            // 4) Saven enkel als er geen expliciete transactie is
            var hasTx = _uow.HasActiveTransaction;
            var saved = _uow.SaveIfNoActiveTransaction(); // 0 als hasTx==true, anders echte savecount
            var result = hasTx ? 1 : saved;

            response.InsertedId = bo.Id;

            if (_uow.HasActiveTransaction)
                response.AddSaveChangesResult(1, "Grondwaarde gemarkeerd voor update", "Geen wijzigingen");
            else
                response.AddSaveChangesResult(result, "Grondwaarde opgeslagen", "Geen wijzigingen");

            return response;
        }




        public Response DeleteConstructionValues(List<int> ids)
        {
            var response = new Response();
            foreach (var id in ids) _uow.UnitConstructionValues.DeleteObject(id);
            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Record(s) verwijderd", "Geen records verwijderd");
            return response;
        }
    }
}

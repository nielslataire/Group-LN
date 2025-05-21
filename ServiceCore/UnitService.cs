using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using FacadeCore;
using DALCore;
using DALCore.Models;
using ServiceCore.Translators;
using ServiceCore.Helpers;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
//using System.Linq.Dynamic.Core;
//using System.Linq.Dynamic;


namespace ServiceCore
{
    public class UnitService : IUnitService
    {
        public GetResponse<UnitBO> GetUnitById(int Id)
        {
            GetResponse<UnitBO> response = new GetResponse<UnitBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitsDAO();
            var _entity = dao.GetNoTracking().Where(m => m.Id == Id)
                .Include(m => m.AttachedUnit)
                .Include(m => m.LinkedUnit)
                .Include(m => m.Project)
                .Include(m => m.Type)
                .ThenInclude(m => m.Group)
                .Include(m => m.InverseLinkedUnit)
                .Include(m => m.InverseAttachedUnit)
                .Include(m => m.LevelNavigation)
                .FirstOrDefault();
            UnitBO unit = new UnitBO();

            var err = UnitTranslator.TranslateEntityToBO(_entity, unit);
            if (err == ErrorCode.Success)
                response.Value = unit;
            else
                response.AddError(err.ToString());
            return response;
        }
        public GetResponse<UnitBO> GetUnitsByProjectId(int ProjectId)
        {
            GetResponse<UnitBO> response = new GetResponse<UnitBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitsDAO();
            var entities = dao.GetNoTracking().Where(m => m.ProjectId == ProjectId && m.TypeId != 0)
                .Include(m => m.AttachedUnit)
                .Include(m => m.LinkedUnit)
                .Include(m => m.Project)
                .Include(m => m.Type)
                .ThenInclude(m => m.Group)
                .Include(m => m.InverseLinkedUnit)
                .Include(m => m.LevelNavigation)
                .OrderBy(x => x.Name);

            foreach (var _entity in entities)
            {
                UnitBO bo = new UnitBO();
                var err = UnitTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<UnitBO> GetUnitsByProjectId(int ProjectId, int UnitTypeId)
        {
            GetResponse<UnitBO> response = new GetResponse<UnitBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitsDAO();
            var entities = dao.GetNoTracking().Where(m => m.ProjectId == ProjectId && m.TypeId == UnitTypeId);
            foreach (var _entity in entities)
            {
                UnitBO bo = new UnitBO();
                var err = UnitTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<UnitWithDetailsBO> GetUnitsWithDetailsByProjectId(int ProjectId)
        {
            GetResponse<UnitWithDetailsBO> response = new GetResponse<UnitWithDetailsBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitsDAO();
            var entities = dao.GetNoTracking().Where(m => m.ProjectId == ProjectId);
            foreach (var _entity in entities)
            {
                UnitWithDetailsBO bo = new UnitWithDetailsBO();
                var err = UnitTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                {
                    foreach (var room in _entity.UnitRooms)
                    {
                        RoomBO boroom = new RoomBO();
                        err = UnitRoomTranslator.TranslateEntityToBO(room, boroom);
                        if (err == ErrorCode.Success)
                            bo.Rooms.Add(boroom);
                        
                       
                    }
                    response.AddValue(bo);
                }
                else
                    response.AddError(err.ToString());
            }
            response.Values = response.Values.OrderBy(m => m.Name, new AlphanumComparator()).ThenBy(m => m.Level).ToList();
            //response.Values = response.Values.OrderBy(m => m.Name, new AlphanumComparator()).ThenBy(m => m.Level, new AlphanumComparator()).ToList();
            return response;
        }
        public GetResponse<UnitBO> GetUnitsByAccountId(int AccountId)
        {
            GetResponse<UnitBO> response = new GetResponse<UnitBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitsDAO();
            var entities = dao.GetNoTracking().Where(m => m.ClientAccountId == AccountId);
            foreach (var _entity in entities)
            {
                UnitBO bo = new UnitBO();
                var err = UnitTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<GroupUnitsBO> GetGroupedUnitsByProjectId(int ProjectId)
        {
            GetResponse<GroupUnitsBO> response = new GetResponse<GroupUnitsBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitsDAO();
            var entities = dao.GetNoTracking().Where(m => m.ProjectId == ProjectId && m.LinkedUnit == null)
                .Include(m => m.AttachedUnit)
                .Include(m => m.LinkedUnit)
                .Include(m => m.Project)
                .Include(m => m.Type)
                .ThenInclude(m => m.Group)
                .Include(m => m.InverseLinkedUnit)
                .OrderBy(x => x.Name).GroupBy(x => x.TypeId);
;

            foreach (var _entity in entities)
            {
                GroupUnitsBO bo = new GroupUnitsBO();
                bo.Id = (int)_entity.Key;
                foreach (var item in _entity)
                {
                    UnitBO boUnit = new UnitBO();
                    var err = UnitTranslator.TranslateEntityToBO(item, boUnit);
                    if (err == ErrorCode.Success)
                        bo.Units.Add(boUnit);
                    else
                        response.AddError(err.ToString());
                }
                bo.Units = bo.Units.OrderBy(m => m.Level).ThenBy(m => m.Name, new AlphanumComparator()).ToList();
                response.AddValue(bo);
            }
            return response;
        }
        public GetResponse<GroupUnitsWithAttachedUnitsBO> GetGroupedUnitsForSaleByProjectId(int ProjectId)
        {
            GetResponse<GroupUnitsWithAttachedUnitsBO> response = new GetResponse<GroupUnitsWithAttachedUnitsBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitsDAO();
            var entities = dao.GetNoTracking().Where(m => m.ProjectId == ProjectId && m.ClientAccountId == null && m.AttachedUnit == null && m.LinkedUnit == null).OrderBy(x => x.Name).GroupBy(x => x.TypeId);

            foreach (var _entity in entities)
            {
                GroupUnitsWithAttachedUnitsBO bo = new GroupUnitsWithAttachedUnitsBO();
                bo.Id = (int)_entity.Key;
                foreach (var item in _entity)
                {
                    UnitWithAttachedUnitsBO boUnit = new UnitWithAttachedUnitsBO();
                    var err = UnitTranslator.TranslateEntityToBO(item, boUnit.Unit);
                    foreach (var attachedunit in item.InverseAttachedUnit)
                    {
                        UnitBO boattachedunit = new UnitBO();
                        err = UnitTranslator.TranslateEntityToBO(attachedunit, boattachedunit);
                        if (err == ErrorCode.Success)
                            boUnit.AttachedUnits.Add(boattachedunit);
                        else
                            response.AddError(err.ToString());
                        foreach (var attachedunit2 in attachedunit.InverseAttachedUnit)
                        {
                            UnitBO boattachedunit2 = new UnitBO();
                            err = UnitTranslator.TranslateEntityToBO(attachedunit2, boattachedunit2);
                            if (err == ErrorCode.Success)
                                boUnit.AttachedUnits.Add(boattachedunit2);
                            else
                                response.AddError(err.ToString());
                            foreach (var attachedunit3 in attachedunit2.InverseAttachedUnit)
                            {
                                UnitBO boattachedunit3 = new UnitBO();
                                err = UnitTranslator.TranslateEntityToBO(attachedunit3, boattachedunit3);
                                if (err == ErrorCode.Success)
                                    boUnit.AttachedUnits.Add(boattachedunit3);
                                else
                                    response.AddError(err.ToString());
                            }
                        }
                    }
                    if (err == ErrorCode.Success)
                        bo.Units.Add(boUnit);
                    else
                        response.AddError(err.ToString());
                }
                bo.Units = bo.Units.OrderBy(m => m.Unit.Level).ThenBy(m => m.Unit.Name, new AlphanumComparator()).ToList();
                response.AddValue(bo);
            }
            return response;
        }
        public GetResponse<GroupUnitsWithAttachedUnitsWithDetailsBO> GetGroupedUnitsForSaleWithDetailsByProjectId(int ProjectId)
        {
            GetResponse<GroupUnitsWithAttachedUnitsWithDetailsBO> response = new GetResponse<GroupUnitsWithAttachedUnitsWithDetailsBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitsDAO();
            var entities = dao.GetNoTracking().Where(m => m.ProjectId == ProjectId && m.ClientAccountId == null && m.AttachedUnit == null && m.LinkedUnit == null).OrderBy(x => x.Name).GroupBy(x => x.TypeId);

            foreach (var _entity in entities)
            {
                GroupUnitsWithAttachedUnitsWithDetailsBO bo = new GroupUnitsWithAttachedUnitsWithDetailsBO();
                bo.Id = (int)_entity.Key;
                foreach (var item in _entity)
                {
                    UnitWithAttachedUnitsWithDetailsBO boUnit = new UnitWithAttachedUnitsWithDetailsBO();
                    var err = UnitTranslator.TranslateEntityToBO(item, boUnit.Unit);
                    foreach (var room in item.UnitRooms)
                    {
                        RoomBO boroom = new RoomBO();
                        err = UnitRoomTranslator.TranslateEntityToBO(room, boroom);
                        if (err == ErrorCode.Success)
                            boUnit.Unit.Rooms.Add(boroom);
                        else
                            response.AddError(err.ToString());
                    }
                    foreach (var attachedunit in item.InverseAttachedUnit)
                    {
                        UnitWithDetailsBO boattachedunit = new UnitWithDetailsBO();
                        err = UnitTranslator.TranslateEntityToBO(attachedunit, boattachedunit);
                        if (err == ErrorCode.Success)
                            boUnit.AttachedUnits.Add(boattachedunit);
                        else
                            response.AddError(err.ToString());
                        foreach (var room in attachedunit.UnitRooms)
                        {
                            RoomBO boroom = new RoomBO();
                            err = UnitRoomTranslator.TranslateEntityToBO(room, boroom);
                            if (err == ErrorCode.Success)
                                boattachedunit.Rooms.Add(boroom);
                            else
                                response.AddError(err.ToString());
                        }
                        foreach (var attachedunit2 in attachedunit.InverseAttachedUnit)
                        {
                            UnitWithDetailsBO boattachedunit2 = new UnitWithDetailsBO();
                            err = UnitTranslator.TranslateEntityToBO(attachedunit2, boattachedunit2);
                            if (err == ErrorCode.Success)
                                boUnit.AttachedUnits.Add(boattachedunit2);
                            else
                                response.AddError(err.ToString());
                            foreach (var room in attachedunit2.UnitRooms)
                            {
                                RoomBO boroom = new RoomBO();
                                err = UnitRoomTranslator.TranslateEntityToBO(room, boroom);
                                if (err == ErrorCode.Success)
                                    boattachedunit2.Rooms.Add(boroom);
                                else
                                    response.AddError(err.ToString());
                            }
                            foreach (var attachedunit3 in attachedunit2.InverseAttachedUnit)
                            {
                                UnitWithDetailsBO boattachedunit3 = new UnitWithDetailsBO();
                                err = UnitTranslator.TranslateEntityToBO(attachedunit3, boattachedunit3);
                                if (err == ErrorCode.Success)
                                    boUnit.AttachedUnits.Add(boattachedunit3);
                                else
                                    response.AddError(err.ToString());
                                foreach (var room in attachedunit3.UnitRooms)
                                {
                                    RoomBO boroom = new RoomBO();
                                    err = UnitRoomTranslator.TranslateEntityToBO(room, boroom);
                                    if (err == ErrorCode.Success)
                                        boattachedunit3.Rooms.Add(boroom);
                                    else
                                        response.AddError(err.ToString());
                                }
                            }
                        }
                    }
                    if (err == ErrorCode.Success)
                        bo.Units.Add(boUnit);
                    else
                        response.AddError(err.ToString());
                }
                bo.Units = bo.Units.OrderBy(m => m.Unit.Level).ThenBy(m => m.Unit.Name, new AlphanumComparator()).ToList();
                response.AddValue(bo);
            }
            return response;
        }
        public GetResponse<GroupUnitsBO> GetGroupedUnitsByAccountId(int AccountId)
        {
            GetResponse<GroupUnitsBO> response = new GetResponse<GroupUnitsBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitsDAO();
            var entities = dao.GetNoTracking().Where(m => m.ClientAccountId == AccountId).OrderBy(x => x.Name).GroupBy(x => x.TypeId);
            foreach (var _entity in entities)
            {
                GroupUnitsBO bo = new GroupUnitsBO();
                bo.Id = (int)_entity.Key;
                foreach (var item in _entity)
                {
                    UnitBO boUnit = new UnitBO();
                    var err = UnitTranslator.TranslateEntityToBO(item, boUnit);
                    if (err == ErrorCode.Success)
                        bo.Units.Add(boUnit);
                    else
                        response.AddError(err.ToString());
                }
                bo.Units = bo.Units.OrderBy(m => m.Name, new AlphanumComparator()).ToList();
                response.AddValue(bo);
            }
            return response;
        }
        public GetResponse<IdNameBO> GetAvailableUnitsByProjectId(int ProjectId)
        {
            GetResponse<IdNameBO> response = new GetResponse<IdNameBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitsDAO();
            var entities = dao.GetNoTracking().Where(m => m.ProjectId == ProjectId && m.ClientAccountId == null && m.LinkedUnit == null);
            foreach (var _entity in entities)
                response.AddValue(_entity.GetIdName());
            return response;
        }
        public GetResponse<IdNameBO> GetUnitsByProjectIdForSelect(int ProjectId, bool WithLinked)
        {
            GetResponse<IdNameBO> response = new GetResponse<IdNameBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitsDAO();
            IEnumerable<Units> entities;
            if (WithLinked == true)
                entities = dao.GetNoTracking().Where(m => m.ProjectId == ProjectId)
                .Include(m => m.Type)
                .ThenInclude(m => m.Group);
            else
                entities = dao.GetNoTracking().Where(m => m.ProjectId == ProjectId && m.Type.Id != 11)
                .Include(m => m.Type)
                .ThenInclude(m => m.Group);

            foreach (var _entity in entities)
                response.AddValue(_entity.GetIdName());
            return response;
        }
        public GetResponse<IdNameBO> GetUnitsByProjectIdForSelect(int ProjectId, int UnitTypeId)
        {
            GetResponse<IdNameBO> response = new GetResponse<IdNameBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitsDAO();
           var  entities = dao.GetNoTracking().Where(m => m.ProjectId == ProjectId && m.TypeId == UnitTypeId)
                .Include(m => m.Type)
                .ThenInclude(m => m.Group);
            foreach (var _entity in entities)
                response.AddValue(_entity.GetIdName());
            return response;
        }
        public GetResponse<IdNameBO> GetUnitsByProjectIdForSelectAttachedUnit(int ProjectId)
        {
            GetResponse<IdNameBO> response = new GetResponse<IdNameBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitsDAO();
            var entities = dao.GetNoTracking().Where(m => m.ProjectId == ProjectId && m.LinkedUnit == null)
                .Include(m => m.Type)
                .ThenInclude(m => m.Group);
            foreach (var _entity in entities)
                response.AddValue(_entity.GetIdName());
            return response;
        }
        public GetResponse<IdNameBO> GetUnitsByProjectIdForSelectAttachedUnit(int ProjectId, int UnitId)
        {
            GetResponse<IdNameBO> response = new GetResponse<IdNameBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitsDAO();
            var entities = dao.GetNoTracking().Where(m => m.ProjectId == ProjectId && m.LinkedUnit == null && m.Id != UnitId)
                .Include(m => m.Type)
                .ThenInclude(m => m.Group);
            foreach (var _entity in entities)
                response.AddValue(_entity.GetIdName());
            return response;
        }

        public GetResponse<UnitTypeBO> GetUniqueUnitTypesInProjectByProjectId(int projectid)
        {
            GetResponse<UnitTypeBO> response = new GetResponse<UnitTypeBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitsDAO();
            var entities = dao.GetNoTracking().Where(m => m.ProjectId == projectid).Select(m => m.Type).Distinct();
            foreach (var _entity in entities)
            {
                UnitTypeBO bo = new UnitTypeBO();
                var err = UnitTypeTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<UnitWithStagesBO> GetClientUnitsWithStages(int ClientAcccountId)
        {
            GetResponse<UnitWithStagesBO> response = new GetResponse<UnitWithStagesBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitsDAO();
            var entities = dao.GetNoTracking().Where(m => m.ClientAccountId == ClientAcccountId);
            foreach (var _entity in entities)
            {
                UnitWithStagesBO unitwithstages = new UnitWithStagesBO();
                UnitBO bo = new UnitBO();
                var err = UnitTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                {
                    unitwithstages.Unit = bo;
                    if (_entity.PaymentGroup is not null)
                    {
                        if (_entity.PaymentGroup.InvoicingPaymentStages is not null)
                        {
                            foreach (var item in _entity.PaymentGroup.InvoicingPaymentStages)
                            {
                                ProjectPaymentStageBO stage = new ProjectPaymentStageBO();
                                var err2 = ProjectPaymentStageTranslator.TranslateEntityToBO(item, stage);
                                if (err2 == ErrorCode.Success)
                                    unitwithstages.PaymentStages.Add(stage);
                                else
                                    response.AddError(err2.ToString());
                            }
                        }
                    }
                    response.AddValue(unitwithstages);
                }
                else
                    response.AddError(err.ToString());
            }
            return response;
        }

        public Response InsertUpdateUnit(UnitBO bo)
        {
            Response response = new Response();
            if ((string.IsNullOrWhiteSpace(bo.Name)))
                response.AddError("name is mandatory");
            if ((!response.Success))
                return response;

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitsDAO();
            Units _entity = null/* TODO Change to default(_) if this is not a reference type */;

            if ((bo.Id == 0))
                _entity = dao.GetNew();
            else
                _entity = dao.GetById(bo.Id);
            if ((_entity != null))
            {
                var err = UnitTranslator.TranslateBOToEntity(_entity, bo, uow);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("unit not found");
            response.AddError(uow.SaveChanges());
            response.InsertedId = _entity.Id;
            return response;
        }
        public Response InsertUpdateUnitToClientAccount(UnitBO bo)
        {
            Response response = new Response();
            if ((bo.ClientAccountId == 0))
                response.AddError("No ClientAccount selected");
            else if ((bo.Id == 0))
                response.AddError("No Unit selected");
            if ((!response.Success))
                return response;

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitsDAO();
            Units _entity = null/* TODO Change to default(_) if this is not a reference type */;
            _entity = dao.GetById(bo.Id);

            if ((_entity != null))
            {
                _entity.ClientAccountId = bo.ClientAccountId;
                _entity.ConstructionValueSold = bo.ConstructionValueSold;
                _entity.LandValueSold = bo.LandValueSold;
            }
            else
                response.AddError("unit not found");
            response.AddError(uow.SaveChanges());
            return response;
        }

        public Response DeleteUnit(List<int> ids)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitsDAO();
            foreach (var id in ids)
            {
                var entities = dao.GetNormal().Where(m => m.AttachedUnitId == id);
                foreach (var _entity in entities)
                    _entity.AttachedUnitId = null;
                var entities1 = dao.GetNormal().Where(m => m.LinkedUnitId == id);
                foreach (var _entity in entities1)
                    _entity.LinkedUnitId = null;
                response.Messages.AddRange(uow.SaveChanges());
                dao.DeleteObject(id);
            }
            response.Messages.AddRange(uow.SaveChanges());
            return response;
        }
        public Response DeleteUnitFromClientAccountByUnitId(List<int> ids)
        {
            GetResponse<UnitBO> response = new GetResponse<UnitBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitsDAO();
            foreach (var id in ids)
            {
                var _entity = dao.GetById(id);
                _entity.ConstructionValueSold = null;
                _entity.LandValueSold = null;
                _entity.ClientAccountId = null;
                response.Messages.AddRange(uow.SaveChanges());
            }

            return response;
        }
        public Response DeleteUnitFromClientAccountByAccountId(List<int> ids)
        {
            GetResponse<UnitBO> response = new GetResponse<UnitBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitsDAO();
            foreach (var id in ids)
            {
                var entities = dao.GetNoTracking().Where(m => m.ClientAccountId == id);
                List<int> idlist = new List<int>();
                foreach (var _entity in entities)
                    idlist.Add(_entity.Id);
                response = (GetResponse<UnitBO>)DeleteUnitFromClientAccountByUnitId(idlist);
            }
            return response;
        }


        // UNIT GROUP TYPES
        public GetResponse<UnitGroupTypeBO> GetUnitGroupTypes()
        {
            GetResponse<UnitGroupTypeBO> response = new GetResponse<UnitGroupTypeBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitGroupTypesDAO();
            var entities = dao.GetNoTracking().Where(m => m.Selectable == true);
            foreach (var _entity in entities)
            {
                UnitGroupTypeBO bo = new UnitGroupTypeBO();
                var err = UnitGroupTypeTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public Response InsertUpdateUnitGroupType(UnitGroupTypeBO bo)
        {
            Response response = new Response();
            if ((string.IsNullOrWhiteSpace(bo.Name)))
                response.AddError("name is mandatory");
            if ((!response.Success))
                return response;

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitGroupTypesDAO();
            UnitGroupTypes _entity = null/* TODO Change to default(_) if this is not a reference type */;

            if ((bo.Id == 0))
                _entity = dao.GetNew();
            else
                _entity = dao.GetById(bo.Id);
            if ((_entity != null))
            {
                var err = UnitGroupTypeTranslator.TranslateBOToEntity(_entity, bo);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("unitgrouptype not found");
            response.AddError(uow.SaveChanges());
            return response;
        }
        public Response DeleteUnitGroupType(List<int> ids)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();
            foreach (var id in ids)
                uow.GetUnitGroupTypesDAO().DeleteObject(id);
            response.Messages.AddRange(uow.SaveChanges());
            return response;
        }

        // UNIT TYPES
        public GetResponse<UnitTypeBO> GetUnitTypesByGroupId(int GroupId)
        {
            GetResponse<UnitTypeBO> response = new GetResponse<UnitTypeBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitTypesDAO();
            var entities = dao.GetNoTracking().Where(m => m.GroupId == GroupId && m.Selectable == true);
            foreach (var _entity in entities)
            {
                UnitTypeBO bo = new UnitTypeBO();
                var err = UnitTypeTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public Response InsertUpdateUnitType(UnitTypeBO bo)
        {
            Response response = new Response();
            if ((string.IsNullOrWhiteSpace(bo.Name)))
                response.AddError("name is mandatory");
            if ((!response.Success))
                return response;

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitTypesDAO();
            UnitTypes _entity = null/* TODO Change to default(_) if this is not a reference type */;

            if ((bo.Id == 0))
                _entity = dao.GetNew();
            else
                _entity = dao.GetById(bo.Id);
            if ((_entity != null))
            {
                var err = UnitTypeTranslator.TranslateBOToEntity(_entity, bo);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("UnitType not found");
            response.AddError(uow.SaveChanges());
            return response;
        }
        public Response DeleteUnitType(List<int> ids)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();
            foreach (var id in ids)
                uow.GetUnitTypesDAO().DeleteObject(id);
            response.Messages.AddRange(uow.SaveChanges());
            return response;
        }

        // UNIT ROOMS
        public GetResponse<RoomBO> GetRooms(int UnitId)
        {
            GetResponse<RoomBO> response = new GetResponse<RoomBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitRoomsDAO();
            var entities = dao.GetNoTracking().Where(m => m.UnitId == UnitId);
            foreach (var _entity in entities)
            {
                RoomBO bo = new RoomBO();
                var err = UnitRoomTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<RoomType> GetUniqueRoomTypesInProjectByProjectId(int projectid)
        {
            GetResponse<RoomType> response = new GetResponse<RoomType>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitRoomsDAO();
            var entities = dao.GetNoTracking().Where(m => m.Unit.ProjectId == projectid && m.Unit.ClientAccountId == null).Where(i => i.Type == (int)RoomType.Terras | i.Type == (int)RoomType.Slaapkamer | i.Type == (int)RoomType.Dakterras | i.Type == (int)RoomType.Tuin | i.Type == (int)RoomType.Zolder).Select(m => m.Type).Distinct();
            foreach (var _entity in entities)
                response.AddValue((RoomType)_entity);
            return response;
        }
        public Response InsertUpdateRoom(RoomBO bo)
        {
            Response response = new Response();
            if ((bo.UnitId == 0))
                response.AddError("Er moet een eenheid geselecteerd zijn");
            else if ((bo.Type == 0))
                response.AddError("Er moet een kamertype geselecteerd zijn");
            else if ((bo.Number < 1))
                response.AddError("Er moet een aantal vermeld zijn");
            if ((!response.Success))
                return response;

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitRoomsDAO();
            UnitRooms _entity = null/* TODO Change to default(_) if this is not a reference type */;
            if ((bo.Id == 0))
                _entity = dao.GetNew();
            else
                _entity = dao.GetById(bo.Id);
            if ((_entity != null))
            {
                var err = UnitRoomTranslator.TranslateBOToEntity(_entity, bo);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("UnitRoom not found");
            response.AddError(uow.SaveChanges());
            return response;
        }
        public Response DeleteRooms(List<int> ids)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();
            foreach (var id in ids)
                uow.GetUnitRoomsDAO().DeleteObject(id);
            response.Messages.AddRange(uow.SaveChanges());
            return response;
        }

        // UNIT CONSTRUCTION VALUES
        public GetResponse<UnitConstructionValueBO> GetConstructionValues(int unitid)
        {
            GetResponse<UnitConstructionValueBO> response = new GetResponse<UnitConstructionValueBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitConstructionValuesDAO();
            var entities = dao.GetNoTracking().Where(m => m.UnitId == unitid)
                .Include(m => m.PaymentGroup)
                .Include(m => m.Unit);
            foreach (var _entity in entities)
            {
                UnitConstructionValueBO bo = new UnitConstructionValueBO();
                var err = UnitConstructionValueTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<UnitConstructionValueBO> GetConstructionValue(int id)
        {
            GetResponse<UnitConstructionValueBO> response = new GetResponse<UnitConstructionValueBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitConstructionValuesDAO();
            var _entity = dao.GetById(id);
            UnitConstructionValueBO unitcv = new UnitConstructionValueBO();
            var err = UnitConstructionValueTranslator.TranslateEntityToBO(_entity, unitcv);
            if (err == ErrorCode.Success)
                response.Value = unitcv;
            else
                response.AddError(err.ToString());
            return response;
        }
        public Response InsertUpdateConstructionValue(UnitConstructionValueBO bo)
        {
            Response response = new Response();
            if ((bo.UnitId == 0))
                response.AddError("Er moet een unit geselecteerd zijn");
            if ((!response.Success))
                return response;

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitConstructionValuesDAO();
            UnitConstructionValue _entity = null/* TODO Change to default(_) if this is not a reference type */;
            if ((bo.Id == 0))
                _entity = dao.GetNew();
            else
                _entity = dao.GetById(bo.Id);
            if ((_entity != null))
            {
                var err = UnitConstructionValueTranslator.TranslateBOToEntity(_entity, bo);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("Unitconstructionvalue not found");
            response.AddError(uow.SaveChanges());
            response.InsertedId = _entity.Id;
            return response;
        }
        public Response DeleteConstructionValues(List<int> ids)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();
            foreach (var id in ids)
                uow.GetUnitConstructionValuesDAO().DeleteObject(id);
            response.Messages.AddRange(uow.SaveChanges());
            return response;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;

namespace FacadeCore
{
    public interface IUnitService
    {
        // UNITS
        GetResponse<UnitBO> GetUnitById(int Id);
        GetResponse<UnitBO> GetUnitsByProjectId(int ProjectId);
        GetResponse<UnitBO> GetUnitsByProjectId(int ProjectId, int UnitTypeId);
        GetResponse<UnitWithDetailsBO> GetUnitsWithDetailsByProjectId(int ProjectId);
        GetResponse<UnitBO> GetUnitsByAccountId(int AccountId);
        GetResponse<GroupUnitsBO> GetGroupedUnitsByProjectId(int ProjectId);
        GetResponse<GroupUnitsWithAttachedUnitsBO> GetGroupedUnitsForSaleByProjectId(int ProjectId);
        GetResponse<GroupUnitsWithAttachedUnitsWithDetailsBO> GetGroupedUnitsForSaleWithDetailsByProjectId(int ProjectId);
        GetResponse<GroupUnitsBO> GetGroupedUnitsByAccountId(int AccountId);
        GetResponse<IdNameBO> GetAvailableUnitsByProjectId(int Id);
        GetResponse<IdNameBO> GetUnitsByProjectIdForSelect(int Id, bool WithLinked);
        GetResponse<IdNameBO> GetUnitsByProjectIdForSelect(int Id, int UnitTypeId);
        GetResponse<IdNameBO> GetUnitsByProjectIdForSelectAttachedUnit(int Id);
        GetResponse<IdNameBO> GetUnitsByProjectIdForSelectAttachedUnit(int Id, int UnitId);

        GetResponse<UnitTypeBO> GetUniqueUnitTypesInProjectByProjectId(int id);
        GetResponse<UnitWithStagesBO> GetClientUnitsWithStages(int ClientAcccountId);
        Response InsertUpdateUnit(UnitBO bo);
        Response InsertUpdateUnitToClientAccount(UnitBO bo);
        Response DeleteUnit(List<int> ids);
        Response DeleteUnitFromClientAccountByUnitId(List<int> ids);
        Response DeleteUnitFromClientAccountByAccountId(List<int> ids);



        // UNIT GROUP TYPES
        GetResponse<UnitGroupTypeBO> GetUnitGroupTypes();
        Response InsertUpdateUnitGroupType(UnitGroupTypeBO bo);
        Response DeleteUnitGroupType(List<int> ids);

        // UNIT TYPES
        GetResponse<UnitTypeBO> GetUnitTypesByGroupId(int GroupId);
        Response InsertUpdateUnitType(UnitTypeBO bo);
        Response DeleteUnitType(List<int> ids);

        // UNIT ROOMS
        GetResponse<RoomBO> GetRooms(int UnitId);
        GetResponse<RoomType> GetUniqueRoomTypesInProjectByProjectId(int projectid);
        Response InsertUpdateRoom(RoomBO bo);
        Response DeleteRooms(List<int> ids);

        // UNIT CONSTRUCTION VALUE
        GetResponse<UnitConstructionValueBO> GetConstructionValues(int UnitId);
        GetResponse<UnitConstructionValueBO> GetConstructionValue(int id);
        Response InsertUpdateConstructionValue(UnitConstructionValueBO bo);
        Response DeleteConstructionValues(List<int> ids);
    }
}

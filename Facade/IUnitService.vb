Imports BO
Public Interface IUnitService
    'UNITS
    Function GetUnitById(Id As Integer) As GetResponse(Of UnitBO)
    Function GetUnitsByProjectId(ProjectId As Integer) As GetResponse(Of UnitBO)
    Function GetUnitsByProjectId(ProjectId As Integer, UnitTypeId As Integer) As GetResponse(Of UnitBO)
    Function GetUnitsWithDetailsByProjectId(ProjectId As Integer) As GetResponse(Of UnitWithDetailsBO)
    Function GetUnitsByAccountId(AccountId As Integer) As GetResponse(Of UnitBO)
    Function GetGroupedUnitsByProjectId(ProjectId As Integer) As GetResponse(Of GroupUnitsBO)
    Function GetGroupedUnitsForSaleByProjectId(ProjectId As Integer) As GetResponse(Of GroupUnitsWithAttachedUnitsBO)
    Function GetGroupedUnitsForSaleWithDetailsByProjectId(ProjectId As Integer) As GetResponse(Of GroupUnitsWithAttachedUnitsWithDetailsBO)
    Function GetGroupedUnitsByAccountId(AccountId As Integer) As GetResponse(Of GroupUnitsBO)
    Function GetAvailableUnitsByProjectId(Id As Integer) As GetResponse(Of IdNameBO)
    Function GetUnitsByProjectIdForSelect(Id As Integer, WithLinked As Boolean) As GetResponse(Of IdNameBO)
    Function GetUnitsByProjectIdForSelect(Id As Integer, UnitTypeId As Integer) As GetResponse(Of IdNameBO)
    Function GetUnitsByProjectIdForSelectAttachedUnit(Id As Integer) As GetResponse(Of IdNameBO)
    Function GetUnitsByProjectIdForSelectAttachedUnit(Id As Integer, UnitId As Integer) As GetResponse(Of IdNameBO)

    Function GetUniqueUnitTypesInProjectByProjectId(id As Integer) As GetResponse(Of UnitTypeBO)
    Function GetClientUnitsWithStages(ClientAcccountId As Integer) As GetResponse(Of UnitWithStagesBO)
    Function InsertUpdateUnit(bo As UnitBO) As Response
    Function InsertUpdateUnitToClientAccount(bo As UnitBO) As Response
    Function DeleteUnit(ids As List(Of Integer)) As Response
    Function DeleteUnitFromClientAccountByUnitId(ids As List(Of Integer)) As Response
    Function DeleteUnitFromClientAccountByAccountId(ids As List(Of Integer)) As Response



    'UNIT GROUP TYPES
    Function GetUnitGroupTypes() As GetResponse(Of UnitGroupTypeBO)
    Function InsertUpdateUnitGroupType(bo As UnitGroupTypeBO) As Response
    Function DeleteUnitGroupType(ids As List(Of Integer)) As Response

    'UNIT TYPES
    Function GetUnitTypesByGroupId(GroupId As Integer) As GetResponse(Of UnitTypeBO)
    Function InsertUpdateUnitType(bo As UnitTypeBO) As Response
    Function DeleteUnitType(ids As List(Of Integer)) As Response

    'UNIT ROOMS
    Function GetRooms(UnitId As Integer) As GetResponse(Of RoomBO)
    Function GetUniqueRoomTypesInProjectByProjectId(projectid As Integer) As GetResponse(Of RoomType)
    Function InsertUpdateRoom(bo As RoomBO) As Response
    Function DeleteRooms(ids As List(Of Integer)) As Response

    'UNIT CONSTRUCTION VALUE
    Function GetConstructionValues(UnitId As Integer) As GetResponse(Of UnitConstructionValueBO)
    Function GetConstructionValue(id As Integer) As GetResponse(Of UnitConstructionValueBO)
    Function InsertUpdateConstructionValue(bo As UnitConstructionValueBO) As Response
    Function DeleteConstructionValues(ids As List(Of Integer)) As Response



End Interface

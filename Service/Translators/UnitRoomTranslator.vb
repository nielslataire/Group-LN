Imports BO
Imports DAL

Public Class UnitRoomTranslator
    Public Shared Function TranslateEntityToBO(_entity As UnitRooms, bo As RoomBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id = _entity.RoomId
        bo.UnitId = _entity.UnitId
        bo.Number = _entity.Number
        bo.Surface = _entity.Surface
        bo.Type = _entity.Type
        Return ErrorCode.Success
    End Function

    Friend Shared Function TranslateBOToEntity(_entity As UnitRooms, bo As RoomBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.UnitId = bo.UnitId
        _entity.Number = bo.Number
        _entity.Surface = bo.Surface
        _entity.Type = bo.Type
        Return ErrorCode.Success
    End Function

End Class

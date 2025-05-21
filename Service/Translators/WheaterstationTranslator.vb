Imports BO
Imports DAL

Public Class WheaterstationTranslator
    Public Shared Function TranslateEntityToBO(_entity As WheaterStations, bo As WheaterStationBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id = _entity.ID
        bo.Name = _entity.Name
        bo.Visible = _entity.Visible
        Return ErrorCode.Success
    End Function

    Friend Shared Function TranslateBOToEntity(_entity As WheaterStations, bo As WheaterStationBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.Name = bo.Name
        _entity.Visible = bo.Visible
        Return ErrorCode.Success
    End Function
End Class

Imports BO
Imports DAL

Public Class VacationDayTranslator
    Public Shared Function TranslateEntityToBO(_entity As VacationDays, bo As VacationDayBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id = _entity.Id
        bo.VacationDay = _entity.VacationDay
        If Not _entity.ProjectId Is Nothing Then bo.ProjectId = _entity.ProjectId
        Return ErrorCode.Success
    End Function

    Friend Shared Function TranslateBOToEntity(_entity As VacationDays, bo As VacationDayBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.VacationDay = bo.VacationDay
        If Not bo.ProjectId = 0 Then _entity.ProjectId = bo.ProjectId
        Return ErrorCode.Success
    End Function
End Class

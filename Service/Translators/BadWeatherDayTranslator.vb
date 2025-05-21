Imports BO
Imports DAL

Public Class BadWeatherDayTranslator
    Public Shared Function TranslateEntityToBO(_entity As BadWeatherDays, bo As BadWeatherDayBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id = _entity.Id
        bo.WeatherStationId = _entity.WeatherstationId
        bo.Type = _entity.Type
        bo.BWDate = _entity.Date
        Return ErrorCode.Success
    End Function

    Friend Shared Function TranslateBOToEntity(_entity As BadWeatherDays, bo As BadWeatherDayBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.WeatherstationId = bo.WeatherStationId
        _entity.Type = bo.Type
        _entity.Date = bo.BWDate

        Return ErrorCode.Success
    End Function
End Class

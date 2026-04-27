Imports BO
Imports DAL

Public Class UnitFinishingOptionTranslator
    Public Shared Function TranslateEntityToBO(_entity As UnitFinishingOption, bo As UnitFinishingOptionBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id = _entity.Id
        bo.UnitId = _entity.UnitId
        bo.Name = _entity.Name
        bo.SortOrder = _entity.SortOrder
        bo.IsDefault = _entity.IsDefault
        For Each cv In _entity.UnitConstructionValues
            Dim bocv As New UnitConstructionValueBO
            Dim err = UnitConstructionValueTranslator.TranslateEntityToBO(cv, bocv)
            If err = ErrorCode.Success Then
                bo.ConstructionValues.Add(bocv)
            End If
        Next
        Return ErrorCode.Success
    End Function
End Class

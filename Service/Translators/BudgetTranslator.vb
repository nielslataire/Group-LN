Imports BO
Imports DAL
Imports System.Text.RegularExpressions
Public Class BudgetTranslator
    Public Shared Function TranslateEntityToBO(_entity As ProjectBudget, bo As BudgetActivityBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id = _entity.ID
        bo.ProjectId = _entity.ProjectID
        bo.Price = _entity.Price
        Dim act As New ActivityBO
        bo.Activity = act
        Dim err = ActivityTranslator.TranslateEntityToBO(_entity.Activity, bo.Activity)
        If (err <> ErrorCode.Success) Then Return err
        Return ErrorCode.Success
    End Function

    Friend Shared Function TranslateBOToEntity(_entity As ProjectBudget, bo As BudgetActivityBO, uow As UnitOfWork) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.ProjectID = bo.ProjectId
        _entity.ActivityID = bo.Activity.ID
        _entity.Price = bo.Price
        Return ErrorCode.Success
    End Function

End Class

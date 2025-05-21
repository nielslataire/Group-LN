Imports BO
Imports DAL

Public Class UtilityPercentageTranslator
    Public Shared Function TranslateEntityToBO(_entity As UtilityPercentage, bo As UtilityPercentageBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.ContractId = _entity.ContractId
        bo.InvoiceDetailId = _entity.IncommingInvoiceDetailId
        bo.Percentage = _entity.Percentage
        bo.ClientAccountId = _entity.ClientAccountId
        Return ErrorCode.Success
    End Function

    Friend Shared Function TranslateBOToEntity(_entity As UtilityPercentage, bo As UtilityPercentageBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.ClientAccountId = bo.ClientAccountId
        _entity.ContractId = bo.ContractId
        _entity.IncommingInvoiceDetailId = bo.InvoiceDetailId
        _entity.Percentage = bo.Percentage
        Return ErrorCode.Success
    End Function
End Class

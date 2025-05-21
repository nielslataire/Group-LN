Imports BO
Imports DAL

Public Class ProjectPaymentGroupTranslator
    Public Shared Function TranslateEntityToBO(_entity As InvoicingPaymentGroup, bo As ProjectPaymentGroupBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id = _entity.Id
        bo.Name = _entity.Name
        bo.ProjectId = _entity.ProjectId
        bo.VatPercentage = _entity.VatPercentage
        For Each item In _entity.InvoicingPaymentStages
            Dim stage As New ProjectPaymentStageBO
            Dim err = ProjectPaymentStageTranslator.TranslateEntityToBO(item, stage)
            If err <> ErrorCode.Success Then Return err
            bo.PaymentStages.Add(stage)
        Next
        Return ErrorCode.Success
    End Function

    Friend Shared Function TranslateBOToEntity(_entity As InvoicingPaymentGroup, bo As ProjectPaymentGroupBO, uow As UnitOfWork) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.Name = bo.Name
        _entity.ProjectId = bo.ProjectId
        _entity.VatPercentage = bo.VatPercentage
        Dim err = HandleStages(_entity, bo.PaymentStages, uow)
        If (err <> ErrorCode.Success) Then Return err
        Return ErrorCode.Success
    End Function

    Private Shared Function HandleStages(_entity As InvoicingPaymentGroup, stages As List(Of ProjectPaymentStageBO), uow As UnitOfWork) As ErrorCode
        If (stages Is Nothing) Then Return ErrorCode.Success
        If (stages.Count = 0) Then Return ErrorCode.Success
        For Each x In stages
            If (x.Id = 0) Then
                'insert
                Dim stage As New InvoicingPaymentStages
                Dim err = ProjectPaymentStageTranslator.TranslateBOToEntity(stage, x, uow)
                If err <> ErrorCode.Success Then Return err
                _entity.InvoicingPaymentStages.Add(stage)
            Else
                'update
                Dim stage = _entity.InvoicingPaymentStages.FirstOrDefault(Function(f) f.Id = x.Id)
                If (stage IsNot Nothing) Then
                    Dim err = ProjectPaymentStageTranslator.TranslateBOToEntity(stage, x, uow)
                    If err <> ErrorCode.Success Then Return err
                End If
            End If
        Next
        'delete
        Dim delList As New List(Of InvoicingPaymentStages)
        For Each x In _entity.InvoicingPaymentStages
            If (Not stages.Any(Function(f) f.Id = x.Id)) Then
                delList.Add(x)
            End If
        Next
        For Each x In delList
            uow.GetProjectPaymentStagesDAO.DeleteObject(x.Id)
            _entity.InvoicingPaymentStages.Remove(x)
        Next
        Return ErrorCode.Success
    End Function
End Class

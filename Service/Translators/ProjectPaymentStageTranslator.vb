Imports BO
Imports DAL

Public Class ProjectPaymentStageTranslator
    Public Shared Function TranslateEntityToBO(_entity As InvoicingPaymentStages, bo As ProjectPaymentStageBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id = _entity.Id
        bo.Name = _entity.Name
        bo.GroupId = _entity.GroupId
        bo.Invoicable = _entity.Invoicable
        bo.Percentage = _entity.Percentage
        bo.VatPercentage = _entity.InvoicingPaymentGroup.VatPercentage
        bo.InvoiceCount = _entity.InvoicesDetails.Count
        bo.GroupName = _entity.InvoicingPaymentGroup.Name
        If Not _entity.ProjectDocs Is Nothing Then
            Dim doc As New ProjectDocBO
            bo.Doc = doc
            ProjectDocsTranslator.TranslateEntityToBO(_entity.ProjectDocs, bo.Doc)
        End If
        Return ErrorCode.Success
    End Function

    Friend Shared Function TranslateBOToEntity(_entity As InvoicingPaymentStages, bo As ProjectPaymentStageBO, uow As UnitOfWork) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.Name = bo.Name
        _entity.GroupId = bo.GroupId
        _entity.Invoicable = bo.Invoicable
        _entity.Percentage = bo.Percentage
        If Not bo.Doc Is Nothing Then
            If bo.Doc.Docid = 0 Then
                Dim doc As New ProjectDocs
                ProjectDocsTranslator.TranslateBOToEntity(doc, bo.Doc, uow)
                _entity.ProjectDocs = doc
            Else
                _entity.DocId = bo.Doc.Docid
            End If
        Else
            _entity.DocId = Nothing
        End If
        Return ErrorCode.Success
    End Function

End Class

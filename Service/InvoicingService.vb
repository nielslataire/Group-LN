Imports BO
Imports DAL
Imports Facade
Imports System.Linq.Expressions

Public Class InvoicingService
    Implements IInvoicingService
    Public Function GetInvoices() As GetResponse(Of InvoiceBO) Implements IInvoicingService.GetInvoices
        Dim response As New GetResponse(Of InvoiceBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetInvoicesDAO()

        Dim entities = dao.GetNoTracking
        For Each _entity In entities
            Dim invoice As New InvoiceBO

            Dim err = InvoiceTranslator.TranslateEntityToBO(_entity, invoice)
            If err = ErrorCode.Success Then
                response.AddValue(invoice)
            Else
                response.AddError(err.ToString())
            End If

        Next
        Return response
    End Function
    Public Function GetClientInvoices(id As Integer, Optional itype As Integer = 1) As GetResponse(Of InvoiceBO) Implements IInvoicingService.GetClientInvoices
        Dim response As New GetResponse(Of InvoiceBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetInvoicesDAO()

        Dim entities = dao.GetNoTracking.Where(Function(m) m.ClientId = id AndAlso m.ClientType = itype)
        For Each _entity In entities
            Dim invoice As New InvoiceBO

            Dim err = InvoiceTranslator.TranslateEntityToBO(_entity, invoice)
            If err = ErrorCode.Success Then
                response.AddValue(invoice)
            Else
                response.AddError(err.ToString())
            End If

        Next
        Return response
    End Function
    Public Function GetInvoiceById(id As Integer) As GetResponse(Of InvoiceBO) Implements IInvoicingService.GetInvoiceByID
        Dim response As New GetResponse(Of InvoiceBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetInvoicesDAO()

        Dim _entity = dao.GetById(id)
        Dim invoice As New InvoiceBO

        Dim err = InvoiceTranslator.TranslateEntityToBO(_entity, invoice)
        If err = ErrorCode.Success Then
            response.Value = invoice
        Else
            response.AddError(err.ToString())
        End If
        Return response
    End Function
    Public Function GetInvoiceFileByFilename(name As String) As GetResponse(Of InvoiceFileBO) Implements IInvoicingService.GetInvoiceFileByFilename
        Dim response As New GetResponse(Of InvoiceFileBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetInvoicesDAO()

        Dim _entity = dao.GetNoTracking.Where(Function(m) m.Filename = name).FirstOrDefault
        Dim invoice As New InvoiceFileBO
        If Not _entity Is Nothing Then
            invoice.Filename = _entity.Filename
            invoice.DbId = _entity.Id
            invoice.ClientId = _entity.ClientId
            invoice.InvoiceDate = _entity.Date
            response.Value = invoice
        Else
            response.AddError("no invoice found")
        End If

        'Dim err = InvoiceTranslator.TranslateEntityToBO(_entity, invoice)
        'If err = ErrorCode.Success Then
        '    response.Value = invoice
        'Else
        '    response.AddError(err.ToString())
        'End If
        Return response
    End Function

End Class

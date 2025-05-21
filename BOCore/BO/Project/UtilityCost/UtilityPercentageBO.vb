Imports System.ComponentModel.DataAnnotations

Public Class UtilityPercentageBO
    Public Sub New()

    End Sub
    Private _clientaccountid As Integer
    Public Property ClientAccountId() As Integer
        Get
            Return _clientaccountid
        End Get
        Set(ByVal value As Integer)
            _clientaccountid = value
        End Set
    End Property
    Private _invoicedetailId As Integer
    Public Property InvoiceDetailId() As Integer
        Get
            Return _invoicedetailId
        End Get
        Set(ByVal value As Integer)
            _invoicedetailId = value
        End Set
    End Property
    Private _contractId As Integer
    Public Property ContractId() As Integer
        Get
            Return _contractId
        End Get
        Set(ByVal value As Integer)
            _contractId = value
        End Set
    End Property
    Private _percentage As Decimal
    <Display(Name:="Percentage")>
    <UIHint("Percentage")>
    Public Property Percentage() As Decimal
        Get
            Return _percentage
        End Get
        Set(ByVal value As Decimal)
            _percentage = value
        End Set
    End Property

End Class

Imports System.ComponentModel.DataAnnotations

Public Class UtilityCostBO
    Public Sub New()

    End Sub
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property
    Private _description As String
    Public Property Description() As String
        Get
            Return _description
        End Get
        Set(ByVal value As String)
            _description = value
        End Set
    End Property
    Private _price As Decimal
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:C}")>
    <Display(Name:="Prijs")>
    <UIHint("Currency")>
    Public Property Price() As Decimal
        Get
            Return _price
        End Get
        Set(ByVal value As Decimal)
            _price = value
        End Set
    End Property
    Private _companyname As String
    Public Property CompanyName() As String
        Get
            Return _companyname
        End Get
        Set(ByVal value As String)
            _companyname = value
        End Set
    End Property
    Private _invoicedetailid As Integer
    Public Property InvoiceDetailId() As Integer
        Get
            Return _invoicedetailid
        End Get
        Set(ByVal value As Integer)
            _invoicedetailid = value
        End Set
    End Property
    Private _contractid As Integer
    Public Property ContractId() As Integer
        Get
            Return _contractid
        End Get
        Set(ByVal value As Integer)
            _contractid = value
        End Set
    End Property
    Private _percentage As Decimal
    Public Property Percentage() As Decimal
        Get
            Return _percentage
        End Get
        Set(ByVal value As Decimal)
            _percentage = value
        End Set
    End Property

End Class

Imports System.ComponentModel.DataAnnotations
Imports System.Web.Mvc
Public Class ContractActivityBO
    Public Sub New()
        ' _insurancedata = New InsuranceBO
        _additionalorders = New List(Of ContractAdditionalOrderBO)
    End Sub
    Private _id As Integer
    Public Property ContractActivityId() As Integer
        Get
            Return _id
        End Get
        Set(ByVal value As Integer)
            _id = value
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
    Private _activity As ActivityBO
    Public Property Activity() As ActivityBO
        Get
            Return _activity
        End Get
        Set(ByVal value As ActivityBO)
            _activity = value
        End Set
    End Property

    Private _price As Decimal
    '<DisplayFormat(DataFormatString:="{0:0,00}")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:C}")>
    <UIHint("Currency")>
    <Display(Name:="Prijs")>
    Public Property Price() As Decimal
        Get
            Return _price
        End Get
        Set(ByVal value As Decimal)
            _price = value
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
    Private _insurancedata As InsuranceBO
    Public Property InsuranceData() As InsuranceBO
        Get
            Return _insurancedata
        End Get
        Set(ByVal value As InsuranceBO)
            _insurancedata = value
        End Set
    End Property
    Private _additionalorders As List(Of ContractAdditionalOrderBO)
    Public Property AdditionalOrders() As List(Of ContractAdditionalOrderBO)
        Get
            Return _additionalorders
        End Get
        Set(ByVal value As List(Of ContractAdditionalOrderBO))
            _additionalorders = value
        End Set
    End Property
End Class

Imports System.ComponentModel.DataAnnotations
Imports System.Web.Mvc
Public Class InInvoiceDetailBO
    Public Sub New()

    End Sub
    Private _id As Integer
    Public Property Id() As Integer
        Get
            Return _id
        End Get
        Set(ByVal value As Integer)
            _id = value
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
    Private _contractID As Integer
    Public Property ContractID() As Integer
        Get
            Return _contractID
        End Get
        Set(ByVal value As Integer)
            _contractID = value
        End Set
    End Property
    Private _type As String
    Public Property Type() As String
        Get
            Return _type
        End Get
        Set(ByVal value As String)
            _type = value
        End Set
    End Property
    Private _clientaccountid As Integer
    Public Property ClientAccountID() As Integer
        Get
            Return _clientaccountid
        End Get
        Set(ByVal value As Integer)
            _clientaccountid = value
        End Set
    End Property

End Class

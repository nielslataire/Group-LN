Imports System.ComponentModel.DataAnnotations
Imports System.Web.Mvc
Public Class IncommingInvoiceDetailBO
    Public Sub New()
        _changeorders = New List(Of IdNameBO)
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
    Private _incomminginvoiceid As Integer
    Public Property IncommingInvoiceID() As Integer
        Get
            Return _incomminginvoiceid
        End Get
        Set(ByVal value As Integer)
            _incomminginvoiceid = value
        End Set
    End Property
    Private _description As String
    <Required(ErrorMessage:="Gelieve een detailomschrijving in te geven")>
    <Display(Name:="Omschrijving")>
    Public Property Description() As String
        Get
            Return _description
        End Get
        Set(ByVal value As String)
            _description = value
        End Set
    End Property
    Private _contractactivityid As Integer
    Public Property ContractActivityID() As Integer
        Get
            Return _contractactivityid
        End Get
        Set(ByVal value As Integer)
            _contractactivityid = value
        End Set
    End Property
    Private _contractactivitytext As String
    Public Property ContractActivityText() As String
        Get
            Return _contractactivitytext
        End Get
        Set(ByVal value As String)
            _contractactivitytext = value
        End Set
    End Property
    Private _price As Decimal
    <Required(ErrorMessage:="Gelieve een prijs in te geven")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:C}")>
    <Display(Name:="Eenheidsprijs")>
    <UIHint("Currency")>
    Public Property Price() As Decimal
        Get
            Return _price
        End Get
        Set(ByVal value As Decimal)
            _price = value
        End Set
    End Property
    Private _incomminginvoicetype As IncommingInvoiceType

    <Display(Name:="Eenheid")>
    Public Property IncommingInvoiceType() As IncommingInvoiceType
        Get
            Return _incomminginvoicetype
        End Get
        Set(ByVal value As IncommingInvoiceType)
            _incomminginvoicetype = value
        End Set
    End Property
    Private _changeorderid As Integer
    Public Property ChangeOrderId() As Integer
        Get
            Return _changeorderid
        End Get
        Set(ByVal value As Integer)
            _changeorderid = value
        End Set
    End Property
    Private _changeorders As List(Of IdNameBO)
    Public Property ChangeOrders() As List(Of IdNameBO)
        Get
            Return _changeorders
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _changeorders = value
        End Set
    End Property
    Private _activityId As Integer
    Public Property ActivityID() As String
        Get
            Return _activityId
        End Get
        Set(ByVal value As String)
            _activityId = value
        End Set
    End Property


End Class

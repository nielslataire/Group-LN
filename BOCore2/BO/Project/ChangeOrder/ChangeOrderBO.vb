Imports System.ComponentModel.DataAnnotations
Imports System.Web.Mvc
Public Class ChangeOrderBO
    Public Sub New()
        _details = New List(Of ChangeOrderDetailBO)
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
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property
    Private _clientAccountId As Integer
    <Required(ErrorMessage:="Gelieve een klant te selecteren")>
    Public Property ClientAccountID() As Integer
        Get
            Return _clientAccountId
        End Get
        Set(ByVal value As Integer)
            _clientAccountId = value
        End Set
    End Property
    Private _clientName As String
    <Display(Name:="Klantnaam")>
    Public Property ClientName() As String
        Get
            Return _clientName
        End Get
        Set(ByVal value As String)
            _clientName = value
        End Set
    End Property

    Private _description As String
    <Required(ErrorMessage:="Gelieve een omschrijving in te geven")>
    <Display(Name:="Omschrijving")>
    Public Property Description() As String
        Get
            Return _description
        End Get
        Set(ByVal value As String)
            _description = value
        End Set
    End Property

    Private _changeorderdate As Date
    <Required(ErrorMessage:="Gelieve een datum in te geven")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:dd/MM/yyyy}")>
    <DataType(DataType.Date)>
    <Display(Name:="Datum")>
    <UIHint("Date")>
    Public Property ChangeOrderDate() As Date
        Get
            Return _changeorderdate
        End Get
        Set(ByVal value As Date)
            _changeorderdate = value
        End Set
    End Property

    Private _expirationdate As Date
    <Required(ErrorMessage:="Gelieve een vervaldatum in te geven")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:dd/MM/yyyy}")>
    <DataType(DataType.Date)>
    <Display(Name:="Vervaldatum")>
    <UIHint("Date")>
    Public Property ExpirationDate() As Date
        Get
            Return _expirationdate
        End Get
        Set(ByVal value As Date)
            _expirationdate = value
        End Set
    End Property
    Private _comment As String
    <Display(Name:="Extra Info")>
    <DisplayFormat(HtmlEncode:=True)>
    Public Property Comment() As String
        Get
            Return _comment
        End Get
        Set(ByVal value As String)
            _comment = value
        End Set
    End Property
    Private _datesendtoclient As Date?
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:dd/MM/yyyy}")>
    <DataType(DataType.Date)>
    <Display(Name:="Datum verzonden")>
    <UIHint("Date")>
    Public Property DateSendToClient() As Date?
        Get
            Return _datesendtoclient
        End Get
        Set(ByVal value As Date?)
            _datesendtoclient = value
        End Set
    End Property

    Private _dateagreement As Date?
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:dd/MM/yyyy}")>
    <DataType(DataType.Date)>
    <Display(Name:="Datum akkoord")>
    <UIHint("Date")>
    Public Property DateAgreement() As Date?
        Get
            Return _dateagreement
        End Get
        Set(ByVal value As Date?)
            _dateagreement = value
        End Set
    End Property
    Private _invoicable As Boolean
    <UIHint("Boolean")>
    <Display(Name:="Te Factureren door bouwheer")>
    Public Property Invoiceable() As Boolean
        Get
            Return _invoicable
        End Get
        Set(ByVal value As Boolean)
            _invoicable = value
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
    Private _details As List(Of ChangeOrderDetailBO)
    Public Property Details() As List(Of ChangeOrderDetailBO)
        Get
            Return _details
        End Get
        Set(ByVal value As List(Of ChangeOrderDetailBO))
            _details = value
        End Set
    End Property
    Private _changeorderconditions As String
    <Display(Name:="Voorwaarden")>
    Public Property ChangeOrderConditions() As String
        Get
            Return _changeorderconditions
        End Get
        Set(ByVal value As String)
            _changeorderconditions = value
        End Set
    End Property
    'HELPER
    Public ReadOnly Property Totaal() As Decimal
        Get
            Dim itotaal As New Decimal
            For Each detail In Details
                itotaal = itotaal + detail.Totaal
            Next
            Return itotaal
        End Get
    End Property
End Class

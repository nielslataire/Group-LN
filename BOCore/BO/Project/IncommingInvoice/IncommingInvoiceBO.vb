Imports System.ComponentModel.DataAnnotations

Public Class IncommingInvoiceBO
    Public Sub New()

        _details = New List(Of IncommingInvoiceDetailBO)
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
    Private _incomminginvoicedate As DateOnly
    <Required(ErrorMessage:="Gelieve een datum in te geven")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:dd/MM/yyyy}")>
    <DataType(DataType.Date)>
    <Display(Name:="Datum")>
    <UIHint("Date")>
    Public Property IncommingInvoiceDate() As DateOnly
        Get
            Return _incomminginvoicedate
        End Get
        Set(ByVal value As DateOnly)
            _incomminginvoicedate = value
        End Set
    End Property
    Private _contractid As Integer?
    Public Property ContractID() As Integer?
        Get
            Return _contractid
        End Get
        Set(ByVal value As Integer?)
            _contractid = value
        End Set
    End Property
    Private _companyId As Integer?
    Public Property CompanyId() As Integer?
        Get
            Return _companyId
        End Get
        Set(ByVal value As Integer?)
            _companyId = value
        End Set
    End Property
    Private _invoiceExternalId As String
    <Display(Name:="Referentie")>
    Public Property InvoiceExternalId() As String
        Get
            Return _invoiceExternalId
        End Get
        Set(ByVal value As String)
            _invoiceExternalId = value
        End Set
    End Property
    Private _invoiceprice As Decimal
    <Required(ErrorMessage:="Gelieve een prijs in te geven")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:C}")>
    <Display(Name:="Factuurbedrag")>
    <UIHint("Currency")>
    Public Property InvoicePrice() As Decimal
        Get
            Return _invoiceprice
        End Get
        Set(ByVal value As Decimal)
            _invoiceprice = value
        End Set
    End Property
    Private _details As List(Of IncommingInvoiceDetailBO)
    <Display(Name:="Contract")>
    Public Property Details() As List(Of IncommingInvoiceDetailBO)
        Get
            Return _details
        End Get
        Set(ByVal value As List(Of IncommingInvoiceDetailBO))
            _details = value
        End Set
    End Property

    ' Kolommen bestonden al in de DB (DALCore.Models.IncommingInvoices, verrijkings-/Octopus-pijplijn)
    ' maar werden door de translator niet doorgegeven ("dode infrastructuur", zie IncommingInvoiceDetailV2.cshtml) —
    ' hier enkel doorgegeven, geen nieuwe kolom. Legacy Add/EditIncommingInvoice zetten deze nooit, dus blijven
    ' gewoon Nothing/0 voor elke factuur die via dat scherm aangemaakt is — geen gedragsverandering daar.
    Private _duedate As DateOnly?
    <Display(Name:="Vervaldatum")>
    Public Property DueDate() As DateOnly?
        Get
            Return _duedate
        End Get
        Set(ByVal value As DateOnly?)
            _duedate = value
        End Set
    End Property

    Private _statusid As Byte
    <Display(Name:="Status")>
    Public Property StatusId() As Byte
        Get
            Return _statusid
        End Get
        Set(ByVal value As Byte)
            _statusid = value
        End Set
    End Property

    ' Nothing = "niet opgegeven" (legacy Add/Edit-formulier kent dit veld niet) — de translator schrijft
    ' de kolom enkel weg als dit een waarde heeft, anders blijft wat al in de DB stond ongewijzigd. Zie
    ' ServiceCore/Translators/IncommingInvoiceTranslator.cs voor de volledige toelichting.
    Private _kind As IncommingInvoiceKind?
    <Display(Name:="Soort")>
    Public Property Kind() As IncommingInvoiceKind?
        Get
            Return _kind
        End Get
        Set(ByVal value As IncommingInvoiceKind?)
            _kind = value
        End Set
    End Property

End Class

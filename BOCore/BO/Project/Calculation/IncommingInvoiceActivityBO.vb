Imports System.ComponentModel.DataAnnotations

Public Class IncommingInvoiceActivityBO
    Public Sub New()

    End Sub
    Private _invoiceid As Integer
    Public Property InvoiceId() As Integer
        Get
            Return _invoiceid
        End Get
        Set(ByVal value As Integer)
            _invoiceid = value
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
    Private _invoicedetailid As Integer
    Public Property InvoiceDetailId() As Integer
        Get
            Return _invoicedetailid
        End Get
        Set(ByVal value As Integer)
            _invoicedetailid = value
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
    Private _company As IdNameBO
    Public Property Company() As IdNameBO
        Get
            Return _company
        End Get
        Set(ByVal value As IdNameBO)
            _company = value
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
    Private _description As String
    <Display(Name:="Omschrijving")>
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
    Private _invoicedate As DateOnly
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:dd/MM/yyyy}")>
    <DataType(DataType.Date)>
    <Display(Name:="Factuurdatum")>
    <UIHint("Date")>
    Public Property Invoicedate() As DateOnly
        Get
            Return _invoicedate
        End Get
        Set(ByVal value As DateOnly)
            _invoicedate = value
        End Set
    End Property
    <Display(Name:="Factuur nummer")>
    Private _externalinvoiceid As String
    Public Property ExternalInvoiceId() As String
        Get
            Return _externalinvoiceid
        End Get
        Set(ByVal value As String)
            _externalinvoiceid = value
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

    ' Van de factuurkop (IncommingInvoices), niet van deze regel — alleen-lezen, voor de Nacalculatie-
    ' betaalstatus/vervaldatum-weergave (design-handoff 42b/42f). Zie IncommingInvoiceBO.vb voor dezelfde
    ' velden op de factuurkop zelf.
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

    ' Van de factuurkop, alleen-lezen (zie IncommingInvoiceBO.Kind voor de volledige toelichting).
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

    ''' <summary>Factuurbedrag met het juiste teken voor Nacalculatie-sommen: een Creditnota telt negatief
    ''' (design-handoff 42e punt 07), ongeacht met welk teken <see cref="Price"/> zelf werd ingevoerd.</summary>
    Public ReadOnly Property SignedPrice() As Decimal
        Get
            Return If(_kind = IncommingInvoiceKind.Creditnota, -Math.Abs(_price), _price)
        End Get
    End Property
End Class

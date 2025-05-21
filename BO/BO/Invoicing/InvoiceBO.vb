Imports System.ComponentModel.DataAnnotations
Imports System.Web.Mvc
Public Class InvoiceBO
    Public Sub New()
        _rows = New List(Of InvoiceRowBO)
        _postalcode = New PostalCodeBO
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
    'Private _unitid As Integer
    'Public Property UnitId() As Integer
    '    Get
    '        Return _unitid
    '    End Get
    '    Set(ByVal value As Integer)
    '        _unitid = value
    '    End Set
    'End Property
    'Private _paymentstageid As Integer
    'Public Property PaymentStageId() As Integer
    '    Get
    '        Return _paymentstageid
    '    End Get
    '    Set(ByVal value As Integer)
    '        _paymentstageid = value
    '    End Set
    'End Property
    Private _filename As String
    Public Property Filename() As String
        Get
            Return _filename
        End Get
        Set(ByVal value As String)
            _filename = value
        End Set
    End Property
    Private _date As Date
    <UIHint("Date")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:dd/MM/yyyy}")>
    <DataType(DataType.Date)>
    <Display(Name:="Factuurdatum")>
    Public Property Invoicedate() As Date
        Get
            Return _date
        End Get
        Set(ByVal value As Date)
            _date = value
        End Set
    End Property
    Private _clientid As Integer
    Public Property ClientId() As Integer
        Get
            Return _clientid
        End Get
        Set(ByVal value As Integer)
            _clientid = value
        End Set
    End Property
    Private _clienttype As ClientType
    Public Property ClientType() As ClientType
        Get
            Return _clienttype
        End Get
        Set(ByVal value As ClientType)
            _clienttype = value
        End Set
    End Property

    Private _rows As List(Of InvoiceRowBO)
    Public Property Rows() As List(Of InvoiceRowBO)
        Get
            Return _rows
        End Get
        Set(ByVal value As List(Of InvoiceRowBO))
            _rows = value
        End Set
    End Property
    Private _publicId As String
    <Display(Name:="Factuurdatum")>
    Public Property PublicId() As String
        Get
            Return _publicId
        End Get
        Set(ByVal value As String)
            _publicId = value
        End Set
    End Property
    Private _expirationdate As Date
    <UIHint("Date")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:dd/MM/yyyy}")>
    <DataType(DataType.Date)>
    <Display(Name:="Vervaldatum")>
    Public Property ExpirationDate() As Date
        Get
            Return _expirationdate
        End Get
        Set(ByVal value As Date)
            _expirationdate = value
        End Set
    End Property
    Private _vatnumber As String
    <Display(Name:="BTW-nummer")>
    Public Property VatNumber() As String
        Get
            Return _vatnumber
        End Get
        Set(ByVal value As String)
            _vatnumber = value
        End Set
    End Property
    Private _clientname As String
    <Display(Name:="Klantnaam")>
    Public Property ClientName() As String
        Get
            Return _clientname
        End Get
        Set(ByVal value As String)
            _clientname = value
        End Set
    End Property
    Private _adress As String
    <Display(Name:="Adres")>
    Public Property Adress() As String
        Get
            Return _adress
        End Get
        Set(ByVal value As String)
            _adress = value
        End Set
    End Property
    Private _postalcode As PostalCodeBO
    <Display(Name:="Postcode")>
    Public Property PostalCode() As PostalCodeBO
        Get
            Return _postalcode
        End Get
        Set(ByVal value As PostalCodeBO)
            _postalcode = value
        End Set
    End Property
    Private _bankaccount As String
    <Display(Name:="Rekeningnummer")>
    Public Property BankAccount() As String
        Get
            Return _bankaccount
        End Get
        Set(ByVal value As String)
            _bankaccount = value
        End Set
    End Property
    Private _extrainfo As String
    Public Property ExtraInfo() As String
        Get
            Return _extrainfo
        End Get
        Set(ByVal value As String)
            _extrainfo = value
        End Set
    End Property
    Private _text As String
    Public Property Text() As String
        Get
            Return _text
        End Get
        Set(ByVal value As String)
            _text = value
        End Set
    End Property
    Private _detailtext As String
    Public Property DetailText() As String
        Get
            Return _detailtext
        End Get
        Set(ByVal value As String)
            _detailtext = value
        End Set
    End Property


End Class

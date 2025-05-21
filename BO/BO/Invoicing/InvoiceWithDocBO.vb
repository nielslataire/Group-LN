Imports System.ComponentModel.DataAnnotations
Imports System.Web.Mvc
Public Class InvoiceBO
    Public Sub New()
        _rows = New List(Of InvoiceRowBO)
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


End Class

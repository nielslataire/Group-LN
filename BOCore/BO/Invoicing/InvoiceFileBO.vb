Imports System.ComponentModel.DataAnnotations
Public Class InvoiceFileBO
    Public Sub New()

    End Sub
    Private _DbId As Integer
    Public Property DbId() As Integer
        Get
            Return _DbId
        End Get
        Set(ByVal value As Integer)
            _DbId = value
        End Set
    End Property
    Private _ClientId As Integer
    Public Property ClientId() As Integer
        Get
            Return _ClientId
        End Get
        Set(ByVal value As Integer)
            _ClientId = value
        End Set
    End Property
    Private _filename As String
    Public Property Filename() As String
        Get
            Return _filename
        End Get
        Set(ByVal value As String)
            _filename = value
        End Set
    End Property
    Private _fullpath As String
    Public Property FullPath() As String
        Get
            Return _fullpath
        End Get
        Set(ByVal value As String)
            _fullpath = value
        End Set
    End Property
    Private _invoicename As String
    Public Property InvoiceName() As String
        Get
            Return _invoicename
        End Get
        Set(ByVal value As String)
            _invoicename = value
        End Set
    End Property
    Private _invoicenumber As Integer
    Public Property InvoiceNumber() As Integer
        Get
            Return _invoicenumber
        End Get
        Set(ByVal value As Integer)
            _invoicenumber = value
        End Set
    End Property
    Private _invoicenumberlong As String
    Public Property InvoiceNumberLong() As String
        Get
            Return _invoicenumberlong
        End Get
        Set(ByVal value As String)
            _invoicenumberlong = value
        End Set
    End Property
    Private _invoicedate As DateOnly?
    <UIHint("Date")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:dd/MM/yyyy}")>
    <DataType(DataType.Date)>
    <Display(Name:="Factuurdatum")>
    Public Property InvoiceDate() As DateOnly?
        Get
            Return _invoicedate
        End Get
        Set(ByVal value As DateOnly?)
            _invoicedate = value
        End Set
    End Property
    Private _invoicedatechanged As DateOnly?
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:dd/MM/yyyy}")>
    <DataType(DataType.Date)>
    Public Property InvoiceDateChanged() As DateOnly?
        Get
            Return _invoicedatechanged
        End Get
        Set(ByVal value As DateOnly?)
            _invoicedatechanged = value
        End Set
    End Property
    Private _deletable As Boolean
    Public Property Deletable() As Boolean
        Get
            Return _deletable
        End Get
        Set(ByVal value As Boolean)
            _deletable = value
        End Set
    End Property

End Class

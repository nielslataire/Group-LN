Imports System.ComponentModel.DataAnnotations
Public Class ProjectDocBO
    Private _docid As Integer
    Public Property Docid() As Integer
        Get
            Return _docid
        End Get
        Set(ByVal value As Integer)
            _docid = value
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
    Private _clientaccountid As Integer?
    Public Property ClientAccountId() As Integer?
        Get
            Return _clientaccountid
        End Get
        Set(ByVal value As Integer?)
            _clientaccountid = value
        End Set
    End Property
    Private _name As String
    Public Property Name() As String
        Get
            Return _name
        End Get
        Set(ByVal value As String)
            _name = value
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
    Private _sortorder As Integer
    Public Property SortOrder() As Integer
        Get
            Return _sortorder
        End Get
        Set(ByVal value As Integer)
            _sortorder = value
        End Set
    End Property
    Private _type As ProjectDocType
    Public Property Type() As ProjectDocType
        Get
            Return _type
        End Get
        Set(ByVal value As ProjectDocType)
            _type = value
        End Set
    End Property
    Private _docdate As DateOnly?
    <UIHint("Date")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:dd/MM/yyyy}")>
    <DataType(DataType.Date)>
    <Display(Name:="Documentdatum")>
    Public Property DocDate() As DateOnly?
        Get
            Return _docdate
        End Get
        Set(ByVal value As DateOnly?)
            _docdate = value
        End Set
    End Property
End Class

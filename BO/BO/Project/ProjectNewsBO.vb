Imports System.ComponentModel.DataAnnotations
Imports System.Web.Mvc
Public Class ProjectNewsBO
    Public Sub New()
        _picture = New ProjectPictureBO
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
    Private _titlenl As String
    <Display(Name:="Titel")>
    Public Property TitleNL() As String
        Get
            Return _titlenl
        End Get
        Set(ByVal value As String)
            _titlenl = value
        End Set
    End Property

    Private _textnl As String
    <Display(Name:="Tekst")>
    Public Property TextNL() As String
        Get
            Return _textnl
        End Get
        Set(ByVal value As String)
            _textnl = value
        End Set
    End Property
    Private _date As DateTime
    <Display(Name:="Datum")>
    Public Property NewsDate() As DateTime
        Get
            Return _date
        End Get
        Set(ByVal value As DateTime)
            _date = value
        End Set
    End Property
    Private _projectId As Integer
    <Display(Name:="ProjectId")>
    Public Property ProjectId() As Integer
        Get
            Return _projectId
        End Get
        Set(ByVal value As Integer)
            _projectId = value
        End Set
    End Property
    Private _picture As ProjectPictureBO
    Public Property Picture() As ProjectPictureBO
        Get
            Return _picture
        End Get
        Set(ByVal value As ProjectPictureBO)
            _picture = value
        End Set
    End Property
    Private _author As String
    Public Property Author() As String
        Get
            Return _author
        End Get
        Set(ByVal value As String)
            _author = value
        End Set
    End Property
End Class

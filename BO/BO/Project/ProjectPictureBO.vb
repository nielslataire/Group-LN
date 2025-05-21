Imports System.ComponentModel.DataAnnotations
Imports System.Web.Mvc
Public Class ProjectPictureBO
    Private _id As Integer
    Public Property Id() As Integer
        Get
            Return _id
        End Get
        Set(ByVal value As Integer)
            _id = value
        End Set
    End Property
    Private _name As String
    <Display(Name:="Naam")>
    Public Property Name() As String
        Get
            Return _name
        End Get
        Set(ByVal value As String)
            _name = value
        End Set
    End Property

    Private _caption As String
    <Display(Name:="Caption")>
    Public Property Caption() As String
        Get
            Return _caption
        End Get
        Set(ByVal value As String)
            _caption = value
        End Set
    End Property
    Private _type As PictureType
    Public Property Type() As PictureType
        Get
            Return _type
        End Get
        Set(ByVal value As PictureType)
            _type = value
        End Set
    End Property
    Private _datetimeuploaded As DateTime
    Public Property DateTimeUploaded() As DateTime
        Get
            Return _datetimeuploaded
        End Get
        Set(ByVal value As DateTime)
            _datetimeuploaded = value
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
    Private _facebookidcopro As String
    Public Property FacebookIdCopro() As String
        Get
            Return _facebookidcopro
        End Get
        Set(ByVal value As String)
            _facebookidcopro = value
        End Set
    End Property

  

End Class

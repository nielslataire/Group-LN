Imports BO
Imports System.ComponentModel.DataAnnotations
Imports System.Web.Mvc
Public Class ProjectModel
    Public Sub New()
        _projects = New List(Of ProjectBO)

    End Sub
    Private _projects As List(Of ProjectBO)
    Public Property Projects() As List(Of ProjectBO)
        Get
            Return _projects
        End Get
        Set(ByVal value As List(Of ProjectBO))
            _projects = value
        End Set
    End Property
    Private _statuses As List(Of ProjectStatusBO)
    Public Property Statuses() As List(Of ProjectStatusBO)
        Get
            Return _statuses
        End Get
        Set(ByVal value As List(Of ProjectStatusBO))
            _statuses = value
        End Set
    End Property
End Class
Public Class ProjectDetailModel
    Public Sub New()
        _developer = New CompanyBO
        _builder = New CompanyBO
        _architect = New CompanyBO
        _Engineer = New CompanyBO
        _securitycoordinator = New CompanyBO
        _epbreporter = New CompanyBO
        _news = New List(Of ProjectNewsBO)
    End Sub
    Private _data As ProjectBO
    Public Property Data() As ProjectBO
        Get
            Return _data
        End Get
        Set(ByVal value As ProjectBO)
            _data = value
        End Set
    End Property
    Private _developer As CompanyBO
    <Display(Name:="Projectontwikkelaar")>
    Public Property Developer() As CompanyBO
        Get
            Return _developer
        End Get
        Set(ByVal value As CompanyBO)
            _developer = value
        End Set
    End Property
    Private _builder As CompanyBO
    <Display(Name:="Bouwheer")>
    Public Property Builder() As CompanyBO
        Get
            Return _builder
        End Get
        Set(ByVal value As CompanyBO)
            _builder = value
        End Set
    End Property
    Private _architect As CompanyBO
    <Display(Name:="Architect")>
    Public Property Architect() As CompanyBO
        Get
            Return _architect
        End Get
        Set(ByVal value As CompanyBO)
            _architect = value
        End Set
    End Property
    Private _Engineer As CompanyBO
    <Display(Name:="Ingenieur Stabiliteit")>
    Public Property Engineer() As CompanyBO
        Get
            Return _Engineer
        End Get
        Set(ByVal value As CompanyBO)
            _Engineer = value
        End Set
    End Property
    Private _securitycoordinator As CompanyBO
    <Display(Name:="Veiligheidscoördinator")>
    Public Property SecurityCoordinator() As CompanyBO
        Get
            Return _securitycoordinator
        End Get
        Set(ByVal value As CompanyBO)
            _securitycoordinator = value
        End Set
    End Property
    Private _epbreporter As CompanyBO
    <Display(Name:="EPB-verslaggever")>
    Public Property EpbReporter() As CompanyBO
        Get
            Return _epbreporter
        End Get
        Set(ByVal value As CompanyBO)
            _epbreporter = value
        End Set
    End Property
    Private _news As List(Of ProjectNewsBO)
    Public Property News() As List(Of ProjectNewsBO)
        Get
            Return _news
        End Get
        Set(ByVal value As List(Of ProjectNewsBO))
            _news = value
        End Set
    End Property






End Class
Public Class ProjectPhotosModel
    Public Sub New()
        _photos = New List(Of ProjectPictureBO)
    End Sub
    Private _photos As List(Of ProjectPictureBO)
    Public Property Photos() As List(Of ProjectPictureBO)
        Get
            Return _photos
        End Get
        Set(ByVal value As List(Of ProjectPictureBO))
            _photos = value
        End Set
    End Property
    Private _projectId As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectId
        End Get
        Set(ByVal value As Integer)
            _projectId = value
        End Set
    End Property
    Private _projectname As String
    Public Property ProjectName() As String
        Get
            Return _projectname
        End Get
        Set(ByVal value As String)
            _projectname = value
        End Set

    End Property
    Private _projectcity As String
    Public Property ProjectCity() As String
        Get
            Return _projectcity
        End Get
        Set(ByVal value As String)
            _projectcity = value
        End Set
    End Property
    Private _projectslug As String
    Public Property ProjectSlug() As String
        Get
            Return _projectslug
        End Get
        Set(ByVal value As String)
            _projectslug = value
        End Set
    End Property

End Class
Public Class ProjectNewsModel
    Public Sub New()
        _news = New List(Of ProjectNewsBO)
    End Sub
    Private _news As List(Of ProjectNewsBO)
    Public Property News() As List(Of ProjectNewsBO)
        Get
            Return _news
        End Get
        Set(ByVal value As List(Of ProjectNewsBO))
            _news = value
        End Set
    End Property
    Private _projectId As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectId
        End Get
        Set(ByVal value As Integer)
            _projectId = value
        End Set
    End Property
    Private _projectname As String
    Public Property ProjectName() As String
        Get
            Return _projectname
        End Get
        Set(ByVal value As String)
            _projectname = value
        End Set

    End Property
    Private _projectcity As String
    Public Property ProjectCity() As String
        Get
            Return _projectcity
        End Get
        Set(ByVal value As String)
            _projectcity = value
        End Set
    End Property
    Private _projectslug As String
    Public Property ProjectSlug() As String
        Get
            Return _projectslug
        End Get
        Set(ByVal value As String)
            _projectslug = value
        End Set
    End Property
    Private _NewsId As Integer
    Public Property NewsId() As Integer
        Get
            Return _NewsId
        End Get
        Set(ByVal value As Integer)
            _NewsId = value
        End Set
    End Property

End Class



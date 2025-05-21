Imports BO
Imports System.ComponentModel.DataAnnotations
Imports System.Web.Mvc
Public Class ProjectModel
    Public Sub New()
        _projects = New List(Of ProjectBO)
        _salesdata = New List(Of ProjectSalesDataBO)
        _salessettings = New List(Of ProjectSalesSettingsBO)

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
    Private _salesdata As List(Of ProjectSalesDataBO)
    Public Property SalesData() As List(Of ProjectSalesDataBO)
        Get
            Return _salesdata
        End Get
        Set(ByVal value As List(Of ProjectSalesDataBO))
            _salesdata = value
        End Set
    End Property
    Private _salessettings As List(Of ProjectSalesSettingsBO)
    Public Property SalesSettings() As List(Of ProjectSalesSettingsBO)
        Get
            Return _salessettings
        End Get
        Set(ByVal value As List(Of ProjectSalesSettingsBO))
            _salessettings = value
        End Set
    End Property
End Class
Public Class ProjectDetailModel
    Public Sub New()
        _news = New List(Of ProjectNewsBO)
        _units = New List(Of UnitWithDetailsBO)
        _salesdata = New ProjectSalesDataBO
        _docs = New List(Of ProjectDocBO)
        _salessettings = New ProjectSalesSettingsBO
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
    Private _news As List(Of ProjectNewsBO)
    Public Property News() As List(Of ProjectNewsBO)
        Get
            Return _news
        End Get
        Set(ByVal value As List(Of ProjectNewsBO))
            _news = value
        End Set
    End Property
    Private _units As List(Of UnitWithDetailsBO)
    Public Property Units() As List(Of UnitWithDetailsBO)
        Get
            Return _units
        End Get
        Set(ByVal value As List(Of UnitWithDetailsBO))
            _units = value
        End Set
    End Property
    Private _salesdata As ProjectSalesDataBO
    Public Property SalesData() As ProjectSalesDataBO
        Get
            Return _salesdata
        End Get
        Set(ByVal value As ProjectSalesDataBO)
            _salesdata = value
        End Set
    End Property
    Private _salessettings As ProjectSalesSettingsBO
    Public Property SalesSetttings() As ProjectSalesSettingsBO
        Get
            Return _salessettings
        End Get
        Set(ByVal value As ProjectSalesSettingsBO)
            _salessettings = value
        End Set
    End Property
    Private _docs As List(Of ProjectDocBO)
    Public Property Docs() As List(Of ProjectDocBO)
        Get
            Return _docs
        End Get
        Set(ByVal value As List(Of ProjectDocBO))
            _docs = value
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

End Class

Public Class ProjectSendPlanModel
    Public Sub New()

    End Sub
    Private _unitid As Integer
    Public Property UnitId() As Integer
        Get
            Return _unitid
        End Get
        Set(ByVal value As Integer)
            _unitid = value
        End Set
    End Property
    Private _email As String
    <EmailAddress(ErrorMessage:="Emailadres is niet geldig")>
    <UIHint("Email")>
    <Display(Name:="Emailadres")>
    <Required(ErrorMessage:="Uw emailadres is verplicht")>
    Public Property Email() As String
        Get
            Return _email
        End Get
        Set(ByVal value As String)
            _email = value
        End Set
    End Property
    Private _phone As String
    <Display(Name:="Telefoonnummer")>
    Public Property Phone() As String
        Get
            Return _phone
        End Get
        Set(ByVal value As String)
            _phone = value
        End Set
    End Property
End Class
Public Class ProjectSendDocModel
    Public Sub New()

    End Sub
    Private _docid As Integer
    Public Property DocId() As Integer
        Get
            Return _docid
        End Get
        Set(ByVal value As Integer)
            _docid = value
        End Set
    End Property
    Private _email As String
    <EmailAddress(ErrorMessage:="Emailadres is niet geldig")>
    <UIHint("Email")>
    <Display(Name:="Emailadres")>
    <Required(ErrorMessage:="Uw emailadres is verplicht")>
    Public Property Email() As String
        Get
            Return _email
        End Get
        Set(ByVal value As String)
            _email = value
        End Set
    End Property
    Private _phone As String
    <Display(Name:="Telefoonnummer")>
    Public Property Phone() As String
        Get
            Return _phone
        End Get
        Set(ByVal value As String)
            _phone = value
        End Set
    End Property
End Class
Public Class ProjectSendMailModel
    Public Sub New()

    End Sub
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property
    Private _email As String
    <EmailAddress(ErrorMessage:="Emailadres is niet geldig")>
    <UIHint("Email")>
    <Display(Name:="Emailadres")>
    <Required(ErrorMessage:="Uw emailadres is verplicht")>
    Public Property Email() As String
        Get
            Return _email
        End Get
        Set(ByVal value As String)
            _email = value
        End Set
    End Property
    Private _phone As String
    <Display(Name:="Telefoonnummer")>
    Public Property Phone() As String
        Get
            Return _phone
        End Get
        Set(ByVal value As String)
            _phone = value
        End Set
    End Property
    Private _firstname As String
    <Display(Name:="Voornaam")>
    Public Property Firstname() As String
        Get
            Return _firstname
        End Get
        Set(ByVal value As String)
            _firstname = value
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
    Private _question As String
    <Display(Name:="Uw vraag")>
    Public Property Question() As String
        Get
            Return _question
        End Get
        Set(ByVal value As String)
            _question = value
        End Set
    End Property
End Class


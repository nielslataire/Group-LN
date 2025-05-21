Imports BO
Public Class LatestNews
    Private _News As ProjectNewsBO
    Public Property News() As ProjectNewsBO
        Get
            Return _News
        End Get
        Set(ByVal value As ProjectNewsBO)
            _News = value
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
    Private _projectslug As String
    Public Property ProjectSlug() As String
        Get
            Return _projectslug
        End Get
        Set(ByVal value As String)
            _projectslug = value
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


End Class

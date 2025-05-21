Imports BO
Public Class HomeModel
    Public Sub New()
        _projects = New List(Of ProjectBO)
        _oldprojects = New List(Of ProjectBO)
        m_DeedofSaleWarnings = New List(Of ClientAccountBO)
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
    Private _oldprojects As List(Of ProjectBO)
    Public Property OldProjects() As List(Of ProjectBO)
        Get
            Return _oldprojects
        End Get
        Set(ByVal value As List(Of ProjectBO))
            _oldprojects = value
        End Set
    End Property
    Private m_selectedsearch As IdNameBO
    Public Property SelectedSearch() As IdNameBO
        Get
            Return m_selectedsearch
        End Get
        Set(ByVal value As IdNameBO)
            m_selectedsearch = value
        End Set
    End Property
    Private m_DeedofSaleWarnings As List(Of ClientAccountBO)
    Public Property DeedofSaleWarnings() As List(Of ClientAccountBO)
        Get
            Return m_DeedofSaleWarnings
        End Get
        Set(ByVal value As List(Of ClientAccountBO))
            m_DeedofSaleWarnings = value
        End Set
    End Property
    Private _insurancewarnings As List(Of WarningBO)
    Public Property InsuranceWarnings() As List(Of WarningBO)
        Get
            Return _insurancewarnings
        End Get
        Set(ByVal value As List(Of WarningBO))
            _insurancewarnings = value
        End Set
    End Property
    Private _projectInfo As List(Of WarningBO)
    Public Property ProjectInfo() As List(Of WarningBO)
        Get
            Return _projectInfo
        End Get
        Set(ByVal value As List(Of WarningBO))
            _projectInfo = value
        End Set
    End Property
End Class

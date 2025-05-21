Public Class ContactModel
    Private m_Id As Integer
    Public Property Id() As Integer
        Get
            Return m_Id
        End Get
        Set(ByVal value As Integer)
            m_Id = value
        End Set
    End Property
    Private m_Weergavenaam1 As String
    Public Property Weergavenaam1() As String
        Get
            Return m_Weergavenaam1
        End Get
        Set(ByVal value As String)
            m_Weergavenaam1 = value
        End Set
    End Property
    
    Private m_Weergavenaam2 As String
    Public Property Weergavenaam2() As String
        Get
            Return m_Weergavenaam2

        End Get
        Set(ByVal value As String)
            m_Weergavenaam2 = value
        End Set
    End Property
    Private m_CompanyId As Integer
    Public Property CompanyId() As Integer
        Get
            Return m_CompanyId
        End Get
        Set(ByVal value As Integer)
            m_CompanyId = value
        End Set
    End Property


End Class

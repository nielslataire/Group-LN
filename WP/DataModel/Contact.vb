Imports Newtonsoft.Json

Public Class Contact
    Public Sub New()

    End Sub
    <JsonProperty("Id")> _
    Public Property ContactId() As Integer
        Get
            Return m_ContactId
        End Get
        Set(value As Integer)
            m_ContactId = value
        End Set
    End Property

    <JsonProperty("Weergavenaam1")> _
    Public Property Weergavenaam1() As String
        Get
            Return m_Weergavenaam1
        End Get
        Set(value As String)
            m_Weergavenaam1 = value
        End Set
    End Property
    <JsonProperty("Weergavenaam2")> _
    Public Property Weergavenaam2() As String
        Get
            Return m_Weergavenaam2
        End Get
        Set(ByVal value As String)
            m_Weergavenaam2 = value
        End Set
    End Property
    Private m_CompanyId As Integer
    <JsonProperty("CompanyId")> _
    Public Property CompanyId() As Integer
        Get
            Return m_CompanyId
        End Get
        Set(ByVal value As Integer)
            m_CompanyId = value
        End Set
    End Property



    Private m_ContactId As Integer
    Private m_Weergavenaam1 As String
    Private m_Weergavenaam2 As String


End Class

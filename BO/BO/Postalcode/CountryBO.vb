Public Class CountryBO
    Private m_CountryId As String
    Public Property CountryID() As String
        Get
            Return m_CountryId
        End Get
        Set(ByVal value As String)
            m_CountryId = value
        End Set
    End Property
    Private m_Name As String
    Public Property Name() As String
        Get
            Return m_Name
        End Get
        Set(ByVal value As String)
            m_Name = value
        End Set
    End Property
    Private m_ISOCode As String
    Public Property ISOCode() As String
        Get
            Return m_ISOCode
        End Get
        Set(ByVal value As String)
            m_ISOCode = value
        End Set
    End Property



End Class

Public Class PostalCodeBO
    Public Sub New()
        m_Country = New CountryBO
        m_Provincie = New ProvincieBO
    End Sub

    Private m_PostcodeId As Integer?
    Public Property PostcodeId() As Integer?
        Get
            Return m_PostcodeId
        End Get
        Set(ByVal value As Integer?)
            m_PostcodeId = value
        End Set
    End Property
    Private m_Postcode As String
    Public Property Postcode() As String
        Get
            Return m_Postcode
        End Get
        Set(ByVal value As String)
            m_Postcode = value
        End Set
    End Property
    Private m_Gemeente As String
    Public Property Gemeente() As String
        Get
            Return m_Gemeente
        End Get
        Set(ByVal value As String)
            m_Gemeente = value
        End Set
    End Property
    Private m_Country As CountryBO
    Public Property Country() As CountryBO
        Get
            Return m_Country
        End Get
        Set(ByVal value As CountryBO)
            m_Country = value
        End Set
    End Property
    Private m_Provincie As ProvincieBO
    Public Property Provincie() As ProvincieBO
        Get
            Return m_Provincie
        End Get
        Set(ByVal value As ProvincieBO)
            m_Provincie = value
        End Set
    End Property
   





End Class

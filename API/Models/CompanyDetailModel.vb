Public Class CompanyDetailModel
    Private m_Id As Integer

    Private m_CompanyId As Integer
    Public Property CompanyId() As Integer
        Get
            Return m_CompanyId
        End Get
        Set(ByVal value As Integer)
            m_CompanyId = value
        End Set
    End Property
    Private m_Bedrijfsnaam As String
    Public Property Bedrijfsnaam() As String
        Get
            Return m_Bedrijfsnaam
        End Get
        Set(ByVal value As String)
            m_Bedrijfsnaam = value
        End Set
    End Property
    Private m_GSM As String
    Public Property GSM() As String
        Get
            Return m_GSM
        End Get
        Set(ByVal value As String)
            m_GSM = value
        End Set
    End Property
    Private m_Telefoon2 As String
    Public Property Telefoon2() As String
        Get
            Return m_Telefoon2
        End Get
        Set(ByVal value As String)
            m_Telefoon2 = value
        End Set
    End Property
    Private m_Telefoon1 As String
    Public Property Telefoon1() As String
        Get
            Return m_Telefoon1
        End Get
        Set(ByVal value As String)
            m_Telefoon1 = value
        End Set
    End Property
    Private m_Email As String
    Public Property Email() As String
        Get
            Return m_Email
        End Get
        Set(ByVal value As String)
            m_Email = value
        End Set
    End Property
    Private m_Straat As String
    Public Property Straat() As String
        Get
            Return m_Straat
        End Get
        Set(ByVal value As String)
            m_Straat = value
        End Set
    End Property
    Private m_Huisnummer As String
    Public Property Huisnummer() As String
        Get
            Return m_Huisnummer
        End Get
        Set(ByVal value As String)
            m_Huisnummer = value
        End Set
    End Property
    Private m_Toevoeging As String
    Public Property Toevoeging() As String
        Get
            Return m_Toevoeging
        End Get
        Set(ByVal value As String)
            m_Toevoeging = value
        End Set
    End Property
    Private m_Busnummer As String
    Public Property Busnummer() As String
        Get
            Return m_Busnummer
        End Get
        Set(ByVal value As String)
            m_Busnummer = value
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
    Private m_contacts As New List(Of ContactModel)
    Public Property Contacts() As List(Of ContactModel)
        Get
            Return m_contacts
        End Get
        Set(ByVal value As List(Of ContactModel))
            m_contacts = value
        End Set
    End Property















End Class

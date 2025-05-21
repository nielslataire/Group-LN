Imports Newtonsoft.Json
Public Class CompanyDetail
    Private m_Id As Integer

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
    Private m_Bedrijfsnaam As String
    <JsonProperty("Bedrijfsnaam")> _
    Public Property Bedrijfsnaam() As String
        Get
            Return m_Bedrijfsnaam
        End Get
        Set(ByVal value As String)
            m_Bedrijfsnaam = value
        End Set
    End Property
    Private m_GSM As String
    <JsonProperty("GSM")> _
    Public Property GSM() As String
        Get
            Return m_GSM
        End Get
        Set(ByVal value As String)
            m_GSM = value
        End Set
    End Property
    Private m_Telefoon2 As String
    <JsonProperty("Telefoon2")> _
    Public Property Telefoon2() As String
        Get
            Return m_Telefoon2
        End Get
        Set(ByVal value As String)
            m_Telefoon2 = value
        End Set
    End Property
    Private m_Telefoon1 As String
    <JsonProperty("Telefoon1")> _
    Public Property Telefoon1() As String
        Get
            Return m_Telefoon1
        End Get
        Set(ByVal value As String)
            m_Telefoon1 = value
        End Set
    End Property
    Private m_Email As String
    <JsonProperty("Email")> _
    Public Property Email() As String
        Get
            Return m_Email
        End Get
        Set(ByVal value As String)
            m_Email = value
        End Set
    End Property
    Private m_Straat As String
    <JsonProperty("Straat")> _
    Public Property Straat() As String
        Get
            Return m_Straat
        End Get
        Set(ByVal value As String)
            m_Straat = value
        End Set
    End Property
    Private m_Huisnummer As String
    <JsonProperty("Huisnummer")> _
    Public Property Huisnummer() As String
        Get
            Return m_Huisnummer
        End Get
        Set(ByVal value As String)
            m_Huisnummer = value
        End Set
    End Property
    Private m_Toevoeging As String
    <JsonProperty("Toevoeging")> _
    Public Property Toevoeging() As String
        Get
            Return m_Toevoeging
        End Get
        Set(ByVal value As String)
            m_Toevoeging = value
        End Set
    End Property
    Private m_Busnummer As String
    <JsonProperty("Busnummer")> _
    Public Property Busnummer() As String
        Get
            Return m_Busnummer
        End Get
        Set(ByVal value As String)
            m_Busnummer = value
        End Set
    End Property
    Private m_Postcode As String
    <JsonProperty("Postcode")> _
    Public Property Postcode() As String
        Get
            Return m_Postcode
        End Get
        Set(ByVal value As String)
            m_Postcode = value
        End Set
    End Property
    Private m_Gemeente As String
    <JsonProperty("Gemeente")> _
    Public Property Gemeente() As String
        Get
            Return m_Gemeente
        End Get
        Set(ByVal value As String)
            m_Gemeente = value
        End Set
    End Property
    Public ReadOnly Property Adresregel1() As String
        Get
            Dim s As String = ""
            s = Straat & " " & Huisnummer
            If Not Toevoeging = "" Then
                s = s & Toevoeging
            End If
            If Not Busnummer = "" Then
                s = s & " Bus " & Busnummer
            End If
            Return s
        End Get

    End Property
    Public ReadOnly Property Adresregel2() As String
        Get
            Dim s As String = ""
            s = Postcode & " " & Gemeente
            Return s
        End Get

    End Property
    Private m_contacts As New List(Of Contact)
    <JsonProperty("Contacts")> _
    Public Property Contacts() As List(Of Contact)
        Get
            Return m_contacts
        End Get
        Set(ByVal value As List(Of Contact))
            m_contacts = value
        End Set
    End Property
End Class

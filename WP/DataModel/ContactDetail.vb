Imports Newtonsoft.Json

Public Class ContactDetail

    Public Sub New(ContactId As Integer, ContactNaam As String, ContactVoornaam As String)
        Me.ContactId = ContactId
        Me.ContactNaam = ContactNaam
        Me.ContactVoornaam = ContactVoornaam
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

    <JsonProperty("Naam")> _
    Public Property ContactNaam() As String
        Get
            Return m_ContactNaam
        End Get
        Set(value As String)
            m_ContactNaam = value
        End Set
    End Property
    <JsonProperty("Voornaam")> _
    Public Property ContactVoornaam() As String
        Get
            Return m_ContactVoornaam
        End Get
        Set(value As String)
            m_ContactVoornaam = value
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
    Private m_telefoon As String
    <JsonProperty("Telefoon")> _
    Public Property Telefoon() As String
        Get
            Return m_telefoon
        End Get
        Set(ByVal value As String)
            m_telefoon = value
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
    Private m_Functie As String
    <JsonProperty("Functie")> _
     Public Property Functie() As String
        Get
            Return m_Functie
        End Get
        Set(ByVal value As String)
            m_Functie = value
        End Set
    End Property







    Public Property WeergaveNaam() As String
        Get
            If m_ContactNaam = "" Then
                Return m_ContactVoornaam
            Else
                Return m_ContactNaam & " " & m_ContactVoornaam
            End If

        End Get
        Set(value As String)
            m_WeergaveNaam = value
        End Set
    End Property

    Private m_ContactId As Integer
    Private m_ContactNaam As String
    Private m_ContactVoornaam As String
    Private m_WeergaveNaam As String




End Class

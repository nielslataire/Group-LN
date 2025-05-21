Imports System.ComponentModel.DataAnnotations

Public Class CompanyBO
    Public Sub New()
        m_Postcode = New PostalCodeBO
        m_contacts = New List(Of ContactBO)
        m_Departments = New List(Of DepartmentBO)
        m_Activities = New List(Of ActivityBO)
    End Sub

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
    <Required(ErrorMessage:="Gelieve de bedrijfsnaam in te vullen")>
    <Display(Name:="Bedrijfsnaam")>
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
    <DisplayFormat(ApplyFormatInEditMode:=True, ConvertEmptyStringToNull:=True, DataFormatString:="{0:###/##.##.##}")>
    Public Property Telefoon1() As String
        Get
            Return m_Telefoon1
        End Get
        Set(ByVal value As String)
            m_Telefoon1 = value
        End Set
    End Property
    Private m_Email As String
    <EmailAddress>
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
    Private m_URL As String
    Public Property URL() As String
        Get
            Return m_URL
        End Get
        Set(ByVal value As String)
            m_URL = value
        End Set
    End Property
    Private m_OndNr As String
    Public Property OndNr() As String
        Get
            Return m_OndNr
        End Get
        Set(ByVal value As String)
            m_OndNr = value
        End Set
    End Property
    Private m_RegNr As String
    Public Property RegNr() As String
        Get
            Return m_RegNr
        End Get
        Set(ByVal value As String)
            m_RegNr = value
        End Set
    End Property
    Private m_Opmerking As String
    Public Property Opmerking() As String
        Get
            Return m_Opmerking
        End Get
        Set(ByVal value As String)
            m_Opmerking = value
        End Set
    End Property

    Private m_Postcode As PostalCodeBO
    Public Property Postcode() As PostalCodeBO
        Get
            Return m_Postcode
        End Get
        Set(ByVal value As PostalCodeBO)
            m_Postcode = value
        End Set
    End Property

    Private m_contacts As New List(Of ContactBO)
    Public Property Contacts() As List(Of ContactBO)
        Get
            Return m_contacts
        End Get
        Set(ByVal value As List(Of ContactBO))
            m_contacts = value
        End Set
    End Property
    Private m_Departments As List(Of DepartmentBO)
    Public Property Departments() As List(Of DepartmentBO)
        Get
            Return m_Departments
        End Get
        Set(ByVal value As List(Of DepartmentBO))
            m_Departments = value
        End Set
    End Property
    Private m_Activities As List(Of ActivityBO)
    Public Property Activities() As List(Of ActivityBO)
        Get
            Return m_Activities
        End Get
        Set(ByVal value As List(Of ActivityBO))
            m_Activities = value
        End Set
    End Property
    <UIHint("TelefoonFormat")>
    Public ReadOnly Property FormattedTelefoon() As String
        Get
            Return Telefoon1
        End Get

    End Property
    <UIHint("TelefoonFormat")>
    Public ReadOnly Property FormattedGSM() As String
        Get
            Return GSM
        End Get

    End Property

End Class

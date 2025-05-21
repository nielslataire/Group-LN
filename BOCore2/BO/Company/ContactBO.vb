Imports System.ComponentModel.DataAnnotations
Public Class ContactBO

    Public Sub New()
        Company = New IdNameBO()
        Department = New IdNameBO()
    End Sub
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

    'todo: rest of the properties
    Private _Salutation As String
    <Display(Name:="Aanspreking")>
    Public Property Salutation() As String
        Get
            Return _Salutation
        End Get
        Set(ByVal value As String)
            _Salutation = value
        End Set
    End Property

    Private _name As String
    <Required(ErrorMessage:="Gelieve de naam in te vullen")>
  <Display(Name:="Naam")>
    Public Property Name() As String
        Get
            Return _name
        End Get
        Set(ByVal value As String)
            _name = value
        End Set
    End Property
    Private _Firstname As String
      <Display(Name:="Voornaam")>
    Public Property Firstname() As String
        Get
            Return _Firstname
        End Get
        Set(ByVal value As String)
            _Firstname = value
        End Set
    End Property
    Private _JobFunction As String
    <Display(Name:="Functie")>
    Public Property JobFunction() As String
        Get
            Return _JobFunction
        End Get
        Set(ByVal value As String)
            _JobFunction = value
        End Set
    End Property
    Private _Phone As String
    <Display(Name:="Telefoon")>
      Public Property Phone() As String
        Get
            Return _Phone
        End Get
        Set(ByVal value As String)
            _Phone = value
        End Set
    End Property
    Private _CellPhone As String
      <Display(Name:="GSM")>
    Public Property CellPhone() As String
        Get
            Return _CellPhone
        End Get
        Set(ByVal value As String)
            _CellPhone = value
        End Set
    End Property
    Private _Email As String
    <Display(Name:="Email")>
    <EmailAddress>
    Public Property Email() As String
        Get
            Return _Email
        End Get
        Set(ByVal value As String)
            _Email = value
        End Set
    End Property

    Private _Company As IdNameBO
    Public Property Company() As IdNameBO
        Get
            Return _Company
        End Get
        Set(ByVal value As IdNameBO)
            _Company = value
        End Set
    End Property
    Private _Department As IdNameBO
    Public Property Department() As IdNameBO
        Get
            Return _Department
        End Get
        Set(ByVal value As IdNameBO)
            _Department = value
        End Set
    End Property

    <UIHint("TelefoonFormat")>
    Public ReadOnly Property FormattedTelefoon() As String
        Get
            Return Phone
        End Get

    End Property
    <UIHint("TelefoonFormat")>
    Public ReadOnly Property FormattedGSM() As String
        Get
            Return CellPhone
        End Get

    End Property
End Class

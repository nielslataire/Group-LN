Imports System.ComponentModel.DataAnnotations
Public Class DepartmentBO

    Public Sub New()
        Postalcode = New PostalCodeBO()
        Company = New IdNameBO()
        m_contacts = New List(Of ContactBO)
    End Sub
    Private _id As Integer
    Public Property ID() As Integer
        Get
            Return _id
        End Get
        Set(ByVal value As Integer)
            _id = value
        End Set
    End Property
    Private _name As String
    <Required(ErrorMessage:="Gelieve een naam in te vullen")>
      Public Property Name() As String
        Get
            Return _name
        End Get
        Set(ByVal value As String)
            _name = value
        End Set
    End Property

    Private _street As String
    Public Property Street() As String
        Get
            Return _street
        End Get
        Set(ByVal value As String)
            _street = value
        End Set
    End Property
    Private _housenumber As String
    Public Property Housenumber() As String
        Get
            Return _housenumber
        End Get
        Set(ByVal value As String)
            _housenumber = value
        End Set
    End Property
    Private _Busnumber As String
    Public Property Busnumber() As String
        Get
            Return _Busnumber
        End Get
        Set(ByVal value As String)
            _Busnumber = value
        End Set
    End Property
    Private _Postalcode As PostalCodeBO
    Public Property Postalcode() As PostalCodeBO
        Get
            Return _Postalcode
        End Get
        Set(ByVal value As PostalCodeBO)
            _Postalcode = value
        End Set
    End Property
    Private _Phone As String
    Public Property Phone() As String
        Get
            Return _Phone
        End Get
        Set(ByVal value As String)
            _Phone = value
        End Set
    End Property
    Private _CellPhone As String
    Public Property CellPhone() As String
        Get
            Return _CellPhone
        End Get
        Set(ByVal value As String)
            _CellPhone = value
        End Set
    End Property
    Private _Email As String
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

    Private m_contacts As List(Of ContactBO)
    Public Property Contacts() As List(Of ContactBO)
        Get
            Return m_contacts
        End Get
        Set(ByVal value As List(Of ContactBO))
            m_contacts = value
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

Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.Web.Mvc
Public Class RoleViewModel
    Public Property Id() As String
        Get
            Return m_Id
        End Get
        Set(value As String)
            m_Id = Value
        End Set
    End Property
    Private m_Id As String
    <Required(AllowEmptyStrings:=False)> _
    <Display(Name:="RoleName")> _
    Public Property Name() As String
        Get
            Return m_Name
        End Get
        Set(value As String)
            m_Name = Value
        End Set
    End Property
    Private m_Name As String
End Class

Public Class EditUserViewModel
    Public Property Id() As String
        Get
            Return m_Id
        End Get
        Set(value As String)
            m_Id = Value
        End Set
    End Property
    Private m_Id As String
    Private _Username As String
    <Required>
    <Display(Name:="Gebruikersnaam")>
    Public Property Username() As String
        Get
            Return _Username
        End Get
        Set(ByVal value As String)
            _Username = value
        End Set
    End Property

    <Required(AllowEmptyStrings:=False)> _
    <Display(Name:="Email")> _
    <EmailAddress> _
    Public Property Email() As String
        Get
            Return m_Email
        End Get
        Set(value As String)
            m_Email = Value
        End Set
    End Property
    Private m_Email As String
    Private _Cellphone As String
    <Required>
    <Display(Name:="Mobiele telefoon")>
    Public Property Cellphone() As String
        Get
            Return _Cellphone
        End Get
        Set(ByVal value As String)
            _Cellphone = value
        End Set
    End Property

    Private _Name As String
    <Required>
    <Display(Name:="Naam")>
        Public Property Name() As String
        Get
            Return _Name
        End Get
        Set(ByVal value As String)
            _Name = value
        End Set
    End Property
    Private _Forename As String
    <Required>
    <Display(Name:="Voornaam")>
        Public Property Forename() As String
        Get
            Return _Forename
        End Get
        Set(ByVal value As String)
            _Forename = value
        End Set
    End Property
    Private _JobFunction As String
    <Required>
    <Display(Name:="Functie")>
        Public Property JobFunction() As String
        Get
            Return _JobFunction
        End Get
        Set(ByVal value As String)
            _JobFunction = value
        End Set
    End Property

    Public Property RolesList() As IEnumerable(Of SelectListItem)
        Get
            Return m_RolesList
        End Get
        Set(value As IEnumerable(Of SelectListItem))
            m_RolesList = Value
        End Set
    End Property
    Private m_RolesList As IEnumerable(Of SelectListItem)
End Class
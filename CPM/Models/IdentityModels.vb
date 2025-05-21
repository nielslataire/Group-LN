Imports System.Security.Claims
Imports System.Threading.Tasks
Imports Microsoft.AspNet.Identity
Imports Microsoft.AspNet.Identity.EntityFramework
Imports Microsoft.AspNet.Identity.Owin
Imports System.ComponentModel.DataAnnotations

' You can add profile data for the user by adding more properties to your ApplicationUser class, please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
Public Class ApplicationUser
    Inherits IdentityUser



    Public Async Function GenerateUserIdentityAsync(manager As UserManager(Of ApplicationUser)) As Task(Of ClaimsIdentity)
        ' Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
        Dim userIdentity = Await manager.CreateIdentityAsync(Me, DefaultAuthenticationTypes.ApplicationCookie)
        ' Add custom user claims here
        Return userIdentity
    End Function

    Private _Cellphone As String
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
    <Display(Name:="Functie")>
      Public Property JobFunction() As String
        Get
            Return _JobFunction
        End Get
        Set(ByVal value As String)
            _JobFunction = value
        End Set
    End Property

    <Display(Name:="Weergavenaam")>
    Public ReadOnly Property Displayname() As String
        Get
            Return _Name & " " & _Forename
        End Get

    End Property


End Class

Public Class ApplicationDbContext
    Inherits IdentityDbContext(Of ApplicationUser)
    Public Sub New()
        MyBase.New("connectionstring", throwIfV1Schema:=False)
    End Sub

    Public Shared Function Create() As ApplicationDbContext
        Return New ApplicationDbContext()
    End Function
End Class

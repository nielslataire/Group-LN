Imports System.ComponentModel.DataAnnotations
Imports System.Linq
Public Class MailModel
    Private _voornaam As String
    <Required(ErrorMessage:="Gelieve uw voornaam in te vullen")>
    <Display(Name:="Voornaam")>
    Public Property Voornaam() As String
        Get
            Return _voornaam
        End Get
        Set(ByVal value As String)
            _voornaam = value
        End Set
    End Property
    Private _achternaam As String
    <Required(ErrorMessage:="Gelieve uw achternaam in te vullen")>
    <Display(Name:="Achternaam")>
    Public Property Achternaam() As String
        Get
            Return _achternaam
        End Get
        Set(ByVal value As String)
            _achternaam = value
        End Set
    End Property
    Private _emailto As String
    <Required(ErrorMessage:="Gelieve uw email adres in te vullen")>
     <Display(Name:="E-mailadres")>
     <EmailAddress(ErrorMessage:="Het ingevulde email adres is niet in het correcte formaat")>
    Public Property EmailTo() As String
        Get
            Return _emailto
        End Get
        Set(ByVal value As String)
            _emailto = value
        End Set
    End Property
    Private _phone As String
    <Display(Name:="Telefoonnummer")>
    <Phone(ErrorMessage:="Uw telefoonnummer is niet in het correcte formaat")>
    Public Property Phone() As String
        Get
            Return _phone
        End Get
        Set(ByVal value As String)
            _phone = value
        End Set
    End Property
    Private _title As String
    <Required(ErrorMessage:="Gelieve een onderwerp te kiezen")>
    <Display(Name:="Onderwerp")>
    Public Property Title() As String
        Get
            Return _title
        End Get
        Set(ByVal value As String)
            _title = value
        End Set
    End Property
    Private _message As String
    <Required(ErrorMessage:="Gelieve uw bericht in te vullen")>
    <Display(Name:="Uw bericht")>
    Public Property Message() As String
        Get
            Return _message
        End Get
        Set(ByVal value As String)
            _message = value
        End Set
    End Property
    Private _privacyAkkoord As Boolean
    <Display(Name:="Privacy-akkoord")>
    Public Property PrivacyAkkoord() As Boolean
        Get
            Return _privacyAkkoord
        End Get
        Set(ByVal value As Boolean)
            _privacyAkkoord = value
        End Set
    End Property

    ' Volledige naam, samengesteld uit Voornaam + Achternaam — gebruikt voor e-mails en opslag.
    Public ReadOnly Property FullName() As String
        Get
            Return String.Join(" ", {Voornaam, Achternaam}.Where(Function(s) Not String.IsNullOrWhiteSpace(s)))
        End Get
    End Property
End Class

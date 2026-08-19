Imports System.ComponentModel.DataAnnotations
Imports System.Linq

Namespace Models.Vacatures
    Public Class SollicitatieModel
        Public Property VacatureId As Integer
        Public Property VacatureSlug As String
        Public Property VacatureTitel As String

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

        Private _email As String
        <Required(ErrorMessage:="Gelieve uw e-mailadres in te vullen")>
        <Display(Name:="E-mailadres")>
        <EmailAddress(ErrorMessage:="Het ingevulde e-mailadres is niet in het correcte formaat")>
        Public Property Email() As String
            Get
                Return _email
            End Get
            Set(ByVal value As String)
                _email = value
            End Set
        End Property

        Private _telefoon As String
        <Required(ErrorMessage:="Gelieve uw telefoonnummer in te vullen")>
        <Display(Name:="Telefoonnummer")>
        <Phone(ErrorMessage:="Uw telefoonnummer is niet in het correcte formaat")>
        Public Property Telefoon() As String
            Get
                Return _telefoon
            End Get
            Set(ByVal value As String)
                _telefoon = value
            End Set
        End Property

        Private _motivatie As String
        <Display(Name:="Motivatie")>
        Public Property Motivatie() As String
            Get
                Return _motivatie
            End Get
            Set(ByVal value As String)
                _motivatie = value
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
End Namespace

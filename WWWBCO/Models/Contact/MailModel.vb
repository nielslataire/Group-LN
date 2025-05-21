Imports System.ComponentModel.DataAnnotations
Public Class MailModel
    Private _emailto As String
    <Required(ErrorMessage:="Gelieve uw email adres in te vullen")>
     <Display(Name:="Uw email adres")>
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

    <Display(Name:="Uw telefoonnummer/GSM")>
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
    <Required(ErrorMessage:="Gelieve het onderwerp in te vullen")>
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
    <Display(Name:="Bericht")>
    Public Property Message() As String
        Get
            Return _message
        End Get
        Set(ByVal value As String)
            _message = value
        End Set
    End Property
    Private _contactname As String
    <Required(ErrorMessage:="Gelieve uw naam in te vullen")>
    <Display(Name:="Uw naam")>
    Public Property ContactName() As String
        Get
            Return _contactname
        End Get
        Set(ByVal value As String)
            _contactname = value
        End Set
    End Property
End Class

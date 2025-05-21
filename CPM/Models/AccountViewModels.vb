Imports System.ComponentModel.DataAnnotations

Public Class ExternalLoginConfirmationViewModel
    <Required>
    <Display(Name:="Email")>
    Public Property Email As String
End Class

Public Class ExternalLoginListViewModel
    Public Property ReturnUrl As String
End Class

Public Class SendCodeViewModel
    Public Property SelectedProvider As String
    Public Property Providers As ICollection(Of System.Web.Mvc.SelectListItem)
    Public Property ReturnUrl As String
    Public Property RememberMe As Boolean
End Class

Public Class VerifyCodeViewModel
    <Required>
    Public Property Provider As String
    
    <Required>
    <Display(Name:="Code")>
    Public Property Code As String
    
    Public Property ReturnUrl As String
    
    <Display(Name:="Onthoud deze browser?")>
    Public Property RememberBrowser As Boolean

    Public Property RememberMe As Boolean
End Class

Public Class ForgotViewModel
    <Required>
    <Display(Name:="Email")>
    Public Property Email As String
End Class

'Public Class LoginViewModel
'    <Required>
'    <Display(Name:="Login")>
'    Public Property Login As String

'    <Required>
'    <DataType(DataType.Password)>
'    <Display(Name:="Paswoord")>
'    Public Property Password As String

'    <Display(Name:="Onthoud mij?")>
'    Public Property RememberMe As Boolean
'End Class

Public Class LoginViewModel
    <Required>
    <Display(Name:="Gebruikersnaam")>
    Public Property Username As String

    <Required>
    <DataType(DataType.Password)>
    <Display(Name:="Paswoord")>
    Public Property Password As String

    <Display(Name:="Onthoud mij?")>
    Public Property RememberMe As Boolean
End Class

Public Class RegisterViewModel
    <Required>
    <Display(Name:="Gebruikersnaam")>
    Public Property Username As String

    <Required>
    <Display(Name:="Naam")>
    Public Property Name As String

    <Required>
    <Display(Name:="Voornaam")>
    Public Property Forename As String

    <Required>
    <Display(Name:="Functie")>
    Public Property JobFunction As String

    <Required>
    <Phone>
    <Display(Name:="GSM")>
    Public Property Cellphone As String

    <Required>
    <EmailAddress>
    <Display(Name:="Email")>
    Public Property Email As String


    <StringLength(100, ErrorMessage:="Uw {0} moet minstens {2} karakters lang zijn.", MinimumLength:=6)>
    <DataType(DataType.Password)>
    <Display(Name:="Paswoord")>
    Public Property Password As String

    <DataType(DataType.Password)>
    <Display(Name:="Bevestig paswoord")>
    <Compare("Password", ErrorMessage:="De paswoorden komen niet overeen.")>
    Public Property ConfirmPassword As String
End Class

Public Class ResetPasswordViewModel
    <Required>
    <EmailAddress>
    <Display(Name:="Email")>
    Public Property Email() As String

    <Required>
    <StringLength(100, ErrorMessage:="Uw {0} moet minstens {2} karakters lang zijn.", MinimumLength:=6)>
    <DataType(DataType.Password)>
    <Display(Name:="Paswoord")>
    Public Property Password() As String

    <DataType(DataType.Password)>
    <Display(Name:="Bevestig paswoord")>
    <Compare("Password", ErrorMessage:="De paswoorden komen niet overeen.")>
    Public Property ConfirmPassword() As String

    Public Property Code() As String
End Class

Public Class ForgotPasswordViewModel
    <Required>
    <EmailAddress>
    <Display(Name:="Email")>
    Public Property Email() As String
End Class
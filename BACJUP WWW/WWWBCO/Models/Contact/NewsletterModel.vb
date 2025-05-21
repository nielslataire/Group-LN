Imports System.ComponentModel.DataAnnotations
Public Class NewsletterModel
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
  
End Class

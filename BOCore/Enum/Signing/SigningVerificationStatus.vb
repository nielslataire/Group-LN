Imports System.ComponentModel.DataAnnotations

''' <summary>Status van één OTP-cyclus (<c>SigningVerification</c>). De code zelf wordt nergens bewaard.</summary>
Public Enum SigningVerificationStatus As Integer

    ''' <summary>Code aangemaakt en aan het kanaal overhandigd.</summary>
    <Display(Name:="Verzonden")>
    Sent = 0

    ''' <summary>De provider heeft het bericht aanvaard (message-id gekend).</summary>
    <Display(Name:="Aanvaard door provider")>
    Accepted = 1

    ''' <summary>De provider meldde aflevering (enkel bij kanalen die dat terugkoppelen).</summary>
    <Display(Name:="Afgeleverd")>
    Delivered = 2

    <Display(Name:="Geverifieerd")>
    Verified = 3

    ''' <summary>Te veel foute pogingen; er is een nieuwe code nodig.</summary>
    <Display(Name:="Geblokkeerd")>
    Failed = 4

    <Display(Name:="Verlopen")>
    Expired = 5

    ''' <summary>Vervangen door een nieuwere code van dezelfde ondertekenaar.</summary>
    <Display(Name:="Vervangen")>
    Superseded = 6

    ''' <summary>Gebruikt voor een definitieve ondertekening; kan niet opnieuw dienen.</summary>
    <Display(Name:="Verbruikt")>
    Consumed = 7

End Enum

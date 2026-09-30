Imports System.ComponentModel.DataAnnotations

''' <summary>
''' "Voor zover mogelijk" geldigheidscheck voor een GlV2-telefoon-/gsm-veld (GlV2Telefoon.cshtml/
''' GlV2Gsm.cshtml), server-side tegenhanger van de client-side check in gl-v2-error-summary.js.
''' Gebruikt GlV2PhonePrefixes.IsValid — zie die klasse voor het opslagformaat ("+32 495123456") en
''' waarom de digit-bereiken een benadering zijn, geen volwaardige internationale validatie. Leeg
''' geldt als geldig: verplicht-zijn is een eigen, aparte regel (RequiredAttribute), niet dit
''' attribuut. Standaardboodschap gebruikt {0} = veldnaam ([Display(Name:=...)]), zelfde conventie
''' als DutchRequiredMessageProvider (CPMCore) voor [Required].
''' </summary>
<AttributeUsage(AttributeTargets.Property, AllowMultiple:=False)>
Public Class GlV2PhoneAttribute
    Inherits ValidationAttribute

    Public Sub New()
        MyBase.New("{0} is geen geldig telefoonnummer.")
    End Sub

    Public Overrides Function IsValid(value As Object) As Boolean
        Dim s = TryCast(value, String)
        Return GlV2PhonePrefixes.IsValid(s)
    End Function

End Class

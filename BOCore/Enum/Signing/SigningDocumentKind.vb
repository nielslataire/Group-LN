Imports System.ComponentModel.DataAnnotations

''' <summary>Soort bestand in een ondertekeningsdossier (<c>SigningDocument</c>). Elk bestand is onveranderlijk.</summary>
Public Enum SigningDocumentKind As Integer

    ''' <summary>De PDF zoals ze ter ondertekening werd aangeboden; haar SHA-256 is dé referentie.</summary>
    <Display(Name:="Origineel")>
    Original = 0

    ''' <summary>Origineel + ondertekeningsblad, aangemaakt na voltooiing.</summary>
    <Display(Name:="Ondertekend document")>
    Final = 1

    <Display(Name:="Auditrapport")>
    AuditReport = 2

    <Display(Name:="Bijlage")>
    Attachment = 3

    ''' <summary>De getekende handtekening (PNG) van één ondertekenaar — enkel visueel, geen bewijs op zich.</summary>
    <Display(Name:="Handtekeningafbeelding")>
    SignatureImage = 4

End Enum

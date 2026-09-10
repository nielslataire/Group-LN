Imports System.ComponentModel.DataAnnotations

''' <summary>Type externe partij dat verantwoordelijk kan zijn voor een mijlpaal of dossier.</summary>
Public Enum MijlpaalPartijType As Integer

    <Display(Name:="Geen")>
    Geen = 0

    <Display(Name:="Bedrijf")>
    Bedrijf = 1

    <Display(Name:="Klant")>
    Klant = 2

    <Display(Name:="Contactpersoon bedrijf")>
    BedrijfContact = 3

End Enum

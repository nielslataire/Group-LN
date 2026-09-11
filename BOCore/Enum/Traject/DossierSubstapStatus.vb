Imports System.ComponentModel.DataAnnotations

''' <summary>Status van één stap in een dossier-checklist (bv. de stappen van een omgevingsvergunning).</summary>
Public Enum DossierSubstapStatus As Integer

    <Display(Name:="Nog niet gestart")>
    NietGestart = 0

    <Display(Name:="Bezig")>
    Bezig = 1

    <Display(Name:="Afgerond")>
    Afgerond = 2

    <Display(Name:="Niet van toepassing")>
    NietVanToepassing = 3

End Enum

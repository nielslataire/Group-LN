Imports System.ComponentModel.DataAnnotations

''' <summary>Soort projectdossier / workstream. Concrete types worden vanaf increment 4 uitgewerkt.</summary>
Public Enum DossierKind As Integer

    <Display(Name:="Vrij dossier")>
    Vrij = 0

    <Display(Name:="Nutsaansluiting")>
    NutsAansluiting = 1

    <Display(Name:="Omgevingsvergunning")>
    Omgevingsvergunning = 2

    <Display(Name:="Grondverwerving")>
    Grondverwerving = 3

    <Display(Name:="Akte")>
    Akte = 4

    <Display(Name:="Verzekering")>
    Verzekering = 5

    <Display(Name:="Verkoopdossier")>
    VerkoopDossier = 6

    <Display(Name:="Facturatiemijlpaal")>
    FacturatieMijlpaal = 7

    <Display(Name:="Nutsafrekening")>
    Nutsafrekening = 8

End Enum

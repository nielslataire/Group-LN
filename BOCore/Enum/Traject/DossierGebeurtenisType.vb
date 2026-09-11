Imports System.ComponentModel.DataAnnotations

''' <summary>Type regel in de opvolgingstijdlijn van een <c>ProjectDossier</c>.</summary>
Public Enum DossierGebeurtenisType As Integer

    <Display(Name:="Opmerking")>
    Opmerking = 0

    <Display(Name:="Statuswijziging")>
    StatusWijziging = 1

    <Display(Name:="Document toegevoegd")>
    DocumentToegevoegd = 2

    <Display(Name:="Aanvraag verstuurd")>
    AanvraagVerstuurd = 3

    <Display(Name:="Antwoord ontvangen")>
    AntwoordOntvangen = 4

    <Display(Name:="Herinnering verstuurd")>
    HerinneringVerstuurd = 5

End Enum

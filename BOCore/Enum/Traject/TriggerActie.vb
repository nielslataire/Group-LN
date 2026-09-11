Imports System.ComponentModel.DataAnnotations

''' <summary>
''' Actie die een mijlpaal-trigger uitvoert. <c>MaakDossier</c> en <c>StuurDossierAanvraagMail</c>
''' worden pas functioneel vanaf increment 4 (dossier-model); tot dan geeft de dispatcher een
''' "Overgeslagen"-resultaat. <c>MaakTaak</c> wordt functioneel vanaf increment 6 ("Mijn taken").
''' </summary>
Public Enum TriggerActie As Integer

    <Display(Name:="Verwittig rol")>
    VerwittigRol = 0

    <Display(Name:="Verwittig gebruiker")>
    VerwittigGebruiker = 1

    <Display(Name:="Maak taak")>
    MaakTaak = 2

    <Display(Name:="Maak dossier")>
    MaakDossier = 3

    <Display(Name:="Zet projectvlag")>
    ZetProjectVlag = 4

    <Display(Name:="Zet projectstatus")>
    ZetProjectStatus = 5

    <Display(Name:="Plan herinnering")>
    PlanHerinnering = 6

    <Display(Name:="Deblokkeer volgende fase")>
    DeblokkeerVolgendeFase = 7

    <Display(Name:="Stuur dossier-aanvraagmail")>
    StuurDossierAanvraagMail = 8

End Enum

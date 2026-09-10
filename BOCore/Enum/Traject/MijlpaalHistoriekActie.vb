Imports System.ComponentModel.DataAnnotations

''' <summary>Type wijziging in de historiek van een mijlpaal (spiegelt ConstructionIssueHistoryAction).</summary>
Public Enum MijlpaalHistoriekActie As Integer

    <Display(Name:="Aangemaakt")>
    Aangemaakt = 0

    <Display(Name:="Bijgewerkt")>
    Bijgewerkt = 1

    <Display(Name:="Statuswijziging")>
    Statuswijziging = 2

    <Display(Name:="Doeldatum gewijzigd")>
    DoeldatumGewijzigd = 3

    <Display(Name:="Verantwoordelijke toegewezen")>
    VerantwoordelijkeToegewezen = 4

    <Display(Name:="Opmerking toegevoegd")>
    OpmerkingToegevoegd = 5

    <Display(Name:="Automatisch afgeleid uit bron")>
    AutomatischAfgeleid = 6

    <Display(Name:="Trigger uitgevoerd")>
    TriggerUitgevoerd = 7

End Enum

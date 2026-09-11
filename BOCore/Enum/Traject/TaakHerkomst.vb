Imports System.ComponentModel.DataAnnotations

''' <summary>Waar een <c>ProjectTaak</c> vandaan komt — puur informatief, stuurt geen gedrag.</summary>
Public Enum TaakHerkomst As Integer

    <Display(Name:="Handmatig")>
    Handmatig = 0

    <Display(Name:="Trigger")>
    Trigger = 1

    <Display(Name:="Mijlpaal")>
    Mijlpaal = 2

    <Display(Name:="Dossier")>
    Dossier = 3

    <Display(Name:="Systeem")>
    Systeem = 4

End Enum

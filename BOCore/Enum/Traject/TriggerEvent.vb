Imports System.ComponentModel.DataAnnotations

''' <summary>Moment waarop een mijlpaal-trigger geëvalueerd/afgevuurd wordt.</summary>
Public Enum TriggerEvent As Integer

    <Display(Name:="Bij bereiken")>
    BijBereiken = 0

    <Display(Name:="Bij overschrijding")>
    BijOverschrijding = 1

    <Display(Name:="X dagen voor streefdatum")>
    XDagenVoorDoeldatum = 2

    <Display(Name:="Bij statuswijziging")>
    BijStatuswijziging = 3

    <Display(Name:="Bij afronding van de fase")>
    BijFaseAfronding = 4

End Enum

Imports System.ComponentModel.DataAnnotations

''' <summary>Prioriteit van een <c>ProjectTaak</c>.</summary>
Public Enum TaakPrioriteit As Integer

    <Display(Name:="Laag")>
    Laag = 0

    <Display(Name:="Normaal")>
    Normaal = 1

    <Display(Name:="Hoog")>
    Hoog = 2

    <Display(Name:="Dringend")>
    Dringend = 3

End Enum

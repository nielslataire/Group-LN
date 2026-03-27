Imports System.ComponentModel.DataAnnotations

Public Enum SalesDisplayStatus As Integer

    <Display(Name:="Nieuw")>
    Beschikbaar = 1

    <Display(Name:="Binnenkort")>
    Binnenkort = 2

    <Display(Name:="Lancering")>
    Lancering = 3

    <Display(Name:="Uitverkocht")>
    Uitverkocht = 8

End Enum

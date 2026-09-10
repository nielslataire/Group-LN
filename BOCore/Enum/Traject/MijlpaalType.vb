Imports System.ComponentModel.DataAnnotations

''' <summary>Aard van een mijlpaal — bepaalt o.a. iconen en groepering in de UI.</summary>
Public Enum MijlpaalType As Integer

    <Display(Name:="Algemeen")>
    Algemeen = 0

    <Display(Name:="Administratief")>
    Administratief = 1

    <Display(Name:="Vergunning")>
    Vergunning = 2

    <Display(Name:="Werf")>
    Werf = 3

    <Display(Name:="Verkoop")>
    Verkoop = 4

    <Display(Name:="Financieel")>
    Financieel = 5

    <Display(Name:="Keuring")>
    Keuring = 6

    <Display(Name:="Oplevering")>
    Oplevering = 7

End Enum

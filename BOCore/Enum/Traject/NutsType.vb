Imports System.ComponentModel.DataAnnotations

''' <summary>Soort nutsaansluiting waarvoor bij de netbeheerder een aanvraag loopt.</summary>
Public Enum NutsType As Integer

    <Display(Name:="Elektriciteit")>
    Elektriciteit = 0

    <Display(Name:="Gas")>
    Gas = 1

    <Display(Name:="Water")>
    Water = 2

    <Display(Name:="Riolering")>
    Riolering = 3

    <Display(Name:="Telecom / data")>
    Telecom = 4

    <Display(Name:="Overig")>
    Overig = 5

End Enum

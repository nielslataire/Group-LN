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

    ''' <summary>Behouden voor backward compatibility met bestaande data; nieuwe dossiers kiezen Proximus/Telenet/Overig.</summary>
    <Display(Name:="Telecom / data")>
    Telecom = 4

    <Display(Name:="Overig")>
    Overig = 5

    <Display(Name:="Proximus")>
    Proximus = 6

    <Display(Name:="Telenet")>
    Telenet = 7

End Enum

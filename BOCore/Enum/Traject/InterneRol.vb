Imports System.ComponentModel.DataAnnotations

''' <summary>
''' Gedeelde interne-rol-enum voor de trajectopvolging. Wordt gebruikt als "verantwoordelijke rol"
''' op mijlpalen/dossiers/taken en gemapt vanuit <c>CPMCore.Models.DashboardType</c>.
''' </summary>
Public Enum InterneRol As Integer

    <Display(Name:="Onbekend")>
    Onbekend = 0

    <Display(Name:="Projectleider / werfleider")>
    Projectleider = 1

    <Display(Name:="Projectontwikkelaar")>
    Projectontwikkelaar = 2

    <Display(Name:="CEO / CFO")>
    CeoCfo = 3

    <Display(Name:="Boekhouder")>
    Boekhouder = 4

    <Display(Name:="Verkoper")>
    Verkoper = 5

    <Display(Name:="Architect")>
    Architect = 6

    <Display(Name:="Extern")>
    Extern = 7

End Enum

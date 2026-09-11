Imports System.ComponentModel.DataAnnotations

''' <summary>Status van een persoonlijke/interne taak (<c>ProjectTaak</c> — "Mijn taken").</summary>
Public Enum TaakStatus As Integer

    <Display(Name:="Open")>
    Open = 0

    <Display(Name:="Bezig")>
    Bezig = 1

    <Display(Name:="Wachtend")>
    Wachtend = 2

    <Display(Name:="Afgerond")>
    Afgerond = 3

    <Display(Name:="Geannuleerd")>
    Geannuleerd = 4

End Enum

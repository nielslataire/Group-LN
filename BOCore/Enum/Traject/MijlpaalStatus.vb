Imports System.ComponentModel.DataAnnotations

''' <summary>Status van een individuele mijlpaal (keypoint).</summary>
Public Enum MijlpaalStatus As Integer

    <Display(Name:="Nog te doen")>
    Open = 0

    <Display(Name:="Bezig")>
    Bezig = 1

    <Display(Name:="Bereikt")>
    Bereikt = 2

    <Display(Name:="Niet van toepassing")>
    NietVanToepassing = 3

    <Display(Name:="Geblokkeerd")>
    Geblokkeerd = 4

End Enum

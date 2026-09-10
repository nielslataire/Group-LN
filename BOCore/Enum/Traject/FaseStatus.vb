Imports System.ComponentModel.DataAnnotations

''' <summary>Status van een fase binnen een projecttraject.</summary>
Public Enum FaseStatus As Integer

    <Display(Name:="Gepland")>
    Gepland = 0

    <Display(Name:="Actief")>
    Actief = 1

    <Display(Name:="Afgerond")>
    Afgerond = 2

    <Display(Name:="Overgeslagen")>
    Overgeslagen = 3

End Enum

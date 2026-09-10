Imports System.ComponentModel.DataAnnotations

''' <summary>Status van een volledig projecttraject.</summary>
Public Enum TrajectStatus As Integer

    <Display(Name:="Nog niet gestart")>
    NietGestart = 0

    <Display(Name:="Lopend")>
    Lopend = 1

    <Display(Name:="Gepauzeerd")>
    Gepauzeerd = 2

    <Display(Name:="Afgerond")>
    Afgerond = 3

    <Display(Name:="Stopgezet")>
    Stopgezet = 4

End Enum

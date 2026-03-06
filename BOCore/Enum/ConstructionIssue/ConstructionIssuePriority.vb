Imports System.ComponentModel.DataAnnotations

Public Enum ConstructionIssuePriority As Integer

    <Display(Name:="Laag")>
    Low = 0

    <Display(Name:="Normaal")>
    Normal = 1

    <Display(Name:="Hoog")>
    High = 2

    <Display(Name:="Dringend")>
    Critical = 3

End Enum
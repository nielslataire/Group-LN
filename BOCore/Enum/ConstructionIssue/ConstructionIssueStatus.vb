Imports System.ComponentModel.DataAnnotations

Public Enum ConstructionIssueStatus As Integer

    <Display(Name:="Open")>
    Open = 0

    <Display(Name:="Toegewezen")>
    Assigned = 1

    <Display(Name:="In uitvoering")>
    InProgress = 2

    <Display(Name:="Klaar voor controle")>
    WaitingInspection = 3

    <Display(Name:="Opgelost")>
    Resolved = 4

    <Display(Name:="Afgesloten")>
    Closed = 5

    <Display(Name:="Afgewezen")>
    Rejected = 6

    <Display(Name:="Heropend")>
    Reopened = 7

End Enum
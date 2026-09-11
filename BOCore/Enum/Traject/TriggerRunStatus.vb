Imports System.ComponentModel.DataAnnotations

''' <summary>Resultaat van één trigger-uitvoering (spiegelt IssueNotificationRun.Status).</summary>
Public Enum TriggerRunStatus As Integer

    <Display(Name:="Geslaagd")>
    Geslaagd = 0

    <Display(Name:="Mislukt")>
    Mislukt = 1

    <Display(Name:="Overgeslagen")>
    Overgeslagen = 2

End Enum

Imports System.ComponentModel.DataAnnotations

Public Enum ConstructionIssueType As Integer

    <Display(Name:="Uitvoeringsfout")>
    ExecutionDefect = 0

    <Display(Name:="Afwerkingspunt")>
    FinishRemark = 1

    <Display(Name:="Schade")>
    Damage = 2

    <Display(Name:="Opmerking klant")>
    CustomerRemark = 3

    <Display(Name:="Veiligheidspunt")>
    SafetyIssue = 4

    <Display(Name:="Administratieve opmerking")>
    AdministrativeRemark = 5

End Enum
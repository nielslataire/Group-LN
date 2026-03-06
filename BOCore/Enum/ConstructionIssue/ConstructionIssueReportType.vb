Imports System.ComponentModel.DataAnnotations

Public Enum ConstructionIssueReportType As Integer

    <Display(Name:="Werfinspectie")>
    SiteInspection = 0

    <Display(Name:="Vooroplevering")>
    PreDelivery = 1

    <Display(Name:="Na oplevering")>
    PostDelivery = 2

    <Display(Name:="Garantie-inspectie")>
    Warranty = 3

End Enum
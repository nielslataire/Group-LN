Imports System.ComponentModel.DataAnnotations

Public Enum ConstructionIssueResponsiblePartyType As Integer

    <Display(Name:="Aannemer")>
    Contractor = 0

    <Display(Name:="Projectontwikkelaar")>
    Developer = 1

    <Display(Name:="Architect")>
    Architect = 2

    <Display(Name:="Ingenieur")>
    Engineer = 3

    <Display(Name:="Leverancier")>
    Supplier = 4

    <Display(Name:="Andere")>
    Other = 5

End Enum
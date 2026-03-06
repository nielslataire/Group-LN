Imports System.ComponentModel.DataAnnotations

Public Enum ConstructionIssuePhase As Integer

    <Display(Name:="Tijdens uitvoering")>
    DuringConstruction = 0

    <Display(Name:="Vooroplevering")>
    PreDeliveryInspection = 1

    <Display(Name:="Voorlopige oplevering")>
    AfterDelivery = 2

    <Display(Name:="Garantieperiode")>
    WarrantyPeriod = 3

End Enum
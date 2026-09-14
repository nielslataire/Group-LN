Imports System.ComponentModel.DataAnnotations

Public Enum VoortgangFase As Integer
    <Display(Name:="Opstart")>
    Opstart = 0
    <Display(Name:="In voorbereiding")>
    InVoorbereiding = 1
    <Display(Name:="In uitvoering")>
    InUitvoering = 2
    <Display(Name:="Eindfase")>
    Eindfase = 3
    <Display(Name:="Afgewerkt")>
    Afgewerkt = 4
End Enum

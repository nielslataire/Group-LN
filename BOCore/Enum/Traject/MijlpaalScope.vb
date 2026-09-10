Imports System.ComponentModel.DataAnnotations

''' <summary>Bepaalt of een sjabloon-mijlpaal één keer op projectniveau wordt aangemaakt of één keer per eenheid.</summary>
Public Enum MijlpaalScope As Integer

    <Display(Name:="Project")>
    Project = 0

    <Display(Name:="Per eenheid")>
    PerEenheid = 1

End Enum

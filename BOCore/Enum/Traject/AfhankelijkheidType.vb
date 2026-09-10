Imports System.ComponentModel.DataAnnotations

''' <summary>Type afhankelijkheid tussen twee mijlpalen.</summary>
Public Enum AfhankelijkheidType As Integer

    <Display(Name:="Einde → start (finish-to-start)")>
    FinishToStart = 0

    <Display(Name:="Start → start")>
    StartToStart = 1

    <Display(Name:="Einde → einde")>
    FinishToFinish = 2

End Enum

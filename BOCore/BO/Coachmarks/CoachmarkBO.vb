Imports System.Collections.Generic

' ── Placement enum ───────────────────────────────────────────────────────────

Public Enum CoachmarkPlacement
    Bottom = 0
    [Top] = 1
    [Left] = 2
    [Right] = 3
End Enum

' ── Registry definitie (wordt ingevuld in CoachmarkRegistry, CPMCore) ─────────

Public Class CoachmarkDefinition
    ''' <summary>Unieke sleutel voor deze stap, bv. "Projects.Issues.Planning".</summary>
    Public Property FeatureKey As String

    ''' <summary>Pagina waarop de coachmark actief is, bv. "Projects.Detail.Issues".</summary>
    Public Property PageKey As String

    ''' <summary>CSS-selector van het doelelement.</summary>
    Public Property TargetSelector As String

    Public Property Title As String
    Public Property Message As String
    Public Property Placement As CoachmarkPlacement = CoachmarkPlacement.Bottom

    ''' <summary>
    ''' Optionele sequence-sleutel. Nothing = single coachmark.
    ''' Alle definities met dezelfde SequenceKey vormen een multi-step tour.
    ''' </summary>
    Public Property SequenceKey As String

    ''' <summary>Volgorde binnen de sequence (0-based).</summary>
    Public Property StepIndex As Integer = 0

    ''' <summary>Datum waarop de coachmark actief wordt.</summary>
    Public Property ReleaseDate As DateTime

    ''' <summary>Aantal dagen na ReleaseDate dat de coachmark nog getoond mag worden. 0 = onbeperkt.</summary>
    Public Property MaxShowDays As Integer = 60
End Class

' ── Service output (teruggegeven aan de view) ─────────────────────────────────

Public Class CoachmarkFlow
    ''' <summary>
    ''' Sleutel voor API-calls: FeatureKey voor singles, SequenceKey voor sequences.
    ''' </summary>
    Public Property StateKey As String

    Public Property IsSequence As Boolean
    Public Property CurrentStep As Integer
    Public Property Steps As IReadOnlyList(Of CoachmarkStepData) = New List(Of CoachmarkStepData)()
End Class

Public Class CoachmarkStepData
    Public Property TargetSelector As String
    Public Property Title As String
    Public Property Message As String

    ''' <summary>lowercase: "bottom" | "top" | "left" | "right"</summary>
    Public Property Placement As String = "bottom"
End Class

Imports System.ComponentModel.DataAnnotations

''' <summary>Bewerkbaar sjabloon inclusief fases en mijlpalen (admin-UI).</summary>
Public Class TrajectSjabloonBO

    Public Property Id As Integer?

    <Required, StringLength(200)>
    Public Property Naam As String = String.Empty

    Public Property ProjectType As Integer?

    Public Property IsStandaard As Boolean

    Public Property IsActief As Boolean = True

    Public Property Omschrijving As String

    Public Property Fases As List(Of TrajectSjabloonFaseBO) = New List(Of TrajectSjabloonFaseBO)

End Class

''' <summary>Fase binnen een <see cref="TrajectSjabloonBO"/>.</summary>
Public Class TrajectSjabloonFaseBO

    Public Property Id As Integer?

    <Required, StringLength(200)>
    Public Property Naam As String = String.Empty

    <Required, StringLength(50)>
    Public Property Code As String = String.Empty

    Public Property Volgorde As Integer

    <StringLength(20)>
    Public Property KleurCode As String

    Public Property StandaardProjectStatusId As Integer?

    Public Property Mijlpalen As List(Of TrajectSjabloonMijlpaalBO) = New List(Of TrajectSjabloonMijlpaalBO)

End Class

''' <summary>Mijlpaal-definitie binnen een <see cref="TrajectSjabloonFaseBO"/>.</summary>
Public Class TrajectSjabloonMijlpaalBO

    Public Property Id As Integer?

    <Required, StringLength(200)>
    Public Property Naam As String = String.Empty

    <Required, StringLength(50)>
    Public Property Code As String = String.Empty

    Public Property Volgorde As Integer

    Public Property MijlpaalType As Integer

    ''' <summary>BOCore.MijlpaalScope — 0 = projectniveau, 1 = per eenheid.</summary>
    Public Property Scope As Integer

    Public Property VerantwoordelijkeRol As Integer?

    <StringLength(50)>
    Public Property DoeldatumAnkerCode As String

    Public Property DoeldatumOffsetDagen As Integer?

    Public Property IsVerplicht As Boolean = True

    Public Property BronBinding As Integer?

    <StringLength(100)>
    Public Property BronParam As String

    Public Property DossierKind As Integer?

    Public Property Omschrijving As String

    Public Property Triggers As List(Of TrajectSjabloonMijlpaalTriggerBO) = New List(Of TrajectSjabloonMijlpaalTriggerBO)

End Class

''' <summary>Trigger-definitie binnen een <see cref="TrajectSjabloonMijlpaalBO"/>.</summary>
Public Class TrajectSjabloonMijlpaalTriggerBO

    Public Property Id As Integer?

    ''' <summary>BOCore.TriggerEvent.</summary>
    Public Property TriggerEvent As Integer

    ''' <summary>BOCore.TriggerActie.</summary>
    Public Property TriggerActie As Integer

    Public Property OffsetDagen As Integer?

    Public Property ActieParametersJson As String

    Public Property MagProjectWijzigen As Boolean

    Public Property IsActief As Boolean = True

    <StringLength(300)>
    Public Property Omschrijving As String

End Class

Imports System.ComponentModel.DataAnnotations

''' <summary>Payload om een mijlpaal aan te maken of te bewerken.</summary>
Public Class MijlpaalUpsertBO

    Public Property Id As Integer?

    <Required>
    Public Property ProjecttrajectId As Integer

    Public Property ProjecttrajectFaseId As Integer?

    ''' <summary>Leeg = mijlpaal op projectniveau; gezet = mijlpaal voor die ene eenheid.</summary>
    Public Property UnitId As Integer?

    <Required, StringLength(200)>
    Public Property Naam As String = String.Empty

    <StringLength(50)>
    Public Property Code As String

    Public Property Volgorde As Integer

    Public Property MijlpaalType As Integer

    Public Property Status As Integer

    <DataType(DataType.Date)>
    Public Property Doeldatum As DateOnly?

    <DataType(DataType.Date)>
    Public Property WerkelijkeDatum As DateOnly?

    Public Property VerantwoordelijkeRol As Integer?

    Public Property VerantwoordelijkePartijType As Integer?

    Public Property VerantwoordelijkePartijId As Integer?

    <StringLength(128)>
    Public Property VerantwoordelijkeUserId As String

    Public Property IsVerplicht As Boolean = True

    Public Property Opmerking As String

End Class

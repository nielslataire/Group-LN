Imports System.ComponentModel.DataAnnotations

''' <summary>Payload voor een statuswijziging van één mijlpaal.</summary>
Public Class MijlpaalStatusChangeBO

    <Required>
    Public Property MijlpaalId As Integer

    <Required>
    Public Property NieuweStatus As Integer

    <DataType(DataType.Date)>
    Public Property WerkelijkeDatum As DateOnly?

    Public Property Opmerking As String

End Class

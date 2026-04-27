Imports System.ComponentModel.DataAnnotations

Public Class UnitFinishingOptionBO
    Public Property Id As Integer
    Public Property UnitId As Integer
    <Display(Name:="Naam")>
    Public Property Name As String
    Public Property SortOrder As Integer
    Public Property IsDefault As Boolean
    Public Property ConstructionValues As New List(Of UnitConstructionValueBO)

    Public ReadOnly Property TotalValue As Decimal
        Get
            Return ConstructionValues.Sum(Function(cv) cv.Value)
        End Get
    End Property
End Class

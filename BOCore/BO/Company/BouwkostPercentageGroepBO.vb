Public Class BouwkostPercentageGroepBO
    Public Property Id As Integer
    Public Property Naam As String
    Public Property Volgorde As Integer
    Public Property Percentages As New List(Of BouwkostPercentageBO)
End Class

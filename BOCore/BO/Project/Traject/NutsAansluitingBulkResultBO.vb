''' <summary>Resultaat van een bulk-aanmaak: hoeveel dossiers aangemaakt, en welke eenheden overgeslagen werden (bestond al).</summary>
Public Class NutsAansluitingBulkResultBO
    Public Property AantalAangemaakt As Integer
    Public Property OvergeslagenEenheden As New List(Of String)
End Class

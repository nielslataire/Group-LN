Public Class BudgetOppervlaktesTotaalBO
    Public Property TotaalBewoonbaar() As Decimal
    Public Property TotaalGereduceerd() As Decimal
    Public Property TotaalGrondopp() As Decimal
    Public Property AantalWooneenheden() As Integer
    Public Property AantalParkeerplaatsen() As Integer
    Public Property AantalCommercieel() As Integer
    Public Property AantalTotaal() As Integer

    Public ReadOnly Property GemiddeldeM2PerEenheid() As Decimal
        Get
            If AantalWooneenheden > 0 Then
                Return TotaalBewoonbaar / AantalWooneenheden
            End If
            Return 0D
        End Get
    End Property
End Class

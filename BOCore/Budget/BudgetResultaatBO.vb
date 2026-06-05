Namespace Budget

    Public Class BudgetKostenPostBO
        Public Property Omschrijving As String
        Public Property Bedrag As Decimal
        Public Property Percentage As Decimal
        Public Property IsPerc As Boolean
        Public Property BasisBedrag As Decimal
    End Class

    Public Class BudgetResultaatBO
        Public Property BudgetVersieId As Integer
        Public Property VersieNaam As String
        Public Property Versienummer As Integer
        Public Property AantalEenheden As Integer
        Public Property TotaalGBA As Decimal

        Public Property TotaalBouw As Decimal
        Public Property KostenOpBouw As New List(Of BudgetKostenPostBO)
        Public Property Forfaits As New List(Of BudgetKostenPostBO)
        Public Property Financiering As New List(Of BudgetKostenPostBO)
        Public Property Onvoorzien As Decimal

        Public ReadOnly Property TotaalKosten As Decimal
            Get
                Return TotaalBouw _
                    + KostenOpBouw.Sum(Function(k) k.Bedrag) _
                    + Forfaits.Sum(Function(k) k.Bedrag) _
                    + Financiering.Sum(Function(k) k.Bedrag) _
                    + Onvoorzien
            End Get
        End Property

        Public ReadOnly Property TotaalPerEenheid As Decimal
            Get
                Return If(AantalEenheden > 0, TotaalKosten / AantalEenheden, 0D)
            End Get
        End Property

        Public ReadOnly Property TotaalPerM2GBA As Decimal
            Get
                Return If(TotaalGBA > 0, TotaalKosten / TotaalGBA, 0D)
            End Get
        End Property
    End Class

End Namespace

Namespace Budget

    ''' <summary>
    ''' Prijsreferentie (€/m²) voor de verkooplijnen van de budgetwizard: algemeen (ProjectId leeg) of projectspecifiek.
    ''' PrijsType = "Bouw" (op gereduceerde oppervlakte) of "Grond" (op grondoppervlakte).
    ''' </summary>
    Public Class BudgetPrijsReferentieBO
        Public Property Id As Integer
        Public Property ProjectId As Integer?
        Public Property ProjectNaam As String
        Public Property PrijsType As String
        Public Property Code As Integer
        Public Property PrijsPerM2 As Decimal
        Public Property Omschrijving As String
        Public Property Datum As Date?
        Public Property Bron As String

        Public ReadOnly Property IsAlgemeen As Boolean
            Get
                Return Not ProjectId.HasValue
            End Get
        End Property
    End Class

End Namespace

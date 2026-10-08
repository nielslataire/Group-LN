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
        ''' <summary>Migratie 077: gearchiveerd = niet meer kiesbaar op stap 8, maar blijft bestaan omdat budgetten ernaar verwijzen.</summary>
        Public Property Gearchiveerd As Boolean
        ''' <summary>Gevuld door de service: minstens één verkooplijn gebruikt deze code (CodeBouw/CodeGrond). Dan niet verwijderbaar, wel archiveerbaar.</summary>
        Public Property IsInGebruik As Boolean

        Public ReadOnly Property IsAlgemeen As Boolean
            Get
                Return Not ProjectId.HasValue
            End Get
        End Property

        ''' <summary>"B-01" / "G-07": type-letter + code op twee cijfers (design-handoff 38a).</summary>
        Public ReadOnly Property CodeLabel As String
            Get
                Return If(String.Equals(PrijsType, "Grond", StringComparison.OrdinalIgnoreCase), "G", "B") & "-" & Code.ToString("00")
            End Get
        End Property

        ''' <summary>Actualiteit (38a): een referentie zonder datum of ouder dan 12 maanden is "ouder dan 12 m.".</summary>
        Public ReadOnly Property IsOuderDan12Maanden As Boolean
            Get
                Return Not Datum.HasValue OrElse Datum.Value.Date < Date.Today.AddMonths(-12)
            End Get
        End Property
    End Class

End Namespace

Imports System.Text.Json

Namespace Budget

    ''' <summary>
    ''' De 13 reductiefactoren (VMSW-wegingsconventie) waarmee stap 2 een gereduceerde oppervlakte berekent.
    ''' Standaard = de vaste VMSW-waarden; per budgetversie overschrijfbaar (BudgetVersie.VmswFactoren, JSON).
    ''' </summary>
    Public Class VmswFactorenBO
        Public Property Bewoonbaar As Decimal = 1.0D
        Public Property Tuin As Decimal = 0.05D
        Public Property TerrasPrefab As Decimal = 1.0D
        Public Property TerrasGelijkvloers As Decimal = 0.1D
        Public Property Dakterras As Decimal = 0.33D
        Public Property GaragesBovengronds As Decimal = 0.9D
        Public Property GarBergOndergronds As Decimal = 0.5D
        Public Property BergGelijkvloers As Decimal = 0.4D
        Public Property Carports As Decimal = 0.3D
        Public Property DoorritGvl As Decimal = 0.6D
        Public Property Zolder As Decimal = 0.3D
        Public Property GemeenschappelijkeDelen As Decimal = 1.0D
        Public Property Wegenis As Decimal = 0.1D

        Public Shared ReadOnly Property Standaard As VmswFactorenBO
            Get
                Return New VmswFactorenBO()
            End Get
        End Property

        ''' <summary>Alle 13 factoren in vaste volgorde als (sleutel, label, waarde) — voor het tabelletje op stap 2.</summary>
        Public Function Lijst() As List(Of Tuple(Of String, String, Decimal))
            Return New List(Of Tuple(Of String, String, Decimal)) From {
                Tuple.Create("bewoonbaar", "Bewoonbaar", Bewoonbaar),
                Tuple.Create("tuin", "Tuin", Tuin),
                Tuple.Create("terrasPrefab", "Terras prefab", TerrasPrefab),
                Tuple.Create("terrasGelijkvloers", "Terras gelijkvloers", TerrasGelijkvloers),
                Tuple.Create("dakterras", "Dakterras", Dakterras),
                Tuple.Create("garagesBovengronds", "Garages bovengronds", GaragesBovengronds),
                Tuple.Create("garBergOndergronds", "Garage/berging ondergronds", GarBergOndergronds),
                Tuple.Create("bergGelijkvloers", "Berging gelijkvloers", BergGelijkvloers),
                Tuple.Create("carports", "Carports", Carports),
                Tuple.Create("doorritGvl", "Doorrit GVL", DoorritGvl),
                Tuple.Create("zolder", "Zolder", Zolder),
                Tuple.Create("gemeenschappelijkeDelen", "Gemeenschappelijke delen", GemeenschappelijkeDelen),
                Tuple.Create("wegenis", "Wegenis", Wegenis)}
        End Function

        ''' <summary>Wijkt minstens één factor af van de standaard VMSW?</summary>
        Public ReadOnly Property IsAangepast As Boolean
            Get
                Dim s = Standaard
                Return Bewoonbaar <> s.Bewoonbaar OrElse Tuin <> s.Tuin OrElse TerrasPrefab <> s.TerrasPrefab OrElse TerrasGelijkvloers <> s.TerrasGelijkvloers _
                    OrElse Dakterras <> s.Dakterras OrElse GaragesBovengronds <> s.GaragesBovengronds OrElse GarBergOndergronds <> s.GarBergOndergronds _
                    OrElse BergGelijkvloers <> s.BergGelijkvloers OrElse Carports <> s.Carports OrElse DoorritGvl <> s.DoorritGvl OrElse Zolder <> s.Zolder _
                    OrElse GemeenschappelijkeDelen <> s.GemeenschappelijkeDelen OrElse Wegenis <> s.Wegenis
            End Get
        End Property

        ''' <summary>JSON, of Nothing als alles standaard is (zodat de kolom NULL blijft).</summary>
        Public Function NaarJson() As String
            If Not IsAangepast Then Return Nothing
            Return JsonSerializer.Serialize(Me)
        End Function

        ''' <summary>Leest de JSON; leeg of ongeldig = standaard VMSW.</summary>
        Public Shared Function VanJson(json As String) As VmswFactorenBO
            If String.IsNullOrWhiteSpace(json) Then Return Standaard
            Try
                Dim f = JsonSerializer.Deserialize(Of VmswFactorenBO)(json)
                Return If(f, Standaard)
            Catch
                Return Standaard
            End Try
        End Function
    End Class

End Namespace

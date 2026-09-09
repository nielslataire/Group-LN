Public Class BouwkostPercentageBO
    Public Property Id As Integer
    Public Property GroepId As Integer
    Public Property GroepNaam As String
    Public Property Naam As String
    Public Property Percentage As Decimal
    Public Property Volgorde As Integer
    ''' <summary>Niet leeg voor vaste systeemrijen (standaard voor Budget > Parameters).</summary>
    Public Property Sleutel As String
    Public ReadOnly Property IsSysteem As Boolean
        Get
            Return Not String.IsNullOrEmpty(Sleutel)
        End Get
    End Property
End Class

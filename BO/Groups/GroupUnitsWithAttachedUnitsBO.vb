Public Class GroupUnitsWithAttachedUnitsBO
    Public Sub New()
        _units = New List(Of UnitWithAttachedUnitsBO)
    End Sub
    Private _id As Integer
    Public Property Id() As Integer
        Get
            Return _id
        End Get
        Set(ByVal value As Integer)
            _id = value
        End Set
    End Property
    Private _name As String
    Public Property Name() As String
        Get
            Return _name
        End Get
        Set(ByVal value As String)
            _name = value
        End Set
    End Property
    Private _units As List(Of UnitWithAttachedUnitsBO)
    Public Property Units() As List(Of UnitWithAttachedUnitsBO)
        Get
            Return _units
        End Get
        Set(ByVal value As List(Of UnitWithAttachedUnitsBO))
            _units = value
        End Set
    End Property
End Class

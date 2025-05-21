Public Class UnitWithDetailsBO
    Inherits UnitBO
    Public Sub New()
        _rooms = New List(Of RoomBO)

    End Sub
    Private _rooms As List(Of RoomBO)
    Public Property Rooms() As List(Of RoomBO)
        Get
            Return _rooms
        End Get
        Set(ByVal value As List(Of RoomBO))
            _rooms = value
        End Set
    End Property

End Class

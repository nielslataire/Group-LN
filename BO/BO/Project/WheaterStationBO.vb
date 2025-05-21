Public Class WheaterStationBO
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
    Private _visible As Boolean
    Public Property Visible() As Boolean
        Get
            Return _visible
        End Get
        Set(ByVal value As Boolean)
            _visible = value
        End Set
    End Property



End Class

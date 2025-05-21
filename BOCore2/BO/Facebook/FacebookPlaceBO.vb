Public Class FacebookPlaceBO
    Public Sub New()

    End Sub
    Private _id As String
    Public Property Id() As String
        Get
            Return _id
        End Get
        Set(ByVal value As String)
            _id = value
        End Set
    End Property
    Private _location As String
    Public Property Location() As String
        Get
            Return _location
        End Get
        Set(ByVal value As String)
            _location = value
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

End Class

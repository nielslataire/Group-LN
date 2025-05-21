Public Class IdNameBO

    Private _id As Integer
    Public Property ID() As Integer
        Get
            Return _id
        End Get
        Set(ByVal value As Integer)
            _id = value
        End Set
    End Property
    Private _display As String
    Public Property Display() As String
        Get
            Return _display
        End Get
        Set(ByVal value As String)
            _display = value
        End Set
    End Property
    Private _group As String
    Public Property Group() As String
        Get
            Return _Group
        End Get
        Set(ByVal value As String)
            _Group = value
        End Set
    End Property




End Class

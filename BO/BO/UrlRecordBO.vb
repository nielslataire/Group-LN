Public Class UrlRecordBO
    Private _controller As String
    Public Property Controller() As String
        Get
            Return _controller
        End Get
        Set(ByVal value As String)
            _controller = value
        End Set
    End Property
    Private _action As String
    Public Property Action() As String
        Get
            Return _action
        End Get
        Set(ByVal value As String)
            _action = value
        End Set
    End Property
    Private _id As Integer
    Public Property Id() As Integer
        Get
            Return _id
        End Get
        Set(ByVal value As Integer)
            _id = value
        End Set
    End Property
    Private _url As String
    Public Property URL() As String
        Get
            Return _url
        End Get
        Set(ByVal value As String)
            _url = value
        End Set
    End Property




End Class

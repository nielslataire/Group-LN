Public Class Message

    Private _Type As MessageType
    Public Property Type() As MessageType
        Get
            Return _Type
        End Get
        Set(ByVal value As MessageType)
            _Type = value
        End Set
    End Property

    Private _message As String
    Public Property Message() As String
        Get
            Return _message
        End Get
        Set(ByVal value As String)
            _message = value
        End Set
    End Property


End Class

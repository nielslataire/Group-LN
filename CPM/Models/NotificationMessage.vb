Public Class NotificationMessage
    Public Enum MessageType
        success
        [error]
        info
    End Enum
    Private _message As String
    Public Property Message() As String
        Get
            Return _message
        End Get
        Set(ByVal value As String)
            _message = value
        End Set
    End Property
    Private _messagetitle As String
    Public Property MessageTitle() As String
        Get
            Return _messagetitle
        End Get
        Set(ByVal value As String)
            _messagetitle = value
        End Set
    End Property



End Class

Public Class Response
    Public Sub New()
        Messages = New List(Of Message)()
    End Sub


    Private _messages As List(Of Message)
    Public Property Messages() As List(Of Message)
        Get
            Return _messages
        End Get
        Set(ByVal value As List(Of Message))
            _messages = value
        End Set
    End Property
    Private _insertedId As Integer
    Public Property InsertedId() As Integer
        Get
            Return _insertedId
        End Get
        Set(ByVal value As Integer)
            _insertedId = value
        End Set
    End Property

    Public ReadOnly Property Success() As Boolean
        Get
            Return Messages.Any(Function(a) a.Type = MessageType.Error) = False
        End Get
    End Property


    Public Sub AddError(message As String)
        If (Messages Is Nothing) Then Messages = New List(Of Message)()
        Messages.Add(New Message() With {.Type = MessageType.Error, .Message = message})
    End Sub
    Public Sub AddError(message As List(Of Message))
        If (Messages Is Nothing) Then Messages = New List(Of Message)()
        Messages.AddRange(message)
    End Sub

End Class

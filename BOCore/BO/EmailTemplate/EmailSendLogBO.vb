Public Class EmailSendLogBO

    Public Property ID As Integer
    Public Property ProjectId As Integer
    Public Property ContactEmail As String
    Public Property ContactNaam As String
    Public Property EmailTemplateId As Nullable(Of Integer)
    Public Property TemplateNaam As String
    Public Property Onderwerp As String
    Public Property VerzondenDoorUserId As Integer
    Public Property VerzondenDoorNaam As String
    Public Property VerzondenOp As DateTime

End Class

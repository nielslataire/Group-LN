Public Class Response
    Public Sub New()
        Messages = New List(Of Message)()
    End Sub

    Public Property Messages As List(Of Message)
    Public Property InsertedId As Integer

    Public ReadOnly Property Success As Boolean
        Get
            Return Not Messages.Any(Function(a) a.Type = MessageType.Error)
        End Get
    End Property

    Public Sub AddError(message As String)
        If Messages Is Nothing Then Messages = New List(Of Message)()
        Messages.Add(New Message() With {.Type = MessageType.Error, .Message = message})
    End Sub

    ' ✅ gefixt: andere naam, en voeg toe aan Me.Messages
    Public Sub AddErrors(errs As IEnumerable(Of Message))
        If errs Is Nothing Then Exit Sub
        If Messages Is Nothing Then Messages = New List(Of Message)()
        Messages.AddRange(errs)
    End Sub

    Public Sub AddSuccess(message As String)
        If Messages Is Nothing Then Messages = New List(Of Message)()
        Messages.Add(New Message() With {.Type = MessageType.Success, .Message = message})
    End Sub

    Public Sub AddSaveChangesResult(affectedRecords As Integer, successMessage As String, errorMessage As String)
        If Messages Is Nothing Then Messages = New List(Of Message)()
        If affectedRecords > 0 Then
            Messages.Add(New Message() With {
                .Type = MessageType.Success,
                .Message = $"{successMessage} ({affectedRecords})"
            })
        Else
            Messages.Add(New Message() With {
                .Type = MessageType.Error,
                .Message = errorMessage
            })
        End If
    End Sub

    ' ✅ handige helper om een child-response te mergen
    Public Sub Merge(other As Response)
        If other Is Nothing Then Exit Sub
        If other.Messages IsNot Nothing AndAlso other.Messages.Count > 0 Then
            If Me.Messages Is Nothing Then Me.Messages = New List(Of Message)()
            Me.Messages.AddRange(other.Messages)
        End If
        If other.InsertedId <> 0 Then
            Me.InsertedId = other.InsertedId
        End If
    End Sub
End Class

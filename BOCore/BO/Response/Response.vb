Public Class Response
    Public Sub New()
        Messages = New List(Of Message)()
    End Sub

    Public Property Messages As List(Of Message)
    Public Property InsertedId As Integer

    ' Handige flags
    Public ReadOnly Property Success As Boolean
        Get
            Return Not Messages.Any(Function(a) a.Type = MessageType.Error)
        End Get
    End Property
    Public ReadOnly Property HasErrors As Boolean
        Get
            Return Messages.Any(Function(a) a.Type = MessageType.Error)
        End Get
    End Property
    Public ReadOnly Property HasInfos As Boolean
        Get
            Return Messages.Any(Function(a) a.Type = MessageType.Info)
        End Get
    End Property
    Public ReadOnly Property HasWarnings As Boolean
        Get
            Return Messages.Any(Function(a) a.Type = MessageType.Warning)
        End Get
    End Property

    ' --- Add helpers ---
    Public Sub AddError(message As String)
        If Messages Is Nothing Then Messages = New List(Of Message)()
        Messages.Add(New Message() With {.Type = MessageType.Error, .Message = message})
    End Sub

    Public Sub AddErrors(errs As IEnumerable(Of Message))
        If errs Is Nothing Then Exit Sub
        If Messages Is Nothing Then Messages = New List(Of Message)()
        Messages.AddRange(errs)
    End Sub

    Public Sub AddSuccess(message As String)
        If Messages Is Nothing Then Messages = New List(Of Message)()
        Messages.Add(New Message() With {.Type = MessageType.Success, .Message = message})
    End Sub

    Public Sub AddInfo(message As String)
        If Messages Is Nothing Then Messages = New List(Of Message)()
        Messages.Add(New Message() With {.Type = MessageType.Info, .Message = message})
    End Sub

    Public Sub AddWarning(message As String)
        If Messages Is Nothing Then Messages = New List(Of Message)()
        Messages.Add(New Message() With {.Type = MessageType.Warning, .Message = message})
    End Sub

    ' NIEUW: 0 wijzigingen => Info (géén error)
    Public Sub AddSaveChangesResult(affectedRecords As Integer, successMessage As String, zeroMessage As String, Optional treatZeroAsInfo As Boolean = True)
        If Messages Is Nothing Then Messages = New List(Of Message)()
        If affectedRecords > 0 Then
            Messages.Add(New Message() With {
                .Type = MessageType.Success,
                .Message = $"{successMessage} ({affectedRecords})"
            })
        Else
            If treatZeroAsInfo Then
                Messages.Add(New Message() With {
                    .Type = MessageType.Info,
                    .Message = zeroMessage
                })
            Else
                Messages.Add(New Message() With {
                    .Type = MessageType.Error,
                    .Message = zeroMessage
                })
            End If
        End If
    End Sub

    ' Backwards compat: oude signatuur → standaard als Info behandelen
    Public Sub AddSaveChangesResult(affectedRecords As Integer, successMessage As String, errorMessage As String)
        AddSaveChangesResult(affectedRecords, successMessage, errorMessage, treatZeroAsInfo:=True)
    End Sub

    ' Merge helper
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

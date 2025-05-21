Imports BO

Partial Public Class Project
    Public Function GetIdName() As IdNameBO
        Dim bo As New IdNameBO
        bo.ID = Me.ProjectID
        bo.Display = Me.ProjectName
        Return bo
    End Function
End Class

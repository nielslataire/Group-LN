Imports BO
Partial Public Class ProjectStatus
    Public Function GetIdName() As IdNameBO
        Dim bo As New IdNameBO
        bo.ID = Me.StatusID
        bo.Display = Me.StatusName

        Return bo
    End Function
End Class

Imports BO
Partial Public Class ChangeOrder
    Public Function GetIdName() As IdNameBO
        Dim bo As New IdNameBO
        bo.ID = Me.ID
        bo.Display = Me.Description
        Return bo
    End Function
End Class

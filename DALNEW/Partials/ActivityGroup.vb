Imports BO

Public Class ActivityGroup
    Public Function GetIdName() As IdNameBO
        Dim bo As New IdNameBO
        bo.ID = Me.GroupId
        bo.Display = "Deel " & Me.Lot & " - " & Me.Name
        bo.Group = Me.Lot
        Return bo
    End Function
End Class

Imports BO

Partial Public Class Activity
    Public Function GetIdName() As IdNameBO
        Dim bo As New IdNameBO
        bo.ID = Me.ActivityID
        bo.Display = Me.Omschrijving
        bo.Group = "Deel " & Me.ActivityGroup.Lot & " - " & Me.ActivityGroup.Name
        Return bo
    End Function
End Class

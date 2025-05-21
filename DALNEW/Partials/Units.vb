Imports BO

Partial Public Class Units
    Public Function GetIdName() As IdNameBO
        Dim bo As New IdNameBO
        bo.ID = Me.Id
        If Me.UnitTypes IsNot Nothing AndAlso Not Me.UnitTypes.ID = 11 Then
            bo.Display = Me.UnitTypes.Name & " " & Me.Name
        Else
            bo.Display = Me.Name
        End If
        bo.Group = Me.UnitTypes.UnitGroupTypes.Name
        Return bo
    End Function
End Class

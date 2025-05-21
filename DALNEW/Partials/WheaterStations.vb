Imports BO

Partial Public Class WheaterStations
    Public Function GetIdName() As IdNameBO
        Dim bo As New IdNameBO
        bo.ID = Me.ID
        bo.Display = Me.Name
        Return bo
    End Function
End Class

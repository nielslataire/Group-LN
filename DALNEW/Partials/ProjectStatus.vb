Imports BO
Partial Public Class Country
    Public Function GetIdName() As IdNameBO
        Dim bo As New IdNameBO
        bo.ID = Me.ID
        bo.Display = Me.LandNaam
        bo.Group = Me.LandISOCode
        Return bo
    End Function
End Class

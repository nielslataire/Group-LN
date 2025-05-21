Imports BO

Partial Public Class Provincie
    Public Function GetIdName() As IdNameBO
        Dim bo As New IdNameBO()
        bo.ID = Me.ProvincieID
        bo.Display = Me.ProvincieName
        bo.Group = Me.Country.LandNaam
        Return bo
    End Function
End Class

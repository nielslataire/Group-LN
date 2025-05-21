Imports BO

Partial Public Class ClientAccount
    Public Function GetIdName() As IdNameBO
        Dim bo As New IdNameBO
        bo.ID = Me.Id
        If Me.CompanyName = "" Or Me.CompanyName Is Nothing Then
            bo.Display = Me.Name
        Else
            bo.Display = Me.CompanyName
        End If
        'bo.Group = Me.
        Return bo
    End Function

End Class

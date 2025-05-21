Imports BO
Partial Public Class InsuranceCompanies
    Public Function GetIdName() As IdNameBO
        Dim bo As New IdNameBO
        bo.ID = Me.Id
        bo.Display = Me.Name

        Return bo
    End Function
End Class

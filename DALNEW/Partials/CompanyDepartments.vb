Imports BO
Partial Public Class CompanyDepartments
    Public Function GetIdName() As IdNameBO
        Dim bo As New IdNameBO
        bo.ID = Me.DepartmentId
        bo.Display = Me.Naam
        Return bo
    End Function
End Class

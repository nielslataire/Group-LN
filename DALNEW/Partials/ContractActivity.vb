Imports BO
Partial Public Class ContractActivity
    Public Function GetIdName() As IdNameBO
        Dim bo As New IdNameBO
        bo.ID = Me.Id
        bo.Display = Me.Activity.Omschrijving & " - " & Me.Contract.CompanyInfo.BedrijfsNaam
        Return bo
    End Function
End Class

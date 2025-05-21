Imports BO
Partial Public Class Contract
    Public Function GetIdName() As IdNameBO
        Dim bo As New IdNameBO
        bo.ID = Me.ID
        bo.Display = Me.CompanyInfo.BedrijfsNaam
        For Each act In Me.ContractActivity
            bo.Display = bo.Display & " - " & act.Activity.Omschrijving
        Next
        Return bo
    End Function
End Class

Imports BO
Partial Public Class CompanyInfo
    Public Function GetIdName() As IdNameBO
        Dim bo As New IdNameBO
        bo.ID = Me.CompanyID
        bo.Display = Me.BedrijfsNaam

        Return bo
    End Function
    Public Function GetIdNameForSearch() As SelectBO
        Dim bo As New SelectBO
        bo.ID = Me.CompanyID
        bo.Text = Me.BedrijfsNaam
        bo.Extra = "Company"

        Return bo
    End Function
End Class

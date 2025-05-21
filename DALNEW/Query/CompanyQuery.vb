Imports System.Linq.Expressions
Imports BO
Public Class CompanyQuery

    Public Shared Function GetFilterQeury(filter As CompanyFilter) As Expression(Of Func(Of CompanyInfo, Boolean))
        Dim query As Expression(Of Func(Of CompanyInfo, Boolean)) = GetNameQuery(filter.CompanyName)
        query = query.And(GetActivitesAndQuery(filter.SelectedActivities))
        query = query.And(GetProvincesOrQuery(filter.SelectedProvince))
        Return query
    End Function

    Public Shared Function GetNameQuery(name As String) As Expression(Of Func(Of CompanyInfo, Boolean))
        If String.IsNullOrWhiteSpace(name) Then Return Nothing
        Return Function(w) w.BedrijfsNaam.Contains(name)
    End Function
    Public Shared Function GetActivitesAndQuery(activities As List(Of Integer)) As Expression(Of Func(Of CompanyInfo, Boolean))
        If activities Is Nothing OrElse activities.Count = 0 Then Return Nothing
        Dim query As Expression(Of Func(Of CompanyInfo, Boolean)) = Nothing
        For Each Activity In activities
            Dim id As Integer = Activity
            query = query.And(Function(w) w.Activity.Any(Function(a) a.ActivityID = id))
        Next
        Return query
    End Function
    Public Shared Function GetProvincesOrQuery(provinces As List(Of Integer)) As Expression(Of Func(Of CompanyInfo, Boolean))
        If provinces Is Nothing OrElse provinces.Count = 0 Then Return Nothing
        Return Function(f) f.PostalCode.ProvincieID.HasValue And provinces.Contains(f.PostalCode.ProvincieID)
    End Function

End Class

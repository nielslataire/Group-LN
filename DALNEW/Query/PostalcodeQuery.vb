Imports System.Linq.Expressions
Imports BO
Public Class PostalcodeQuery
    Public Shared Function GetCityContainsQuery(search As String) As Expression(Of Func(Of PostalCode, Boolean))
        Return Function(f) f.Gemeente.ToLower().Contains(search)
    End Function
    Public Shared Function GetPostalCodeContainsQuery(search As String) As Expression(Of Func(Of PostalCode, Boolean))
        Return Function(f) f.Postcode.Contains(search)
    End Function
    Public Shared Function GetCountryQuery(countryID As Integer) As Expression(Of Func(Of PostalCode, Boolean))
        Return Function(f) f.CountryID = countryID
    End Function
    Public Shared Function GetPostalCodeStartsWithQuery(search As String) As Expression(Of Func(Of PostalCode, Boolean))
        Return Function(f) f.Postcode.StartsWith(search)
    End Function
End Class

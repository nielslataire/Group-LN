'------------------------------------------------------------------------------
' <auto-generated>
'     This code was generated from a template.
'
'     Manual changes to this file may cause unexpected behavior in your application.
'     Manual changes to this file will be overwritten if the code is regenerated.
' </auto-generated>
'------------------------------------------------------------------------------

Imports System
Imports System.Collections.Generic

Partial Public Class InsuranceCompanies
    Public Property Id As Integer
    Public Property Name As String
    Public Property PostcodeID As Integer
    Public Property Straat As String
    Public Property Huisnummer As String
    Public Property Busnummer As String
    Public Property Toevoeging As String

    Public Overridable Property PostalCode As PostalCode
    Public Overridable Property Insurances As ICollection(Of Insurances) = New HashSet(Of Insurances)

End Class

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

Partial Public Class Provincie
    Public Property ProvincieID As Integer
    Public Property ProvincieName As String
    Public Property CountryID As Integer

    Public Overridable Property Country As Country
    Public Overridable Property PostalCode As ICollection(Of PostalCode) = New HashSet(Of PostalCode)

End Class

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

Partial Public Class Country
    Public Property LandISOCode As String
    Public Property LandNaam As String
    Public Property Selectable As Boolean
    Public Property ID As Integer

    Public Overridable Property PostalCode As ICollection(Of PostalCode) = New HashSet(Of PostalCode)
    Public Overridable Property Provincie As ICollection(Of Provincie) = New HashSet(Of Provincie)

End Class

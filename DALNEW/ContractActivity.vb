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

Partial Public Class ContractActivity
    Public Property Id As Integer
    Public Property ContractID As Integer
    Public Property ActivityID As Integer
    Public Property Price As Nullable(Of Decimal)
    Public Property Description As String

    Public Overridable Property Activity As Activity
    Public Overridable Property ChangeOrder As ICollection(Of ChangeOrder) = New HashSet(Of ChangeOrder)
    Public Overridable Property Contract As Contract
    Public Overridable Property Insurances As Insurances
    Public Overridable Property IncommingInvoiceDetail As ICollection(Of IncommingInvoiceDetail) = New HashSet(Of IncommingInvoiceDetail)

End Class

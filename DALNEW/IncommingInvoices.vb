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

Partial Public Class IncommingInvoices
    Public Property ID As Integer
    Public Property [Date] As Date
    Public Property Price As Decimal
    Public Property ContractID As Nullable(Of Integer)
    Public Property ExternalID As String
    Public Property CompanyID As Nullable(Of Integer)
    Public Property ProjectID As Integer

    Public Overridable Property CompanyInfo As CompanyInfo
    Public Overridable Property Contract As Contract
    Public Overridable Property Project As Project
    Public Overridable Property IncommingInvoiceDetail As ICollection(Of IncommingInvoiceDetail) = New HashSet(Of IncommingInvoiceDetail)

End Class

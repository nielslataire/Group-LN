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

Partial Public Class Insurances
    Public Property Id As Integer
    Public Property InsuranceCompanyID As Nullable(Of Integer)
    Public Property Startdate As Nullable(Of Date)
    Public Property Period As Nullable(Of Integer)
    Public Property ExtensionPeriod As Nullable(Of Integer)
    Public Property GuaranteePeriod As Nullable(Of Integer)
    Public Property Type As Integer
    Public Property Enddate As Nullable(Of Date)
    Public Property ContractActivityID As Integer

    Public Overridable Property ContractActivity As ContractActivity
    Public Overridable Property InsuranceCompanies As InsuranceCompanies

End Class

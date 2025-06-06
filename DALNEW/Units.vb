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

Partial Public Class Units
    Public Property Id As Integer
    Public Property Name As String
    Public Property TypeId As Nullable(Of Integer)
    Public Property ProjectId As Integer
    Public Property Landshare As Nullable(Of Decimal)
    Public Property LevelId As Nullable(Of Integer)
    Public Property ClientAccountID As Nullable(Of Integer)
    Public Property Street As String
    Public Property Housenumber As String
    Public Property Busnumber As String
    Public Property PreKad As String
    Public Property ConstructionValueSold As Nullable(Of Decimal)
    Public Property LandValueSold As Nullable(Of Decimal)
    Public Property ConstructionValue As Nullable(Of Decimal)
    Public Property LandValue As Nullable(Of Decimal)
    Public Property AttachedUnitId As Nullable(Of Integer)
    Public Property LinkedUnitId As Nullable(Of Integer)
    Public Property IsLink As Boolean
    Public Property Surface As Nullable(Of Decimal)
    Public Property Level As Nullable(Of Integer)
    Public Property Plan As String
    Public Property PaymentGroupId As Nullable(Of Integer)
    Public Property GroundSurface As Nullable(Of Decimal)
    Public Property ConstructionValueId As Nullable(Of Integer)
    Public Property LandValueInvoiceId As Nullable(Of Integer)

    Public Overridable Property Project As Project
    Public Overridable Property ProjectLevels As ProjectLevels
    Public Overridable Property ClientAccount As ClientAccount
    Public Overridable Property UnitTypes As UnitTypes
    Public Overridable Property AttachedUnit_Unit As ICollection(Of Units) = New HashSet(Of Units)
    Public Overridable Property AttachedUnit_attachedunit As Units
    Public Overridable Property LinkedUnit_Unit As ICollection(Of Units) = New HashSet(Of Units)
    Public Overridable Property LinkedUnit_linkedunit As Units
    Public Overridable Property UnitRooms As ICollection(Of UnitRooms) = New HashSet(Of UnitRooms)
    Public Overridable Property InvoicingPaymentGroup As InvoicingPaymentGroup
    Public Overridable Property InvoicesDetails As ICollection(Of InvoicesDetails) = New HashSet(Of InvoicesDetails)
    Public Overridable Property UnitConstructionValue As ICollection(Of UnitConstructionValue) = New HashSet(Of UnitConstructionValue)
    Public Overridable Property Invoices As Invoices

End Class

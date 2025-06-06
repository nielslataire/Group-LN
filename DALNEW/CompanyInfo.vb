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

Partial Public Class CompanyInfo
    Public Property CompanyID As Integer
    Public Property BedrijfsNaam As String
    Public Property Straat As String
    Public Property Huisnummer As String
    Public Property Toevoeging As String
    Public Property Busnummer As String
    Public Property Telefoon1 As String
    Public Property Telefoon2 As String
    Public Property GSM As String
    Public Property Fax1 As String
    Public Property Fax2 As String
    Public Property Email As String
    Public Property WEBURL As String
    Public Property Ondernemingsnummer As String
    Public Property RegistratieNr As String
    Public Property Bank As String
    Public Property Opmerkingen As String
    Public Property Documentatie As String
    Public Property Type As String
    Public Property LandCode As String
    Public Property PostCodeID As Nullable(Of Integer)
    Public Property Postcode As String
    Public Property Gemeente As String

    Public Overridable Property CompanyInfo1 As CompanyInfo
    Public Overridable Property CompanyInfo2 As CompanyInfo
    Public Overridable Property PostalCode As PostalCode
    Public Overridable Property Activity As ICollection(Of Activity) = New HashSet(Of Activity)
    Public Overridable Property CompanyContacts As ICollection(Of CompanyContacts) = New HashSet(Of CompanyContacts)
    Public Overridable Property CompanyDepartments As ICollection(Of CompanyDepartments) = New HashSet(Of CompanyDepartments)
    Public Overridable Property Developer As ICollection(Of Project) = New HashSet(Of Project)
    Public Overridable Property Engineer As ICollection(Of Project) = New HashSet(Of Project)
    Public Overridable Property SecurityCoordinator As ICollection(Of Project) = New HashSet(Of Project)
    Public Overridable Property EPBReporter As ICollection(Of Project) = New HashSet(Of Project)
    Public Overridable Property Architect As ICollection(Of Project) = New HashSet(Of Project)
    Public Overridable Property Builder As ICollection(Of Project) = New HashSet(Of Project)
    Public Overridable Property Contract As ICollection(Of Contract) = New HashSet(Of Contract)
    Public Overridable Property IncommingInvoices As ICollection(Of IncommingInvoices) = New HashSet(Of IncommingInvoices)

End Class

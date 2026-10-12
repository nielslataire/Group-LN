Imports System.ComponentModel.DataAnnotations

''' <summary>Soort van de factuur zelf (header-niveau, design-handoff 42c/42e punt 07) — niet te
''' verwarren met <see cref="IncommingInvoiceType"/>, dat per FACTUURLIJN aangeeft of het een
''' contract-, meerwerk- of geen-contract-kost is. Nothing/0 = Vordering (het huidige, enige gedrag
''' voor elke factuur die via het legacy Add/EditIncommingInvoice-scherm ingevoerd is).</summary>
Public Enum IncommingInvoiceKind As Byte
    <Display(Name:="Vordering")>
    Vordering = 0
    <Display(Name:="Eindvordering")>
    Eindvordering = 1
    <Display(Name:="Creditnota")>
    Creditnota = 2
End Enum

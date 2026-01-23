Imports System.ComponentModel.DataAnnotations

Public Enum InvoiceStatus As Integer
    <Display(Name:="Onbekend")>
    Unknown = 0
    <Display(Name:="Concept")>
    Draft = 1
    <Display(Name:="Genummerd")>
    Issued = 2
    <Display(Name:="Verzonden")>
    Sent = 3
    <Display(Name:="Deels betaald")>
    PartiallyPaid = 4
    <Display(Name:="Betaald")>
    Paid = 5
    <Display(Name:="Vervallen")>
    Overdue = 6
    <Display(Name:="Geannuleerd")>
    Cancelled = 7
    <Display(Name:="Geboekt")>
    Booked = 8
    <Display(Name:="Bezig met genereren")>
    Generating = 9
End Enum
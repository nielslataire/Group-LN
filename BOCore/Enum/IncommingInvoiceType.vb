Imports System.ComponentModel.DataAnnotations
Public Enum IncommingInvoiceType As Integer
    <Display(Name:="Contract")>
    Contract = 1
    <Display(Name:="Meerwerk")>
    Meerwerk = 2
    <Display(Name:="Meerwerk voor klant")>
    Meerwerk_Klant = 3
    <Display(Name:="Geen Contract")>
    Geen_Contract = 4

End Enum


Imports System.ComponentModel.DataAnnotations

Public Class ProjectContractSliceBO

    Public Property Id As Integer
    Public Property ProjectId As Integer

    <Display(Name:="Omschrijving")>
    Public Property Description As String

    <Display(Name:="Percentage")>
    Public Property Percentage As Decimal

    Public Property SortOrder As Integer

    Public Property InvoiceId As Integer?

    Public Property InvoicePublicId As String

End Class

Imports System

Public Class InvoiceEmailLogBO
    Public Property Id As Integer
    Public Property InvoiceId As Integer
    Public Property ToAddress As String
    Public Property CcAddress As String
    Public Property Subject As String
    Public Property ProviderId As String
    Public Property SentAt As DateTime
    Public Property Status As String
End Class
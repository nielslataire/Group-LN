Imports System

Public Class InvoiceUblBO
    Public Property InvoiceId As Integer
    Public Property UblVersion As String
    Public Property Profile As String
    Public Property XmlContent As String
    Public Property GeneratedAt As DateTime
    Public Property SentViaPeppol As Boolean
    Public Property PeppolDocId As String
End Class
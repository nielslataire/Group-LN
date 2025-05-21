Imports BO
Public Class FacturatieModel
    Public Sub New()
        _folders = New List(Of String)
        _files = New List(Of InvoiceFileBO)
    End Sub
    Private _folders As List(Of String)
    Public Property Folders() As List(Of String)
        Get
            Return _folders
        End Get
        Set(ByVal value As List(Of String))
            _folders = value
        End Set
    End Property
    Private _files As List(Of InvoiceFileBO)
    Public Property Files() As List(Of InvoiceFileBO)
        Get
            Return _files
        End Get
        Set(ByVal value As List(Of InvoiceFileBO))
            _files = value
        End Set
    End Property
End Class

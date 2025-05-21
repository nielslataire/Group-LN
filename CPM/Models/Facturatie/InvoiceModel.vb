Imports BO
Public Class InvoiceModel
    Public Sub New()

    End Sub
    Private _invoice As InvoiceBO
    Public Property Invoice() As InvoiceBO
        Get
            Return _invoice
        End Get
        Set(ByVal value As InvoiceBO)
            _invoice = value
        End Set
    End Property
    Private _client As ClientAccountWithUnitsBO
    Public Property Client() As ClientAccountWithUnitsBO
        Get
            Return _client
        End Get
        Set(ByVal value As ClientAccountWithUnitsBO)
            _client = value
        End Set
    End Property

End Class

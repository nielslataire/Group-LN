Public Class CompanyInvoiceSummaryBO
    Public Sub New()
        _company = New IdNameBO
    End Sub
    Private _company As IdNameBO
    Public Property Company() As IdNameBO
        Get
            Return _company
        End Get
        Set(ByVal value As IdNameBO)
            _company = value
        End Set
    End Property
    Private _totalInvoiced As Decimal
    Public Property TotalInvoiced() As Decimal
        Get
            Return _totalInvoiced
        End Get
        Set(ByVal value As Decimal)
            _totalInvoiced = value
        End Set
    End Property

End Class

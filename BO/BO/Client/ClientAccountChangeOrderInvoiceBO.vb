Imports System.ComponentModel.DataAnnotations
Public Class ClientAccountChangeOrderInvoiceBO
    Public Sub New()

    End Sub
    Private _clientaccountid As Integer
    Public Property ClientAccountId() As Integer
        Get
            Return _clientaccountid
        End Get
        Set(ByVal value As Integer)
            _clientaccountid = value
        End Set
    End Property
    Private _coid As Integer
    Public Property ChangeOrderId() As Integer
        Get
            Return _coid
        End Get
        Set(ByVal value As Integer)
            _coid = value
        End Set
    End Property
    Private _codId As Integer
    Public Property ChangeOrderDetailId() As Integer
        Get
            Return _codId
        End Get
        Set(ByVal value As Integer)
            _codId = value
        End Set
    End Property
End Class

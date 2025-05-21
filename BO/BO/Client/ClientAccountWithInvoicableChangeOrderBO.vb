Imports System.ComponentModel.DataAnnotations

Public Class ClientAccountWithInvoicableChangeOrderBO
    Public Sub New()
        _client = New ClientAccountBO
        _changeOrders = New List(Of ChangeOrderBO)
    End Sub
    Private _client As ClientAccountBO
    Public Property Client() As ClientAccountBO
        Get
            Return _client
        End Get
        Set(ByVal value As ClientAccountBO)
            _client = value
        End Set
    End Property
    Private _changeOrders As List(Of ChangeOrderBO)
    Public Property ChangeOrders() As List(Of ChangeOrderBO)
        Get
            Return _changeOrders
        End Get
        Set(ByVal value As List(Of ChangeOrderBO))
            _changeOrders = value
        End Set
    End Property



End Class

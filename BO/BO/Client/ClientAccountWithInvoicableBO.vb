Imports System.ComponentModel.DataAnnotations

Public Class ClientAccountWithInvoicableBO
    Public Sub New()
        _client = New ClientAccountBO
        _units = New List(Of UnitWithStagesBO)
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
    Private _units As List(Of UnitWithStagesBO)
    Public Property Units() As List(Of UnitWithStagesBO)
        Get
            Return _units
        End Get
        Set(ByVal value As List(Of UnitWithStagesBO))
            _units = value
        End Set
    End Property




End Class

Imports System.ComponentModel.DataAnnotations

Public Class ClientAccountWithUnitsBO
    Public Sub New()
        _client = New ClientAccountBO
        _units = New List(Of UnitBO)

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
    Private _units As List(Of UnitBO)
    Public Property Units() As List(Of UnitBO)
        Get
            Return _units
        End Get
        Set(ByVal value As List(Of UnitBO))
            _units = value
        End Set
    End Property



End Class

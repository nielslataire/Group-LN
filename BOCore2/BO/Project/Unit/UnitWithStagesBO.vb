Imports System.ComponentModel.DataAnnotations
Imports System.Web.Mvc
Public Class UnitWithStagesBO
    Public Sub New()
        _unit = New UnitBO
        _paymentstages = New List(Of ProjectPaymentStageBO)
    End Sub
    Private _unit As UnitBO
    Public Property Unit() As UnitBO
        Get
            Return _unit
        End Get
        Set(ByVal value As UnitBO)
            _unit = value
        End Set
    End Property
    Private _paymentstages As List(Of ProjectPaymentStageBO)
    Public Property PaymentStages() As List(Of ProjectPaymentStageBO)
        Get
            Return _paymentstages
        End Get
        Set(ByVal value As List(Of ProjectPaymentStageBO))
            _paymentstages = value
        End Set
    End Property


End Class

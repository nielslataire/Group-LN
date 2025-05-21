Imports System.ComponentModel.DataAnnotations
Imports System.Web.Mvc
Public Class ProjectPaymentGroupBO
    Public Sub New()
        _paymentstages = New List(Of ProjectPaymentStageBO)
    End Sub
    Private _id As Integer
    Public Property Id() As Integer
        Get
            Return _id
        End Get
        Set(ByVal value As Integer)
            _id = value
        End Set
    End Property
    Private _name As String
    <Required(ErrorMessage:="Gelieve de naam van de betalingsschijven in te vullen")>
    <Display(Name:="Naam betalingsschijven")>
    Public Property Name() As String
        Get
            Return _name
        End Get
        Set(ByVal value As String)
            _name = value
        End Set
    End Property
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
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
    Private _vatpercentage As Decimal
    <Range(1, Integer.MaxValue, ErrorMessage:="Gelieve een waarde in te vullen groter dan 1")>
    <Display(Name:="BTW percentage")>
    <DisplayFormat(DataFormatString:="{0:0,00}")>
    <UIHint("Percentage")>
    Public Property VatPercentage() As Decimal
        Get
            Return _vatpercentage
        End Get
        Set(ByVal value As Decimal)
            _vatpercentage = value
        End Set
    End Property




End Class

Imports System.ComponentModel.DataAnnotations
Imports System.Web.Mvc
Public Class UnitConstructionValueBO
    Public Sub New()

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
    Private _description As String
    <Display(Name:="Omschrijving")>
    Public Property Description() As String
        Get
            Return _description
        End Get
        Set(ByVal value As String)
            _description = value
        End Set
    End Property
    Private _value As Decimal
    '<DisplayFormat(DataFormatString:="{0:0,00}")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:C}")>
    <UIHint("Currency")>
    <Display(Name:="Bouwwaarde")>
    Public Property Value() As Decimal
        Get
            Return _value
        End Get
        Set(ByVal value As Decimal)
            _value = value
        End Set
    End Property
    Private _valuesold As Decimal
    '<DisplayFormat(DataFormatString:="{0:0,00}")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:C}")>
    <UIHint("Currency")>
    <Display(Name:="Bouwwaarde")>
    Public Property ValueSold() As Decimal
        Get
            Return _valuesold
        End Get
        Set(ByVal value As Decimal)
            _valuesold = value
        End Set
    End Property
    Private _paymentgroupid As Integer?
    Public Property PaymentGroupId() As Integer?
        Get
            Return _paymentgroupid
        End Get
        Set(ByVal value As Integer?)
            _paymentgroupid = value
        End Set
    End Property
    Private _unitId As Integer
    Public Property UnitId() As Integer
        Get
            Return _unitId
        End Get
        Set(ByVal value As Integer)
            _unitId = value
        End Set
    End Property
    Private _reduction As Decimal
    '<DisplayFormat(DataFormatString:="{0:0,00}")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:C}")>
    <UIHint("Currency")>
    <Display(Name:="Bouwwaarde")>
    Public Property Reduction() As Decimal
        Get
            Return _reduction
        End Get
        Set(ByVal value As Decimal)
            _reduction = value
        End Set
    End Property

End Class

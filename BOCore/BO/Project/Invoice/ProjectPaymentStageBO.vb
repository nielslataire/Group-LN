Imports System.ComponentModel.DataAnnotations

Public Class ProjectPaymentStageBO
    Private _id As Integer
    Public Property Id() As Integer
        Get
            Return _id
        End Get
        Set(ByVal value As Integer)
            _id = value
        End Set
    End Property
    Private _groupId As Integer
    Public Property GroupId() As Integer
        Get
            Return _groupId
        End Get
        Set(ByVal value As Integer)
            _groupId = value
        End Set
    End Property
    Private _name As String
    <Required(ErrorMessage:="Gelieve de schijfnaam in te vullen")>
    <Display(Name:="Schijf")>
    Public Property Name() As String
        Get
            Return _name
        End Get
        Set(ByVal value As String)
            _name = value
        End Set
    End Property
    Private _percentage As Decimal
    <Range(1, Integer.MaxValue, ErrorMessage:="Gelieve een waarde in te vullen groter dan 1")>
    <Display(Name:="%")>
    <DisplayFormat(DataFormatString:="{0:0,00}")>
    <UIHint("Percentage")>
    Public Property Percentage() As Decimal
        Get
            Return _percentage
        End Get
        Set(ByVal value As Decimal)
            _percentage = value
        End Set
    End Property
    Private _invoicable As Boolean?
    <Display(Name:="Te Factureren")>
    Public Property Invoicable() As Boolean?
        Get
            Return _invoicable
        End Get
        Set(ByVal value As Boolean?)
            _invoicable = value
        End Set
    End Property
    Private _doc As ProjectDocBO
    Public Property Doc() As ProjectDocBO
        Get
            Return _doc
        End Get
        Set(ByVal value As ProjectDocBO)
            _doc = value
        End Set
    End Property
    Private _invoicecount As Integer
    Public Property InvoiceCount() As Integer
        Get
            Return _invoicecount
        End Get
        Set(ByVal value As Integer)
            _invoicecount = value
        End Set
    End Property
    Private _vatpercentage As Decimal?
    <Display(Name:="%")>
    <DisplayFormat(DataFormatString:="{0:0,00}")>
    <UIHint("Percentage")>
    Public Property VatPercentage() As Decimal?
        Get
            Return _vatpercentage
        End Get
        Set(ByVal value As Decimal?)
            _vatpercentage = value
        End Set
    End Property
    Private _groupName As String
    Public Property GroupName() As String
        Get
            Return _groupName
        End Get
        Set(ByVal value As String)
            _groupName = value
        End Set
    End Property


End Class

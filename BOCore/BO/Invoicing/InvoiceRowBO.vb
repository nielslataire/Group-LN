Imports System.ComponentModel.DataAnnotations

Public Class InvoiceRowBO
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
    Private _invoiceid As Integer
    Public Property InvoiceId() As Integer
        Get
            Return _invoiceid
        End Get
        Set(ByVal value As Integer)
            _invoiceid = value
        End Set
    End Property
    Private _stageid As Integer?
    Public Property StageId() As Integer?
        Get
            Return _stageid
        End Get
        Set(ByVal value As Integer?)
            _stageid = value
        End Set
    End Property
    Private _unitid As Integer?
    Public Property UnitId() As Integer?
        Get
            Return _unitid
        End Get
        Set(ByVal value As Integer?)
            _unitid = value
        End Set
    End Property
    Private _changeorderdetailid As Integer?
    Public Property ChangeOrderDetailId() As Integer?
        Get
            Return _changeorderdetailid
        End Get
        Set(ByVal value As Integer?)
            _changeorderdetailid = value
        End Set
    End Property
    Private _constructionvalueid As Integer?
    Public Property ConstructionValueId() As Integer?
        Get
            Return _constructionvalueid
        End Get
        Set(ByVal value As Integer?)
            _constructionvalueid = value
        End Set
    End Property
    Private _Text As String
    Public Property Text() As String
        Get
            Return _Text
        End Get
        Set(ByVal value As String)
            _Text = value
        End Set
    End Property
    Private _vatpercentage As Decimal?
    Public Property VatPercentage() As Decimal?
        Get
            Return _vatpercentage
        End Get
        Set(ByVal value As Decimal?)
            _vatpercentage = value
        End Set
    End Property
    Private _price As Decimal?
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:C}")>
    <UIHint("Currency")>
    Public Property Price() As Decimal?
        Get
            Return _price
        End Get
        Set(ByVal value As Decimal?)
            _price = value
        End Set
    End Property
    Private _groupname As String
    Public Property GroupName() As String
        Get
            Return _groupname
        End Get
        Set(ByVal value As String)
            _groupname = value
        End Set
    End Property
    Private _UtilityCost As Boolean?
    Public Property UtilityCost() As Boolean?
        Get
            Return _UtilityCost
        End Get
        Set(ByVal value As Boolean?)
            _UtilityCost = value
        End Set
    End Property


End Class

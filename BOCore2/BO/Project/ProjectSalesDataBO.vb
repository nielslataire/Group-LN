Imports System.ComponentModel.DataAnnotations
Imports System.Web.Mvc
Public Class ProjectSalesDataBO
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property
    Private _percentagesold As Decimal
    Public Property PercentageSold() As Decimal
        Get
            Return _percentagesold
        End Get
        Set(ByVal value As Decimal)
            _percentagesold = value
        End Set
    End Property
    Private _valuesold As Decimal
    Public Property ValueSold() As Decimal
        Get
            Return _valuesold
        End Get
        Set(ByVal value As Decimal)
            _valuesold = value
        End Set
    End Property
    Private _valueforsale As Decimal
    Public Property ValueForSale() As Decimal
        Get
            Return _valueforsale
        End Get
        Set(ByVal value As Decimal)
            _valueforsale = value
        End Set
    End Property
    Private _livingunitssold As Integer
    Public Property LivingUnitsSold() As Integer
        Get
            Return _livingunitssold
        End Get
        Set(ByVal value As Integer)
            _livingunitssold = value
        End Set
    End Property
    Private _livingunits As Integer
    Public Property LivingUnits() As Integer
        Get
            Return _livingunits
        End Get
        Set(ByVal value As Integer)
            _livingunits = value
        End Set
    End Property
    Private _percentagelivingunitssold As Decimal
    Public Property PercentageLivingUnitsSold() As Decimal
        Get
            Return _percentagelivingunitssold
        End Get
        Set(ByVal value As Decimal)
            _percentagelivingunitssold = value
        End Set
    End Property
    Private _numberappartments As Integer
    Public Property NumberAppartments() As Integer
        Get
            Return _numberappartments
        End Get
        Set(ByVal value As Integer)
            _numberappartments = value
        End Set
    End Property
    Private _numberhouses As Integer
    Public Property NumberHouses() As Integer
        Get
            Return _numberhouses
        End Get
        Set(ByVal value As Integer)
            _numberhouses = value
        End Set
    End Property
    Private _numbercommercial As Integer
    Public Property NumberCommercial() As Integer
        Get
            Return _numbercommercial
        End Get
        Set(ByVal value As Integer)
            _numbercommercial = value
        End Set
    End Property
    Private _startingprice As Decimal
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:C}")>
    <UIHint("Currency")>
    Public Property StartingPrice() As Decimal
        Get
            Return _startingprice
        End Get
        Set(ByVal value As Decimal)
            _startingprice = value
        End Set
    End Property

End Class

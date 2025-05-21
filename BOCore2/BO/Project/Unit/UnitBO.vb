Imports System.ComponentModel.DataAnnotations
Imports System.Web.Mvc
Public Class UnitBO
    Public Sub New()
        _type = New UnitTypeBO
        _linkedunits = New List(Of UnitBO)
        _constructionvalues = New List(Of UnitConstructionValueBO)
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
    <Display(Name:="Naam")>
    Public Property Name() As String
        Get
            Return _name
        End Get
        Set(ByVal value As String)
            _name = value
        End Set
    End Property
    Private _type As UnitTypeBO
    <Display(Name:="Type Eenheid")>
    Public Property Type() As UnitTypeBO
        Get
            Return _type
        End Get
        Set(ByVal value As UnitTypeBO)
            _type = value
        End Set
    End Property
    Private _level As Integer
    <Display(Name:="Verdieping")>
    Public Property Level() As Integer
        Get
            Return _level
        End Get
        Set(ByVal value As Integer)
            _level = value
        End Set
    End Property

    Private _projectId As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectId
        End Get
        Set(ByVal value As Integer)
            _projectId = value
        End Set
    End Property
    Private _projectname As String
    Public Property ProjectName() As String
        Get
            Return _projectname
        End Get
        Set(ByVal value As String)
            _projectname = value
        End Set
    End Property

    Private _landshare As Integer
    <Display(Name:="Verdeeleenheid basisakte")>
    Public Property Landshare() As Integer
        Get
            Return _landshare
        End Get
        Set(ByVal value As Integer)
            _landshare = value
        End Set
    End Property
    Private _street As String
    <Display(Name:="Straat")>
        Public Property Street() As String
        Get
            Return _street
        End Get
        Set(ByVal value As String)
            _street = value
        End Set
    End Property
    Private _housenumber As String
    <Display(Name:="Huisnr.")>
        Public Property HouseNumber() As String
        Get
            Return _housenumber
        End Get
        Set(ByVal value As String)
            _housenumber = value
        End Set
    End Property
    Private _busnumber As String
    <Display(Name:="Bus")>
        Public Property BusNumber() As String
        Get
            Return _busnumber
        End Get
        Set(ByVal value As String)
            _busnumber = value
        End Set
    End Property
    Private _prekad As String
    <Display(Name:="Kadastraal nr.")>
    Public Property PreKad() As String
        Get
            Return _prekad
        End Get
        Set(ByVal value As String)
            _prekad = value
        End Set
    End Property
    Private _constructionvaluesold As Decimal
    '<DisplayFormat(DataFormatString:="{0:0,00}")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:C}")>
    <UIHint("Currency")>
    <Display(Name:="Bouwwaarde")>
    Public Property ConstructionValueSold() As Decimal
        Get
            Return _constructionvaluesold
        End Get
        Set(ByVal value As Decimal)
            _constructionvaluesold = value
        End Set
    End Property
    Private _landvaluesold As Decimal
    '<DisplayFormat(DataFormatString:="{0:0,00}")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:C}")>
    <UIHint("Currency")>
    <Display(Name:="Grondwaarde")>
    Public Property LandValueSold() As Decimal
        Get
            Return _landvaluesold
        End Get
        Set(ByVal value As Decimal)
            _landvaluesold = value
        End Set
    End Property
    Private _constructionvalue As Decimal
    '<DisplayFormat(DataFormatString:="{0:0,00}")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:C}")>
    <UIHint("Currency")>
    <Display(Name:="Bouwwaarde")>
    Public Property ConstructionValue() As Decimal
        Get
            Return _constructionvalue
        End Get
        Set(ByVal value As Decimal)
            _constructionvalue = value
        End Set
    End Property
    Private _landvalue As Decimal
    '<DisplayFormat(DataFormatString:="{0:0,00}")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:C}")>
    <UIHint("Currency")>
    <Display(Name:="Grondwaarde")>
    Public Property LandValue() As Decimal
        Get
            Return _landvalue
        End Get
        Set(ByVal value As Decimal)
            _landvalue = value
        End Set
    End Property
    Private _totalValue As Decimal
    '<DisplayFormat(DataFormatString:="{0:0,00}")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:C}")>
    <UIHint("Currency")>
    <Display(Name:="Verkoopprijs")>
    Public ReadOnly Property TotalValue() As Decimal
        Get
            Return _landvalue + _constructionvalues.Sum(Function(m) m.Value)
        End Get

    End Property
    Private _totalValueSold As Decimal
    '<DisplayFormat(DataFormatString:="{0:0,00}")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:C}")>
    <UIHint("Currency")>
    <Display(Name:="Prijs verkocht")>
    Public ReadOnly Property TotalValueSold() As Decimal
        Get
            Return _landvaluesold + _constructionvalues.Sum(Function(m) m.ValueSold)
        End Get

    End Property
    Private _totalConstructionValues As Decimal
    '<DisplayFormat(DataFormatString:="{0:0,00}")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:C}")>
    <UIHint("Currency")>
    <Display(Name:="Totaal bouwwaardes")>
    Public ReadOnly Property TotalConstructionValues() As Decimal
        Get
            Return _constructionvalues.Sum(Function(m) m.Value)
        End Get

    End Property
    Private _clientaccountid As Integer
    Public Property ClientAccountId() As Integer
        Get
            Return _clientaccountid
        End Get
        Set(ByVal value As Integer)
            _clientaccountid = value
        End Set
    End Property
    Private _attachedunitid As Integer?
    Public Property AttachedUnitsId() As Integer?
        Get
            Return _attachedunitid
        End Get
        Set(ByVal value As Integer?)
            _attachedunitid = value
        End Set
    End Property
    Private _linkedunitid As Integer?
    Public Property LinkedUnitId() As Integer?
        Get
            Return _linkedunitid
        End Get
        Set(ByVal value As Integer?)
            _linkedunitid = value
        End Set
    End Property
    Private _linkedunits As List(Of UnitBO)
    Public Property LinkedUnits() As List(Of UnitBO)
        Get
            Return _linkedunits
        End Get
        Set(ByVal value As List(Of UnitBO))
            _linkedunits = value
        End Set
    End Property
    Private _islink As Boolean
    Public Property IsLink() As Boolean
        Get
            Return _islink
        End Get
        Set(ByVal value As Boolean)
            _islink = value
        End Set
    End Property
    Private _surface As Decimal
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:F}")>
    <UIHint("Surface")>
    <Display(Name:="Oppervlakte")>
    Public Property Surface() As Decimal
        Get
            Return _surface
        End Get
        Set(ByVal value As Decimal)
            _surface = value
        End Set
    End Property
    Private _groundsurface As Decimal
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:F}")>
    <UIHint("Surface")>
    <Display(Name:="Grondoppervlakte")>
    Public Property GroundSurface() As Decimal
        Get
            Return _groundsurface
        End Get
        Set(ByVal value As Decimal)
            _groundsurface = value
        End Set
    End Property
    Private _plan As String
    <Display(Name:="Plan")>
    Public Property Plan() As String
        Get
            Return _plan
        End Get
        Set(ByVal value As String)
            _plan = value
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
    Private _constructionvalues As List(Of UnitConstructionValueBO)

    Public Property ConstructionValues() As List(Of UnitConstructionValueBO)
        Get
            Return _constructionvalues
        End Get
        Set(ByVal value As List(Of UnitConstructionValueBO))
            _constructionvalues = value
        End Set
    End Property

End Class

Public Class HeroSearchOptionsModel
    Public Sub New()
        _regios = New List(Of String)
        _pricebrackets = New List(Of HeroPriceBracket)
        _unitcategories = New List(Of HeroUnitCategory)
    End Sub
    Private _regios As List(Of String)
    Public Property Regios() As List(Of String)
        Get
            Return _regios
        End Get
        Set(ByVal value As List(Of String))
            _regios = value
        End Set
    End Property
    Private _pricebrackets As List(Of HeroPriceBracket)
    Public Property PriceBrackets() As List(Of HeroPriceBracket)
        Get
            Return _pricebrackets
        End Get
        Set(ByVal value As List(Of HeroPriceBracket))
            _pricebrackets = value
        End Set
    End Property
    Private _unitcategories As List(Of HeroUnitCategory)
    Public Property UnitCategories() As List(Of HeroUnitCategory)
        Get
            Return _unitcategories
        End Get
        Set(ByVal value As List(Of HeroUnitCategory))
            _unitcategories = value
        End Set
    End Property
    Private _showtypefield As Boolean
    Public Property ShowTypeField() As Boolean
        Get
            Return _showtypefield
        End Get
        Set(ByVal value As Boolean)
            _showtypefield = value
        End Set
    End Property
End Class

Public Class HeroPriceBracket
    Private _minvalue As Decimal?
    Public Property MinValue() As Decimal?
        Get
            Return _minvalue
        End Get
        Set(ByVal value As Decimal?)
            _minvalue = value
        End Set
    End Property
    Private _maxvalue As Decimal?
    Public Property MaxValue() As Decimal?
        Get
            Return _maxvalue
        End Get
        Set(ByVal value As Decimal?)
            _maxvalue = value
        End Set
    End Property
    Private _label As String
    Public Property Label() As String
        Get
            Return _label
        End Get
        Set(ByVal value As String)
            _label = value
        End Set
    End Property
End Class

Public Class HeroUnitCategory
    Private _key As String
    Public Property Key() As String
        Get
            Return _key
        End Get
        Set(ByVal value As String)
            _key = value
        End Set
    End Property
    Private _label As String
    Public Property Label() As String
        Get
            Return _label
        End Get
        Set(ByVal value As String)
            _label = value
        End Set
    End Property
End Class

Imports System.ComponentModel.DataAnnotations

Public Class BudgetOppervlaktesBO
    Private _id As Integer
    Public Property Id() As Integer
        Get
            Return _id
        End Get
        Set(ByVal value As Integer)
            _id = value
        End Set
    End Property

    Private _budgetVersieId As Integer
    Public Property BudgetVersieId() As Integer
        Get
            Return _budgetVersieId
        End Get
        Set(ByVal value As Integer)
            _budgetVersieId = value
        End Set
    End Property

    Private _eenheidNaam As String
    <Display(Name:="Naam")>
    Public Property EenheidNaam() As String
        Get
            Return _eenheidNaam
        End Get
        Set(ByVal value As String)
            _eenheidNaam = value
        End Set
    End Property

    Private _unitGroupTypeId As Integer?
    Public Property UnitGroupTypeId() As Integer?
        Get
            Return _unitGroupTypeId
        End Get
        Set(ByVal value As Integer?)
            _unitGroupTypeId = value
        End Set
    End Property

    Private _unitTypeId As Integer?
    Public Property UnitTypeId() As Integer?
        Get
            Return _unitTypeId
        End Get
        Set(ByVal value As Integer?)
            _unitTypeId = value
        End Set
    End Property

    Private _sortOrder As Integer
    Public Property SortOrder() As Integer
        Get
            Return _sortOrder
        End Get
        Set(ByVal value As Integer)
            _sortOrder = value
        End Set
    End Property

    Private _groupTypeName As String
    Public Property GroupTypeName() As String
        Get
            Return _groupTypeName
        End Get
        Set(ByVal value As String)
            _groupTypeName = value
        End Set
    End Property

    Private _typeName As String
    Public Property TypeName() As String
        Get
            Return _typeName
        End Get
        Set(ByVal value As String)
            _typeName = value
        End Set
    End Property

    Private _typeShortcode As String
    Public Property TypeShortcode() As String
        Get
            Return _typeShortcode
        End Get
        Set(ByVal value As String)
            _typeShortcode = value
        End Set
    End Property

    Private _bewoonbareOpp As Decimal
    <Display(Name:="Bewoonbaar")>
    Public Property BewoonbareOpp() As Decimal
        Get
            Return _bewoonbareOpp
        End Get
        Set(ByVal value As Decimal)
            _bewoonbareOpp = value
        End Set
    End Property

    Private _tuin As Decimal
    <Display(Name:="Tuin")>
    Public Property Tuin() As Decimal
        Get
            Return _tuin
        End Get
        Set(ByVal value As Decimal)
            _tuin = value
        End Set
    End Property

    Private _terrasPrefab As Decimal
    <Display(Name:="Terras prefab")>
    Public Property TerrasPrefab() As Decimal
        Get
            Return _terrasPrefab
        End Get
        Set(ByVal value As Decimal)
            _terrasPrefab = value
        End Set
    End Property

    Private _terrasGelijkvloers As Decimal
    <Display(Name:="Terras gvl.")>
    Public Property TerrasGelijkvloers() As Decimal
        Get
            Return _terrasGelijkvloers
        End Get
        Set(ByVal value As Decimal)
            _terrasGelijkvloers = value
        End Set
    End Property

    Private _dakterras As Decimal
    <Display(Name:="Dakterras")>
    Public Property Dakterras() As Decimal
        Get
            Return _dakterras
        End Get
        Set(ByVal value As Decimal)
            _dakterras = value
        End Set
    End Property

    Private _garagesParkingsBovenGr As Decimal
    <Display(Name:="Gar. bgr.")>
    Public Property GaragesParkingsBovenGr() As Decimal
        Get
            Return _garagesParkingsBovenGr
        End Get
        Set(ByVal value As Decimal)
            _garagesParkingsBovenGr = value
        End Set
    End Property

    Private _garBergOndergronds As Decimal
    <Display(Name:="Berg. ond.")>
    Public Property GarBergOndergronds() As Decimal
        Get
            Return _garBergOndergronds
        End Get
        Set(ByVal value As Decimal)
            _garBergOndergronds = value
        End Set
    End Property

    Private _bergGelijkvloers As Decimal
    <Display(Name:="Berg. gvl.")>
    Public Property BergGelijkvloers() As Decimal
        Get
            Return _bergGelijkvloers
        End Get
        Set(ByVal value As Decimal)
            _bergGelijkvloers = value
        End Set
    End Property

    Private _carports As Decimal
    <Display(Name:="Carport")>
    Public Property Carports() As Decimal
        Get
            Return _carports
        End Get
        Set(ByVal value As Decimal)
            _carports = value
        End Set
    End Property

    Private _doorritGVL As Decimal
    <Display(Name:="Doorrit")>
    Public Property DoorritGVL() As Decimal
        Get
            Return _doorritGVL
        End Get
        Set(ByVal value As Decimal)
            _doorritGVL = value
        End Set
    End Property

    Private _zolder As Decimal
    <Display(Name:="Zolder")>
    Public Property Zolder() As Decimal
        Get
            Return _zolder
        End Get
        Set(ByVal value As Decimal)
            _zolder = value
        End Set
    End Property

    Private _gemeenschappelijkeDelen As Decimal
    <Display(Name:="Gem. delen")>
    Public Property GemeenschappelijkeDelen() As Decimal
        Get
            Return _gemeenschappelijkeDelen
        End Get
        Set(ByVal value As Decimal)
            _gemeenschappelijkeDelen = value
        End Set
    End Property

    Private _wegenis As Decimal
    <Display(Name:="Wegenis")>
    Public Property Wegenis() As Decimal
        Get
            Return _wegenis
        End Get
        Set(ByVal value As Decimal)
            _wegenis = value
        End Set
    End Property

    Private _grondopp As Decimal
    <Display(Name:="Grond")>
    Public Property Grondopp() As Decimal
        Get
            Return _grondopp
        End Get
        Set(ByVal value As Decimal)
            _grondopp = value
        End Set
    End Property

    ' ── Berekende eigenschappen ───────────────────────────────────────────────

    Public ReadOnly Property OppGereduceerd() As Decimal
        Get
            Return (_bewoonbareOpp * 1.0D) +
                   (_tuin * 0.05D) +
                   (_terrasPrefab * 1.0D) +
                   (_terrasGelijkvloers * 0.1D) +
                   (_dakterras * 0.33D) +
                   (_garagesParkingsBovenGr * 0.9D) +
                   (_garBergOndergronds * 0.5D) +
                   (_bergGelijkvloers * 0.4D) +
                   (_carports * 0.3D) +
                   (_doorritGVL * 0.6D) +
                   (_zolder * 0.3D) +
                   (_gemeenschappelijkeDelen * 1.0D) +
                   (_wegenis * 0.1D)
        End Get
    End Property

    Public ReadOnly Property FormulaOppGereduceerd() As String
        Get
            Dim parts As New List(Of String)
            If _bewoonbareOpp <> 0 Then parts.Add($"{_bewoonbareOpp:F2}×1,0")
            If _tuin <> 0 Then parts.Add($"{_tuin:F2}×0,05")
            If _terrasPrefab <> 0 Then parts.Add($"{_terrasPrefab:F2}×1,0")
            If _terrasGelijkvloers <> 0 Then parts.Add($"{_terrasGelijkvloers:F2}×0,1")
            If _dakterras <> 0 Then parts.Add($"{_dakterras:F2}×0,33")
            If _garagesParkingsBovenGr <> 0 Then parts.Add($"{_garagesParkingsBovenGr:F2}×0,9")
            If _garBergOndergronds <> 0 Then parts.Add($"{_garBergOndergronds:F2}×0,5")
            If _bergGelijkvloers <> 0 Then parts.Add($"{_bergGelijkvloers:F2}×0,4")
            If _carports <> 0 Then parts.Add($"{_carports:F2}×0,3")
            If _doorritGVL <> 0 Then parts.Add($"{_doorritGVL:F2}×0,6")
            If _zolder <> 0 Then parts.Add($"{_zolder:F2}×0,3")
            If _gemeenschappelijkeDelen <> 0 Then parts.Add($"{_gemeenschappelijkeDelen:F2}×1,0")
            If _wegenis <> 0 Then parts.Add($"{_wegenis:F2}×0,1")
            If parts.Count = 0 Then Return "0,00"
            Return String.Join(" + ", parts) & $" = {OppGereduceerd:F2}"
        End Get
    End Property
End Class

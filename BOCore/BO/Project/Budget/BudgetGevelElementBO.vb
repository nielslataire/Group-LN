Imports System.ComponentModel.DataAnnotations

Public Class BudgetGevelElementBO

    Private Shared ReadOnly TypesMetHoogte As String() =
        {"GevelNieuwbouw", "GevelBestaand", "RaamNieuwbouw", "RaamBestaand"}

    Private Shared ReadOnly TypesLengteAlleen As String() =
        {"Ballustrade", "Zichtscherm"}

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

    Private _elementType As String
    <Display(Name:="Type")>
    Public Property ElementType() As String
        Get
            Return _elementType
        End Get
        Set(ByVal value As String)
            _elementType = value
        End Set
    End Property

    Private _eenheidNaam As String
    <Display(Name:="Eenheid")>
    Public Property EenheidNaam() As String
        Get
            Return _eenheidNaam
        End Get
        Set(ByVal value As String)
            _eenheidNaam = value
        End Set
    End Property

    Private _beschrijving As String
    <Display(Name:="Beschrijving")>
    Public Property Beschrijving() As String
        Get
            Return _beschrijving
        End Get
        Set(ByVal value As String)
            _beschrijving = value
        End Set
    End Property

    Private _aantal As Decimal
    <Display(Name:="Aantal")>
    Public Property Aantal() As Decimal
        Get
            Return _aantal
        End Get
        Set(ByVal value As Decimal)
            _aantal = value
        End Set
    End Property

    Private _breedte As Decimal?
    <Display(Name:="Breedte (m)")>
    Public Property Breedte() As Decimal?
        Get
            Return _breedte
        End Get
        Set(ByVal value As Decimal?)
            _breedte = value
        End Set
    End Property

    Private _hoogte As Decimal?
    <Display(Name:="Hoogte (m)")>
    Public Property Hoogte() As Decimal?
        Get
            Return _hoogte
        End Get
        Set(ByVal value As Decimal?)
            _hoogte = value
        End Set
    End Property

    Private _lengte As Decimal?
    <Display(Name:="Lengte (m)")>
    Public Property Lengte() As Decimal?
        Get
            Return _lengte
        End Get
        Set(ByVal value As Decimal?)
            _lengte = value
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

    ' ── Berekende eigenschappen ───────────────────────────────────────────────

    Public ReadOnly Property HeeftHoogte() As Boolean
        Get
            Return TypesMetHoogte.Contains(_elementType)
        End Get
    End Property

    Public ReadOnly Property HeeftLengteAlleen() As Boolean
        Get
            Return TypesLengteAlleen.Contains(_elementType)
        End Get
    End Property

    Public ReadOnly Property EenheidLabel() As String
        Get
            If HeeftLengteAlleen Then Return "lm"
            Return "m²"
        End Get
    End Property

    Public ReadOnly Property ResultaatM2() As Decimal
        Get
            If HeeftHoogte Then
                Return _aantal * If(_breedte.HasValue, _breedte.Value, 0D) *
                                 If(_hoogte.HasValue, _hoogte.Value, 0D)
            ElseIf Not HeeftLengteAlleen Then
                Return _aantal * If(_breedte.HasValue, _breedte.Value, 0D) *
                                 If(_lengte.HasValue, _lengte.Value, 0D)
            Else
                Return 0D
            End If
        End Get
    End Property

    Public ReadOnly Property ResultaatLm() As Decimal
        Get
            If HeeftLengteAlleen Then
                Return _aantal * If(_lengte.HasValue, _lengte.Value, 0D)
            Else
                Return 0D
            End If
        End Get
    End Property

    Public ReadOnly Property FormulaResultaat() As String
        Get
            Dim b As String = If(_breedte.HasValue, _breedte.Value.ToString("F3"), "0")
            Dim h As String = If(_hoogte.HasValue, _hoogte.Value.ToString("F3"), "0")
            Dim l As String = If(_lengte.HasValue, _lengte.Value.ToString("F3"), "0")
            Dim n As String = _aantal.ToString("F2")

            If HeeftHoogte Then
                Return $"{n} × {b} × {h} = {ResultaatM2:F2} m²"
            ElseIf HeeftLengteAlleen Then
                Return $"{n} × {l} = {ResultaatLm:F2} lm"
            Else
                Return $"{n} × {b} × {l} = {ResultaatM2:F2} m²"
            End If
        End Get
    End Property
End Class

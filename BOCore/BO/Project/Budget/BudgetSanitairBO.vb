Imports System.ComponentModel.DataAnnotations

Public Class BudgetSanitairBO

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
    <Display(Name:="Eenheid")>
    Public Property EenheidNaam() As String
        Get
            Return _eenheidNaam
        End Get
        Set(ByVal value As String)
            _eenheidNaam = value
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

    Private _badkamer As Integer
    <Display(Name:="Badkamer")>
    Public Property Badkamer() As Integer
        Get
            Return _badkamer
        End Get
        Set(ByVal value As Integer)
            _badkamer = value
        End Set
    End Property

    Private _toiletInBadkamer As Integer
    <Display(Name:="Toilet in BK")>
    Public Property ToiletInBadkamer() As Integer
        Get
            Return _toiletInBadkamer
        End Get
        Set(ByVal value As Integer)
            _toiletInBadkamer = value
        End Set
    End Property

    Private _afzonderlijkToilet As Integer
    <Display(Name:="Afz. toilet")>
    Public Property AfzonderlijkToilet() As Integer
        Get
            Return _afzonderlijkToilet
        End Get
        Set(ByVal value As Integer)
            _afzonderlijkToilet = value
        End Set
    End Property

    Private _doucheInBadkamer As Integer
    <Display(Name:="Douche in BK")>
    Public Property DoucheInBadkamer() As Integer
        Get
            Return _doucheInBadkamer
        End Get
        Set(ByVal value As Integer)
            _doucheInBadkamer = value
        End Set
    End Property

    Private _douchekamer As Integer
    <Display(Name:="Douchekamer")>
    Public Property Douchekamer() As Integer
        Get
            Return _douchekamer
        End Get
        Set(ByVal value As Integer)
            _douchekamer = value
        End Set
    End Property

    Public ReadOnly Property TotaalElementen() As Integer
        Get
            Return _badkamer + _toiletInBadkamer + _afzonderlijkToilet +
                   _doucheInBadkamer + _douchekamer
        End Get
    End Property

End Class

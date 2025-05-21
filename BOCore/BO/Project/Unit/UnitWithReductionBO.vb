Imports System.ComponentModel.DataAnnotations

Public Class UnitWithReductionBO
    Sub New()
        _constructionreductions = New List(Of ConstructionReductionBO)
    End Sub
    Private _base As UnitBO
    Public Property Base() As UnitBO
        Get
            Return _base
        End Get
        Set(ByVal value As UnitBO)
            _base = value
        End Set
    End Property

    Private _reductionlandvalue As Decimal
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:C}")>
    <UIHint("Currency")>
    <Display(Name:="Korting grondwaarde")>
    Public Property ReductionLandValue() As Decimal
        Get
            Return _reductionlandvalue
        End Get
        Set(ByVal value As Decimal)
            _reductionlandvalue = value
        End Set
    End Property
    Private _constructionreductions As List(Of ConstructionReductionBO)
    Public Property ConstructionReductions() As List(Of ConstructionReductionBO)
        Get
            Return _constructionreductions
        End Get
        Set(ByVal value As List(Of ConstructionReductionBO))
            _constructionreductions = value
        End Set
    End Property
End Class

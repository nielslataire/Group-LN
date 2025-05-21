Imports System.ComponentModel.DataAnnotations

Public Class ConstructionReductionBO
    Public Sub New()

    End Sub

    Private _value As Decimal
    '<DisplayFormat(DataFormatString:="{0:0,00}")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:C}")>
    <UIHint("Currency")>
    <Display(Name:="Korting bouwwwaarde")>
    Public Property Value() As Decimal
        Get
            Return _value
        End Get
        Set(ByVal value As Decimal)
            _value = value
        End Set
    End Property
    Private _constructionvalueid As Integer
    Public Property ConstructionValueId() As Integer
        Get
            Return _constructionvalueid
        End Get
        Set(ByVal value As Integer)
            _constructionvalueid = value
        End Set
    End Property
End Class

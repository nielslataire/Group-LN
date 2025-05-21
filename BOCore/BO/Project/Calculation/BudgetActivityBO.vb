Imports System.ComponentModel.DataAnnotations
Public Class BudgetActivityBO
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
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property
    Private _activity As ActivityBO
    Public Property Activity() As ActivityBO
        Get
            Return _activity
        End Get
        Set(ByVal value As ActivityBO)
            _activity = value
        End Set
    End Property
    Private _price As Decimal
    '<DisplayFormat(DataFormatString:="{0:0,00}")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:C}")>
    <UIHint("Currency")>
    <Display(Name:="Prijs")>
    Public Property Price() As Decimal
        Get
            Return _price
        End Get
        Set(ByVal value As Decimal)
            _price = value
        End Set
    End Property
End Class

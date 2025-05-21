Imports System.ComponentModel.DataAnnotations

Public Class ContractAdditionalOrderBO
    Public Sub New()
        ' _insurancedata = New InsuranceBO
    End Sub
    Private _id As Integer
    Public Property ContractActivityId() As Integer
        Get
            Return _id
        End Get
        Set(ByVal value As Integer)
            _id = value
        End Set
    End Property
    Private _activityname As String
    Public Property ActivityName() As String
        Get
            Return _activityname
        End Get
        Set(ByVal value As String)
            _activityname = value
        End Set
    End Property
    Private _description As String
    Public Property Description() As String
        Get
            Return _description
        End Get
        Set(ByVal value As String)
            _description = value
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

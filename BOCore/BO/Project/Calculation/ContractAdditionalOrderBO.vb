Imports System.ComponentModel.DataAnnotations

''' <summary>
''' Bijbestelling op een bestaand lot (ContractActivity) van een leverancierscontract.
''' Heeft een vrije omschrijving en een prijs; de prijs verhoogt de effectieve
''' lotprijs en daarmee de totale contractprijs.
''' </summary>
Public Class ContractAdditionalOrderBO
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

    Private _contractActivityId As Integer
    Public Property ContractActivityId() As Integer
        Get
            Return _contractActivityId
        End Get
        Set(ByVal value As Integer)
            _contractActivityId = value
        End Set
    End Property

    ''' <summary>Naam van het lot waar deze bijbestelling onder hangt (enkel voor weergave).</summary>
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
    <Display(Name:="Omschrijving")>
    Public Property Description() As String
        Get
            Return _description
        End Get
        Set(ByVal value As String)
            _description = value
        End Set
    End Property

    Private _price As Decimal
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

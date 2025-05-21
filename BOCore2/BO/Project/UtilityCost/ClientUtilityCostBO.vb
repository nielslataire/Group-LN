Imports System.ComponentModel.DataAnnotations
Imports System.Web.Mvc
Public Class ClientUtilityCostBO
    Public Sub New()
        _utilityCost = New List(Of UtilityCostBO)
    End Sub
    Private _clientaccountid As Integer
    Public Property ClientAccountId() As Integer
        Get
            Return _clientaccountid
        End Get
        Set(ByVal value As Integer)
            _clientaccountid = value
        End Set
    End Property
    Private _clientname As String
    Public Property Clientname() As String
        Get
            Return _clientname
        End Get
        Set(ByVal value As String)
            _clientname = value
        End Set
    End Property
    Private _utilityCost As List(Of UtilityCostBO)
    Public Property UtilityCost() As List(Of UtilityCostBO)
        Get
            Return _utilityCost
        End Get
        Set(ByVal value As List(Of UtilityCostBO))
            _utilityCost = value
        End Set
    End Property


End Class

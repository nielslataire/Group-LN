Imports System.ComponentModel.DataAnnotations
Imports System.Web.Mvc
Public Class UnitGroupTypeBO
    Public Sub New()
        _unittypes = New List(Of UnitTypeBO)
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
    Private _name As String
    <Display(Name:="Naam")>
    Public Property Name() As String
        Get
            Return _name
        End Get
        Set(ByVal value As String)
            _name = value
        End Set
    End Property
    Private _unittypes As List(Of UnitTypeBO)
    Public Property UnitTypes() As List(Of UnitTypeBO)
        Get
            Return _unittypes
        End Get
        Set(ByVal value As List(Of UnitTypeBO))
            _unittypes = value
        End Set
    End Property




End Class

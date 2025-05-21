Imports System.ComponentModel.DataAnnotations
Imports System.Web.Mvc
Public Class UnitWithAttachedUnitsBO
    Public Sub New()
        _unit = New UnitBO
        _attachedunits = New List(Of UnitBO)
    End Sub
    Private _unit As UnitBO
    Public Property Unit() As UnitBO
        Get
            Return _unit
        End Get
        Set(ByVal value As UnitBO)
            _unit = value
        End Set
    End Property
    Private _attachedunits As List(Of UnitBO)
    Public Property AttachedUnits() As List(Of UnitBO)
        Get
            Return _attachedunits
        End Get
        Set(ByVal value As List(Of UnitBO))
            _attachedunits = value
        End Set
    End Property

End Class

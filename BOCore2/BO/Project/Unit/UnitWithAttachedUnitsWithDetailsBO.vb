Imports System.ComponentModel.DataAnnotations
Imports System.Web.Mvc
Public Class UnitWithAttachedUnitsWithDetailsBO
    Public Sub New()
        _unit = New UnitWithDetailsBO
        _attachedunits = New List(Of UnitWithDetailsBO)
    End Sub
    Private _unit As UnitWithDetailsBO
    Public Property Unit() As UnitWithDetailsBO
        Get
            Return _unit
        End Get
        Set(ByVal value As UnitWithDetailsBO)
            _unit = value
        End Set
    End Property
    Private _attachedunits As List(Of UnitWithDetailsBO)
    Public Property AttachedUnits() As List(Of UnitWithDetailsBO)
        Get
            Return _attachedunits
        End Get
        Set(ByVal value As List(Of UnitWithDetailsBO))
            _attachedunits = value
        End Set
    End Property

End Class

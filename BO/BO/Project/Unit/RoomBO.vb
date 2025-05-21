Imports System.ComponentModel.DataAnnotations
Imports System.Web.Mvc
Public Class RoomBO
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
    Private _unitid As Integer
    Public Property UnitId() As Integer
        Get
            Return _unitid
        End Get
        Set(ByVal value As Integer)
            _unitid = value
        End Set
    End Property
    Private _surface As Decimal
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:F}")>
    <UIHint("Surface")>
    <Display(Name:="Oppervlakte")>
    Public Property Surface() As Decimal
        Get
            Return _surface
        End Get
        Set(ByVal value As Decimal)
            _surface = value
        End Set
    End Property
    Private _number As Integer
    <Display(Name:="Aantal")>
    Public Property Number() As Integer
        Get
            Return _number
        End Get
        Set(ByVal value As Integer)
            _number = value
        End Set
    End Property
    Private _type As RoomType
    <Display(Name:="Type")>
    Public Property Type() As RoomType
        Get
            Return _type
        End Get
        Set(ByVal value As RoomType)
            _type = value
        End Set
    End Property
    Private _remark As String
    <Display(Name:="Opmerking")>
    Public Property Remark() As String
        Get
            Return _remark
        End Get
        Set(ByVal value As String)
            _remark = value
        End Set
    End Property



End Class

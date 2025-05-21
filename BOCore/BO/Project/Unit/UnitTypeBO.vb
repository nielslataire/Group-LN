Imports System.ComponentModel.DataAnnotations

Public Class UnitTypeBO
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
    Private _shortcode As String
    <Display(Name:="Afkorting")>
    Public Property Shortcode() As String
        Get
            Return _shortcode
        End Get
        Set(ByVal value As String)
            _shortcode = value
        End Set
    End Property
    Private _groupid As Integer
    Public Property GroupId() As Integer
        Get
            Return _groupid
        End Get
        Set(ByVal value As Integer)
            _groupid = value
        End Set
    End Property

End Class

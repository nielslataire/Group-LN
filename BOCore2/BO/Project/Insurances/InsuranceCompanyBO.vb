Imports System.ComponentModel.DataAnnotations
Imports System.Web.Mvc
Public Class InsuranceCompanyBO
    Public Sub New()
        _postalcode = New PostalCodeBO
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
    Private _street As String
    <Display(Name:="Adres")>
    Public Property Street() As String
        Get
            Return _street
        End Get
        Set(ByVal value As String)
            _street = value
        End Set
    End Property
    Private _housenumber As String
    Public Property HouseNumber() As String
        Get
            Return _housenumber
        End Get
        Set(ByVal value As String)
            _housenumber = value
        End Set
    End Property
    Private _busnumber As String
    Public Property BusNumber() As String
        Get
            Return _busnumber
        End Get
        Set(ByVal value As String)
            _busnumber = value
        End Set
    End Property
    Private _addition As String
    Public Property Addition() As String
        Get
            Return _addition
        End Get
        Set(ByVal value As String)
            _addition = value
        End Set
    End Property
    Private _postalcode As PostalCodeBO
    <Display(Name:="Gemeente")>
    <DisplayFormat(HtmlEncode:=True)>
    Public Property Postalcode() As PostalCodeBO
        Get
            Return _postalcode
        End Get
        Set(ByVal value As PostalCodeBO)
            _postalcode = value
        End Set
    End Property
End Class

Imports BO
Imports System.ComponentModel.DataAnnotations
Imports System.Web.Mvc
Public Class ReferencesModel
    Public Sub New()
        _projects = New List(Of ProjectBO)

    End Sub
    Private _projects As List(Of ProjectBO)
    Public Property Projects() As List(Of ProjectBO)
        Get
            Return _projects
        End Get
        Set(ByVal value As List(Of ProjectBO))
            _projects = value
        End Set
    End Property


End Class
Public Class ReferenceDetailModel
    Public Sub New()
        _developer = New CompanyBO
        _builder = New CompanyBO
        _architect = New CompanyBO
        _data = New ProjectBO   
    End Sub
    Private _data As ProjectBO
    Public Property Data() As ProjectBO
        Get
            Return _data
        End Get
        Set(ByVal value As ProjectBO)
            _data = value
        End Set
    End Property
    Private _developer As CompanyBO
    <Display(Name:="Projectontwikkelaar")>
    Public Property Developer() As CompanyBO
        Get
            Return _developer
        End Get
        Set(ByVal value As CompanyBO)
            _developer = value
        End Set
    End Property
    Private _builder As CompanyBO
    <Display(Name:="Bouwheer")>
    Public Property Builder() As CompanyBO
        Get
            Return _builder
        End Get
        Set(ByVal value As CompanyBO)
            _builder = value
        End Set
    End Property
    Private _architect As CompanyBO
    <Display(Name:="Architect")>
    Public Property Architect() As CompanyBO
        Get
            Return _architect
        End Get
        Set(ByVal value As CompanyBO)
            _architect = value
        End Set
    End Property

End Class





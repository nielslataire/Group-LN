Imports System.ComponentModel.DataAnnotations

Public Class CompanyFilter

    Private _companyName As String
    <Display(Name:="Bedrijfsnaam")>
    Public Property CompanyName() As String
        Get
            Return _companyName
        End Get
        Set(ByVal value As String)
            _companyName = value
        End Set
    End Property
    Private _selectedActivities As List(Of Integer)
    Public Property SelectedActivities() As List(Of Integer)
        Get
            Return _selectedActivities
        End Get
        Set(ByVal value As List(Of Integer))
            _selectedActivities = value
        End Set
    End Property
    Private _activities As List(Of IdNameBO)
    Public Property Activities() As List(Of IdNameBO)
        Get
            Return _activities
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _activities = value
        End Set
    End Property

    Private _provinces As List(Of IdNameBO)
    Public Property Provinces() As List(Of IdNameBO)
        Get
            Return _provinces
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _provinces = value
        End Set
    End Property

    Private _selectedProvince As List(Of Integer)
    Public Property SelectedProvince() As List(Of Integer)
        Get
            Return _selectedProvince
        End Get
        Set(ByVal value As List(Of Integer))
            _selectedProvince = value
        End Set
    End Property
End Class

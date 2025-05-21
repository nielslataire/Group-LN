Imports BO
Imports System.ComponentModel.DataAnnotations

Public Class DepartmentModel
    Public Sub New()
        _departement = New DepartmentBO()
        _countries = New List(Of IdNameBO)
    
    End Sub

    Private _departement As DepartmentBO
    Public Property Department() As DepartmentBO
        Get
            Return _departement
        End Get
        Set(ByVal value As DepartmentBO)
            _departement = value
        End Set
    End Property
    Private _countries As List(Of IdNameBO)
    Public Property Countries() As List(Of IdNameBO)
        Get
            Return _countries
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _countries = value
        End Set
    End Property
    Private _selectedCountry As Integer
    Public Property SelectedCountry() As Integer
        Get
            Return _selectedCountry
        End Get
        Set(ByVal value As Integer)
            _selectedCountry = value
        End Set
    End Property
    Private _selectedPostalCode As Integer
    <Range(1, Integer.MaxValue, ErrorMessage:="Gelieve een gemeente te selecteren")>
    Public Property SelectedPostalcode() As Integer
        Get
            Return _selectedPostalCode
        End Get
        Set(ByVal value As Integer)
            _selectedPostalCode = value
        End Set
    End Property

End Class

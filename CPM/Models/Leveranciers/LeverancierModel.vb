Imports BO
Imports System.ComponentModel.DataAnnotations
'GENERAL
Public Class LeverancierModel

    Public Sub New()
        _company = New CompanyBO()
        _countries = New List(Of IdNameBO)
        _listactivities = New List(Of IdNameBO)
        _addedActivities = New List(Of ActivityBO)
        _addedDepartments = New List(Of DepartmentBO)
        _addedContacts = New List(Of ContactBO)
    End Sub
    <UIHint("TelefoonFormat")>
    Public ReadOnly Property FormattedTelefoon() As String
        Get
            Return Company.Telefoon1
        End Get

    End Property
    <UIHint("TelefoonFormat")>
    Public ReadOnly Property FormattedGSM() As String
        Get
            Return Company.GSM
        End Get

    End Property
    Public ReadOnly Property FormattedONDNR() As String
        Get
            Return Company.Postcode.Country.ISOCode & Company.OndNr
        End Get

    End Property

    Private _company As CompanyBO
    Public Property Company() As CompanyBO
        Get
            Return _company
        End Get
        Set(ByVal value As CompanyBO)
            _company = value
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
    Public Property SelectedPostalcode() As Integer
        Get
            Return _selectedPostalCode
        End Get
        Set(ByVal value As Integer)
            _selectedPostalCode = value
        End Set
    End Property
    Private _listactivities As List(Of IdNameBO)
    Public Property ListActivities() As List(Of IdNameBO)
        Get
            Return _listactivities
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _listactivities = value
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
    Private _addedActivities As List(Of ActivityBO)
    Public Property AddedActivities() As List(Of ActivityBO)
        Get
            Return _addedActivities
        End Get
        Set(ByVal value As List(Of ActivityBO))
            _addedActivities = value
        End Set
    End Property
    Private _addedDepartments As List(Of DepartmentBO)
    Public Property AddedDepartments() As List(Of DepartmentBO)
        Get
            Return _addedDepartments
        End Get
        Set(ByVal value As List(Of DepartmentBO))
            _addedDepartments = value
        End Set
    End Property
    Private _SelectedDepartementCountry As Integer
    Public Property SelectedDepartmentCountry() As Integer
        Get
            Return _SelectedDepartementCountry
        End Get
        Set(ByVal value As Integer)
            _SelectedDepartementCountry = value
        End Set
    End Property
    Private _SelectedDepartmentPostalcode As Integer
    Public Property SelectedDepartmentPostalcode() As Integer
        Get
            Return _SelectedDepartmentPostalcode
        End Get
        Set(ByVal value As Integer)
            _SelectedDepartmentPostalcode = value
        End Set
    End Property
    Private _addedContacts As List(Of ContactBO)
    Public Property AddedContacts() As List(Of ContactBO)
        Get
            Return _addedContacts
        End Get
        Set(ByVal value As List(Of ContactBO))
            _addedContacts = value
        End Set
    End Property




End Class
'SEARCH PAGE
Public Class LeverancierSearchModel
    Public Sub New()
        Filter = New CompanyFilter()
        Searchempty = False
    End Sub

    Private _filter As CompanyFilter
    Public Property Filter() As CompanyFilter
        Get
            Return _filter
        End Get
        Set(ByVal value As CompanyFilter)
            _filter = value
        End Set
    End Property


    Private _companies As List(Of CompanyBO)
    Public Property Companies() As List(Of CompanyBO)
        Get
            Return _companies
        End Get
        Set(ByVal value As List(Of CompanyBO))
            _companies = value
        End Set
    End Property
    Private m_searchempty As Boolean
    Public Property Searchempty() As Boolean
        Get
            Return m_searchempty
        End Get
        Set(ByVal value As Boolean)
            m_searchempty = value
        End Set
    End Property


End Class
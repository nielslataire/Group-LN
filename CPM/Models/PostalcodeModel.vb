Imports BO
Public Class PostalcodeModel
    Public Sub New()
        _countries = New List(Of IdNameBO)
    End Sub
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
    Public Property CountryId() As Integer
        Get
            Return _selectedCountry
        End Get
        Set(ByVal value As Integer)
            _selectedCountry = value
        End Set
    End Property
    Private _selectedPostalCode As Integer
    Public Property PostalCodeId() As Integer
        Get
            Return _selectedPostalCode
        End Get
        Set(ByVal value As Integer)
            _selectedPostalCode = value
        End Set
    End Property
End Class

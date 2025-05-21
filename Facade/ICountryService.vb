Imports BO
Public Interface ICountryService
    Function GetCountrys() As GetResponse(Of CountryBO)
    Function GetCountryById(id As Integer) As GetResponse(Of CountryBO)
    Function GetVisibleCountriesForSelect() As GetResponse(Of IdNameBO)
End Interface

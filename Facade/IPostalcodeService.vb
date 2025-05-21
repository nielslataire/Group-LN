Imports BO
Public Interface IPostalcodeService
    Function GetPostalcodeById(PostalcodeId As Integer) As GetResponse(Of PostalCodeBO)
    Function GetPostalcodeByCountry(CountryId As Integer) As GetResponse(Of PostalCodeBO)
    Function GetPostalcodeByCountryAndSearchstring(CountryId As Integer, Searchstring As String) As GetResponse(Of PostalCodeBO)

    Function GetPostalcodeByCountryCodeAndPostalcode(Countrycode As String, Postalcode As String) As GetResponse(Of PostalCodeBO)
End Interface

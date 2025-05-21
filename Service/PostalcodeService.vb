Imports BO
Imports DAL
Imports Facade

Public Class PostalcodeService
    Implements IPostalcodeService
    Public Function GetPostalcodeById(id As Integer) As GetResponse(Of PostalCodeBO) Implements IPostalcodeService.GetPostalcodeById
        Dim response As New GetResponse(Of PostalCodeBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetPostalcodeDAO

        Dim _entity = dao.GetById(id)
        Dim postalcode As New PostalCodeBO

        Dim err = PostalcodeTranslator.TranslateEntityToBO(_entity, postalcode)
        If err = ErrorCode.Success Then
            response.Value = postalcode
        Else
            response.AddError(err.ToString())
        End If
        Return response
    End Function
    Public Function GetPostalcodeByCountry(CountryId As Integer) As GetResponse(Of PostalCodeBO) Implements IPostalcodeService.GetPostalcodeByCountry
        Dim response As New GetResponse(Of PostalCodeBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetPostalcodeDAO()
        Dim entities = dao.GetNoTracking()
        For Each _entity In entities
            Dim bo As New PostalCodeBO()
            bo.PostcodeId = _entity.PostcodeID
            bo.Postcode = _entity.Postcode

            response.AddValue(bo)
        Next
        Return response
    End Function
    Public Function GetPostalcodeByCountryAndSearchstring(CountryId As Integer, Searchstring As String) As GetResponse(Of PostalCodeBO) Implements IPostalcodeService.GetPostalcodeByCountryAndSearchstring
        Dim response As New GetResponse(Of PostalCodeBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetPostalcodeDAO()
        Dim query = PostalcodeQuery.GetCountryQuery(CountryId)
        Dim subquery = PostalcodeQuery.GetCityContainsQuery(Searchstring)
        subquery = subquery.Or(PostalcodeQuery.GetPostalCodeStartsWithQuery(Searchstring))
        query = query.And(subquery)
        Dim entities = dao.GetNoTracking().Where(query)
        For Each _entity In entities
            Dim bo As New PostalCodeBO()
            bo.PostcodeId = _entity.PostcodeID
            bo.Postcode = _entity.Postcode
            bo.Gemeente = _entity.Gemeente

            response.AddValue(bo)
        Next
        Return response
    End Function

    Public Function GetPostalcodeByCountryCodeAndPostalcode(Countrycode As String, Postalcode As String) As GetResponse(Of PostalCodeBO) Implements IPostalcodeService.GetPostalcodeByCountryCodeAndPostalcode
        'Dim response As New GetResponse(Of PostalCodeBO)
        'Dim uow As New UnitOfWork(False)
        'Dim dao = uow.GetPostalcodeDAO()
        'Dim entities = dao.GetNoTracking.Where(Function(m) m.Country.LandISOCode = Countrycode And m.Country
        'For Each _entity In entities
        '    Dim bo As New PostalCodeBO()
        '    bo.PostcodeId = _entity.PostcodeID
        '    bo.Postcode = _entity.Postcode
        '    bo.Gemeente = _entity.Gemeente

        '    response.AddValue(bo)
        'Next
        'Return response
    End Function
End Class

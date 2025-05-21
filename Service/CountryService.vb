Imports BO
Imports DAL
Imports Facade

Public Class CountryService
    Implements ICountryService

    Public Function GetCountrys() As GetResponse(Of CountryBO) Implements ICountryService.GetCountrys
        Dim response As New GetResponse(Of CountryBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetCountryDAO()
        Dim entities = dao.GetNoTracking()
        For Each _entity In entities
            Dim bo As New CountryBO()
            bo.CountryID = _entity.ID
            bo.Name = _entity.LandNaam
            bo.ISOCode = _entity.LandISOCode

            response.AddValue(bo)
        Next
        Return response
    End Function
    Public Function GetCountryById(id As Integer) As GetResponse(Of CountryBO) Implements ICountryService.GetCountryById
        Dim response As New GetResponse(Of CountryBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetCountryDAO()
        Dim _entity = dao.GetById(id)

        Dim bo As New CountryBO()
        bo.CountryID = _entity.ID
        bo.Name = _entity.LandNaam
        bo.ISOCode = _entity.LandISOCode

        response.AddValue(bo)
        Return response
    End Function
    Public Function GetVisibleCountriesForSelect() As GetResponse(Of IdNameBO) Implements ICountryService.GetVisibleCountriesForSelect
        Dim response As New GetResponse(Of IdNameBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetCountryDAO()
        Dim entities = dao.GetNoTracking().Where(CountryQuery.GetVisibleQuery(True))
        For Each _entity In entities
            response.AddValue(_entity.GetIdName())
        Next
        Return response
    End Function
End Class

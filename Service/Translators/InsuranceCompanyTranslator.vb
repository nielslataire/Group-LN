Imports DAL
Imports BO
Imports System.Text.RegularExpressions
Public Class InsuranceCompanyTranslator

    Friend Shared Function TranslateEntityToBO(_entity As InsuranceCompanies, bo As InsuranceCompanyBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        'Algemene gegevens
        bo.Id = _entity.Id
        bo.Name = _entity.Name
        If (_entity.PostalCode IsNot Nothing) Then
            bo.Postalcode.Postcode = _entity.PostalCode.Postcode
            bo.Postalcode.Gemeente = _entity.PostalCode.Gemeente
            bo.Postalcode.PostcodeId = _entity.PostalCode.PostcodeID
            If _entity.PostalCode.Country IsNot Nothing Then
                bo.Postalcode.Country.Name = _entity.PostalCode.Country.LandNaam
                bo.Postalcode.Country.CountryID = _entity.PostalCode.Country.ID
                bo.Postalcode.Country.ISOCode = _entity.PostalCode.Country.LandISOCode
            End If
            If _entity.PostalCode.Provincie IsNot Nothing Then
                bo.Postalcode.Provincie.Name = _entity.PostalCode.Provincie.ProvincieName
                bo.Postalcode.Provincie.ProvincieId = _entity.PostalCode.Provincie.ProvincieID
            End If
        End If
        bo.Street = _entity.Straat
        bo.HouseNumber = _entity.Huisnummer
        bo.BusNumber = _entity.Busnummer
        bo.Addition = _entity.Toevoeging



        Return ErrorCode.Success
    End Function
    Friend Shared Function TranslateBOToEntity(_entity As InsuranceCompanies, bo As InsuranceCompanyBO, uow As UnitOfWork) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull

        _entity.Name = bo.Name
        _entity.Straat = bo.Street
        _entity.Huisnummer = bo.HouseNumber
        _entity.Busnummer = bo.BusNumber
        _entity.Toevoeging = bo.Addition

        If (bo.Postalcode IsNot Nothing And bo.Postalcode.PostcodeId <> 0) Then
            _entity.PostcodeID = bo.Postalcode.PostcodeId
        End If

        Return ErrorCode.Success
    End Function

End Class

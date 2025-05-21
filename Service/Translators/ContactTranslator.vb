Imports DAL
Imports BO
Imports System.Text.RegularExpressions
Public Class ContactTranslator
    Public Shared Function TranslateEntityToBO(_entity As CompanyContacts, bo As ContactBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id = _entity.ContactID
        bo.CellPhone = _entity.GSM
        bo.Email = _entity.Email
        bo.Firstname = _entity.ContactVoornaam
        bo.JobFunction = _entity.Functie
        bo.Name = _entity.ContactNaam
        bo.Phone = _entity.Telefoon
        bo.Salutation = _entity.Aanspreking
        If (_entity.CompanyInfo IsNot Nothing) Then
            bo.Company = _entity.CompanyInfo.GetIdName()
        End If
        If (_entity.CompanyDepartments IsNot Nothing) Then
            bo.Department = _entity.CompanyDepartments.GetIdName()
        End If
        Return ErrorCode.Success
    End Function

    Friend Shared Function TranslateBOToEntity(_entity As CompanyContacts, bo As ContactBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull

        If Not bo.CellPhone Is Nothing Then _entity.GSM = Regex.Replace(bo.CellPhone, "[^0-9]", "")
        _entity.Email = bo.Email
        _entity.ContactVoornaam = bo.Firstname
        _entity.Functie = bo.JobFunction
        _entity.ContactNaam = bo.Name
        If Not bo.Phone Is Nothing Then _entity.Telefoon = Regex.Replace(bo.Phone, "[^0-9]", "")
        _entity.Aanspreking = bo.Salutation

        If bo.Company IsNot Nothing AndAlso bo.Company.ID <> 0 Then
            _entity.CompanyID = bo.Company.ID
        Else
            _entity.CompanyID = Nothing
        End If
        If bo.Department IsNot Nothing AndAlso bo.Department.ID <> 0 Then
            _entity.DepartmentID = bo.Department.ID
        Else
            _entity.DepartmentID = Nothing
        End If

        Return ErrorCode.Success
    End Function

End Class

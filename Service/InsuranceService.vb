Imports BO
Imports Facade
Imports DAL
Imports System.Data.Entity


Public Class InsuranceService
    Implements IInsuranceService
    Function GetInsurancesByProjectId(projectid As Integer) As GetResponse(Of InsuranceBO) Implements IInsuranceService.GetInsurancesByProjectId

    End Function
    Function GetInsuranceById(id As Integer) As GetResponse(Of InsuranceBO) Implements IInsuranceService.GetInsuranceById
        Dim response As New GetResponse(Of InsuranceBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetInsuranceDAO()
        Dim _entity = dao.GetById(id)
        Dim bo As New InsuranceBO()
        Dim err = InsuranceTranslator.TranslateEntityToBO(_entity, bo)
        If err = ErrorCode.Success Then
            response.AddValue(bo)
        Else
            response.AddError(err.ToString())
        End If
        Return response
    End Function
    Function CheckInsurances(Optional userid As String = "") As GetResponse(Of WarningBO) Implements IInsuranceService.CheckInsurances
        Dim response As New GetResponse(Of WarningBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetInsuranceDAO()
        If userid = "" Then
            Dim entities = dao.GetNoTracking.Where(Function(m) m.Type = InsuranceType.ABR AndAlso DbFunctions.AddMonths(m.Startdate, m.Period + m.ExtensionPeriod - 1).Value < Date.Now() AndAlso Not DbFunctions.AddMonths(m.Startdate, m.Period + m.ExtensionPeriod).Value < Date.Now() AndAlso m.ContractActivity.Contract.Project.DeliveryDate Is Nothing AndAlso m.Enddate Is Nothing)
            For Each _entity In entities
                Dim bo As New WarningBO
                bo.ID = _entity.ContractActivityID
                bo.ProjectId = _entity.ContractActivity.Contract.ProjectID
                bo.Display = "De ABR polis van project " & _entity.ContractActivity.Contract.Project.ProjectName & " vervalt binnen één maand, gelieve deze te verlengen !"
                bo.Type = "warning"
                response.AddValue(bo)
            Next
        Else
            Dim entities = dao.GetNoTracking.Where(Function(m) m.Type = InsuranceType.ABR AndAlso DbFunctions.AddMonths(m.Startdate, m.Period + m.ExtensionPeriod - 1).Value < Date.Now() AndAlso Not DbFunctions.AddMonths(m.Startdate, m.Period + m.ExtensionPeriod).Value < Date.Now() AndAlso m.ContractActivity.Contract.Project.DeliveryDate Is Nothing AndAlso m.ContractActivity.Contract.Project.AspNetUserID = userid AndAlso m.Enddate Is Nothing)
            For Each _entity In entities
                Dim bo As New WarningBO
                bo.ID = _entity.ContractActivityID
                bo.ProjectId = _entity.ContractActivity.Contract.ProjectID
                bo.Display = "De ABR polis van project " & _entity.ContractActivity.Contract.Project.ProjectName & " vervalt binnen één maand, gelieve deze te verlengen !"
                bo.Type = "warning"
                response.AddValue(bo)
            Next
        End If
        If userid = "" Then
            Dim entities = dao.GetNoTracking.Where(Function(m) m.Type = InsuranceType.ABR AndAlso DbFunctions.AddMonths(m.Startdate, m.Period + m.ExtensionPeriod).Value < Date.Now() AndAlso m.ContractActivity.Contract.Project.DeliveryDate Is Nothing AndAlso m.Enddate Is Nothing)
            For Each _entity In entities
                Dim bo As New WarningBO
                bo.ID = _entity.ContractActivityID
                bo.ProjectId = _entity.ContractActivity.Contract.ProjectID
                bo.Display = "De ABR polis van project " & _entity.ContractActivity.Contract.Project.ProjectName & " is vervallen, gelieve deze te verlengen !"
                bo.Type = "danger"

                response.AddValue(bo)
            Next
        Else
            Dim entities = dao.GetNoTracking.Where(Function(m) m.Type = InsuranceType.ABR AndAlso DbFunctions.AddMonths(m.Startdate, m.Period + m.ExtensionPeriod).Value < Date.Now() AndAlso m.ContractActivity.Contract.Project.DeliveryDate Is Nothing AndAlso m.ContractActivity.Contract.Project.AspNetUserID = userid AndAlso m.Enddate Is Nothing)
            For Each _entity In entities
                Dim bo As New WarningBO
                bo.ID = _entity.ContractActivityID
                bo.ProjectId = _entity.ContractActivity.Contract.ProjectID
                bo.Display = "De ABR polis van project " & _entity.ContractActivity.Contract.Project.ProjectName & " is vervallen, gelieve deze te verlengen !"
                bo.Type = "danger"
                response.AddValue(bo)
            Next
        End If
        Return Response
    End Function
    Function InsertUpdate(bo As InsuranceBO) As Response Implements IInsuranceService.InsertUpdate
        Dim response As New Response()

        If (Not response.Success) Then Return response

        Dim uow As New UnitOfWork()
        Dim dao = uow.GetInsuranceDAO()
        Dim _entity As Insurances = Nothing

        If (bo.ContractActivityID = 0) Then
            _entity = dao.GetNew()
        Else
            _entity = dao.GetById(bo.ContractActivityID)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = InsuranceTranslator.TranslateBOToEntity(_entity, bo, uow)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("insurance not found")
        End If

        response.AddError(uow.SaveChanges())
        Return response
    End Function
    Function Delete(ids As List(Of Integer)) As Response Implements IInsuranceService.Delete
        'Dim response As New Response()
        'Dim uow As New UnitOfWork()
        'For Each id In ids
        '    uow.GetInsuranceDAO().DeleteObject(id)
        'Next
        'response.Messages.AddRange(uow.SaveChanges())
        'Return response
    End Function
    Function Delete(bos As List(Of InsuranceBO)) As Response Implements IInsuranceService.Delete
        'Return Delete(bos.Select(Function(s) s.Id).ToList())
    End Function
    Function GetInsuranceCompanies() As GetResponse(Of InsuranceCompanyBO) Implements IInsuranceService.GetInsuranceCompanies
        Dim response As New GetResponse(Of InsuranceCompanyBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetInsuranceCompaniesDAO()

        Dim entities = dao.GetNoTracking()
        For Each _entity In entities
            Dim bo As New InsuranceCompanyBO()
            Dim err = InsuranceCompanyTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function
    Function GetInsuranceCompaniesForSelect() As GetResponse(Of IdNameBO) Implements IInsuranceService.GetInsuranceCompaniesForSelect
        Dim response As New GetResponse(Of IdNameBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetInsuranceCompaniesDAO()

        Dim entities = dao.GetNoTracking()
        For Each _entity In entities
            response.AddValue(_entity.GetIdName())
        Next
        response.Values = response.Values.OrderBy(Function(m) m.Display).ToList
        Return response
    End Function
End Class

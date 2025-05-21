Imports BO
Imports DAL
Imports Facade
Imports System.Linq.Expressions

Public Class CompanyService
    Implements ICompanyService

    Public Function GetCompanyByID(id As Integer) As GetResponse(Of CompanyBO) Implements ICompanyService.GetCompanyByID
        Dim response As New GetResponse(Of CompanyBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetCompanyInfoDAO()

        Dim _entity = dao.GetById(id)
        Dim company As New CompanyBO

        Dim err = CompanyTranslator.TranslateEntityToBO(_entity, company)
        If err = ErrorCode.Success Then
            response.Value = company
        Else
            response.AddError(err.ToString())
        End If
        Return response
    End Function
    Public Function GetCompanyForSelectByActivity(actid As Integer) As GetResponse(Of IdNameBO) Implements ICompanyService.GetCompanyForSelectByActivity
        Dim response As New GetResponse(Of IdNameBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetCompanyInfoDAO()
        Dim entities = dao.GetNoTracking.Where(Function(m) m.Activity.Any(Function(i) i.ActivityID = actid))
        For Each _entity In entities
            response.AddValue(_entity.GetIdName())
        Next
        response.Values = response.Values.OrderBy(Function(m) m.Display).ToList
        Return response
    End Function

    Public Function GetCompanyBySearchfilter(filter As CompanyFilter) As GetResponse(Of CompanyBO) Implements ICompanyService.GetCompanyBySearchfilter
        Dim response As New GetResponse(Of CompanyBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetCompanyInfoDAO()
        Dim entitys = dao.GetNoTracking().Where(CompanyQuery.GetFilterQeury(filter)).OrderBy(Function(m) m.BedrijfsNaam)

        For Each _entity In entitys
            Dim company As New CompanyBO
            Dim err = CompanyTranslator.TranslateEntityToBO(_entity, company)
            If err = ErrorCode.Success Then
                response.AddValue(company)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function
    Public Function GetCompanyNameById(id As Integer) As String Implements ICompanyService.GetCompanyNameById
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetCompanyInfoDAO()
        Dim _entity = dao.GetById(id)
        Return _entity.BedrijfsNaam
    End Function
    Public Function GetCompanyForSearchList(searchterm As String) As GetResponse(Of SelectBO) Implements ICompanyService.GetCompanyForSearchList
        Dim response As New GetResponse(Of SelectBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetCompanyInfoDAO()
        Dim entitys = dao.GetNoTracking().Where(CompanyQuery.GetNameQuery(searchterm)).OrderBy(Function(m) m.BedrijfsNaam).Select(Function(m) New SelectBO With {.id = m.CompanyID, .text = m.BedrijfsNaam, .extra = "Company"})
        response.Values = entitys.ToList()
        'For Each _entity In entitys
        '    response.AddValue(_entity.GetIdNameForSearch())
        'Next
        Return response
    End Function



    Public Function InsertUpdate(company As CompanyBO) As Response Implements ICompanyService.InsertUpdate
        Dim response As New Response
        If (String.IsNullOrWhiteSpace(company.Bedrijfsnaam)) Then
            response.AddError("companyname is mandatory")
        End If
        If (Not response.Success) Then Return response

        Dim uow As New UnitOfWork()
        Dim _entity As CompanyInfo = Nothing
        If (company.CompanyId = 0) Then
            _entity = uow.GetCompanyInfoDAO().GetNew()
        Else
            _entity = uow.GetCompanyInfoDAO().GetById(company.CompanyId)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = CompanyTranslator.TranslateBOToEntity(_entity, company, uow)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("company not found")
        End If

        response.AddError(uow.SaveChanges())
        Return response
    End Function
    Function Delete(ids As List(Of Integer)) As Response Implements ICompanyService.Delete
        Dim response As New Response()
        Dim uow As New UnitOfWork()

        For Each id In ids
            uow.GetCompanyInfoDAO().DeleteObject(id)
        Next
        response.Messages.AddRange(uow.SaveChanges())

        Return response
    End Function
    Function Delete(bos As List(Of CompanyBO)) As Response Implements ICompanyService.Delete
        Return Delete(bos.Select(Function(s) s.CompanyId).ToList())
    End Function
    Public Function GetCompanyActivities(companyid As Integer) As GetResponse(Of ActivityBO) Implements ICompanyService.GetCompanyActivities
        Dim uow As New UnitOfWork(False)
        Dim response As New GetResponse(Of ActivityBO)
        Dim dao = uow.GetCompanyInfoDAO()
        Dim _entity = dao.GetById(companyid)
        Dim activities As New List(Of ActivityBO)
        For Each act In _entity.Activity
            Dim activity As New ActivityBO
            Dim err = ActivityTranslator.TranslateEntityToBO(act, activity)
            If err = ErrorCode.Success Then
                response.AddValue(activity)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function

    Public Function AddCompanyActivity(companyid As Integer, activityid As Integer) As Response Implements ICompanyService.AddCompanyActivity
        Dim response As New Response
        Dim uow = New UnitOfWork()
        Dim company = uow.GetCompanyInfoDAO().GetById(companyid)
        If (company Is Nothing) Then
            response.AddError("company not found")
            Return response
        End If
        Dim acitvity = uow.GetActivityDAO().GetById(activityid)
        If (acitvity Is Nothing) Then
            response.AddError("activity not found")
            Return response
        End If
        company.Activity.Add(acitvity)
        response.AddError(uow.SaveChanges())
        Return response
    End Function
    Public Function DeleteCompanyActivity(companyid As Integer, activityid As Integer) As Response Implements ICompanyService.DeleteCompanyActivity
        Dim response As New Response
        Dim uow = New UnitOfWork()
        Dim company = uow.GetCompanyInfoDAO().GetById(companyid)
        If (company Is Nothing) Then
            response.AddError("company not found")
            Return response
        End If
        Dim acitvity = uow.GetActivityDAO().GetById(activityid)
        If (acitvity Is Nothing) Then
            response.AddError("activity not found")
            Return response
        End If
        company.Activity.Remove(acitvity)
        response.AddError(uow.SaveChanges())
        Return response
    End Function

End Class

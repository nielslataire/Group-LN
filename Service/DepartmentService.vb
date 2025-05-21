Imports BO
Imports Facade
Imports DAL

Public Class DepartmentService
    Implements IDepartmentService

    Function GetDepartments() As GetResponse(Of DepartmentBO) Implements IDepartmentService.GetDepartments
        Dim response As New GetResponse(Of DepartmentBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetDepartmentDAO()

        Dim entities = dao.GetNoTracking()
        For Each _entity In entities
            Dim bo As New DepartmentBO()
            Dim err = DepartmentTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function

    Function GetDepartmentsForSelect() As GetResponse(Of IdNameBO) Implements IDepartmentService.GetDepartmentsForSelect
        Dim response As New GetResponse(Of IdNameBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetDepartmentDAO()

        Dim entities = dao.GetNoTracking()
        For Each _entity In entities
            response.AddValue(_entity.GetIdName())
        Next
        Return response
    End Function

    Function GetDepartmentById(id As Integer) As GetResponse(Of DepartmentBO) Implements IDepartmentService.GetDepartmentById
        Dim response As New GetResponse(Of DepartmentBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetDepartmentDAO()
        Dim _entity = dao.GetById(id)
        Dim bo As New DepartmentBO()
        Dim err = DepartmentTranslator.TranslateEntityToBO(_entity, bo)
        If err = ErrorCode.Success Then
            response.AddValue(bo)
        Else
            response.AddError(err.ToString())
        End If

        Return response
    End Function
    Function GetDepartmentByIds(IdList As List(Of Integer)) As GetResponse(Of DepartmentBO) Implements IDepartmentService.GetDepartmentByIds
        Dim response As New GetResponse(Of DepartmentBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetDepartmentDAO()

        For Each id In IdList
            Dim _entity = dao.GetById(id)
            Dim bo As New DepartmentBO()
            Dim err = DepartmentTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function

    Function InsertUpdate(bo As DepartmentBO) As Response Implements IDepartmentService.InsertUpdate
        Dim response As New Response()
        If (String.IsNullOrWhiteSpace(bo.Name)) Then
            response.AddError("name is mandatory")
        End If
        If (Not response.Success) Then Return response

        Dim uow As New UnitOfWork()
        Dim dao = uow.GetDepartmentDAO()
        Dim _entity As CompanyDepartments = Nothing

        If (bo.ID = 0) Then
            _entity = dao.GetNew()
        Else
            _entity = dao.GetById(bo.ID)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = DepartmentTranslator.TranslateBOToEntity(_entity, bo)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("department not found")
        End If

        response.AddError(uow.SaveChanges())
        Return response
    End Function

    Function Delete(ids As List(Of Integer)) As Response Implements IDepartmentService.Delete
        Dim response As New Response()
        Dim uow As New UnitOfWork()

        For Each id In ids
            uow.GetDepartmentDAO().DeleteObject(id)
        Next
        response.Messages.AddRange(uow.SaveChanges())

        Return response
    End Function
  

    Function Delete(bos As List(Of DepartmentBO)) As Response Implements IDepartmentService.Delete
        Return Delete(bos.Select(Function(s) s.ID).ToList())
    End Function

End Class

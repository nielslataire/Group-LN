Imports DAL
Imports BO
Imports Facade

Public Class ProvinceService
    Implements IProvinceService


    Public Function GetProvinces() As GetResponse(Of IdNameBO) Implements IProvinceService.GetProvinces
        Dim response As New GetResponse(Of IdNameBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetProvinceDAO()
        Dim entities = dao.GetNoTracking()
        For Each _entity In entities
            response.AddValue(_entity.GetIdName())
        Next
        Return response
    End Function
End Class

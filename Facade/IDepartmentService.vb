Imports BO

Public Interface IDepartmentService
    Function GetDepartments() As GetResponse(Of DepartmentBO)
    Function GetDepartmentsForSelect() As GetResponse(Of IdNameBO)
    Function GetDepartmentById(id As Integer) As GetResponse(Of DepartmentBO)
    Function GetDepartmentByIds(IdList As List(Of Integer)) As GetResponse(Of DepartmentBO)
    Function InsertUpdate(bo As DepartmentBO) As Response
    Function Delete(ids As List(Of Integer)) As Response
    Function Delete(bos As List(Of DepartmentBO)) As Response
End Interface

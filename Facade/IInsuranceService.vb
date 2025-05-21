Imports BO
Public Interface IInsuranceService
    Function GetInsurancesByProjectId(projectid As Integer) As GetResponse(Of InsuranceBO)
    Function GetInsuranceById(id As Integer) As GetResponse(Of InsuranceBO)
    Function CheckInsurances(Optional userid As String = "") As GetResponse(Of WarningBO)
    Function InsertUpdate(bo As InsuranceBO) As Response
    Function Delete(ids As List(Of Integer)) As Response
    Function Delete(bos As List(Of InsuranceBO)) As Response
    Function GetInsuranceCompanies() As GetResponse(Of InsuranceCompanyBO)
    Function GetInsuranceCompaniesForSelect() As GetResponse(Of IdNameBO)

End Interface

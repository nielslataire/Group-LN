Imports BO
Public Interface ICompanyService
    Function GetCompanyByID(id As Integer) As GetResponse(Of CompanyBO)
    Function GetCompanyForSelectByActivity(actid As Integer) As GetResponse(Of IdNameBO)
    Function GetCompanyBySearchfilter(filter As CompanyFilter) As GetResponse(Of CompanyBO)
    Function GetCompanyNameById(id As Integer) As String
    Function GetCompanyForSearchList(searchterm As String) As GetResponse(Of SelectBO)
    Function InsertUpdate(company As CompanyBO) As Response
    Function Delete(ids As List(Of Integer)) As Response
    Function Delete(bos As List(Of CompanyBO)) As Response

    'COMPANY ACTIVITIES
    Function GetCompanyActivities(companyid As Integer) As GetResponse(Of ActivityBO)
    Function AddCompanyActivity(companyid As Integer, activityid As Integer) As Response
    Function DeleteCompanyActivity(companyid As Integer, activityid As Integer) As Response




End Interface

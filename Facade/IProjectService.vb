Imports BO
Public Interface IProjectService
    Function GetProjectByID(id As Integer) As GetResponse(Of ProjectBO)
    Function GetProjectBySlug(slug As String) As GetResponse(Of ProjectBO)
    Function GetProjects() As GetResponse(Of ProjectBO)
    Function GetProjectsForList(Optional Type As ProjectType = 0, Optional StatusId As Integer = 0, Optional UserId As String = Nothing, Optional BuilderId As Integer = 0, Optional TrimCommercialText As Boolean = False) As GetResponse(Of ProjectBO)
    Function GetProjectNameById(id As Integer) As String
    Function GetProjectCityById(id As Integer) As String
    Function GetProjectSlugById(id As Integer) As String
    Function GetProjectLandshareById(id As Integer) As Integer
    Function GetProjectWeatherstation(projectid As Integer) As Integer
    Function GetProjectsForSearchList(searchterm As String) As GetResponse(Of SelectBO)
    Function GetProjectsWithAvailableUnits() As GetResponse(Of IdNameBO)
    Function GetProjectStartDateConstruction(projectid As Integer) As Date
    Function GetProjectExecutionDays(projectid As Integer) As Integer
    Function GetWorkingDaysLeft(finalconstructiondate As Date, projectid As Integer) As Integer
    Function GetFinalConstructionDay(projectid As Integer, startdate As Date, executiondays As Integer) As Date
    Function GetProjectFolderById(id As Integer) As String
    Function CheckProjectFinished(Optional userid As String = "") As GetResponse(Of WarningBO)
    Function InsertUpdate(project As ProjectBO) As Response
    Function Delete(ids As List(Of Integer)) As Response
    Function Delete(bos As List(Of ProjectBO)) As Response

    'Wheaterstations
    Function GetWheaterstations() As GetResponse(Of WheaterStationBO)
    Function GetWheaterstationsSelect() As GetResponse(Of IdNameBO)
    Function GetWheaterstations(searchterm As String) As GetResponse(Of WheaterStationBO)

    'BadWeatherDays
    Function GetBadWeatherDays(weatherstationid As Integer, type As Integer) As GetResponse(Of BadWeatherDayBO)
    Function GetBadWeatherDays(weatherstationid As Integer, type As Integer, year As Integer) As GetResponse(Of BadWeatherDayBO)
    Function GetClientWeatherDays(weatherstationid As Integer, startdate As Date, enddate As Date) As GetResponse(Of BadWeatherDayBO)
    Function InsertUpdateBadWeatherDay(BWD As BadWeatherDayBO) As Response
    Function DeleteBadWeatherDays(ids As List(Of Integer)) As Response
    Function DeleteBadWeatherDays(bos As List(Of BadWeatherDayBO)) As Response

    'VacationDays
    Function GetVacationDays() As GetResponse(Of VacationDayBO)
    Function GetVacationDaysGeneral() As GetResponse(Of VacationDayBO)
    Function GetProjectVacationDays(projectid As Integer) As GetResponse(Of VacationDayBO)
    Function InsertUpdateVacationDay(vacationday As VacationDayBO) As Response
    Function DeleteVacationDays(ids As List(Of Integer)) As Response
    Function DeleteVacationDays(bos As List(Of VacationDayBO)) As Response

    'Statuses
    Function GetStatuses() As GetResponse(Of ProjectStatusBO)
    Function GetStatusesForSelect() As GetResponse(Of IdNameBO)

    'Pictures
    Function GetPictureById(id As Integer) As GetResponse(Of ProjectPictureBO)
    Function GetPicturesByProjectId(id As Integer) As GetResponse(Of ProjectPictureBO)
    Function GetPicturesByProjectSlug(slug As String) As GetResponse(Of ProjectPictureBO)
    Function GetLatestPictures(number As Integer) As GetResponse(Of ProjectPictureBO)
    Function GetLatestProjectPictures(number As Integer, projectid As Integer) As GetResponse(Of ProjectPictureBO)
    Function InsertUpdatePicture(picture As ProjectPictureBO) As Response
    Function DeletePicture(ids As List(Of Integer)) As Response
    Function SetDefaultProjectPicture(projectid As Integer, pictureid As Integer) As Response
    Function GetFacebookAlbumIdCoproByProjectId(id As Integer) As String

    'News
    Function GetNewsById(id As Integer) As GetResponse(Of ProjectNewsBO)
    Function GetNewsByProjectId(id As Integer) As GetResponse(Of ProjectNewsBO)
    Function GetNewsByProjectSlug(slug As String) As GetResponse(Of ProjectNewsBO)
    Function GetLatestNews(number As Integer, Optional BuilderId As Integer = 0) As GetResponse(Of ProjectNewsBO)
    Function GetLatestProjectNews(number As Integer, projectid As Integer) As GetResponse(Of ProjectNewsBO)
    Function InsertUpdateNews(NewsItem As ProjectNewsBO) As Response
    Function DeleteNews(ids As List(Of Integer)) As Response


    'Levels
    Function GetLevelsByProjectId(id As Integer) As GetResponse(Of ProjectLevelBO)
    Function InsertUpdateLevel(Level As ProjectLevelBO) As Response

    'Sales
    Function GetSalesSettings(projectid As Integer) As GetResponse(Of ProjectSalesSettingsBO)
    Function GetSalesSettings(ids As List(Of Integer)) As GetResponse(Of ProjectSalesSettingsBO)
    Function GetProjectSalesData(ids As List(Of Integer)) As GetResponse(Of ProjectSalesDataBO)
    Function InsertUpdateSalesSettings(salessettings As ProjectSalesSettingsBO) As Response
    Function DeleteSalesSettings(ids As List(Of Integer)) As Response
    Function DeleteSalesSettings(bos As List(Of ProjectSalesSettingsBO)) As Response
    Function GetProjectVatPercentage(projectid As Integer) As Integer

    'Docs
    Function GetProjectDocs(projectid As Integer, Optional type As ProjectDocType = 0) As GetResponse(Of ProjectDocBO)
    Function GetProjectDocsForSelect(projectid As Integer, Optional type As ProjectDocType = ProjectDocType.Sales) As GetResponse(Of IdNameBO)
    Function GetProjectDoc(docid As Integer) As GetResponse(Of ProjectDocBO)
    Function GetClientDocs(clientaccountid As Integer) As GetResponse(Of ProjectDocBO)
    Function GetLatestProjectDocs(number As Integer, projectid As Integer) As GetResponse(Of ProjectDocBO)
    Function GetLatestClientDocs(number As Integer, clientaccountid As Integer) As GetResponse(Of ProjectDocBO)
    Function InsertUpdateProjectDoc(ProjectDoc As ProjectDocBO) As Response
    Function DeleteProjectDoc(ids As List(Of Integer)) As Response
    Function DeleteProjectDoc(bos As List(Of ProjectDocBO)) As Response

    'PaymentGroups
    Function GetProjectPaymentGroups(projectid As Integer) As GetResponse(Of ProjectPaymentGroupBO)
    Function GetProjectPaymentGroup(groupid As Integer) As GetResponse(Of ProjectPaymentGroupBO)
    Function GetProjectPaymentGroupsForSelect(projectid As Integer) As GetResponse(Of IdNameBO)
    Function InsertUpdateProjectPaymentGroup(ProjectPaymentGroup As ProjectPaymentGroupBO) As Response
    Sub LinkPaymentGroupToUnit(unitid As Integer, paymentgroupid As Integer)
    Function DeleteProjectPaymentGroup(ids As List(Of Integer)) As Response
    Function DeleteProjectPaymentGroup(bos As List(Of ProjectPaymentGroupBO)) As Response
    'PaymentStages
    Function GetProjectPaymentStages(groupid As Integer) As GetResponse(Of ProjectPaymentStageBO)
    Function GetProjectPaymentStage(stageid As Integer) As GetResponse(Of ProjectPaymentStageBO)
    Function GetProjectInvoicableUnits(projectid As Integer) As GetResponse(Of UnitWithStagesBO)
    Function InsertUpdateProjectPaymentStage(ProjectPaymentStage As ProjectPaymentStageBO) As Response
    Function UpdateProjectPaymentStageInvoicable(stageid As Integer, invoicable As Boolean) As Response
    Function DeleteProjectPaymentStage(ids As List(Of Integer)) As Response
    Function DeleteProjectPaymentStage(bos As List(Of ProjectPaymentStageBO)) As Response
    Function CheckProjectPaymentStageDocInUse(docid As Integer) As Boolean

    'Invoicing
    Function GetProjectInvoicableChangeOrders(projectid As Integer) As GetResponse(Of ChangeOrderBO)
    Function GetProjectInvoiceableLandValue(projectid As Integer) As GetResponse(Of UnitBO)
    Function GetInvoicesByUnitIds(UnitIds As List(Of Integer)) As GetResponse(Of InvoiceBO)
    Function GetProjectUtilityCost(projectid As Integer, clientid As Integer) As GetResponse(Of UtilityCostBO)
    Function InsertUpdateProjectInvoice(Invoice As InvoiceBO) As Response
    Function InsertUpdateProjectInvoices(Invoices As List(Of InvoiceBO)) As Response

    'Contracts
    Function GetProjectContracts(projectid As Integer) As GetResponse(Of ContractBO)
    Function GetContract(contractid As Integer) As GetResponse(Of ContractBO)
    Function GetProjectContractsForSelect(projectid As Integer) As GetResponse(Of IdNameBO)
    Function GetProjectContractActivitiesForSelect(projectid As Integer) As GetResponse(Of IdNameBO)
    Function InsertUpdateProjectContract(Contract As ContractBO) As Response
    Function DeleteContracts(ids As List(Of Integer)) As Response
    Function GetContractChangeOrdersForSelect(contractid As Integer) As GetResponse(Of IdNameBO)
    Function GetProjectContractsWithoutInvoices(projectid As Integer, Optional activityid As Integer = 0) As GetResponse(Of ContractBO)
    Function GetContractActivityPrice(contractid As Integer) As Decimal
    Function GetProjectContractActivitiesByActivityId(projectid As Integer, activityid As Integer) As GetResponse(Of ContractActivityBO)


    'Budget
    Function GetProjectBudget(projectid As Integer) As GetResponse(Of BudgetActivityBO)
    Function InsertUpdateProjectBudgetActivity(BudgetActivity As BudgetActivityBO) As Response
    Function InsertUpdateProjectBudgetActivities(BudgetActivity As List(Of BudgetActivityBO), projectid As Integer) As Response

    'Change Orders 
    Function GetProjectChangeOrders(projectid As Integer) As GetResponse(Of ChangeOrderBO)
    Function GetClientChangeOrders(clientaccountid As Integer) As GetResponse(Of ChangeOrderBO)
    Function GetChangeOrder(changeorderid As Integer) As GetResponse(Of ChangeOrderBO)
    Function InsertUpdateProjectChangeOrder(changeorder As ChangeOrderBO) As Response
    Function InsertUpdateProjectChangeOrders(changeorders As List(Of ChangeOrderBO), projectid As Integer) As Response
    Function DeleteChangeOrders(ids As List(Of Integer)) As Response
    Function UpdateProjectChangeOrderInvoicable(stageid As Integer, invoicable As Boolean) As Response
    Function UpdateProjectChangeOrderDetailInvoicable(stageid As Integer, invoicable As Boolean) As Response
    Function SetChangeOrderDetailInvoiced(codid As Integer) As Response

    'Incomming Invoices
    Function GetIncommingInvoice(invoiceid As Integer) As GetResponse(Of IncommingInvoiceBO)
    Function InsertUpdateProjectIncommingInvoice(invoice As IncommingInvoiceBO) As Response
    Function GetProjectIncommingInvoicesForRecalculation(projectid As Integer) As GetResponse(Of IncommingInvoiceActivityBO)
    Function GetProjectIncommingInvoicesByActivity(projectid As Integer, activityid As Integer) As GetResponse(Of IncommingInvoiceActivityBO)
    Function DeleteIncommingInvoices(ids As List(Of Integer)) As Response

    'Insurance
    Function GetProjectInsurances(projectid As Integer) As GetResponse(Of InsuranceBO)


    Function Copyids() As Response
End Interface

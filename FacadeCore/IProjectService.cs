using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;

namespace FacadeCore
{
    public interface IProjectService
    {
        GetResponse<ProjectBO> GetProjectByID(int id);
        GetResponse<ProjectBO> GetProjectBySlug(string slug);
        //GetResponse<ProjectBO> GetProjects();
        GetResponse<ProjectBO> GetProjectsForList(ProjectType Type = 0, int StatusId = 0, string UserId = null, int BuilderId = 0, bool TrimCommercialText = false);
        string GetProjectNameById(int id);
        string GetProjectCityById(int id);
        string GetProjectSlugById(int id);
        decimal GetProjectLandshareById(int id);
        int GetProjectWeatherstation(int projectid);
        GetResponse<SelectBO> GetProjectsForSearchList(string searchterm);
        GetResponse<IdNameBO> GetProjectsWithAvailableUnits();
        DateOnly GetProjectStartDateConstruction(int projectid);
        int GetProjectExecutionDays(int projectid);
        int GetWorkingDaysLeft(DateOnly finalconstructiondate, int projectid);
        DateOnly GetFinalConstructionDay(int projectid, DateOnly startdate, int executiondays);
        string GetProjectFolderById(int id);
        GetResponse<WarningBO> CheckProjectFinished(string userid = "");
        Response InsertUpdate(ProjectBO project);
        Response Delete(List<int> ids);
        Response Delete(List<ProjectBO> bos);

        // Wheaterstations
        //GetResponse<WheaterStationBO> GetWheaterstations();
        GetResponse<IdNameBO> GetWheaterstationsSelect();
        GetResponse<WheaterStationBO> GetWheaterstations(string searchterm);

        // BadWeatherDays
        GetResponse<BadWeatherDayBO> GetBadWeatherDays(int weatherstationid, int type);
        GetResponse<BadWeatherDayBO> GetBadWeatherDays(int weatherstationid, int type, int year);
        GetResponse<BadWeatherDayBO> GetClientWeatherDays(int weatherstationid, DateTime startdate, DateTime enddate);
        Response InsertUpdateBadWeatherDay(BadWeatherDayBO BWD);
        Response DeleteBadWeatherDays(List<int> ids);
        Response DeleteBadWeatherDays(List<BadWeatherDayBO> bos);

        // VacationDays
        GetResponse<VacationDayBO> GetVacationDays();
        GetResponse<VacationDayBO> GetVacationDaysGeneral();
        GetResponse<VacationDayBO> GetProjectVacationDays(int projectid);
        Response InsertUpdateVacationDay(VacationDayBO vacationday);
        Response DeleteVacationDays(List<int> ids);
        Response DeleteVacationDays(List<VacationDayBO> bos);

        // Statuses
        GetResponse<ProjectStatusBO> GetStatuses();
        GetResponse<IdNameBO> GetStatusesForSelect();

        // Pictures
        GetResponse<ProjectPictureBO> GetPictureById(int id);
        GetResponse<ProjectPictureBO> GetPicturesByProjectId(int id);
        GetResponse<ProjectPictureBO> GetPicturesByProjectSlug(string slug);
        GetResponse<ProjectPictureBO> GetLatestPictures(int number);
        GetResponse<ProjectPictureBO> GetLatestProjectPictures(int number, int projectid);
        Response InsertUpdatePicture(ProjectPictureBO picture);
        Response DeletePicture(List<int> ids);
        Response SetDefaultProjectPicture(int projectid, int pictureid);
        string GetFacebookAlbumIdCoproByProjectId(int id);

        // News
        GetResponse<ProjectNewsBO> GetNewsById(int id);
        GetResponse<ProjectNewsBO> GetNewsByProjectId(int id);
        GetResponse<ProjectNewsBO> GetNewsByProjectSlug(string slug);
        GetResponse<ProjectNewsBO> GetLatestNews(int number, int BuilderId = 0);
        GetResponse<ProjectNewsBO> GetLatestProjectNews(int number, int projectid);
        Response InsertUpdateNews(ProjectNewsBO NewsItem);
        Response DeleteNews(List<int> ids);


        // Levels
        GetResponse<ProjectLevelBO> GetLevelsByProjectId(int id);
        Response InsertUpdateLevel(ProjectLevelBO Level);

        // Sales
        GetResponse<ProjectSalesSettingsBO> GetSalesSettings(int projectid);
        GetResponse<ProjectSalesSettingsBO> GetSalesSettings(List<int> ids);
        GetResponse<ProjectSalesDataBO> GetProjectSalesData(List<int> ids);
        Response InsertUpdateSalesSettings(ProjectSalesSettingsBO salessettings);
        Response DeleteSalesSettings(List<int> ids);
        Response DeleteSalesSettings(List<ProjectSalesSettingsBO> bos);
        //int GetProjectVatPercentage(int projectid);

        // Docs
        GetResponse<ProjectDocBO> GetProjectDocs(int projectid, ProjectDocType type = 0);
        GetResponse<IdNameBO> GetProjectDocsForSelect(int projectid, ProjectDocType type = ProjectDocType.Sales);
        GetResponse<ProjectDocBO> GetProjectDoc(int docid);
        GetResponse<ProjectDocBO> GetClientDocs(int clientaccountid);
        GetResponse<ProjectDocBO> GetLatestProjectDocs(int number, int projectid);
        GetResponse<ProjectDocBO> GetLatestClientDocs(int number, int clientaccountid);
        Response InsertUpdateProjectDoc(ProjectDocBO ProjectDoc);
        Response DeleteProjectDoc(List<int> ids);
        Response DeleteProjectDoc(List<ProjectDocBO> bos);

        // PaymentGroups
        GetResponse<ProjectPaymentGroupBO> GetProjectPaymentGroups(int projectid);
        GetResponse<ProjectPaymentGroupBO> GetProjectPaymentGroup(int groupid);
        GetResponse<IdNameBO> GetProjectPaymentGroupsForSelect(int projectid);
        Response InsertUpdateProjectPaymentGroup(ProjectPaymentGroupBO ProjectPaymentGroup);
        void LinkPaymentGroupToUnit(int unitid, int paymentgroupid);
        Response DeleteProjectPaymentGroup(List<int> ids);
        Response DeleteProjectPaymentGroup(List<ProjectPaymentGroupBO> bos);
        // PaymentStages
        //GetResponse<ProjectPaymentStageBO> GetProjectPaymentStages(int groupid);
        GetResponse<ProjectPaymentStageBO> GetProjectPaymentStage(int stageid);
        GetResponse<UnitWithStagesBO> GetProjectInvoicableUnits(int projectid);
        Response InsertUpdateProjectPaymentStage(ProjectPaymentStageBO ProjectPaymentStage);
        Response UpdateProjectPaymentStageInvoicable(int stageid, bool invoicable);
        //Response DeleteProjectPaymentStage(List<int> ids);
        //Response DeleteProjectPaymentStage(List<ProjectPaymentStageBO> bos);
        bool CheckProjectPaymentStageDocInUse(int docid);

        // Invoicing
        GetResponse<ChangeOrderBO> GetProjectInvoicableChangeOrders(int projectid);
        //GetResponse<UnitBO> GetProjectInvoiceableLandValue(int projectid);
        GetResponse<InvoiceBO> GetInvoicesByUnitIds(List<int> UnitIds);
        GetResponse<UtilityCostBO> GetProjectUtilityCost(int projectid, int clientid);
        Response InsertUpdateProjectInvoice(InvoiceBO Invoice);
        Response InsertUpdateProjectInvoices(List<InvoiceBO> Invoices);

        // Contracts
        GetResponse<ContractBO> GetProjectContracts(int projectid);
        GetResponse<ContractBO> GetContract(int contractid);
        GetResponse<IdNameBO> GetProjectContractsForSelect(int projectid);
        GetResponse<IdNameBO> GetProjectContractActivitiesForSelect(int projectid);
        Response InsertUpdateProjectContract(ContractBO Contract);
        Response DeleteContracts(List<int> ids);
        GetResponse<IdNameBO> GetContractChangeOrdersForSelect(int contractid);
        GetResponse<ContractBO> GetProjectContractsWithoutInvoices(int projectid, int activityid = 0);
        decimal GetContractActivityPrice(int contractid);
        GetResponse<ContractActivityBO> GetProjectContractActivitiesByActivityId(int projectid, int activityid);


        // Budget
        GetResponse<BudgetActivityBO> GetProjectBudget(int projectid);
        Response InsertUpdateProjectBudgetActivity(BudgetActivityBO BudgetActivity);
        Response InsertUpdateProjectBudgetActivities(List<BudgetActivityBO> BudgetActivity, int projectid);

        // Change Orders 
        GetResponse<ChangeOrderBO> GetProjectChangeOrders(int projectid);
        GetResponse<ChangeOrderBO> GetClientChangeOrders(int number,int clientaccountid);
        GetResponse<ChangeOrderBO> GetChangeOrder(int changeorderid);
        Response InsertUpdateProjectChangeOrder(ChangeOrderBO changeorder);
        //Response InsertUpdateProjectChangeOrders(List<ChangeOrderBO> changeorders, int projectid);
        Response DeleteChangeOrders(List<int> ids);
        Response UpdateProjectChangeOrderInvoicable(int stageid, bool invoicable);
        Response UpdateProjectChangeOrderDetailInvoicable(int stageid, bool invoicable);
        Response SetChangeOrderDetailInvoiced(int codid);

        // Incomming Invoices
        GetResponse<IncommingInvoiceBO> GetIncommingInvoice(int invoiceid);
        Response InsertUpdateProjectIncommingInvoice(IncommingInvoiceBO invoice);
        GetResponse<IncommingInvoiceActivityBO> GetProjectIncommingInvoicesForRecalculation(int projectid);
        GetResponse<IncommingInvoiceActivityBO> GetProjectIncommingInvoicesByActivity(int projectid, int activityid);
        GetResponse<IncommingInvoiceActivityBO> GetProjectIncommingInvoicesByGroup(int projectid, int groupid);
        Response DeleteIncommingInvoices(List<int> ids);

        // Insurance
        GetResponse<InsuranceBO> GetProjectInsurances(int projectid);


        Response Copyids();
    }
}

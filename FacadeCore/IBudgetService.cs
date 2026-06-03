using System;
using System.Collections.Generic;
using BOCore;

namespace FacadeCore
{
    public interface IBudgetService
    {
        // BudgetMaster
        GetResponse<BudgetMasterBO> GetBudgetMasters(int projectId);
        GetResponse<BudgetMasterBO> GetBudgetMaster(int masterId);
        Response CreateBudgetMaster(BudgetMasterBO master, int userId);
        Response UpdateBudgetMaster(BudgetMasterBO master);
        Response ArchiveBudgetMaster(int masterId);

        // BudgetVersie
        GetResponse<BudgetVersieBO> GetBudgetVersies(int masterId);
        GetResponse<BudgetVersieBO> GetActiefVersie(int masterId);
        Response CreateNieuweVersie(int masterId, string versieNaam, string notitie, int userId);
        Response ActiveerVersie(int versieId);

        // BudgetGegevens
        GetResponse<BudgetGegevensBO> GetBudgetGegevens(int versieId);
        Response SaveBudgetGegevens(BudgetGegevensBO bo, int versieId);
    }
}

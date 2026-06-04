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

        // BudgetOppervlaktes
        GetResponse<BudgetOppervlaktesBO> GetBudgetOppervlaktes(int versieId);
        GetResponse<BudgetOppervlaktesTotaalBO> GetBudgetOppervlaktesTotaal(int versieId);
        Response SaveBudgetOppervlaktes(List<BudgetOppervlaktesBO> rows, int versieId);
        Response AddBudgetOppervlaktesRij(BudgetOppervlaktesBO rij, int versieId);
        Response DeleteBudgetOppervlaktesRij(int rijId, int versieId);
        Response UpdateBudgetOppervlaktesRij(BudgetOppervlaktesBO rij);
        Response ReorderBudgetOppervlaktes(List<int> orderedIds, int versieId);

        // BudgetGevelElementen
        GetResponse<BudgetGevelElementBO> GetBudgetGevelElementen(int versieId, string elementType = null);
        GetResponse<BudgetGevelTotaalBO> GetBudgetGevelTotaal(int versieId);
        Response AddBudgetGevelElement(BudgetGevelElementBO element, int versieId);
        Response UpdateBudgetGevelElement(BudgetGevelElementBO element);
        Response DeleteBudgetGevelElement(int elementId, int versieId);
        Response ReorderBudgetGevelElementen(List<int> orderedIds, int versieId, string elementType);

        // BudgetSanitair
        GetResponse<BudgetSanitairBO> GetBudgetSanitair(int versieId);
        GetResponse<BudgetSanitairTotaalBO> GetBudgetSanitairTotaal(int versieId);
        Response AddBudgetSanitairRij(BudgetSanitairBO rij, int versieId);
        Response UpdateBudgetSanitairRij(BudgetSanitairBO rij);
        Response DeleteBudgetSanitairRij(int rijId, int versieId);
        Response SyncSanitairVanOppervlaktes(int versieId);
    }
}

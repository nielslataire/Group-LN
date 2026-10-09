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
        /// <summary>Kopieert de volledige inhoud van een versie (gegevens, oppervlaktes, sanitair, gevels, activiteitenlijnen,
        /// parameters, verkooplijnen) naar een al bestaande, lege doelversie.</summary>
        Response KopieerVersieInhoud(int bronVersieId, int doelVersieId);

        // Versiestatus Concept → Afgerond → Definitief (design-handoff 39a/39j, migratie 078)
        /// <summary>Afronden: status "Afgerond" (blijft bewerkbaar). Faalt op een definitieve versie.</summary>
        Response AfrondenVersie(int versieId);
        /// <summary>Definitief maken: één per project (een eerdere definitieve versie wordt "Afgerond"); alleen-lezen vanaf nu.</summary>
        Response MaakDefinitief(int versieId, string door);
        /// <summary>Terug naar bewerkbaar ("Afgerond"), enkel voor een definitieve versie — nodig om een andere versie definitief te maken.</summary>
        Response OntgrendelDefinitief(int versieId);
        bool IsVergrendeld(int versieId);
        /// <summary>Onthoudt de hoogste bereikte stap (1-9) voor "Concept · stap 6 van 9" in het overzicht.</summary>
        void RegistreerStap(int versieId, int stap);
        Response SetVmswFactoren(int versieId, BOCore.Budget.VmswFactorenBO factoren);
        BOCore.Budget.VmswFactorenBO GetVmswFactoren(int versieId);
        Response BevestigWaarschuwing(int versieId, string sleutel, bool bevestigd, string door = null);

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

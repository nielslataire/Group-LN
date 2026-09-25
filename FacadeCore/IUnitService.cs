using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;

namespace FacadeCore
{
    public interface IUnitService
    {
        // UNITS
        GetResponse<UnitBO> GetUnitById(int Id);
        GetResponse<UnitBO> GetUnitsById(List<int> ids);
        GetResponse<UnitBO> GetUnitsByProjectId(int ProjectId);
        GetResponse<UnitBO> GetUnitsByProjectId(int ProjectId, int UnitTypeId);
        GetResponse<UnitWithDetailsBO> GetUnitsWithDetailsByProjectId(int ProjectId);
        GetResponse<UnitBO> GetUnitsByAccountId(int AccountId);
        GetResponse<UnitWithAttachedUnitsBO> GetUnitsForSaleByProjectId(int ProjectId);
        GetResponse<GroupUnitsBO> GetGroupedUnitsByProjectId(int ProjectId);
        GetResponse<UnitWithAttachedUnitsBO> GetUnitsWithAttachedByProjectId(int projectId);
        GetResponse<GroupUnitsWithAttachedUnitsBO> GetGroupedUnitsForSaleByProjectId(int ProjectId);
        GetResponse<GroupUnitsWithAttachedUnitsWithDetailsBO> GetGroupedUnitsForSaleWithDetailsByProjectId(int ProjectId);
        GetResponse<GroupUnitsBO> GetGroupedUnitsByAccountId(int AccountId);
        GetResponse<IdNameBO> GetAvailableUnitsByProjectId(int Id);
        GetResponse<IdNameBO> GetUnitsByProjectIdForSelect(int Id, bool WithLinked);
        GetResponse<IdNameBO> GetUnitsByProjectIdForSelect(int Id, int UnitTypeId);

        /// <summary>Kandidaten voor de legacy koppeldialoog (Projecten/ModalAddUnitLink): eenheden van
        /// hetzelfde type die nog aan niets vasthangen. Bewust een eigen methode i.p.v. een filter op
        /// GetUnitsByProjectIdForSelect hierboven — die wordt ook gebruikt om de leden van een BESTAANDE
        /// koppeling te tonen, en daar moeten eenheden met een LinkedUnitId juist wél in de lijst staan.
        /// Sluit uit: de eenheid zelf, eenheden die al onder een lot hangen (AttachedUnitId, het
        /// mechanisme waarop de eenhedenboom van Projecten/DetailUnitsV2 rust), eenheden die al lid zijn
        /// van een koppeling, en de samengestelde KOPPELING-rijen zelf (IsLink). Zonder die vier
        /// uitsluitingen kan één eenheid via beide koppelmechanismen tegelijk geteld worden — zie
        /// DESIGN.md, "De twee koppelmechanismen op Units".</summary>
        GetResponse<IdNameBO> GetUnitsForLinkSelect(int projectId, int unitTypeId, int excludeUnitId);
        GetResponse<IdNameBO> GetUnitsByProjectIdForSelectAttachedUnit(int Id);
        GetResponse<IdNameBO> GetUnitsByProjectIdForSelectAttachedUnit(int Id, int UnitId);

        GetResponse<UnitTypeBO> GetUniqueUnitTypesInProjectByProjectId(int id);
        GetResponse<UnitWithStagesBO> GetClientUnitsWithStages(int ClientAcccountId);
        Response SetUnitIsOption(int unitId, bool isOption);

        /// <summary>Schrijft enkel het aandeel basisakte (Units.Landshare) van meerdere eenheden in één
        /// keer. Bewust een smalle update i.p.v. per eenheid een InsertUpdateUnit: die leest een volledige
        /// UnitBO in en schrijft élk veld terug, met alle bijhorende risico's op stille nevenschade, en
        /// zou voor een lijst van 40 eenheden 80 queries kosten. Eenheden die niet bij projectId horen
        /// worden overgeslagen.</summary>
        Response UpdateUnitLandshares(int projectId, IDictionary<int, decimal?> landshareByUnitId);

        /// <summary>
        /// Zet grond-/bouwwaarde uit het budget-verkoopvoorstel op de Units: Units.LandValue ← Grondwaarde,
        /// basis-bouwwaarde (UnitConstructionValue zonder FinishingOptionId) ← Bouwwaarde.
        /// Verkochte units (klant gekoppeld of Sold-waarden gevuld) worden overgeslagen; units met meerdere
        /// basis-bouwwaardelijnen krijgen enkel de grondwaarde en een waarschuwing.
        /// </summary>
        Response UpdateUnitBudgetWaarden(int projectId, IReadOnlyList<BOCore.Budget.UnitBudgetWaardeBO> waarden);
        Response InsertUpdateUnit(UnitBO bo);
        Response InsertUpdateUnitToClientAccount(UnitBO bo);
        Response DeleteUnit(List<int> ids);
        Response DeleteUnitFromClientAccountByUnitId(List<int> ids);
        Response DeleteUnitFromClientAccountByAccountId(List<int> ids);



        // UNIT GROUP TYPES
        GetResponse<UnitGroupTypeBO> GetUnitGroupTypes();
        Response InsertUpdateUnitGroupType(UnitGroupTypeBO bo);
        Response DeleteUnitGroupType(List<int> ids);

        // UNIT TYPES
        GetResponse<UnitTypeBO> GetUnitTypesByGroupId(int GroupId);
        Response InsertUpdateUnitType(UnitTypeBO bo);
        Response DeleteUnitType(List<int> ids);

        // UNIT ROOMS
        GetResponse<RoomBO> GetRooms(int UnitId);
        GetResponse<RoomType> GetUniqueRoomTypesInProjectByProjectId(int projectid);
        Response InsertUpdateRoom(RoomBO bo);
        Response DeleteRooms(List<int> ids);

        // UNIT CONSTRUCTION VALUE
        GetResponse<UnitConstructionValueBO> GetConstructionValues(int UnitId);
        GetResponse<UnitConstructionValueBO> GetConstructionValue(int id);
        Response InsertUpdateConstructionValue(UnitConstructionValueBO bo);
        Response UpdateConstructionValueSold(UnitConstructionValueBO bo);
        Response UpdateLandValueSold(UnitBO bo);

        Response DeleteConstructionValues(List<int> ids);

        // UNIT FINISHING OPTIONS
        GetResponse<UnitFinishingOptionBO> GetFinishingOptions(int unitId);
        GetResponse<UnitFinishingOptionBO> GetFinishingOptionById(int id);
        Response InsertUpdateFinishingOption(UnitFinishingOptionBO bo);
        Response DeleteFinishingOption(int id);
        Response EnsureDefaultFinishingOption(int unitId);
    }
}

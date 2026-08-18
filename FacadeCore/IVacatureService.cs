using BOCore;
using System.Collections.Generic;

namespace FacadeCore
{
    public interface IVacatureService
    {
        GetResponse<VacatureBO> GetVacatures(bool alleenGepubliceerd = false);
        GetResponse<VacatureBO> GetVacatureById(int id);
        GetResponse<VacatureBO> GetVacatureBySlug(string slug);
        Response InsertUpdate(VacatureBO bo);
        Response DeleteVacature(int id);

        Response InsertUpdateTaak(VacatureTaakBO bo);
        Response UpdateTakenVolgorde(int vacatureId, List<int> sortedIds);
        Response DeleteTaak(int id);

        Response InsertUpdateVereiste(VacatureVereisteBO bo);
        Response UpdateVereistenVolgorde(int vacatureId, List<int> sortedIds);
        Response DeleteVereiste(int id);

        Response InsertUpdateVoordeel(VacatureVoordeelBO bo);
        Response UpdateVoordelenVolgorde(int vacatureId, List<int> sortedIds);
        Response DeleteVoordeel(int id);

        Response InsertUpdateSollicitatieStap(VacatureSollicitatieStapBO bo);
        Response UpdateSollicitatieStappenVolgorde(int vacatureId, List<int> sortedIds);
        Response DeleteSollicitatieStap(int id);
    }
}

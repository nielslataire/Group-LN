using BOCore;

namespace FacadeCore
{
    public interface IVacatureService
    {
        GetResponse<VacatureBO> GetVacatures(bool alleenGepubliceerd = false);
        GetResponse<VacatureBO> GetVacatureById(int id);
        GetResponse<VacatureBO> GetVacatureBySlug(string slug);
        Response InsertUpdate(VacatureBO bo);
        Response DeleteVacature(int id);
    }
}

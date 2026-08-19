using BOCore;

namespace FacadeCore
{
    public interface IVacatureSollicitatieService
    {
        GetResponse<VacatureSollicitatieBO> GetSollicitaties(int? vacatureId = null);
        GetResponse<VacatureSollicitatieBO> GetSollicitatieCv(int id);
        Response MarkeerGelezen(int id, bool gelezen);
        Response DeleteSollicitatie(int id);
    }
}

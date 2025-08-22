using FacadeCore;
using BOCore;
using DALCore;

namespace ServiceCore
{
    public class ProvinceService : IProvinceService
    {
        private readonly UnitOfWorkCore _uow;

        public ProvinceService(UnitOfWorkCore uow)
        {
            _uow = uow;
        }

        public GetResponse<IdNameBO> GetProvinces()
        {
            var response = new GetResponse<IdNameBO>();

            // No tracking, gewoon alles naar IdNameBO mappen
            var entities = _uow.Provinces.GetNoTracking()
                .OrderBy(p => p.ProvincieName);
            foreach (var e in entities)
                response.AddValue(e.GetIdName());

            return response;
        }
    }
}

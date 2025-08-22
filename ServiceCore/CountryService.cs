using FacadeCore;
using BOCore;
using DALCore;
using DALCore.Models;
using DALCore.Query;
using System.Linq;

namespace ServiceCore
{
    public class CountryService : ICountryService
    {
        private readonly UnitOfWorkCore _uow;

        public CountryService(UnitOfWorkCore uow)
        {
            _uow = uow;
        }

        public GetResponse<CountryBO> GetCountrys()
        {
            var response = new GetResponse<CountryBO>();

            var entities = _uow.Countries.GetNoTracking(); // zorg dat UnitOfWorkCore.Countries bestaat
            foreach (var e in entities)
            {
                var bo = new CountryBO
                {
                    CountryId = e.Id,
                    Name = e.LandNaam,
                    ISOCode = e.LandIsocode
                };
                response.AddValue(bo);
            }
            return response;
        }

        public GetResponse<CountryBO> GetCountryById(int id)
        {
            var response = new GetResponse<CountryBO>();

            var e = _uow.Countries.GetById(id);
            if (e == null)
            {
                response.AddError("country not found");
                return response;
            }

            var bo = new CountryBO
            {
                CountryId = e.Id,
                Name = e.LandNaam,
                ISOCode = e.LandIsocode
            };
            response.AddValue(bo);

            return response;
        }

        public GetResponse<IdNameBO> GetVisibleCountriesForSelect()
        {
            var response = new GetResponse<IdNameBO>();

            var entities = _uow.Countries.GetNoTracking()
                .Where(CountryQuery.GetVisibleQuery(true));

            foreach (var e in entities)
                response.AddValue(e.GetIdName());

            return response;
        }
    }
}

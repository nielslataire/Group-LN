using BOCore;
using FacadeCore;
using DALCore;
using DALCore.Models;
using ServiceCore.Translators;
using DALCore.Query;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore
{
    public class PostalcodeService : IPostalcodeService
    {
        private readonly UnitOfWorkCore _uow;

        public PostalcodeService(UnitOfWorkCore uow)
        {
            _uow = uow;
        }

        public GetResponse<PostalCodeBO> GetPostalcodeById(int id)
        {
            var response = new GetResponse<PostalCodeBO>();

            var entity = _uow.PostalCodes.GetById(id);   // pas naam aan als jouw UoW-property anders heet
            if (entity == null)
            {
                response.AddError("postalcode not found");
                return response;
            }

            var bo = new PostalCodeBO();
            var err = PostalcodeTranslator.TranslateEntityToBO(entity, bo);
            if (err == ErrorCode.Success) response.Value = bo;
            else response.AddError(err.ToString());

            return response;
        }

        public GetResponse<PostalCodeBO> GetPostalcodeByCountry(int countryId)
        {
            var response = new GetResponse<PostalCodeBO>();

            var entities = _uow.PostalCodes
                .GetNoTracking()
                .Where(PostalCodeQuery.GetCountryQuery(countryId));

            foreach (var e in entities)
            {
                var bo = new PostalCodeBO
                {
                    PostcodeId = e.PostcodeId,
                    Postcode = e.Postcode,
                    Gemeente = e.Gemeente
                };
                response.AddValue(bo);
            }
            return response;
        }

        public async Task<GetResponse<PostalCodeBO>> GetPostalcodeByCountryAndSearchstring(int countryId, string searchstring)
        {
            var response = new GetResponse<PostalCodeBO>();

            var query = PostalCodeQuery.GetCountryQuery(countryId);
            var sub = PostalCodeQuery.GetCityContainsQuery(searchstring)
                       .Or(PostalCodeQuery.GetPostalCodeStartsWithQuery(searchstring));

            var list = await _uow.PostalCodes
                .GetNoTracking()
                .Where(query.And(sub))
                .Select(e => new PostalCodeBO
                {
                    PostcodeId = e.PostcodeId,
                    Postcode = e.Postcode,
                    Gemeente = e.Gemeente
                })
                .ToListAsync(); // <— materialiseren

            foreach (var bo in list)
            {
                response.AddValue(bo);
            }
            return response;
        }
    }
}

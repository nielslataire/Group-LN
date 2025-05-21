using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using FacadeCore;
using DALCore;
using DALCore.Models;
using ServiceCore.Translators;
using DALCore.Query;

namespace ServiceCore
{
    public class PostalcodeService : IPostalcodeService
    {
        public GetResponse<PostalCodeBO> GetPostalcodeById(int id)
        {
            GetResponse<PostalCodeBO> response = new GetResponse<PostalCodeBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetPostalcodeDAO();

            var _entity = dao.GetById(id);
            PostalCodeBO postalcode = new PostalCodeBO();

            var err = PostalcodeTranslator.TranslateEntityToBO(_entity, postalcode);
            if (err == ErrorCode.Success)
                response.Value = postalcode;
            else
                response.AddError(err.ToString());
            return response;
        }
        public GetResponse<PostalCodeBO> GetPostalcodeByCountry(int CountryId)
        {
            GetResponse<PostalCodeBO> response = new GetResponse<PostalCodeBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetPostalcodeDAO();
            var entities = dao.GetNoTracking();
            foreach (var _entity in entities)
            {
                PostalCodeBO bo = new PostalCodeBO();
                bo.PostcodeId = _entity.PostcodeId;
                bo.Postcode = _entity.Postcode;

                response.AddValue(bo);
            }
            return response;
        }
        public GetResponse<PostalCodeBO> GetPostalcodeByCountryAndSearchstring(int CountryId, string Searchstring)
        {
            GetResponse<PostalCodeBO> response = new GetResponse<PostalCodeBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetPostalcodeDAO();
            var query = PostalCodeQuery.GetCountryQuery(CountryId);
            var subquery = PostalCodeQuery.GetCityContainsQuery(Searchstring);
            subquery = subquery.Or(PostalCodeQuery.GetPostalCodeStartsWithQuery(Searchstring));
            query = query.And(subquery);
            var entities = dao.GetNoTracking().Where(query);
            foreach (var _entity in entities)
            {
                PostalCodeBO bo = new PostalCodeBO();
                bo.PostcodeId = _entity.PostcodeId;
                bo.Postcode = _entity.Postcode;
                bo.Gemeente = _entity.Gemeente;

                response.AddValue(bo);
            }
            return response;
        }

        //public GetResponse<PostalCodeBO> GetPostalcodeByCountryCodeAndPostalcode(string Countrycode, string Postalcode)
        //{
        //}
    }
}

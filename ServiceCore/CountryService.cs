using FacadeCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using DALCore;
using DALCore.Models;
using DALCore.Query;
using ServiceCore.Translators;


namespace ServiceCore
{
    public class CountryService : ICountryService
    {
        public GetResponse<CountryBO> GetCountrys()
        {
            GetResponse<CountryBO> response = new GetResponse<CountryBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetCountryDAO();
            var entities = dao.GetNoTracking();
            foreach (var _entity in entities)
            {
                CountryBO bo = new CountryBO();
                bo.CountryId = _entity.Id;
                bo.Name = _entity.LandNaam;
                bo.ISOCode = _entity.LandIsocode;

                response.AddValue(bo);
            }
            return response;
        }
        public GetResponse<CountryBO> GetCountryById(int id)
        {
            GetResponse<CountryBO> response = new GetResponse<CountryBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetCountryDAO();
            var _entity = dao.GetById(id);

            CountryBO bo = new CountryBO();
            bo.CountryId = _entity.Id;
            bo.Name = _entity.LandNaam;
            bo.ISOCode = _entity.LandIsocode;

            response.AddValue(bo);
            return response;
        }
        public GetResponse<IdNameBO> GetVisibleCountriesForSelect()
        {
            GetResponse<IdNameBO> response = new GetResponse<IdNameBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetCountryDAO();
            var entities = dao.GetNoTracking().Where(CountryQuery.GetVisibleQuery(true));
            foreach (var _entity in entities)
                response.AddValue(_entity.GetIdName());
            return response;
        }
    }
}

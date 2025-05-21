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



namespace ServiceCore
{
    public class DepartmentService : IDepartmentService
    {
        public GetResponse<DepartmentBO> GetDepartments()
        {
            GetResponse<DepartmentBO> response = new GetResponse<DepartmentBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetDepartmentDAO();

            var entities = dao.GetNoTracking();
            foreach (var _entity in entities)
            {
                DepartmentBO bo = new DepartmentBO();
                var err = DepartmentTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<IdNameBO> GetDepartmentsForSelect()
        {
            GetResponse<IdNameBO> response = new GetResponse<IdNameBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetDepartmentDAO();

            var entities = dao.GetNoTracking();
            foreach (var _entity in entities)
                response.AddValue(_entity.GetIdName());
            return response;
        }

        public GetResponse<DepartmentBO> GetDepartmentById(int id)
        {
            GetResponse<DepartmentBO> response = new GetResponse<DepartmentBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetDepartmentDAO();
            var _entity = dao.GetById(id);
            DepartmentBO bo = new DepartmentBO();
            var err = DepartmentTranslator.TranslateEntityToBO(_entity, bo);
            if (err == ErrorCode.Success)
                response.AddValue(bo);
            else
                response.AddError(err.ToString());

            return response;
        }
        public GetResponse<DepartmentBO> GetDepartmentByIds(List<int> IdList)
        {
            GetResponse<DepartmentBO> response = new GetResponse<DepartmentBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetDepartmentDAO();

            foreach (var id in IdList)
            {
                var _entity = dao.GetById(id);
                DepartmentBO bo = new DepartmentBO();
                var err = DepartmentTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }

        public Response InsertUpdate(DepartmentBO bo)
        {
            Response response = new Response();
            if ((string.IsNullOrWhiteSpace(bo.Name)))
                response.AddError("name is mandatory");
            if ((!response.Success))
                return response;

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetDepartmentDAO();
            CompanyDepartments _entity = null/* TODO Change to default(_) if this is not a reference type */;

            if ((bo.ID == 0))
                _entity = dao.GetNew();
            else
                _entity = dao.GetById(bo.ID);
            if ((_entity != null))
            {
                var err = DepartmentTranslator.TranslateBOToEntity(_entity, bo);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("department not found");

            response.AddError(uow.SaveChanges());
            return response;
        }

        public Response Delete(List<int> ids)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();

            foreach (var id in ids)
                uow.GetDepartmentDAO().DeleteObject(id);
            response.Messages.AddRange(uow.SaveChanges());

            return response;
        }


        public Response Delete(List<DepartmentBO> bos)
        {
            return Delete(bos.Select(s => s.ID).ToList());
        }
    }
}

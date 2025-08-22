using BOCore;
using FacadeCore;
using DALCore;
using DALCore.Models;
using ServiceCore.Translators;
using System.Linq;

namespace ServiceCore
{
    public class DepartmentService : IDepartmentService
    {
        private readonly UnitOfWorkCore _uow;

        public DepartmentService(UnitOfWorkCore uow)
        {
            _uow = uow;
        }

        public GetResponse<DepartmentBO> GetDepartments()
        {
            var response = new GetResponse<DepartmentBO>();

            var entities = _uow.Departments.GetNoTracking();
            foreach (var e in entities)
            {
                var bo = new DepartmentBO();
                var err = DepartmentTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }

            return response;
        }

        public GetResponse<IdNameBO> GetDepartmentsForSelect()
        {
            var response = new GetResponse<IdNameBO>();

            var entities = _uow.Departments.GetNoTracking();
            foreach (var e in entities)
                response.AddValue(e.GetIdName());

            return response;
        }

        public GetResponse<DepartmentBO> GetDepartmentById(int id)
        {
            var response = new GetResponse<DepartmentBO>();

            var entity = _uow.Departments.GetById(id);
            if (entity == null)
            {
                response.AddError("department not found");
                return response;
            }

            var bo = new DepartmentBO();
            var err = DepartmentTranslator.TranslateEntityToBO(entity, bo);
            if (err == ErrorCode.Success)
                response.AddValue(bo);
            else
                response.AddError(err.ToString());

            return response;
        }

        public GetResponse<DepartmentBO> GetDepartmentByIds(List<int> idList)
        {
            var response = new GetResponse<DepartmentBO>();

            foreach (var id in idList)
            {
                var entity = _uow.Departments.GetById(id);
                if (entity == null)
                {
                    response.AddError($"department id {id} not found");
                    continue;
                }

                var bo = new DepartmentBO();
                var err = DepartmentTranslator.TranslateEntityToBO(entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }

            return response;
        }

        public Response InsertUpdate(DepartmentBO bo)
        {
            var response = new Response();

            if (string.IsNullOrWhiteSpace(bo.Name))
            {
                response.AddError("name is mandatory");
                return response;
            }

            CompanyDepartments entity;

            if (bo.ID == 0)
                entity = _uow.Departments.GetNew();
            else
                entity = _uow.Departments.GetById(bo.ID);

            if (entity == null)
            {
                response.AddError("department not found");
                return response;
            }

            var err = DepartmentTranslator.TranslateBOToEntity(entity, bo);
            if (err != ErrorCode.Success)
            {
                response.AddError(err.ToString());
                return response;
            }

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Afdeling aangepast of toegevoegd", "Afdeling niet aangepast of toegevoegd");

            return response;
        }

        public Response Delete(List<int> ids)
        {
            var response = new Response();

            foreach (var id in ids)
                _uow.Departments.DeleteObject(id);

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Record(s) verwijderd", "Geen records verwijderd");

            return response;
        }

        public Response Delete(List<DepartmentBO> bos)
            => Delete(bos.Select(s => s.ID).ToList());
    }
}

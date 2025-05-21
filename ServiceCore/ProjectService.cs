using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using FacadeCore;
using DALCore;
using DALCore.Query;
using DALCore.Models;
using ServiceCore.Translators;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
//using System.Data.Entity;

namespace ServiceCore
{
    public class ProjectService : IProjectService
    {
        public GetResponse<ProjectBO> GetProjectByID(int id)
        {
            GetResponse<ProjectBO> response = new GetResponse<ProjectBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectDAO();
            var _entity = dao.GetNormal().Where(m => m.ProjectId == id)
                .Include(m => m.PostalCode)
                .Include(m => m.PostalCode.Country)
                .Include(m => m.PostalCode.Provincie)
                .Include(m => m.Developer)
                .Include(m => m.Builder)
                .Include(m => m.Architect)
                .Include(m => m.Engineer)
                .Include(m => m.EpbReporter)
                .Include(m => m.SecurityCoordinator)
                .Include(m => m.WheaterStation)
                .Include(m => m.DefaultPicture)
                .Include(m => m.ProjectDocs)
                .Include(m => m.ProjectPictures)
                .Include(m => m.Status)
                .SingleOrDefault();
            //var _entity = dao.GetById(id);
            ProjectBO project = new ProjectBO();
            var err = ProjectTranslator.TranslateEntityToBO(_entity, project);
            if (err == ErrorCode.Success)
                response.Value = project;
            else
                response.AddError(err.ToString());
            return response;
        }
        public GetResponse<ProjectBO> GetProjectBySlug(string slug)
        {
            GetResponse<ProjectBO> response = new GetResponse<ProjectBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectDAO();
            var _entity = dao.GetNoTracking().Where(m => m.Slug == slug).FirstOrDefault();
            ProjectBO project = new ProjectBO();

            var err = ProjectTranslator.TranslateEntityToBO(_entity, project);
            if (err == ErrorCode.Success)
                response.Value = project;
            else
                response.AddError(err.ToString());
            return response;
        }
        //public GetResponse<ProjectBO> GetProjects()
        //{
        //}
        public GetResponse<ProjectBO> GetProjectsForList(ProjectType Type = 0, int StatusId = 0, string UserId = null, int BuilderId = 0, bool TrimCommercialText = false)
        {
            GetResponse<ProjectBO> response = new GetResponse<ProjectBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectDAO();
            var entities = Enumerable.Empty<dynamic>();

            if (Type != 0)
            {
                if (StatusId != 0)
                {
                    if (UserId is not null)
                    {
                        if (BuilderId != 0)
                            entities = dao.GetNoTracking().Select(m => new { id = m.ProjectId, Name = m.ProjectName, status = m.Status.StatusId, location = m.PostalCode.Gemeente, Defaultpic = m.DefaultPicture.Name, DefaultpicCaption = m.DefaultPicture.Caption, DeliveryDate = m.DeliveryDate, CommercialTitleNl = m.CommercialTitleNl, CommercialTextNl = m.CommercialTextNl, slug = m.Slug, UserId = m.AspNetUserId, BuilderId = m.BuilderId, ProjectType = m.ProjectType }).Where(m => m.status == StatusId && m.UserId == UserId && m.BuilderId == BuilderId && m.ProjectType == (int)Type);
                        else
                            entities = dao.GetNoTracking().Select(m => new { id = m.ProjectId, Name = m.ProjectName, status = m.Status.StatusId, location = m.PostalCode.Gemeente, Defaultpic = m.DefaultPicture.Name, DefaultpicCaption = m.DefaultPicture.Caption, DeliveryDate = m.DeliveryDate, CommercialTitleNl = m.CommercialTitleNl, CommercialTextNl = m.CommercialTextNl, slug = m.Slug, UserId = m.AspNetUserId, ProjectType = m.ProjectType }).Where(m => m.status == StatusId && m.UserId == UserId && m.ProjectType == (int)Type);
                    }
                    else if (BuilderId != 0)
                        entities = dao.GetNoTracking().Select(m => new { id = m.ProjectId, Name = m.ProjectName, status = m.Status.StatusId, location = m.PostalCode.Gemeente, Defaultpic = m.DefaultPicture.Name, DefaultpicCaption = m.DefaultPicture.Caption, DeliveryDate = m.DeliveryDate, CommercialTitleNl = m.CommercialTitleNl, CommercialTextNl = m.CommercialTextNl, slug = m.Slug, BuilderId = m.BuilderId, ProjectType = m.ProjectType }).Where(m => m.status == StatusId && m.BuilderId == BuilderId && m.ProjectType == (int)Type);
                    else
                        entities = dao.GetNoTracking().Select(m => new { id = m.ProjectId, Name = m.ProjectName, status = m.Status.StatusId, location = m.PostalCode.Gemeente, Defaultpic = m.DefaultPicture.Name, DefaultpicCaption = m.DefaultPicture.Caption, DeliveryDate = m.DeliveryDate, CommercialTitleNl = m.CommercialTitleNl, CommercialTextNl = m.CommercialTextNl, slug = m.Slug, ProjectType = m.ProjectType }).Where(m => m.status == StatusId && m.ProjectType == (int)Type);
                }
                else if (UserId is not null)
                {
                    if (BuilderId != 0)
                        entities = dao.GetNoTracking().Select(m => new { id = m.ProjectId, Name = m.ProjectName, status = m.Status.StatusId, location = m.PostalCode.Gemeente, Defaultpic = m.DefaultPicture.Name, DefaultpicCaption = m.DefaultPicture.Caption, DeliveryDate = m.DeliveryDate, CommercialTitleNl = m.CommercialTitleNl, CommercialTextNl = m.CommercialTextNl, slug = m.Slug, UserId = m.AspNetUserId, BuilderId = m.BuilderId, ProjectType = m.ProjectType }).Where(m => m.UserId == UserId && m.BuilderId == BuilderId && m.ProjectType == (int)Type);
                    else
                        entities = dao.GetNoTracking().Select(m => new { id = m.ProjectId, Name = m.ProjectName, status = m.Status.StatusId, location = m.PostalCode.Gemeente, Defaultpic = m.DefaultPicture.Name, DefaultpicCaption = m.DefaultPicture.Caption, DeliveryDate = m.DeliveryDate, CommercialTitleNl = m.CommercialTitleNl, CommercialTextNl = m.CommercialTextNl, slug = m.Slug, UserId = m.AspNetUserId, ProjectType = m.ProjectType }).Where(m => m.UserId == UserId && m.ProjectType == (int)Type);
                }
                else if (BuilderId != 0)
                    entities = dao.GetNoTracking().Select(m => new { id = m.ProjectId, Name = m.ProjectName, status = m.Status.StatusId, location = m.PostalCode.Gemeente, Defaultpic = m.DefaultPicture.Name, DefaultpicCaption = m.DefaultPicture.Caption, DeliveryDate = m.DeliveryDate, CommercialTitleNl = m.CommercialTitleNl, CommercialTextNl = m.CommercialTextNl, slug = m.Slug, BuilderId = m.BuilderId, ProjectType = m.ProjectType }).Where(m => m.BuilderId == BuilderId && m.ProjectType == (int)Type);
                else
                    entities = dao.GetNoTracking().Select(m => new { id = m.ProjectId, Name = m.ProjectName, status = m.Status.StatusId, location = m.PostalCode.Gemeente, Defaultpic = m.DefaultPicture.Name, DefaultpicCaption = m.DefaultPicture.Caption, DeliveryDate = m.DeliveryDate, CommercialTitleNl = m.CommercialTitleNl, CommercialTextNl = m.CommercialTextNl, slug = m.Slug, ProjectType = m.ProjectType }).Where(m => m.ProjectType == (int)Type);
            }
            else if (StatusId != 0)
            {
                if (UserId != null)
                {
                    if (BuilderId != 0)
                        entities = dao.GetNoTracking().Select(m => new { id = m.ProjectId, Name = m.ProjectName, status = m.Status.StatusId, location = m.PostalCode.Gemeente, Defaultpic = m.DefaultPicture.Name, DefaultpicCaption = m.DefaultPicture.Caption, DeliveryDate = m.DeliveryDate, CommercialTitleNl = m.CommercialTitleNl, CommercialTextNl = m.CommercialTextNl, slug = m.Slug, UserId = m.AspNetUserId, BuilderId = m.BuilderId, ProjectType = m.ProjectType }).Where(m => m.status == StatusId && m.UserId == UserId && m.BuilderId == BuilderId);
                    else
                        entities = dao.GetNoTracking().Select(m => new { id = m.ProjectId, Name = m.ProjectName, status = m.Status.StatusId, location = m.PostalCode.Gemeente, Defaultpic = m.DefaultPicture.Name, DefaultpicCaption = m.DefaultPicture.Caption, DeliveryDate = m.DeliveryDate, CommercialTitleNl = m.CommercialTitleNl, CommercialTextNl = m.CommercialTextNl, slug = m.Slug, UserId = m.AspNetUserId, ProjectType = m.ProjectType }).Where(m => m.status == StatusId && m.UserId == UserId);
                }
                else if (BuilderId != 0)
                    entities = dao.GetNoTracking().Select(m => new { id = m.ProjectId, Name = m.ProjectName, status = m.Status.StatusId, location = m.PostalCode.Gemeente, Defaultpic = m.DefaultPicture.Name, DefaultpicCaption = m.DefaultPicture.Caption, DeliveryDate = m.DeliveryDate, CommercialTitleNl = m.CommercialTitleNl, CommercialTextNl = m.CommercialTextNl, slug = m.Slug, BuilderId = m.BuilderId, ProjectType = m.ProjectType }).Where(m => m.status == StatusId && m.BuilderId == BuilderId);
                else
                    entities = dao.GetNoTracking().Select(m => new { id = m.ProjectId, Name = m.ProjectName, status = m.Status.StatusId, location = m.PostalCode.Gemeente, Defaultpic = m.DefaultPicture.Name, DefaultpicCaption = m.DefaultPicture.Caption, DeliveryDate = m.DeliveryDate, CommercialTitleNl = m.CommercialTitleNl, CommercialTextNl = m.CommercialTextNl, slug = m.Slug, ProjectType = m.ProjectType }).Where(m => m.status == StatusId);
            }
            else if (UserId is not null)
            {
                if (BuilderId != 0)
                    entities = dao.GetNoTracking().Select(m => new { id = m.ProjectId, Name = m.ProjectName, status = m.Status.StatusId, location = m.PostalCode.Gemeente, Defaultpic = m.DefaultPicture.Name, DefaultpicCaption = m.DefaultPicture.Caption, DeliveryDate = m.DeliveryDate, CommercialTitleNl = m.CommercialTitleNl, CommercialTextNl = m.CommercialTextNl, slug = m.Slug, UserId = m.AspNetUserId, BuilderId = m.BuilderId, ProjectType = m.ProjectType }).Where(m => m.UserId == UserId && m.BuilderId == BuilderId);
                else
                    entities = dao.GetNoTracking().Select(m => new { id = m.ProjectId, Name = m.ProjectName, status = m.Status.StatusId, location = m.PostalCode.Gemeente, Defaultpic = m.DefaultPicture.Name, DefaultpicCaption = m.DefaultPicture.Caption, DeliveryDate = m.DeliveryDate, CommercialTitleNl = m.CommercialTitleNl, CommercialTextNl = m.CommercialTextNl, slug = m.Slug, UserId = m.AspNetUserId, ProjectType = m.ProjectType }).Where(m => m.UserId == UserId);
            }
            else if (BuilderId != 0)
                entities = dao.GetNoTracking().Select(m => new { id = m.ProjectId, Name = m.ProjectName, status = m.Status.StatusId, location = m.PostalCode.Gemeente, Defaultpic = m.DefaultPicture.Name, DefaultpicCaption = m.DefaultPicture.Caption, DeliveryDate = m.DeliveryDate, CommercialTitleNl = m.CommercialTitleNl, CommercialTextNl = m.CommercialTextNl, slug = m.Slug, BuilderId = m.BuilderId, ProjectType = m.ProjectType }).Where(m => m.BuilderId == BuilderId);
            else
                entities = dao.GetNoTracking().Select(m => new { id = m.ProjectId, Name = m.ProjectName, status = m.Status.StatusId, location = m.PostalCode.Gemeente, Defaultpic = m.DefaultPicture.Name, DefaultpicCaption = m.DefaultPicture.Caption, DeliveryDate = m.DeliveryDate, CommercialTitleNl = m.CommercialTitleNl, CommercialTextNl = m.CommercialTextNl, slug = m.Slug, ProjectType = m.ProjectType });

            foreach (var _entity in entities)
            {
                ProjectBO bo = new ProjectBO();
                bo.Id = _entity.id;
                if (TrimCommercialText == true && _entity.CommercialTextNl.ToString().Length > 150)
                    bo.CommercialTextNL = _entity.CommercialTextNl.ToString().Substring(0, 150) + " ...";
                else
                    bo.CommercialTextNL = _entity.CommercialTextNl;
                bo.CommercialTitleNL = _entity.CommercialTitleNl;
                bo.Name = _entity.Name;
                bo.Slug = _entity.slug;
                bo.Postalcode.Gemeente = _entity.location;
                bo.Status.Id = _entity.status;
                if (_entity.DeliveryDate != null)
                    bo.DeliveryDate = _entity.DeliveryDate;
                if (!string.IsNullOrEmpty(_entity.Defaultpic))
                {
                    bo.DefaultPicture.Name = _entity.Defaultpic;
                    bo.DefaultPicture.Caption = _entity.DefaultpicCaption;
                }
                else
                    bo.DefaultPicture.Name = null;
                bo.ProjectType = (ProjectType)_entity.ProjectType;
                response.AddValue(bo);
            }
            return response;
        }
        public string GetProjectNameById(int id)
        {
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectDAO();
            return dao.GetById(id).ProjectName;
        }
        public string GetProjectCityById(int id)
        {
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectDAO();
            return dao.GetById(id).PostalCode.Gemeente;
        }
        public string GetProjectSlugById(int id)
        {
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectDAO();
            return dao.GetById(id).Slug;
        }
        public decimal GetProjectLandshareById(int id)
        {
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectDAO();
            decimal landshare = 0;
            if (dao.GetById(id).TotalLandShare is not null)  landshare = (decimal)dao.GetById(id).TotalLandShare;     
            return landshare;
        }
        public int GetProjectWeatherstation(int projectid)
        {
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectDAO();
            int i = 0;
            if (dao.GetById(projectid).WheaterStationId.HasValue)
                i = (int)dao.GetById(projectid).WheaterStationId;
            return i;
        }
        public GetResponse<SelectBO> GetProjectsForSearchList(string searchterm)
        {
            GetResponse<SelectBO> response = new GetResponse<SelectBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectDAO();
            var entitys = dao.GetNoTracking().Where(ProjectQuery.GetNameQuery(searchterm)).OrderBy(m => m.ProjectName).Select(m => new SelectBO() { id = m.ProjectId, text = m.ProjectName, extra = "Project" });
            response.Values = entitys.ToList();
            return response;
        }
        public GetResponse<IdNameBO> GetProjectsWithAvailableUnits()
        {
            GetResponse<IdNameBO> response = new GetResponse<IdNameBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectDAO();
            var entities = dao.GetNoTracking().Where(m => m.Units.Any(i => i.ClientAccountId != null | i.ClientAccountId != 0));
            foreach (var _entity in entities)
                response.AddValue(_entity.GetIdName());
            return response;
        }
        public DateOnly GetProjectStartDateConstruction(int projectid)
        {
            UnitOfWork uow = new UnitOfWork();
            DateOnly StartDate = DateOnly.MinValue;
            var dao = uow.GetProjectDAO();
            if (dao.GetById(projectid).StartDateConstruction.HasValue)
            {
                StartDate = (DateOnly)dao.GetById(projectid).StartDateConstruction;
            }
            return StartDate;
        }
        public int GetProjectExecutionDays(int projectid)
        {
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectDAO();
            return (int)dao.GetById(projectid).ExecutionDays;
        }
        public int GetWorkingDaysLeft(DateOnly finalconstructiondate, int projectid)
        {
            if (finalconstructiondate != DateOnly.MinValue)
            {
                UnitOfWork uow = new UnitOfWork();
                var dao = uow.GetVacationDaysDAO();
                var entities = dao.GetNoTracking().Where(m => m.ProjectId == null | m.ProjectId == projectid);
                List<DateOnly> VDS = new List<DateOnly>();
                foreach (var _entity in entities)
                    VDS.Add(_entity.VacationDay);
                return BusinessDaysUntil(DateOnly.FromDateTime(DateTime.Now), finalconstructiondate, VDS.ToArray());
            }
            else
                return -9999;
        }
        public DateOnly GetFinalConstructionDay(int projectid, DateOnly startdate, int executiondays)
        {
            int weatherstationid;
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetVacationDaysDAO();
            var entities = dao.GetNoTracking().Where(m => m.ProjectId == null | m.ProjectId == projectid);
            List<DateOnly> VDS = new List<DateOnly>();
            foreach (var _entity in entities)
                VDS.Add(_entity.VacationDay);
            weatherstationid = GetProjectWeatherstation(projectid);
            if (weatherstationid != 0)
            {
                var BWDdao = uow.GetBadWeatherDaysDAO();
                var BWDentities = BWDdao.GetNoTracking().Where(m => m.WeatherstationId == weatherstationid).GroupBy(m => m.Date);
                List<DateOnly> BWDS = new List<DateOnly>();
                foreach (var ent in BWDentities)
                    BWDS.Add(ent.Key);

                return AddWorkDays(startdate, executiondays, BWDS.ToArray(), VDS.ToArray());
            }
            else
                return DateOnly.MinValue;
        }
        public string GetProjectFolderById(int id)
        {
            string Folder = "";
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectDAO();
            var test = dao.GetNoTracking().Where(m => m.ProjectId == id).FirstOrDefault();  
            Folder = dao.GetNoTracking().Where(m => m.ProjectId == id).FirstOrDefault().ProjectFolder;
            return Folder;
        }
        public GetResponse<WarningBO> CheckProjectFinished(string userid = "")
        {
            GetResponse<WarningBO> response = new GetResponse<WarningBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectDAO();
            if (userid == "")
            {
                var entities = dao.GetNoTracking().Where(m => m.Status.StatusId == 1);
                foreach (var _entity in entities)
                {
                    if (_entity.DeliveryDate == null && _entity.DocDelivery == true)
                    {
                        WarningBO bo = new WarningBO();
                        bo.ID = _entity.ProjectId;
                        bo.ProjectId = _entity.ProjectId;
                        bo.Display = "De voorlopige opleverdatum van project " + _entity.ProjectName + " is niet ingevuld.";
                        bo.Type = "danger";
                        response.AddValue(bo);
                    }
                    else
                    {
                        // Check ProjectDocs
                        WarningBO boDocs = CheckProjectDocs(_entity);
                        if (boDocs.Display != "" && boDocs.Display is not null)
                            response.AddValue(boDocs);

                        if (_entity.DeliveryDateDef == null && _entity.DocDefDelivery == true)
                        {
                            if (_entity.DeliveryDate.Value.AddYears(10).CompareTo(DateOnly.FromDateTime(DateTime.Today)) > 0)
                            {
                                if (_entity.DeliveryDate.Value.AddMonths(12).CompareTo(DateOnly.FromDateTime(DateTime.Today)) <= 0)
                                {
                                    WarningBO bo = new WarningBO();
                                    bo.ID = _entity.ProjectId;
                                    bo.ProjectId = _entity.ProjectId;
                                    bo.Display = "De definitieve oplevering van project " + _entity.ProjectName + " is nog niet gebeurd, gelieve deze aan te vragen!";
                                    bo.Type = "danger";
                                    response.AddValue(bo);
                                }
                                else if (_entity.DeliveryDate.Value.AddMonths(11).CompareTo(DateOnly.FromDateTime(DateTime.Today)) <= 0)
                                {
                                    WarningBO bo = new WarningBO();
                                    bo.ID = _entity.ProjectId;
                                    bo.ProjectId = _entity.ProjectId;
                                    bo.Display = "De definitieve oplevering van project " + _entity.ProjectName + " kan gebeuren vanaf " + _entity.DeliveryDate.Value.AddMonths(11) + " , gelieve deze aan te vragen!";
                                    bo.Type = "warning";
                                    response.AddValue(bo);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                var entities = dao.GetNoTracking().Where(m => m.Status.StatusId == 1 && m.AspNetUserId == userid);
                foreach (var _entity in entities)
                {
                    if (_entity.DeliveryDate == null && _entity.DocDelivery == true)
                    {
                        WarningBO bo = new WarningBO();
                        bo.ID = _entity.ProjectId;
                        bo.ProjectId = _entity.ProjectId;
                        bo.Display = "De voorlopige opleverdatum van project " + _entity.ProjectName + " is niet ingevuld.";
                        bo.Type = "danger";
                        response.AddValue(bo);
                    }
                    else if (_entity.DeliveryDateDef == null && _entity.DocDefDelivery == true)
                    {
                        // Check ProjectDocs
                        WarningBO boDocs = CheckProjectDocs(_entity);
                        if (boDocs.Display != "" && boDocs.Display is not null)
                            response.AddValue(boDocs);

                        if (_entity.DeliveryDate.Value.AddYears(10).CompareTo(DateOnly.FromDateTime(DateTime.Today)) > 0)
                        {
                            if (_entity.DeliveryDate.Value.AddMonths(12).CompareTo(DateOnly.FromDateTime(DateTime.Today)) <= 0)
                            {
                                WarningBO bo = new WarningBO();
                                bo.ID = _entity.ProjectId;
                                bo.ProjectId = _entity.ProjectId;
                                bo.Display = "De definitieve oplevering van project " + _entity.ProjectName + " is nog niet gebeurd, gelieve deze aan te vragen!";
                                bo.Type = "danger";
                                response.AddValue(bo);
                            }
                            else if (_entity.DeliveryDate.Value.AddMonths(11).CompareTo(DateOnly.FromDateTime(DateTime.Today)) <= 0)
                            {
                                WarningBO bo = new WarningBO();
                                bo.ID = _entity.ProjectId;
                                bo.ProjectId = _entity.ProjectId;
                                bo.Display = "De definitieve oplevering van project " + _entity.ProjectName + " kan gebeuren vanaf " + _entity.DeliveryDate.Value.AddMonths(11) + " , gelieve deze aan te vragen!";
                                bo.Type = "warning";
                                response.AddValue(bo);
                            }
                        }
                    }
                }
            }

            return response;
        }
        public WarningBO CheckProjectDocs(Project _entity)
        {
            WarningBO boDocs = new WarningBO();
            if (_entity.DeliveryDate.Value.AddYears(10).CompareTo(DateOnly.FromDateTime(DateTime.Today)) > 0)
            {
                boDocs.ID = _entity.ProjectId;
                boDocs.ProjectId = _entity.ProjectId;
                boDocs.Type = "warning";
                // If _entity.ProjectDocs.Where(Function(l) l.Type = ProjectDocType.EPB).Count = 0 Then If boDocs.Display = "" Then boDocs.Display = "Het project " & _entity.ProjectName & " ontbreekt volgende documenten : EPB Dossier" Else boDocs.Display = boDocs.Display & " , EPB Dossier"
                if ((_entity.DocElectricalInspection == true))
                {
                    if (_entity.ProjectDocs.Where(l => l.Type == (int)ProjectDocType.Electrical_inspection).Count() == 0)
                    {
                        if (boDocs.Display == "" | boDocs.Display is null)
                            boDocs.Display = "Het project " + _entity.ProjectName + " ontbreekt volgende documenten : Elektrische keuring";
                        else
                            boDocs.Display = boDocs.Display + " , Elektrische keuring";
                    }
                }
                if ((_entity.DocWaterInspection == true))
                {
                    if (_entity.ProjectDocs.Where(l => l.Type == (int)ProjectDocType.Water_inspection).Count() == 0)
                    {
                        if (boDocs.Display == "" | boDocs.Display is null)
                            boDocs.Display = "Het project " + _entity.ProjectName + " ontbreekt volgende documenten : Waterkeuring";
                        else
                            boDocs.Display = boDocs.Display + " , Waterkeuring";
                    }
                }
                if ((_entity.DocSewerInspection == true))
                {
                    if (_entity.ProjectDocs.Where(l => l.Type == (int)ProjectDocType.Sewer_inspection).Count() == 0)
                    {
                        if (boDocs.Display == "" | boDocs.Display is null)
                            boDocs.Display = "Het project " + _entity.ProjectName + " ontbreekt volgende documenten : Rioolkeuring";
                        else
                            boDocs.Display = boDocs.Display + " , Rioolkeuring";
                    }
                }
                if ((_entity.DocFireInspection == true))
                {
                    if (_entity.ProjectDocs.Where(l => l.Type == (int)ProjectDocType.Fire_inspection).Count() == 0)
                    {
                        if (boDocs.Display == "" | boDocs.Display is null)
                            boDocs.Display = "Het project " + _entity.ProjectName + " ontbreekt volgende documenten : Brandkeuring";
                        else
                            boDocs.Display = boDocs.Display + " , Brandkeuring";
                    }
                }
                if ((_entity.DocDelivery == true))
                {
                    if (_entity.ProjectDocs.Where(l => l.Type == (int)ProjectDocType.Delivery).Count() == 0)
                    {
                        if (boDocs.Display == "" | boDocs.Display is null)
                            boDocs.Display = "Het project " + _entity.ProjectName + " ontbreekt volgende documenten : Voorlopige oplevering";
                        else
                            boDocs.Display = boDocs.Display + " , Voorlopige oplevering";
                    }
                }
                if ((_entity.DocPid == true))
                {
                    if (_entity.ProjectDocs.Where(l => l.Type == (int)ProjectDocType.PID).Count() == 0)
                    {
                        if (boDocs.Display == "" | boDocs.Display is null)
                            boDocs.Display = "Het project " + _entity.ProjectName + " ontbreekt volgende documenten : PID";
                        else
                            boDocs.Display = boDocs.Display + " , PID";
                    }
                }
            }
            return boDocs;
        }
        public Response InsertUpdate(ProjectBO project)
        {
            Response response = new Response();
            if ((string.IsNullOrWhiteSpace(project.Name)))
                response.AddError("Projectnaam is verplicht");
            if ((!response.Success))
                return response;

            UnitOfWork uow = new UnitOfWork();
            Project _entity = null/* TODO Change to default(_) if this is not a reference type */;
            if ((project.Id == 0))
                _entity = uow.GetProjectDAO().GetNew();
            else
                _entity = uow.GetProjectDAO().GetById(project.Id);
            if ((_entity != null))
            {
                var err = ProjectTranslator.TranslateBOToEntity(_entity, project, uow);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("project not found");
            response.AddError(uow.SaveChanges());
            return response;
        }

        public Response Delete(List<int> ids)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();

            foreach (var id in ids)
                uow.GetProjectDAO().DeleteObject(id);
            response.Messages.AddRange(uow.SaveChanges());

            return response;
        }
        public Response Delete(List<ProjectBO> bos)
        {
            return Delete(bos.Select(s => s.Id).ToList());
        }

        // Wheaterstations
        //public GetResponse<WheaterStationBO> GetWheaterstations()
        //{
        //}

        public GetResponse<IdNameBO> GetWheaterstationsSelect()
        {
            GetResponse<IdNameBO> response = new GetResponse<IdNameBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetWheaterstationsDAO();
            var entities = dao.GetNoTracking();
            entities = entities.OrderBy(m => m.Name);
            foreach (var _entity in entities)
                response.AddValue(_entity.GetIdName());
            return response;
        }
        public GetResponse<WheaterStationBO> GetWheaterstations(string searchterm)
        {
            GetResponse<WheaterStationBO> response = new GetResponse<WheaterStationBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetWheaterstationsDAO();
            var entitys = dao.GetNoTracking().Where(m => m.Name.Contains(searchterm)).OrderBy(m => m.Name);
            foreach (var _entity in entitys)
            {
                WheaterStationBO bo = new WheaterStationBO();
                var err = WheaterstationTranslator.TranslateEntityToBO(_entity, bo);
                if ((err == ErrorCode.Success))
                    response.AddValue(bo);
            }
            return response;
        }

        // Badweatherdays
        public GetResponse<BadWeatherDayBO> GetBadWeatherDays(int weatherstationid, int type)
        {
            GetResponse<BadWeatherDayBO> response = new GetResponse<BadWeatherDayBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetBadWeatherDaysDAO();
            var entitys = dao.GetNoTracking().Where(m => m.WeatherstationId == weatherstationid && m.Type == type);
            foreach (var _entity in entitys)
            {
                BadWeatherDayBO bo = new BadWeatherDayBO();
                var err = BadWeatherDayTranslator.TranslateEntityToBO(_entity, bo);
                if ((err == ErrorCode.Success))
                    response.AddValue(bo);
            }
            return response;
        }
        public GetResponse<BadWeatherDayBO> GetBadWeatherDays(int weatherstationid, int type, int year)
        {
            GetResponse<BadWeatherDayBO> response = new GetResponse<BadWeatherDayBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetBadWeatherDaysDAO();
            var entitys = dao.GetNoTracking().Where(m => m.WeatherstationId == weatherstationid && m.Type == type && m.Date.Year == year);
            foreach (var _entity in entitys)
            {
                BadWeatherDayBO bo = new BadWeatherDayBO();
                var err = BadWeatherDayTranslator.TranslateEntityToBO(_entity, bo);
                if ((err == ErrorCode.Success))
                    response.AddValue(bo);
            }
            return response;
        }
        public GetResponse<BadWeatherDayBO> GetClientWeatherDays(int weatherstationid, DateTime startdate, DateTime enddate)
        {
            GetResponse<BadWeatherDayBO> response = new GetResponse<BadWeatherDayBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetBadWeatherDaysDAO();
            var entitys = dao.GetNoTracking().Where(m => m.WeatherstationId == weatherstationid && m.Date >= DateOnly.FromDateTime(startdate) && m.Date <= DateOnly.FromDateTime(enddate));
            foreach (var _entity in entitys)
            {
                BadWeatherDayBO bo = new BadWeatherDayBO();
                var err = BadWeatherDayTranslator.TranslateEntityToBO(_entity, bo);
                if ((err == ErrorCode.Success))
                    response.AddValue(bo);
            }
            return response;
        }
        public Response InsertUpdateBadWeatherDay(BadWeatherDayBO BWD)
        {
            Response response = new Response();
            if (BWD.BWDate == DateOnly.MinValue)
                response.AddError("Datum is verplicht");
            if ((!response.Success))
                return response;

            UnitOfWork uow = new UnitOfWork();
            BadWeatherDays _entity = null/* TODO Change to default(_) if this is not a reference type */;
            if ((BWD.Id == 0))
                _entity = uow.GetBadWeatherDaysDAO().GetNew();
            else
                _entity = uow.GetBadWeatherDaysDAO().GetById(BWD.Id);
            if ((_entity != null))
            {
                var err = BadWeatherDayTranslator.TranslateBOToEntity(_entity, BWD);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("Badweatherday not found");
            response.AddError(uow.SaveChanges());
            Message m = new Message();
            m.Message = _entity.Id.ToString();
            m.Type = MessageType.Value;
            response.Messages.Add(m);
            return response;
        }
        public Response DeleteBadWeatherDays(List<int> ids)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();
            foreach (var id in ids)
                uow.GetBadWeatherDaysDAO().DeleteObject(id);
            response.Messages.AddRange(uow.SaveChanges());
            return response;
        }
        public Response DeleteBadWeatherDays(List<BadWeatherDayBO> bos)
        {
            return DeleteBadWeatherDays(bos.Select(s => s.Id).ToList());
        }

        // Vacationdays
        public GetResponse<VacationDayBO> GetVacationDays()
        {
            GetResponse<VacationDayBO> response = new GetResponse<VacationDayBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetVacationDaysDAO();
            var entitys = dao.GetNoTracking().Where(m => m.ProjectId == null);
            foreach (var _entity in entitys)
            {
                VacationDayBO bo = new VacationDayBO();
                var err = VacationDayTranslator.TranslateEntityToBO(_entity, bo);
                if ((err == ErrorCode.Success))
                    response.AddValue(bo);
            }
            return response;
        }
        public GetResponse<VacationDayBO> GetProjectVacationDays(int projectid)
        {
            GetResponse<VacationDayBO> response = new GetResponse<VacationDayBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetVacationDaysDAO();
            var entitys = dao.GetNoTracking().Where(m => m.ProjectId == projectid);
            foreach (var _entity in entitys)
            {
                VacationDayBO bo = new VacationDayBO();
                var err = VacationDayTranslator.TranslateEntityToBO(_entity, bo);
                if ((err == ErrorCode.Success))
                    response.AddValue(bo);
            }
            return response;
        }
        public GetResponse<VacationDayBO> GetVacationDaysGeneral()
        {
            GetResponse<VacationDayBO> response = new GetResponse<VacationDayBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetVacationDaysDAO();
            var entitys = dao.GetNoTracking().Where(m => m.ProjectId == null | m.ProjectId == 0);
            foreach (var _entity in entitys)
            {
                VacationDayBO bo = new VacationDayBO();
                var err = VacationDayTranslator.TranslateEntityToBO(_entity, bo);
                if ((err == ErrorCode.Success))
                    response.AddValue(bo);
            }
            return response;
        }
        public Response InsertUpdateVacationDay(VacationDayBO vacationday)
        {
            Response response = new Response();
            if (vacationday is null)
                response.AddError("Datum is verplicht");
 
            if ((!response.Success))
                return response;

            UnitOfWork uow = new UnitOfWork();
            VacationDays _entity = null/* TODO Change to default(_) if this is not a reference type */;
            if ((vacationday.Id == 0))
                _entity = uow.GetVacationDaysDAO().GetNew();
            else
                _entity = uow.GetVacationDaysDAO().GetById(vacationday.Id);
            if ((_entity != null))
            {
                var err = VacationDayTranslator.TranslateBOToEntity(_entity, vacationday);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("Vacationday not found");
            response.AddError(uow.SaveChanges());
            Message m = new Message();
            m.Message = _entity.Id.ToString();
            m.Type = MessageType.Value;
            response.Messages.Add(m);
            return response;
        }
        public Response DeleteVacationDays(List<int> ids)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();
            foreach (var id in ids)
                uow.GetVacationDaysDAO().DeleteObject(id);
            response.Messages.AddRange(uow.SaveChanges());
            return response;
        }
        public Response DeleteVacationDays(List<VacationDayBO> bos)
        {
            return DeleteVacationDays(bos.Select(s => s.Id).ToList());
        }

        // Statuses

        public GetResponse<ProjectStatusBO> GetStatuses()
        {
            GetResponse<ProjectStatusBO> response = new GetResponse<ProjectStatusBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectStatusDAO();

            var entities = dao.GetNoTracking();
            foreach (var _entity in entities)
            {
                ProjectStatusBO bo = new ProjectStatusBO();
                var err = StatusTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<IdNameBO> GetStatusesForSelect()
        {
            GetResponse<IdNameBO> response = new GetResponse<IdNameBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectStatusDAO();
            var entities = dao.GetNoTracking();
            foreach (var _entity in entities)
                response.AddValue(_entity.GetIdName());
            return response;
        }


        // Pictures
        public GetResponse<ProjectPictureBO> GetPictureById(int id)
        {
            GetResponse<ProjectPictureBO> response = new GetResponse<ProjectPictureBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectPicturesDAO();
            var _entity = dao.GetById(id);
            ProjectPictureBO picture = new ProjectPictureBO();

            var err = ProjectPictureTranslator.TranslateEntityToBO(_entity, picture);
            if (err == ErrorCode.Success)
                response.Value = picture;
            else
                response.AddError(err.ToString());
            return response;
        }
        public GetResponse<ProjectPictureBO> GetPicturesByProjectId(int id)
        {
            GetResponse<ProjectPictureBO> response = new GetResponse<ProjectPictureBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectPicturesDAO();
            var entities = dao.GetNoTracking().Where(m => m.ProjectId == id);
            foreach (var _entity in entities)
            {
                ProjectPictureBO bo = new ProjectPictureBO();
                var err = ProjectPictureTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<ProjectPictureBO> GetPicturesByProjectSlug(string slug)
        {
            GetResponse<ProjectPictureBO> response = new GetResponse<ProjectPictureBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectPicturesDAO();
            var entities = dao.GetNoTracking().Where(m => m.ProjectNavigation.Slug == slug);
            foreach (var _entity in entities)
            {
                ProjectPictureBO bo = new ProjectPictureBO();
                var err = ProjectPictureTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<ProjectPictureBO> GetLatestPictures(int number)
        {
            GetResponse<ProjectPictureBO> response = new GetResponse<ProjectPictureBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectPicturesDAO();
            var entities = dao.GetNoTracking().OrderByDescending(m => m.Datetimeuploaded).Take(number);
            foreach (var _entity in entities)
            {
                ProjectPictureBO bo = new ProjectPictureBO();
                var err = ProjectPictureTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<ProjectPictureBO> GetLatestProjectPictures(int number, int projectid)
        {
            GetResponse<ProjectPictureBO> response = new GetResponse<ProjectPictureBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectPicturesDAO();
            var entities = dao.GetNoTracking().Where(m => m.ProjectId == projectid && m.Type != 3).OrderByDescending(m => m.Datetimeuploaded).Take(number);
            foreach (var _entity in entities)
            {
                ProjectPictureBO bo = new ProjectPictureBO();
                var err = ProjectPictureTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public Response InsertUpdatePicture(ProjectPictureBO picture)
        {
            Response response = new Response();
            if ((string.IsNullOrWhiteSpace(picture.Name)))
                response.AddError("Bestandsnaam is verplicht");
            if ((!response.Success))
                return response;

            UnitOfWork uow = new UnitOfWork();
            ProjectPictures _entity = null/* TODO Change to default(_) if this is not a reference type */;
            if ((picture.Id == 0))
                _entity = uow.GetProjectPicturesDAO().GetNew();
            else
                _entity = uow.GetProjectPicturesDAO().GetById(picture.Id);
            if ((_entity != null))
            {
                var err = ProjectPictureTranslator.TranslateBOToEntity(_entity, picture, uow);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("project not found");
            response.AddError(uow.SaveChanges());
            Message PictureId = new Message();
            PictureId.Type = MessageType.Value;
            PictureId.Message = _entity.Id.ToString();
            response.Messages.Add(PictureId);
            return response;
        }
        public Response DeletePicture(List<int> ids)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();

            foreach (var id in ids)
                uow.GetProjectPicturesDAO().DeleteObject(id);
            response.Messages.AddRange(uow.SaveChanges());

            return response;
        }
        public Response SetDefaultProjectPicture(int projectid, int pictureid)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();
            Project _entity = null/* TODO Change to default(_) if this is not a reference type */;
            if ((projectid == 0))
                response.AddError("Er moet een project of foto geselecteerd zijn.");
            if ((!response.Success))
                return response;

            if (pictureid == 0)
            {
                _entity = uow.GetProjectDAO().GetById(projectid);
                if ((_entity != null))
                {
                    if (_entity.DefaultPicture != null)
                        _entity.DefaultPictureId = null;
                }
                else
                    response.AddError("project not found");
                response.AddError(uow.SaveChanges());
            }
            else
            {
                _entity = uow.GetProjectDAO().GetById(projectid);

                if ((_entity != null))
                {
                    foreach (var picture in _entity.ProjectPictures)
                    {
                        if (picture.Type == (int)PictureType.Hoofdfoto)
                            picture.Type = (int)PictureType.Nevenfoto;
                    }
                    if (_entity.DefaultPicture != null)
                    {
                        ProjectPictures picture = null/* TODO Change to default(_) if this is not a reference type */;
                        picture = uow.GetProjectPicturesDAO().GetById((int)_entity.DefaultPictureId);
                        picture.Type = (int)PictureType.Nevenfoto;
                    }
                    _entity.DefaultPictureId = pictureid;
                }
                else
                    response.AddError("project not found");
                response.AddError(uow.SaveChanges());
            }



            return response;
        }
        public string GetFacebookAlbumIdCoproByProjectId(int id)
        {
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectDAO();
            return dao.GetById(id).FacebookAlbumId.ToString();
        }


        // News
        public GetResponse<ProjectNewsBO> GetNewsById(int id)
        {
            GetResponse<ProjectNewsBO> response = new GetResponse<ProjectNewsBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectNewsDAO();
            var _entity = dao.GetById(id);
            ProjectNewsBO news = new ProjectNewsBO();

            var err = ProjectNewsTranslator.TranslateEntityToBO(_entity, news);
            if (err == ErrorCode.Success)
                response.Value = news;
            else
                response.AddError(err.ToString());
            return response;
        }
        public GetResponse<ProjectNewsBO> GetNewsByProjectId(int id)
        {
            GetResponse<ProjectNewsBO> response = new GetResponse<ProjectNewsBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectNewsDAO();
            var entities = dao.GetNoTracking().Where(m => m.ProjectId == id).OrderByDescending(m => m.Date);
            foreach (var _entity in entities)
            {
                ProjectNewsBO bo = new ProjectNewsBO();
                var err = ProjectNewsTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<ProjectNewsBO> GetNewsByProjectSlug(string slug)
        {
            GetResponse<ProjectNewsBO> response = new GetResponse<ProjectNewsBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectNewsDAO();
            var entities = dao.GetNoTracking().Where(m => m.Project.Slug == slug).OrderByDescending(m => m.Date);
            foreach (var _entity in entities)
            {
                ProjectNewsBO bo = new ProjectNewsBO();
                var err = ProjectNewsTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<ProjectNewsBO> GetLatestNews(int number, int BuilderId = 0)
        {
            GetResponse<ProjectNewsBO> response = new GetResponse<ProjectNewsBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectNewsDAO();
            if (BuilderId != 0)
            {
                var entities = dao.GetNoTracking().Where(m => m.Project.BuilderId == BuilderId).OrderByDescending(m => m.Date).Take(number);
                foreach (var _entity in entities)
                {
                    ProjectNewsBO bo = new ProjectNewsBO();
                    var err = ProjectNewsTranslator.TranslateEntityToBO(_entity, bo);
                    if (err == ErrorCode.Success)
                        response.AddValue(bo);
                    else
                        response.AddError(err.ToString());
                }
            }
            else
            {
                var entities = dao.GetNoTracking().OrderByDescending(m => m.Date).Take(number);
                foreach (var _entity in entities)
                {
                    ProjectNewsBO bo = new ProjectNewsBO();
                    var err = ProjectNewsTranslator.TranslateEntityToBO(_entity, bo);
                    if (err == ErrorCode.Success)
                        response.AddValue(bo);
                    else
                        response.AddError(err.ToString());
                }
            }

            return response;
        }
        public GetResponse<ProjectNewsBO> GetLatestProjectNews(int number, int projectid)
        {
            GetResponse<ProjectNewsBO> response = new GetResponse<ProjectNewsBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectNewsDAO();

            var entities = dao.GetNoTracking().Where(m => m.ProjectId == projectid).OrderByDescending(m => m.Date).Take(number)
                .Include(m => m.Picture);
            foreach (var _entity in entities)
            {
                ProjectNewsBO bo = new ProjectNewsBO();
                var err = ProjectNewsTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }


            return response;
        }
        public Response InsertUpdateNews(ProjectNewsBO NewsItem)
        {
            Response response = new Response();
            if ((string.IsNullOrWhiteSpace(NewsItem.TitleNL)))
                response.AddError("Titel is verplicht");
            if ((!response.Success))
                return response;

            UnitOfWork uow = new UnitOfWork();
            ProjectNews _entity = null/* TODO Change to default(_) if this is not a reference type */;
            if ((NewsItem.Id == 0))
            {
                _entity = uow.GetProjectNewsDAO().GetNew();
                if (NewsItem.Picture is not null)
                    _entity.Picture = uow.GetProjectPicturesDAO().GetNew();
            }
            else
            {
                _entity = uow.GetProjectNewsDAO().GetById(NewsItem.Id);
                if (NewsItem.Picture is not null && NewsItem.Picture.Id == 0)
                    _entity.Picture = uow.GetProjectPicturesDAO().GetNew();
                else
                {
                    var err = ProjectPictureTranslator.TranslateEntityToBO(uow.GetProjectPicturesDAO().GetById(NewsItem.Picture.Id), NewsItem.Picture);
                    if ((err != ErrorCode.Success))
                        response.AddError(err.ToString());
                }
            }
            if ((_entity != null))
            {
                var err = ProjectNewsTranslator.TranslateBOToEntity(_entity, NewsItem, uow);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("Newsitem not found");
            response.AddError(uow.SaveChanges());

            return response;
        }
        public Response DeleteNews(List<int> ids)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();

            foreach (var id in ids)
                uow.GetProjectNewsDAO().DeleteObject(id);
            response.Messages.AddRange(uow.SaveChanges());

            return response;
        }

        // Levels
        public GetResponse<ProjectLevelBO> GetLevelsByProjectId(int id)
        {
            GetResponse<ProjectLevelBO> response = new GetResponse<ProjectLevelBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectLevelsDAO();
            var entities = dao.GetNoTracking().Where(x => x.ProjectId == id);
            foreach (var _entity in entities)
            {
                ProjectLevelBO level = new ProjectLevelBO();

                var err = ProjectLevelTranslator.TranslateEntityToBO(_entity, level);
                if (err == ErrorCode.Success)
                    response.AddValue(level);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public Response InsertUpdateLevel(ProjectLevelBO Level)
        {
            Response response = new Response();
            if ((string.IsNullOrWhiteSpace(Level.Name)))
                response.AddError("Naam is verplicht");
            if ((!response.Success))
                return response;

            UnitOfWork uow = new UnitOfWork();
            ProjectLevels _entity = null/* TODO Change to default(_) if this is not a reference type */;
            if ((Level.Id == 0))
                _entity = uow.GetProjectLevelsDAO().GetNew();
            else
                _entity = uow.GetProjectLevelsDAO().GetById(Level.Id);
            if ((_entity != null))
            {
                var err = ProjectLevelTranslator.TranslateBOToEntity(_entity, Level, uow);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("Level not found");
            response.AddError(uow.SaveChanges());

            return response;
        }

        // Sales
        public GetResponse<ProjectSalesSettingsBO> GetSalesSettings(int projectid)
        {
            GetResponse<ProjectSalesSettingsBO> response = new GetResponse<ProjectSalesSettingsBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectSalesSettingsDAO();
            var _entity = dao.GetNoTracking().Where(m => m.Projectid == projectid).FirstOrDefault();
            ProjectSalesSettingsBO settings = new ProjectSalesSettingsBO();

            var err = ProjectSalesSettingsTranslator.TranslateEntityToBO(_entity, settings);
            if (err == ErrorCode.Success)
                response.Value = settings;
            else
                response.AddError(err.ToString());
            return response;
        }
        public GetResponse<ProjectSalesSettingsBO> GetSalesSettings(List<int> ids)
        {
            GetResponse<ProjectSalesSettingsBO> response = new GetResponse<ProjectSalesSettingsBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectSalesSettingsDAO();
            foreach (var id in ids)
            {
                var _entity = dao.GetNoTracking().Where(m => m.Projectid == id).FirstOrDefault();
                ProjectSalesSettingsBO settings = new ProjectSalesSettingsBO();

                var err = ProjectSalesSettingsTranslator.TranslateEntityToBO(_entity, settings);
                if (err == ErrorCode.Success)
                    response.AddValue(settings);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<ProjectSalesDataBO> GetProjectSalesData(List<int> ids)
        {
            GetResponse<ProjectSalesDataBO> response = new GetResponse<ProjectSalesDataBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitsDAO();

            foreach (var id in ids)
            {
                ProjectSalesDataBO salesdata = new ProjectSalesDataBO();
                salesdata.ProjectId = id;
                salesdata.LivingUnits = dao.GetNoTracking().Where(m => m.ProjectId == id && m.AttachedUnit == null && m.LinkedUnit == null).Where(i => i.Type.GroupId == 1 | i.Type.GroupId == 4).Count();
                salesdata.LivingUnitsSold = dao.GetNoTracking().Where(m => m.ProjectId == id && m.AttachedUnit == null && m.LinkedUnit == null && m.ClientAccountId != null).Where(i => i.Type.GroupId == 1 | i.Type.GroupId == 4).Count();
                if (salesdata.LivingUnits != 0)
                    salesdata.PercentageLivingUnitsSold = (decimal)(salesdata.LivingUnitsSold / (double)salesdata.LivingUnits * 100);
                else
                    salesdata.PercentageLivingUnitsSold = 100;
                salesdata.ValueForSale = dao.GetNoTracking().Where(m => m.ProjectId == id && m.ClientAccountId == null && m.AttachedUnit == null && m.LinkedUnit == null).Sum(m => m.LandValue.HasValue ? m.LandValue : 0 + m.UnitConstructionValue.Sum(x => x.Value) > 0 ? m.UnitConstructionValue.Sum(x => x.Value) : 0) ?? 0;
                salesdata.ValueSold = dao.GetNoTracking().Where(m => m.ProjectId == id && m.AttachedUnit == null && m.LinkedUnit == null).Sum(m => m.LandValueSold.HasValue ? m.LandValueSold : 0 + m.UnitConstructionValue.Sum(x => x.ValueSold) > 0 ? m.UnitConstructionValue.Sum(x => x.ValueSold) : 0) ?? 0;
                if ((salesdata.ValueSold + salesdata.ValueForSale) != 0)
                    salesdata.PercentageSold = (decimal)(salesdata.ValueSold / (salesdata.ValueSold + salesdata.ValueForSale) * 100);
                else
                    salesdata.PercentageSold = 100;

                salesdata.NumberAppartments = dao.GetNoTracking().Where(m => m.ProjectId == id && m.AttachedUnit == null && m.LinkedUnit == null && m.Type.Id == 1).Count();
                salesdata.NumberHouses = dao.GetNoTracking().Where(m => m.ProjectId == id && m.AttachedUnit == null && m.LinkedUnit == null && m.Type.Id == 2).Count();
                salesdata.NumberCommercial = dao.GetNoTracking().Where(m => m.ProjectId == id && m.AttachedUnit == null && m.LinkedUnit == null && m.Type.Id == 10).Count();
                salesdata.StartingPrice = dao.GetNoTracking().Where(m => m.ProjectId == id && m.AttachedUnit == null && m.LinkedUnit == null && m.ClientAccountId == null).Where(i => i.Type.GroupId == 1 | i.Type.GroupId == 4).Min(i => i.UnitConstructionValue.Where(l => l.UnitId == i.Id).Sum(x => x.Value) + i.LandValue) ?? 0;
                response.AddValue(salesdata);
            }
            return response;
        }
        public Response InsertUpdateSalesSettings(ProjectSalesSettingsBO salessettings)
        {
            Response response = new Response();
            if ((salessettings.ProjectId <= 0))
                response.AddError("ProjectID is verplicht");
            if ((!response.Success))
                return response;

            UnitOfWork uow = new UnitOfWork();
            ProjectSalesSettings _entity = null/* TODO Change to default(_) if this is not a reference type */;
            if ((salessettings.SettingsId == 0))
                _entity = uow.GetProjectSalesSettingsDAO().GetNew();
            else
                _entity = uow.GetProjectSalesSettingsDAO().GetById(salessettings.SettingsId);
            if ((_entity != null))
            {
                var err = ProjectSalesSettingsTranslator.TranslateBOToEntity(_entity, salessettings, uow);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("SalesSettings not found");
            response.AddError(uow.SaveChanges());

            return response;
        }
        public Response DeleteSalesSettings(List<int> ids)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();
            foreach (var id in ids)
                uow.GetProjectSalesSettingsDAO().DeleteObject(id);
            response.Messages.AddRange(uow.SaveChanges());
            return response;
        }
        public Response DeleteSalesSettings(List<ProjectSalesSettingsBO> bos)
        {
            return DeleteSalesSettings(bos.Select(s => s.SettingsId).ToList());
        }
        public decimal GetProjectVatPercentage(int projectid)
        {
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectSalesSettingsDAO();
            return (decimal)dao.GetNoTracking().Where(m => m.Projectid == projectid).FirstOrDefault().Vatpercentage;
        }
        // Docs
        public GetResponse<ProjectDocBO> GetProjectDocs(int projectid, ProjectDocType type = 0)
        {
            GetResponse<ProjectDocBO> response = new GetResponse<ProjectDocBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectDocsDAO();
            IEnumerable<ProjectDocs> entities;
            if (type != 0)
                entities = dao.GetNoTracking().Where(m => m.ProjectId == projectid && m.ClientAccountId == null && m.Type == (int)type).OrderByDescending(m => m.Name);
            else
                entities = dao.GetNoTracking().Where(m => m.ProjectId == projectid && m.ClientAccountId == null).OrderByDescending(m => m.Name);
            foreach (var _entity in entities)
            {
                ProjectDocBO bo = new ProjectDocBO();
                var err = ProjectDocsTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<IdNameBO> GetProjectDocsForSelect(int projectid, ProjectDocType type = ProjectDocType.Sales)
        {
            GetResponse<IdNameBO> response = new GetResponse<IdNameBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectDocsDAO();
            var entities = dao.GetNoTracking().Where(m => m.ProjectId == projectid && m.Type == (int)type && m.ClientAccountId == null).OrderByDescending(m => m.Name);
            foreach (var _entity in entities)
                response.AddValue(_entity.GetIdName());
            return response;
        }
        public GetResponse<ProjectDocBO> GetProjectDoc(int docid)
        {
            GetResponse<ProjectDocBO> response = new GetResponse<ProjectDocBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectDocsDAO();
            var _entity = dao.GetNoTracking().Where(m => m.Id == docid).FirstOrDefault();

            ProjectDocBO bo = new ProjectDocBO();
            var err = ProjectDocsTranslator.TranslateEntityToBO(_entity, bo);
            if (err == ErrorCode.Success)
                response.AddValue(bo);
            else
                response.AddError(err.ToString());

            return response;
        }
        public GetResponse<ProjectDocBO> GetClientDocs(int clientaccountid)
        {
            GetResponse<ProjectDocBO> response = new GetResponse<ProjectDocBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectDocsDAO();
            var entities = dao.GetNoTracking().Where(m => m.ClientAccountId == clientaccountid).OrderByDescending(m => m.Name);
            foreach (var _entity in entities)
            {
                ProjectDocBO bo = new ProjectDocBO();
                var err = ProjectDocsTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<ProjectDocBO> GetLatestProjectDocs(int number, int projectid)
        {
            GetResponse<ProjectDocBO> response = new GetResponse<ProjectDocBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectDocsDAO();

            var entities = dao.GetNoTracking().Where(m => m.ProjectId == projectid && m.ClientAccountId == null).OrderByDescending(m => m.Date).Take(number);
            foreach (var _entity in entities)
            {
                ProjectDocBO bo = new ProjectDocBO();
                var err = ProjectDocsTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }


            return response;
        }
        public GetResponse<ProjectDocBO> GetLatestClientDocs(int number, int clientaccountid)
        {
            GetResponse<ProjectDocBO> response = new GetResponse<ProjectDocBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectDocsDAO();

            var entities = dao.GetNoTracking().Where(m => m.ClientAccountId == clientaccountid).OrderByDescending(m => m.Date).Take(number);
            foreach (var _entity in entities)
            {
                ProjectDocBO bo = new ProjectDocBO();
                var err = ProjectDocsTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }


            return response;
        }
        public Response InsertUpdateProjectDoc(ProjectDocBO ProjectDoc)
        {
            Response response = new Response();
            if ((ProjectDoc.ProjectId <= 0))
                response.AddError("ProjectID is verplicht");
            if ((ProjectDoc.Filename == null || ProjectDoc.Filename == ""))
                response.AddError("Bestandsnaame is verplicht");
            // If (ProjectDoc.Name Is Nothing OrElse ProjectDoc.Name = "") Then
            // response.AddError("Naam is verplicht")
            // End If
            if ((!response.Success))
                return response;

            UnitOfWork uow = new UnitOfWork();
            ProjectDocs _entity = null/* TODO Change to default(_) if this is not a reference type */;
            IdNameBO _delentity = null/* TODO Change to default(_) if this is not a reference type */;
            if ((ProjectDoc.Docid == 0))
            {
                if (ProjectDoc.Type == ProjectDocType.keycombinationcertificate)
                    _delentity = GetProjectDocsForSelect(ProjectDoc.ProjectId, ProjectDocType.keycombinationcertificate).Values.FirstOrDefault();
                _entity = uow.GetProjectDocsDAO().GetNew();
                ProjectDoc.SortOrder = uow.GetProjectDocsDAO().GetNoTracking().Where(m => m.ProjectId == ProjectDoc.ProjectId).Select(a => a.SortOrder).DefaultIfEmpty(0).Max() + 1;
            }
            else
                _entity = uow.GetProjectDocsDAO().GetById(ProjectDoc.Docid);
            if ((_entity != null))
            {
                var err = ProjectDocsTranslator.TranslateBOToEntity(_entity, ProjectDoc, uow);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
                else if (_delentity is not null)
                    uow.GetProjectDocsDAO().DeleteObject(_delentity.ID);
            }
            else
                response.AddError("ProjectDoc not found");
            response.AddError(uow.SaveChanges());

            return response;
        }
        public Response DeleteProjectDoc(List<int> ids)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();
            foreach (var id in ids)
                uow.GetProjectDocsDAO().DeleteObject(id);
            response.Messages.AddRange(uow.SaveChanges());
            return response;
        }
        public Response DeleteProjectDoc(List<ProjectDocBO> bos)
        {
            return DeleteProjectDoc(bos.Select(s => s.Docid).ToList());
        }


        // PaymentGroups
        public GetResponse<ProjectPaymentGroupBO> GetProjectPaymentGroups(int projectid)
        {
            GetResponse<ProjectPaymentGroupBO> response = new GetResponse<ProjectPaymentGroupBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectPaymentGroupsDAO();
            var entities = dao.GetNoTracking().Where(m => m.ProjectId == projectid).OrderByDescending(m => m.Name);
            foreach (var _entity in entities)
            {
                ProjectPaymentGroupBO bo = new ProjectPaymentGroupBO();
                var err = ProjectPaymentGroupTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            response.Values = response.Values.OrderBy(m => m.Name).ToList();
            return response;
        }
        public GetResponse<ProjectPaymentGroupBO> GetProjectPaymentGroup(int groupid)
        {
            GetResponse<ProjectPaymentGroupBO> response = new GetResponse<ProjectPaymentGroupBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectPaymentGroupsDAO();
            var _entity = dao.GetNoTracking().Where(m => m.Id == groupid).FirstOrDefault();

            ProjectPaymentGroupBO bo = new ProjectPaymentGroupBO();
            var err = ProjectPaymentGroupTranslator.TranslateEntityToBO(_entity, bo);
            if (err == ErrorCode.Success)
                response.AddValue(bo);
            else
                response.AddError(err.ToString());
            return response;
        }
        public GetResponse<IdNameBO> GetProjectPaymentGroupsForSelect(int projectid)
        {
            GetResponse<IdNameBO> response = new GetResponse<IdNameBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectPaymentGroupsDAO();
            var entities = dao.GetNoTracking().Where(m => m.ProjectId == projectid);
            foreach (var _entity in entities)
                response.AddValue(_entity.GetIdName());
            response.Values = response.Values.OrderBy(m => m.Display).ToList();
            return response;
        }
        public Response InsertUpdateProjectPaymentGroup(ProjectPaymentGroupBO ProjectPaymentGroup)
        {
            Response response = new Response();
            if ((ProjectPaymentGroup.ProjectId <= 0))
                response.AddError("ProjectID is verplicht");
            if ((ProjectPaymentGroup.Name == null || ProjectPaymentGroup.Name == ""))
                response.AddError("Naam is verplicht");
            if ((!response.Success))
                return response;
            UnitOfWork uow = new UnitOfWork();
            InvoicingPaymentGroup _entity = null/* TODO Change to default(_) if this is not a reference type */;
            if ((ProjectPaymentGroup.Id == 0))
                _entity = uow.GetProjectPaymentGroupsDAO().GetNew();
            else
                _entity = uow.GetProjectPaymentGroupsDAO().GetById(ProjectPaymentGroup.Id);
            if ((_entity != null))
            {
                var err = ProjectPaymentGroupTranslator.TranslateBOToEntity(_entity, ProjectPaymentGroup, uow);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("ProjectPaymentGroup not found");
            response.AddError(uow.SaveChanges());

            return response;
        }
        public void LinkPaymentGroupToUnit(int unitid, int paymentgroupid)
        {
            UnitOfWork uow = new UnitOfWork();
            Units _entity = null/* TODO Change to default(_) if this is not a reference type */;
            _entity = uow.GetUnitsDAO().GetById(unitid);
            if ((_entity != null))
                _entity.PaymentGroupId = paymentgroupid;
            uow.SaveChanges();
        }
        public Response DeleteProjectPaymentGroup(List<int> ids)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();
            foreach (var id in ids)
                uow.GetProjectPaymentGroupsDAO().DeleteObject(id);
            response.Messages.AddRange(uow.SaveChanges());
            return response;
        }
        public Response DeleteProjectPaymentGroup(List<ProjectPaymentGroupBO> bos)
        {
            return DeleteProjectPaymentGroup(bos.Select(s => s.Id).ToList());
        }

        // PaymentStagessq
        //public GetResponse<ProjectPaymentStageBO> GetProjectPaymentStages(int groupid)
        //{
        //}
        public GetResponse<ProjectPaymentStageBO> GetProjectPaymentStage(int stageid)
        {
            GetResponse<ProjectPaymentStageBO> response = new GetResponse<ProjectPaymentStageBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectPaymentStagesDAO();
            var _entity = dao.GetNoTracking().Where(m => m.Id == stageid).FirstOrDefault();

            ProjectPaymentStageBO bo = new ProjectPaymentStageBO();
            var err = ProjectPaymentStageTranslator.TranslateEntityToBO(_entity, bo);
            if (err == ErrorCode.Success)
                response.AddValue(bo);
            else
                response.AddError(err.ToString());
            return response;
        }
        public GetResponse<UnitWithStagesBO> GetProjectInvoicableUnits(int projectid)
        {
            GetResponse<UnitWithStagesBO> response = new GetResponse<UnitWithStagesBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUnitsDAO();
            var dao2 = uow.GetProjectPaymentStagesDAO();
            var entities = dao.GetNoTracking().Where(m => m.ProjectId == projectid && m.UnitConstructionValue.Any(l => l.PaymentGroup.InvoicingPaymentStages.Any(i => i.Invoicable == true)) && m.ClientAccountId > 0 && m.ClientAccount.DateDeedOfSale != null);
            foreach (var _entity in entities)
            {
                UnitWithStagesBO bo = new UnitWithStagesBO();
                var stages = dao2.GetNoTracking().Where(m => m.InvoicesDetails.Where(i => i.UnitId == _entity.Id).Count() == 0 && m.Invoicable == true && m.Group.UnitConstructionValue.Any(l => l.UnitId == _entity.Id));
                if (stages.Count() > 0)
                {
                    UnitBO unitbo = new UnitBO();
                    foreach (var stage in stages)
                    {
                        ProjectPaymentStageBO stagebo = new ProjectPaymentStageBO();
                        var err2 = ProjectPaymentStageTranslator.TranslateEntityToBO(stage, stagebo);
                        if (err2 == ErrorCode.Success)
                            bo.PaymentStages.Add(stagebo);
                        else
                            response.AddError(err2.ToString());
                    }
                    var err = UnitTranslator.TranslateEntityToBO(_entity, unitbo);
                    if (err == ErrorCode.Success)
                    {
                        bo.Unit = unitbo;
                        response.AddValue(bo);
                    }
                    else
                        response.AddError(err.ToString());
                }
            }
            return response;
        }

        public Response InsertUpdateProjectPaymentStage(ProjectPaymentStageBO ProjectPaymentStage)
        {
            Response response = new Response();
            if ((ProjectPaymentStage.GroupId <= 0))
                response.AddError("GroupID is verplicht");
            if ((ProjectPaymentStage.Name == null || ProjectPaymentStage.Name == ""))
                response.AddError("Naam is verplicht");
            if ((!response.Success))
                return response;
            UnitOfWork uow = new UnitOfWork();
            InvoicingPaymentStages _entity = null/* TODO Change to default(_) if this is not a reference type */;
            if ((ProjectPaymentStage.Id == 0))
                _entity = uow.GetProjectPaymentStagesDAO().GetNew();
            else
                _entity = uow.GetProjectPaymentStagesDAO().GetById(ProjectPaymentStage.Id);
            if ((_entity != null))
            {
                var err = ProjectPaymentStageTranslator.TranslateBOToEntity(_entity, ProjectPaymentStage, uow);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("ProjectPaymentStage not found");
            response.AddError(uow.SaveChanges());

            return response;
        }
        public Response UpdateProjectPaymentStageInvoicable(int stageid, bool invoicable)
        {
            Response response = new Response();
            if ((stageid <= 0))
                response.AddError("StageId is verplicht");
            if ((!response.Success))
                return response;
            UnitOfWork uow = new UnitOfWork();
            InvoicingPaymentStages _entity = null/* TODO Change to default(_) if this is not a reference type */;
            _entity = uow.GetProjectPaymentStagesDAO().GetById(stageid);
            if ((_entity != null))
                _entity.Invoicable = invoicable;
            else
                response.AddError("ProjectPaymentStage not found");
            response.AddError(uow.SaveChanges());
            return response;
        }
        //public Response DeleteProjectPaymentStage(List<int> ids)
        //{
        //}
        //public Response DeleteProjectPaymentStage(List<ProjectPaymentStageBO> bos)
        //{
        //}
        public bool CheckProjectPaymentStageDocInUse(int docid)
        {
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProjectPaymentStagesDAO();
            int @int = dao.GetNoTracking().Where(m => m.DocId == docid).Count();
            if (@int == 0)
                return false;
            else
                return true;
        }

        // INVOICING
        public GetResponse<ChangeOrderBO> GetProjectInvoicableChangeOrders(int projectid)
        {
            GetResponse<ChangeOrderBO> response = new GetResponse<ChangeOrderBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetChangeOrderDAO();
            var entities = dao.GetNoTracking().Where(m => m.ContractActivity.Contract.ProjectId == projectid && m.ChangeOrderDetail.Any(i => i.Invoicable == true && i.Invoiced == false) && m.ClientAccountId > 0 && m.ClientAccount.DateDeedOfSale != null && m.ChangeOrderDetail.Where(i => i.Invoiced == true).Count() < m.ChangeOrderDetail.Count());
            foreach (var _entity in entities)
            {
                ChangeOrderBO bo = new ChangeOrderBO();
                var err = ChangeOrderTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            // 
            // 
            return response;
        }
        //public GetResponse<UnitBO> GetProjectInvoiceableLandValue(int projectid)
        //{
        //}
        public GetResponse<InvoiceBO> GetInvoicesByUnitIds(List<int> UnitIds)
        {
            GetResponse<InvoiceBO> response = new GetResponse<InvoiceBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetInvoicesDAO();
            var entities = dao.GetNoTracking().Where(InvoicesQuery.GetUnitsQuery(UnitIds));
            foreach (var _entity in entities)
            {
                InvoiceBO bo = new InvoiceBO();
                var err = InvoiceTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }

            return response;
        }
        public GetResponse<UtilityCostBO> GetProjectUtilityCost(int projectid, int clientid)
        {
            GetResponse<UtilityCostBO> response = new GetResponse<UtilityCostBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetContractActivityDAO();
            var dao2 = uow.GetIncommingInvoicesDetailDAO();
            var udao = uow.GetUnitsDAO();
            var utilitydao = uow.GetUtilityPercentageDAO();
            var numberofunits = udao.GetNoTracking().Where(m => m.ProjectId == projectid & (m.Type.GroupId == 1 | m.Type.GroupId == 4)).Count();
            var entities = dao.GetNoTracking().Where(m => m.Contract.ProjectId == projectid & m.ActivityId == 280);
            // Alle contractlijnen die nutsaansluitingen zijn
            foreach (var _entity in entities)
            {
                decimal price = 0;
                foreach (var fact in dao2.GetNoTracking().Where(m => m.ContractAct.ActivityId == 280 & m.Type == (decimal)IncommingInvoiceType.Contract & m.IncommingInvoice.ContractId == _entity.ContractId))
                    price = price + (decimal)fact.Price;
                // price = dao2.GetNoTracking.Where(Function(m) m.ContractID = _entity.ContractID).Sum(Function(l) l.IncommingInvoiceDetail.Where(Function(s) s.ContractActivity.ActivityID = 280 And s.Type = IncommingInvoiceType.Contract).Count)
                // price = dao2.GetNoTracking.Where(Function(m) m.ContractID = _entity.ContractID).Sum(Function(l) l.IncommingInvoiceDetail.Where(Function(s) s.ContractActivity.ActivityID = 280 And s.Type = IncommingInvoiceType.Contract).Sum(Function(i) i.Price))

                if (price <= _entity.Price)
                {
                    decimal percentage = 0;
                    decimal percentageother = 0;
                    if ((utilitydao.GetNoTracking().Where(m => m.ContractId == _entity.ContractId & m.ClientAccountId == clientid).Count() > 0))
                        percentage = utilitydao.GetNoTracking().Where(m => m.ContractId == _entity.ContractId & m.ClientAccountId == clientid).Sum(s => s.Percentage);
                    if ((utilitydao.GetNoTracking().Where(m => m.ContractId == _entity.ContractId).Count() > 0))
                        percentageother = utilitydao.GetNoTracking().Where(m => m.ContractId == _entity.ContractId).Sum(s => s.Percentage);
                    var otherunits = utilitydao.GetNoTracking().Where(m => m.ContractId == _entity.ContractId).Count();
                    decimal clientpercentage;
                    if (percentage == 0)
                        clientpercentage = (100 - percentageother) / (decimal)(numberofunits - otherunits);
                    else
                        clientpercentage = percentage;

                    UtilityCostBO bo = new UtilityCostBO();
                    bo.ProjectId = projectid;
                    bo.Price = (decimal)_entity.Price - price;
                    bo.Description = "- NOG TE FACTUREREN -";
                    bo.CompanyName = _entity.Contract.Company.BedrijfsNaam;
                    bo.Percentage = clientpercentage;
                    response.AddValue(bo);
                }
            }
            var dao3 = uow.GetIncommingInvoicesDetailDAO();
            var entities3 = dao3.GetNoTracking().Where(m => m.IncommingInvoice.ProjectId == projectid & (m.ActId == 280 | m.ContractAct.ActivityId == 280));
            foreach (var _entity in entities3)
            {
                decimal percentage = 0;
                decimal percentageother = 0;
                if ((utilitydao.GetNoTracking().Where(m => m.IncommingInvoiceDetailId == _entity.Id & m.ClientAccountId == clientid).Count() > 0))
                    percentage = utilitydao.GetNoTracking().Where(m => m.IncommingInvoiceDetailId == _entity.Id & m.ClientAccountId == clientid).Sum(s => s.Percentage);
                if ((utilitydao.GetNoTracking().Where(m => m.IncommingInvoiceDetailId == _entity.Id).Count() > 0))
                    percentageother = utilitydao.GetNoTracking().Where(m => m.IncommingInvoiceDetailId == _entity.Id).Sum(s => s.Percentage);
                var otherunits = utilitydao.GetNoTracking().Where(m => m.IncommingInvoiceDetailId == _entity.Id).Count();
                decimal clientpercentage;
                if (percentage == 0)
                    clientpercentage = (100 - percentageother) / (decimal)(numberofunits - otherunits);
                else
                    clientpercentage = percentage;
                UtilityCostBO bo = new UtilityCostBO();
                bo.ProjectId = projectid;
                bo.Price = (decimal)_entity.Price;
                bo.Description = _entity.Description;
                if (_entity.IncommingInvoice.CompanyId == null)
                    bo.CompanyName = _entity.IncommingInvoice.Contract.Company.BedrijfsNaam;
                else
                    bo.CompanyName = _entity.IncommingInvoice.Company.BedrijfsNaam;
                bo.Percentage = clientpercentage;
                response.AddValue(bo);
            }
            return response;
        }

        public Response InsertUpdateProjectInvoice(InvoiceBO Invoice)
        {
            Response response = new Response();


            if ((Invoice.Filename == null | Invoice.Filename == ""))
                response.AddError("Bestandsnaam is verplicht");
            if (Invoice.Rows.Count == 0)
                response.AddError("Er is minstens één detailrij nodig");


            if ((!response.Success))
                return response;
            UnitOfWork uow = new UnitOfWork();
            Invoices _entity = null/* TODO Change to default(_) if this is not a reference type */;
            if ((Invoice.Id == 0))
                _entity = uow.GetInvoicesDAO().GetNew();
            else
                _entity = uow.GetInvoicesDAO().GetById(Invoice.Id);
            if ((_entity != null))
            {
                var err = InvoiceTranslator.TranslateBOToEntity(_entity, Invoice, uow);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("Invoice not found");
            response.AddError(uow.SaveChanges());

            return response;
        }
        public Response InsertUpdateProjectInvoices(List<InvoiceBO> Invoices)
        {
            Response response = new Response();
            foreach (var invoice in Invoices)
            {
                if ((invoice.Filename == null | invoice.Filename == ""))
                    response.AddError("Bestandsnaam is verplicht");
                if (invoice.Rows.Count == 0)
                    response.AddError("Er is minstens één detailrij nodig");
                if ((!response.Success))
                    return response;
                UnitOfWork uow = new UnitOfWork();
                Invoices _entity = null/* TODO Change to default(_) if this is not a reference type */;
                if ((invoice.Id == 0))
                    _entity = uow.GetInvoicesDAO().GetNew();
                else
                    _entity = uow.GetInvoicesDAO().GetById(invoice.Id);
                if ((_entity != null))
                {
                    var err = InvoiceTranslator.TranslateBOToEntity(_entity, invoice, uow);
                    if ((err != ErrorCode.Success))
                        response.AddError(err.ToString());
                }
                else
                    response.AddError("Invoice not found");
                response.AddError(uow.SaveChanges());
            }
            return response;
        }

        // Contracts
        public GetResponse<ContractBO> GetProjectContracts(int projectid)
        {
            GetResponse<ContractBO> response = new GetResponse<ContractBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetContractDAO();
            var entities = dao.GetNoTracking().Where(m => m.ProjectId == projectid)
                .Include(m => m.ContractActivity)
                .ThenInclude(m => m.Activity)
                .ThenInclude(m => m.Group)
                .Include(m => m.Company);
            foreach (var _entity in entities)
            {
                ContractBO bo = new ContractBO();
                var err = ContractTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<IdNameBO> GetProjectContractsForSelect(int projectid)
        {
            GetResponse<IdNameBO> response = new GetResponse<IdNameBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetContractDAO();
            var entities = dao.GetNoTracking().Where(m => m.ProjectId == projectid)
                .Include(m => m.Company)
                .Include(m => m.ContractActivity)
                .ThenInclude(m => m.Activity);
            foreach (var _entity in entities)
                response.AddValue(_entity.GetIdName());
            return response;
        }
        public GetResponse<IdNameBO> GetProjectContractActivitiesForSelect(int projectid)
        {
            GetResponse<IdNameBO> response = new GetResponse<IdNameBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetContractDAO();
            var entities = dao.GetNoTracking().Where(m => m.ProjectId == projectid);
            foreach (var _entity in entities)
            {
                foreach (ContractActivity act in _entity.ContractActivity)
                    response.AddValue(act.GetIdName());
            }
            return response;
        }
        public Response InsertUpdateProjectContract(ContractBO Contract)
        {
            Response response = new Response();


            if ((Contract.Company.ID == 0))
                response.AddError("Bedrijf selecteren is verplicht");
            if (Contract.Activities.Count == 0)
                response.AddError("Er is minstens één lot nodig");
            if ((!response.Success))
                return response;
            UnitOfWork uow = new UnitOfWork();
            Contract _entity = null/* TODO Change to default(_) if this is not a reference type */;
            if ((Contract.Id == 0))
                _entity = uow.GetContractDAO().GetNew();
            else
                _entity = uow.GetContractDAO().GetNormal()
                    .Where(m => m.Id == Contract.Id)
                    .Include(m => m.ContractActivity)
                    .ThenInclude(m => m.Activity)
                    .SingleOrDefault();
            if ((_entity != null))
            {
                var err = ContractTranslator.TranslateBOToEntity(_entity, Contract, uow);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("Contract not found");
            response.AddError(uow.SaveChanges());

            return response;
        }
        public GetResponse<ContractBO> GetContract(int contractid)
        {
            GetResponse<ContractBO> response = new GetResponse<ContractBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetContractDAO();
            var _entity = dao.GetNormal().Where(m => m.Id == contractid)
                .Include(m => m.Company)
                .Include(m => m.ContractActivity)
                .ThenInclude(m => m.Activity)
                .Include(m => m.ContractActivity)
                .ThenInclude(m => m.ChangeOrder)
                .FirstOrDefault();
            ContractBO bo = new ContractBO();
            var err = ContractTranslator.TranslateEntityToBO(_entity, bo);
            if (err == ErrorCode.Success)
            {
                response.AddValue(bo);
                return response;
            }
            else
            {
                response.AddError(err.ToString());
                return response;
            }
                
        }

        public Response DeleteContracts(List<int> ids)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();
            foreach (var id in ids)
                uow.GetContractDAO().DeleteObject(id);
            response.Messages.AddRange(uow.SaveChanges());
            return response;
        }
        public GetResponse<IdNameBO> GetContractChangeOrdersForSelect(int contractid)
        {
            GetResponse<IdNameBO> response = new GetResponse<IdNameBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetChangeOrderDAO();
            var entities = dao.GetNoTracking().Where(m => m.ContractActivity.ContractId == contractid);
            foreach (var _entity in entities)
                response.AddValue(_entity.GetIdName());
            return response;
        }

        public GetResponse<ContractBO> GetProjectContractsWithoutInvoices(int projectid, int activityid = 0)
        {
            GetResponse<ContractBO> response = new GetResponse<ContractBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetContractDAO();
            IEnumerable<Contract> entities;
            if (activityid == 0)
                entities = dao.GetNoTracking().Where(m => m.ProjectId == projectid && m.IncommingInvoices.Count == 0)
                    .Include(m => m.ContractActivity)
                    .ThenInclude(m => m.Activity)
                    .ThenInclude(m => m.Group)
                    .Include(m => m.Company);
            else
                entities = dao.GetNoTracking().Where(m => m.ProjectId == projectid && m.ContractActivity.Where(s => s.ActivityId == activityid).Count() > 0 && m.IncommingInvoices.Where(l => l.IncommingInvoiceDetail.Where(i => i.ContractAct.ActivityId == activityid).Count() > 0).Count() == 0)
                    .Include(m => m.ContractActivity)
                    .ThenInclude(m => m.Activity)
                    .ThenInclude(m => m.Group)
                    .Include(m => m.Company);

            foreach (var _entity in entities)
            {
                ContractBO bo = new ContractBO();
                var err = ContractTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public decimal GetContractActivityPrice(int contractactid)
        {
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetContractActivityDAO();
            return (decimal)dao.GetById(contractactid).Price;
        }
        public GetResponse<ContractActivityBO> GetProjectContractActivitiesByActivityId(int projectid, int activityid)
        {
            GetResponse<ContractActivityBO> response = new GetResponse<ContractActivityBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetContractActivityDAO();
            var entities = dao.GetNormal().Where(m => m.Contract.ProjectId == projectid & m.ActivityId == activityid)
                .Include(m => m.Activity)
                .ThenInclude(m => m.Group)
                .Include(m => m.Insurances)
                .ThenInclude(m => m.InsuranceCompany);
            foreach (var _entity in entities)
            {
                ContractActivityBO bo = new ContractActivityBO();
                var err = ContractActivityTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }

        // Budget
        public GetResponse<BudgetActivityBO> GetProjectBudget(int projectid)
        {
            GetResponse<BudgetActivityBO> response = new GetResponse<BudgetActivityBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetBudgetDAO();
            var entities = dao.GetNoTracking().Where(m => m.ProjectId == projectid)
                .Include(m => m.Activity)
                .ThenInclude(m => m.Group);
            foreach (var _entity in entities)
            {
                BudgetActivityBO bo = new BudgetActivityBO();
                var err = BudgetTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public Response InsertUpdateProjectBudgetActivity(BudgetActivityBO BudgetActivity)
        {
            Response response = new Response();
            if ((BudgetActivity.ProjectId == 0))
                response.AddError("Project is niet geselecteerd");
            if (BudgetActivity.Activity.ID == 0)
                response.AddError("Er is geen activiteit geselecteerd");

            if ((!response.Success))
                return response;
            UnitOfWork uow = new UnitOfWork();
            ProjectBudget _entity = null/* TODO Change to default(_) if this is not a reference type */;
            if ((BudgetActivity.Id == 0))
                _entity = uow.GetBudgetDAO().GetNew();
            else
                _entity = uow.GetBudgetDAO().GetById(BudgetActivity.Id);
            if ((_entity != null))
            {
                var err = BudgetTranslator.TranslateBOToEntity(_entity, BudgetActivity, uow);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("BudgetActivity not found");
            response.AddError(uow.SaveChanges());

            return response;
        }
        public Response InsertUpdateProjectBudgetActivities(List<BudgetActivityBO> BudgetActivities, int projectid)
        {
            Response response = new Response();
            foreach (var BudgetActivity in BudgetActivities)
            {
                if ((BudgetActivity.ProjectId == 0))
                    response.AddError("Project is niet geselecteerd");
                if (BudgetActivity.Activity.ID == 0)
                    response.AddError("Er is geen activiteit geselecteerd");

                if ((!response.Success))
                    return response;
                UnitOfWork uow = new UnitOfWork();
                ProjectBudget _entity = null/* TODO Change to default(_) if this is not a reference type */;
                if ((BudgetActivity.Id == 0))
                    _entity = uow.GetBudgetDAO().GetNew();
                else
                    _entity = uow.GetBudgetDAO().GetById(BudgetActivity.Id);
                if ((_entity != null))
                {
                    var err = BudgetTranslator.TranslateBOToEntity(_entity, BudgetActivity, uow);
                    if ((err != ErrorCode.Success))
                        response.AddError(err.ToString());
                }
                else
                    response.AddError("BudgetActivity not found");
                response.AddError(uow.SaveChanges());
            }
            // Verwijderen oude loten
            UnitOfWork uow2 = new UnitOfWork();
            var dao = uow2.GetBudgetDAO();
            var _entities = dao.GetNoTracking().Where(m => m.ProjectId == projectid);
            List<ProjectBudget> delList = new List<ProjectBudget>();
            foreach (var x in _entities)
            {
                if ((!BudgetActivities.Any(f => f.Id == x.Id) && !BudgetActivities.Any(f => f.Id == 0)))
                    delList.Add(x);
            }
            foreach (var x in delList)
                uow2.GetBudgetDAO().DeleteObject(x.Id);
            response.Messages.AddRange(uow2.SaveChanges());
            return response;
        }
        // ChangeOrders
        public GetResponse<ChangeOrderBO> GetProjectChangeOrders(int projectid)
        {
            GetResponse<ChangeOrderBO> response = new GetResponse<ChangeOrderBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetChangeOrderDAO();
            var entities = dao.GetNoTracking().Where(m => m.ContractActivity.Contract.ProjectId == projectid);
            foreach (var _entity in entities)
            {
                ChangeOrderBO bo = new ChangeOrderBO();
                var err = ChangeOrderTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<ChangeOrderBO> GetClientChangeOrders(int clientaccountid)
        {
            GetResponse<ChangeOrderBO> response = new GetResponse<ChangeOrderBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetChangeOrderDAO();
            var entities = dao.GetNoTracking().Where(m => m.ClientAccountId == clientaccountid);
            foreach (var _entity in entities)
            {
                ChangeOrderBO bo = new ChangeOrderBO();
                var err = ChangeOrderTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<ChangeOrderBO> GetChangeOrder(int changeorderid)
        {
            GetResponse<ChangeOrderBO> response = new GetResponse<ChangeOrderBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetChangeOrderDAO();
            var _entity = dao.GetById(changeorderid);
            ChangeOrderBO bo = new ChangeOrderBO();
            var err = ChangeOrderTranslator.TranslateEntityToBO(_entity, bo);
            if (err == ErrorCode.Success)
            {
                response.AddValue(bo);
                return response;
            }
            else
            {
                response.AddError(err.ToString());
                return response;
            }
        }
        public Response InsertUpdateProjectChangeOrder(ChangeOrderBO changeorder)
        {
            Response response = new Response();
            if ((changeorder.ClientAccountID == 0))
                response.AddError("ClientAccount is niet geselecteerd");
            if (changeorder.ContractActivityID == 0)
                response.AddError("Er is geen activiteit geselecteerd");
            if ((!response.Success))
                return response;
            UnitOfWork uow = new UnitOfWork();
            ChangeOrder _entity = null/* TODO Change to default(_) if this is not a reference type */;
            if ((changeorder.Id == 0))
                _entity = uow.GetChangeOrderDAO().GetNew();
            else
                _entity = uow.GetChangeOrderDAO().GetById(changeorder.Id);
            if ((_entity != null))
            {
                var err = ChangeOrderTranslator.TranslateBOToEntity(_entity, changeorder, uow);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("Change Order not found");
            response.AddError(uow.SaveChanges());

            return response;
        }
        //public Response InsertUpdateProjectChangeOrders(List<ChangeOrderBO> changeorders, int projectid)
        //{
        //}
        public Response DeleteChangeOrders(List<int> ids)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();
            foreach (var id in ids)
                uow.GetChangeOrderDAO().DeleteObject(id);
            response.Messages.AddRange(uow.SaveChanges());
            return response;
        }
        public Response UpdateProjectChangeOrderInvoicable(int COid, bool invoicable)
        {
            Response response = new Response();
            if ((COid <= 0))
                response.AddError("COid is verplicht");
            if ((!response.Success))
                return response;
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetChangeOrderDetailDAO();
            var entities = dao.GetNormal().Where(m => m.ChangeOrderId == COid);
            foreach (var _entity in entities)
                _entity.Invoicable = invoicable;
            response.AddError(uow.SaveChanges());
            return response;
        }
        public Response UpdateProjectChangeOrderDetailInvoicable(int CODetailid, bool invoicable)
        {
            Response response = new Response();
            if ((CODetailid <= 0))
                response.AddError("CODetailid is verplicht");
            if ((!response.Success))
                return response;
            UnitOfWork uow = new UnitOfWork();
            ChangeOrderDetail _entity = null/* TODO Change to default(_) if this is not a reference type */;
            _entity = uow.GetChangeOrderDetailDAO().GetById(CODetailid);
            if ((_entity != null))
                _entity.Invoicable = invoicable;
            else
                response.AddError("ChangeOrderDetail not found");
            response.AddError(uow.SaveChanges());
            return response;
        }
        public Response SetChangeOrderDetailInvoiced(int codid)
        {
            Response response = new Response();
            if ((codid == 0))
                response.AddError("Geen OrderDetail ingegeven");
            if ((!response.Success))
                return response;
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetChangeOrderDetailDAO();
            var _entity = dao.GetById(codid);
            if ((_entity != null))
                _entity.Invoiced = true;
            else
                response.AddError("Change Order not found");
            response.AddError(uow.SaveChanges());
            return response;
        }

        // Incomming Invoices
        public GetResponse<IncommingInvoiceBO> GetIncommingInvoice(int invoiceid)
        {
            GetResponse<IncommingInvoiceBO> response = new GetResponse<IncommingInvoiceBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetIncommingInvoicesDAO();
            var _entity = dao.GetNoTracking()
                .Where(m => m.Id == invoiceid)
                .Include(m => m.Company)
                .Include(m => m.Project)
                .Include(m => m.IncommingInvoiceDetail)
                .ThenInclude(m => m.Act)
                .ThenInclude(m => m.Group)
                .Include(m => m.IncommingInvoiceDetail)
                .ThenInclude(m => m.ContractAct)
                .ThenInclude(m => m.Activity)
                .ThenInclude(m => m.Group)
                .Include(m => m.IncommingInvoiceDetail)
                .ThenInclude(m => m.ChangeOrder)
                .ThenInclude(m => m.ChangeOrderDetail)
                 .Include(m => m.Contract)
                .ThenInclude(m => m.Company)
                .SingleOrDefault();
            IncommingInvoiceBO bo = new IncommingInvoiceBO();
            var err = IncommingInvoiceTranslator.TranslateEntityToBO(_entity, bo);
            if (err == ErrorCode.Success)
            {
                response.AddValue(bo);
                return response;
            }
            else
            {
                response.AddError(err.ToString());
                return response;
            }
                
        }
        public Response InsertUpdateProjectIncommingInvoice(IncommingInvoiceBO invoice)
        {
            Response response = new Response();
            if ((invoice.ContractID == 0 && invoice.CompanyId == 0))
                response.AddError("De leverancier is niet geselecteerd");

            if ((!response.Success))
                return response;
            UnitOfWork uow = new UnitOfWork();
            IncommingInvoices _entity = null/* TODO Change to default(_) if this is not a reference type */;
            if ((invoice.Id == 0))
                _entity = uow.GetIncommingInvoicesDAO().GetNew();
            else
                _entity = uow.GetIncommingInvoicesDAO().GetNormal()
                    .Where(m => m.Id == invoice.Id)
                    .Include(m => m.IncommingInvoiceDetail)
                    .SingleOrDefault();
            if ((_entity != null))
            {
                var err = IncommingInvoiceTranslator.TranslateBOToEntity(_entity, invoice, uow);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("Incomming Invoice not found");
            response.AddError(uow.SaveChanges());

            return response;
        }
        public GetResponse<IncommingInvoiceActivityBO> GetProjectIncommingInvoicesForRecalculation(int projectid)
        {
            GetResponse<IncommingInvoiceActivityBO> response = new GetResponse<IncommingInvoiceActivityBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetIncommingInvoicesDetailDAO();
            var entities = dao.GetNoTracking().Where(m => m.IncommingInvoice.ProjectId == projectid)
                .Include(m => m.IncommingInvoice)
                .ThenInclude(m => m.Contract)
                .ThenInclude(m => m.Company)
                .Include(m => m.IncommingInvoice)
                .ThenInclude(m => m.Company)
                .Include(m => m.ContractAct)
                .ThenInclude(m => m.Contract)
                .ThenInclude(m => m.Company)
                .Include(m => m.ContractAct)
                .ThenInclude(m => m.Activity)
                .ThenInclude(m => m.Group)
                .Include(m => m.Act)
                .ThenInclude(m => m.Group)
                .Include(m => m.ChangeOrder);
            foreach (var _entity in entities)
            {
                IncommingInvoiceActivityBO bo = new IncommingInvoiceActivityBO();
                var err = IncommingInvoiceActivityTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<IncommingInvoiceActivityBO> GetProjectIncommingInvoicesByActivity(int projectid, int activityid)
        {
            GetResponse<IncommingInvoiceActivityBO> response = new GetResponse<IncommingInvoiceActivityBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetIncommingInvoicesDetailDAO();
            var entities = dao.GetNoTracking().Where(m => m.ActId == activityid & m.IncommingInvoice.ProjectId == projectid | m.ContractAct.ActivityId == activityid & m.IncommingInvoice.ProjectId == projectid)
                .Include(m => m.ContractAct)
                .ThenInclude(m => m.Contract)
                .ThenInclude(m => m.Company)
                .Include(m => m.ContractAct)
                .ThenInclude(m => m.Activity)
                .ThenInclude(m => m.Group)
                .Include(m => m.Act)
                .ThenInclude(m => m.Group)
                .Include(m => m.IncommingInvoice)
                .ThenInclude(m => m.Company);
            foreach (var _entity in entities)
            {
                IncommingInvoiceActivityBO bo = new IncommingInvoiceActivityBO();
                var err = IncommingInvoiceActivityTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<IncommingInvoiceActivityBO> GetProjectIncommingInvoicesByGroup(int projectid, int groupid)
        {
            GetResponse<IncommingInvoiceActivityBO> response = new GetResponse<IncommingInvoiceActivityBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetIncommingInvoicesDetailDAO();
            var entities = dao.GetNoTracking().Where(m => m.Act.GroupId == groupid & m.IncommingInvoice.ProjectId == projectid | m.ContractAct.Activity.GroupId == groupid & m.IncommingInvoice.ProjectId == projectid)
                .Include(m => m.ContractAct)
                .ThenInclude(m => m.Contract)
                .ThenInclude(m => m.Company)
                .Include(m => m.ContractAct)
                .ThenInclude(m => m.Activity)
                .ThenInclude(m => m.Group)
                .Include(m => m.Act)
                .ThenInclude(m => m.Group)
                .Include(m => m.IncommingInvoice)
                .ThenInclude(m => m.Company);
            foreach (var _entity in entities)
            {
                IncommingInvoiceActivityBO bo = new IncommingInvoiceActivityBO();
                var err = IncommingInvoiceActivityTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }

        public Response DeleteIncommingInvoices(List<int> ids)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();
            foreach (var id in ids)
                uow.GetIncommingInvoicesDAO().DeleteObject(id);
            response.Messages.AddRange(uow.SaveChanges());
            return response;
        }


        // Insurances
        public GetResponse<InsuranceBO> GetProjectInsurances(int projectid)
        {
            GetResponse<InsuranceBO> response = new GetResponse<InsuranceBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetInsuranceDAO();
            var entities = dao.GetNoTracking().Where(m => m.ContractActivity.Contract.ProjectId == projectid);
            foreach (var _entity in entities)
            {
                InsuranceBO bo = new InsuranceBO();
                var err = InsuranceTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }



        // Helpers

        public string GenerateSlug(string phrase)
        {
            string str = RemoveAccent(phrase).ToLower();
            str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
            str = Regex.Replace(str, @"\s+", " ").Trim();
            str.Substring(0, str.Length <= 45 ? str.Length : 45).Trim();
            str = Regex.Replace(str, @"\s", "-");
            return str;
        }
        public string RemoveAccent(string txt)
        {
            byte[] bytes = System.Text.Encoding.GetEncoding("Cyrillic").GetBytes(txt);
            return System.Text.Encoding.ASCII.GetString(bytes);
        }
        public DateOnly AddWorkDays(DateOnly date, int workingDays, Array BWDS, Array VDS)
        {
            int direction = workingDays < 0 ? -1 : 1;
            DateOnly newDate = date;
            DateOnly dag;
            // If a working day count of Zero is passed, return the date passed
            if (workingDays == 0)
                newDate = date;
            else
                while (workingDays != 0)
                {
                    if (newDate.DayOfWeek != DayOfWeek.Saturday && newDate.DayOfWeek != DayOfWeek.Sunday && Array.IndexOf(BWDS, newDate) < 0 && Array.IndexOf(VDS, newDate) < 0)
                        workingDays -= 1;
                    else
                        dag = newDate;
                    // if the original return date falls on a weekend or holiday, this will take it to the previous / next workday, but the "if" statement keeps it from going a day too far.

                    if (workingDays != 0)
                        newDate = newDate.AddDays(1);
                }
            return newDate;
        }
        public int BusinessDaysUntil(DateOnly start, DateOnly end, Array VDS)
        {
            int tld = System.Convert.ToInt32((end.DayNumber - start.DayNumber));
            int direction = tld < 0 ? -1 : 1;
            DateOnly newDate = start.AddDays(1);
            int workingdays = 0;
            // If a working day count of Zero is passed, return the date passed
            if (tld == 0)
                newDate = start;
            else if (tld < 0)
                newDate = start;
            else
                while (tld != 0)
                {
                    if (newDate.DayOfWeek != DayOfWeek.Saturday && newDate.DayOfWeek != DayOfWeek.Sunday && Array.IndexOf(VDS, newDate) < 0)
                        workingdays += 1;
                    else
                    {
                    }
                    // if the original return date falls on a weekend or holiday, this will take it to the previous / next workday, but the "if" statement keeps it from going a day too far.
                    tld -= 1;
                    if (tld != 0)
                        newDate = newDate.AddDays(1);
                }
            return workingdays;
        }

        public Response Copyids()
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetInvoicesDetailsDAO();
            var dao2 = uow.GetUnitConstructionValuesDAO();
            var entities = dao.GetNormal();
            foreach (var _entity in entities)
            {
                var ent2 = dao2.GetNoTracking().Where(m => m.UnitId == _entity.UnitId & m.PaymentGroupId == _entity.PaymentStage.GroupId).FirstOrDefault();
                _entity.ConstructionValueId = ent2.Id;
            }
            response.AddError(uow.SaveChanges());
            return response;
        }
    }
}

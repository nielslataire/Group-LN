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
        private readonly UnitOfWorkCore _uow;

        public ProjectService(UnitOfWorkCore uow)
        {
            _uow = uow;
        }

        public GetResponse<ProjectBO> GetProjectByID(int id)
        {
            var response = new GetResponse<ProjectBO>();

            var entity = _uow.Projects.GetNoTracking()
                .Where(m => m.ProjectId == id)
                .Include(m => m.PostalCode)
                    .ThenInclude(m => m.Country)
                .Include(m => m.PostalCode)
                    .ThenInclude(m => m.Provincie)
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

            var project = new ProjectBO();
            var err = ProjectTranslator.TranslateEntityToBO(entity, project);
            if (err == ErrorCode.Success) response.Value = project;
            else response.AddError(err.ToString());

            return response;
        }

        public GetResponse<ProjectBO> GetProjectBySlug(string slug)
        {
            var response = new GetResponse<ProjectBO>();

            var entity = _uow.Projects.GetNoTracking()
                .FirstOrDefault(m => m.Slug == slug);

            var project = new ProjectBO();
            var err = ProjectTranslator.TranslateEntityToBO(entity, project);
            if (err == ErrorCode.Success) response.Value = project;
            else response.AddError(err.ToString());

            return response;
        }

        private sealed class ProjectListItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Status { get; set; }
            public string Location { get; set; }
            public string DefaultPic { get; set; }
            public string DefaultPicCaption { get; set; }
            public DateOnly? DeliveryDate { get; set; }
            public string CommercialTitleNl { get; set; }
            public string CommercialTextNl { get; set; }
            public string Slug { get; set; }
            public string UserId { get; set; }
            public int? BuilderId { get; set; }
            public int ProjectType { get; set; }
        }

        public GetResponse<ProjectBO> GetProjectsForList(
            ProjectType Type = 0, int StatusId = 0, string UserId = null, int BuilderId = 0, bool TrimCommercialText = false)
        {
            var response = new GetResponse<ProjectBO>();

            IQueryable<ProjectListItem> q = _uow.Projects.GetNoTracking()
                .Select(m => new ProjectListItem
                {
                    Id = m.ProjectId,
                    Name = m.ProjectName,
                    Status = m.Status.StatusId,
                    Location = m.PostalCode.Gemeente,
                    DefaultPic = m.DefaultPicture.Name,
                    DefaultPicCaption = m.DefaultPicture.Caption,
                    DeliveryDate = m.DeliveryDate,
                    CommercialTitleNl = m.CommercialTitleNl,
                    CommercialTextNl = m.CommercialTextNl,
                    Slug = m.Slug,
                    UserId = m.AspNetUserId,
                    BuilderId = m.BuilderId,
                    ProjectType = m.ProjectType
                });

            if (Type != 0) q = q.Where(m => m.ProjectType == (int)Type);
            if (StatusId != 0) q = q.Where(m => m.Status == StatusId);
            if (UserId != null) q = q.Where(m => m.UserId == UserId);
            if (BuilderId != 0) q = q.Where(m => m.BuilderId == BuilderId);

            foreach (var e in q)
            {
                var bo = new ProjectBO
                {
                    Id = e.Id,
                    CommercialTitleNL = e.CommercialTitleNl,
                    Name = e.Name,
                    Slug = e.Slug,
                    ProjectType = (ProjectType)e.ProjectType
                };
                bo.Status.Id = e.Status;
                bo.Postalcode.Gemeente = e.Location;

                if (TrimCommercialText && (e.CommercialTextNl?.Length ?? 0) > 150)
                    bo.CommercialTextNL = e.CommercialTextNl.Substring(0, 150) + " ...";
                else
                    bo.CommercialTextNL = e.CommercialTextNl;

                if (e.DeliveryDate != null) bo.DeliveryDate = e.DeliveryDate;

                if (!string.IsNullOrEmpty(e.DefaultPic))
                {
                    bo.DefaultPicture.Name = e.DefaultPic;
                    bo.DefaultPicture.Caption = e.DefaultPicCaption;
                }
                else
                {
                    bo.DefaultPicture.Name = null;
                }

                response.AddValue(bo);
            }

            return response;
        }
        public string GetProjectNameById(int id)
            => _uow.Projects.GetById(id)?.ProjectName ?? string.Empty;

        public string GetProjectCityById(int id)
            => _uow.Projects.GetById(id)?.PostalCode?.Gemeente ?? string.Empty;

        public string GetProjectSlugById(int id)
            => _uow.Projects.GetById(id)?.Slug ?? string.Empty;

        public decimal GetProjectLandshareById(int id)
        {
            var e = _uow.Projects.GetById(id);
            return e?.TotalLandShare ?? 0m;
        }

        public int GetProjectWeatherstation(int projectid)
        {
            var e = _uow.Projects.GetById(projectid);
            return e?.WheaterStationId ?? 0;
        }

        public GetResponse<SelectBO> GetProjectsForSearchList(string searchterm)
        {
            var response = new GetResponse<SelectBO>();

            response.Values = _uow.Projects.GetNoTracking()
                .Where(ProjectQuery.GetNameQuery(searchterm))
                .OrderBy(m => m.ProjectName)
                .Select(m => new SelectBO { id = m.ProjectId, text = m.ProjectName, extra = "Project" })
                .ToList();

            return response;
        }

        public GetResponse<IdNameBO> GetProjectsWithAvailableUnits()
        {
            var response = new GetResponse<IdNameBO>();

            var entities = _uow.Projects.GetNoTracking()
                .Where(m => m.Units.Any(i => i.ClientAccountId != null | i.ClientAccountId != 0)); // (ongewijzigd)

            foreach (var e in entities)
                response.AddValue(e.GetIdName());

            return response;
        }

        public DateOnly GetProjectStartDateConstruction(int projectid)
        {
            var e = _uow.Projects.GetById(projectid);
            return e?.StartDateConstruction ?? DateOnly.MinValue;
        }

        public int GetProjectExecutionDays(int projectid)
        {
            var e = _uow.Projects.GetById(projectid);
            return (int)(e?.ExecutionDays ?? 0);
        }

        public int GetWorkingDaysLeft(DateOnly finalconstructiondate, int projectid)
        {
            if (finalconstructiondate == DateOnly.MinValue) return -9999;

            var vds = _uow.VacationDays.GetNoTracking()
                .Where(m => m.ProjectId == null || m.ProjectId == projectid)
                .Select(m => m.VacationDay)
                .ToArray();

            return BusinessDaysUntil(DateOnly.FromDateTime(DateTime.Now), finalconstructiondate, vds);
        }

        public DateOnly GetFinalConstructionDay(int projectid, DateOnly startdate, int executiondays)
        {
            var vds = _uow.VacationDays.GetNoTracking()
                .Where(m => m.ProjectId == null || m.ProjectId == projectid)
                .Select(m => m.VacationDay)
                .ToArray();

            var weatherstationid = GetProjectWeatherstation(projectid);
            if (weatherstationid == 0) return DateOnly.MinValue;

            var bwds = _uow.BadWeatherDays.GetNoTracking()
                .Where(m => m.WeatherstationId == weatherstationid)
                .GroupBy(m => m.Date)
                .Select(g => g.Key)
                .ToArray();

            return AddWorkDays(startdate, executiondays, bwds, vds);
        }

        public string GetProjectFolderById(int id)
        {
            return _uow.Projects.GetNoTracking()
                .Where(m => m.ProjectId == id)
                .Select(m => m.ProjectFolder)
                .FirstOrDefault();
        }

        public GetResponse<WarningBO> CheckProjectFinished(string userid = "")
        {
            var response = new GetResponse<WarningBO>();

            var query = _uow.Projects.GetNoTracking()
                .Where(m => m.Status.StatusId == 1);

            if (!string.IsNullOrEmpty(userid))
                query = query.Where(m => m.AspNetUserId == userid);

            foreach (var e in query)
            {
                if (e.DeliveryDate == null && e.DocDelivery == true)
                {
                    response.AddValue(new WarningBO
                    {
                        ID = e.ProjectId,
                        ProjectId = e.ProjectId,
                        Display = $"De voorlopige opleverdatum van project {e.ProjectName} is niet ingevuld.",
                        Type = "danger"
                    });
                }
                else
                {
                    var docWarn = CheckProjectDocs(e);
                    if (!string.IsNullOrEmpty(docWarn.Display))
                        response.AddValue(docWarn);

                    if (e.DeliveryDateDef == null && e.DocDefDelivery == true &&
                        e.DeliveryDate.HasValue &&
                        e.DeliveryDate.Value.AddYears(10).CompareTo(DateOnly.FromDateTime(DateTime.Today)) > 0)
                    {
                        var today = DateOnly.FromDateTime(DateTime.Today);
                        if (e.DeliveryDate.Value.AddMonths(12) <= today)
                        {
                            response.AddValue(new WarningBO
                            {
                                ID = e.ProjectId,
                                ProjectId = e.ProjectId,
                                Display = $"De definitieve oplevering van project {e.ProjectName} is nog niet gebeurd, gelieve deze aan te vragen!",
                                Type = "danger"
                            });
                        }
                        else if (e.DeliveryDate.Value.AddMonths(11) <= today)
                        {
                            response.AddValue(new WarningBO
                            {
                                ID = e.ProjectId,
                                ProjectId = e.ProjectId,
                                Display = $"De definitieve oplevering van project {e.ProjectName} kan gebeuren vanaf {e.DeliveryDate.Value.AddMonths(11)} , gelieve deze aan te vragen!",
                                Type = "warning"
                            });
                        }
                    }
                }
            }

            return response;
        }

        public WarningBO CheckProjectDocs(Project e)
        {
            var boDocs = new WarningBO();

            if (e.DeliveryDate.HasValue &&
                e.DeliveryDate.Value.AddYears(10).CompareTo(DateOnly.FromDateTime(DateTime.Today)) > 0)
            {
                boDocs.ID = e.ProjectId;
                boDocs.ProjectId = e.ProjectId;
                boDocs.Type = "warning";

                void NeedDoc(bool required, ProjectDocType type, string label)
                {
                    if (!required) return;
                    if (e.ProjectDocs.Count(d => d.Type == (int)type) == 0)
                    {
                        boDocs.Display = string.IsNullOrEmpty(boDocs.Display)
                            ? $"Het project {e.ProjectName} ontbreekt volgende documenten : {label}"
                            : $"{boDocs.Display} , {label}";
                    }
                }

                NeedDoc(e.DocElectricalInspection == true, ProjectDocType.Electrical_inspection, "Elektrische keuring");
                NeedDoc(e.DocWaterInspection == true, ProjectDocType.Water_inspection, "Waterkeuring");
                NeedDoc(e.DocSewerInspection == true, ProjectDocType.Sewer_inspection, "Rioolkeuring");
                NeedDoc(e.DocFireInspection == true, ProjectDocType.Fire_inspection, "Brandkeuring");
                NeedDoc(e.DocDelivery == true, ProjectDocType.Delivery, "Voorlopige oplevering");
                NeedDoc(e.DocPid == true, ProjectDocType.PID, "PID");
            }

            return boDocs;
        }

        public Response InsertUpdate(ProjectBO project)
        {
            var response = new Response();
            if (string.IsNullOrWhiteSpace(project.Name))
            {
                response.AddError("Projectnaam is verplicht");
                return response;
            }

            Project entity = project.Id == 0
                ? _uow.Projects.GetNew()
                : _uow.Projects.GetById(project.Id);

            if (entity != null)
            {
                var err = ProjectTranslator.TranslateBOToEntity(entity, project, /* let op: oude uow param */ null);
                if (err != ErrorCode.Success)
                    response.AddError(err.ToString());
            }
            else
            {
                response.AddError("project not found");
            }

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Project aangepast of toegevoegd", "Project niet aangepast of toegevoegd");

            return response;
        }

        public Response Delete(List<int> ids)
        {
            var response = new Response();

            foreach (var id in ids)
                _uow.Projects.DeleteObject(id);

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Record(s) verwijderd", "Geen records verwijderd");

            return response;
        }

        public Response Delete(List<ProjectBO> bos)
            => Delete(bos.Select(s => s.Id).ToList());

        // Wheaterstations
        //public GetResponse<WheaterStationBO> GetWheaterstations()
        //{
        //}

        public GetResponse<IdNameBO> GetWheaterstationsSelect()
        {
            var response = new GetResponse<IdNameBO>();
            var entities = _uow.WheaterStations.GetNoTracking()
                .OrderBy(m => m.Name)
                .AsNoTracking()
                .ToList();

                    foreach (var e in entities)
                response.AddValue(e.GetIdName());

            return response;
        }

        public GetResponse<WheaterStationBO> GetWheaterstations(string searchterm)
        {
            var response = new GetResponse<WheaterStationBO>();
            var entities = _uow.WheaterStations.GetNoTracking()
                .Where(m => m.Name.Contains(searchterm))
                .OrderBy(m => m.Name)
                .ToList();

                    foreach (var e in entities)
            {
                var bo = new WheaterStationBO();
                var err = WheaterstationTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }


        // Badweatherdays
        public GetResponse<BadWeatherDayBO> GetBadWeatherDays(int weatherstationid, int type)
        {
            var response = new GetResponse<BadWeatherDayBO>();

            var entities = _uow.BadWeatherDays.GetNoTracking()
                .Where(m => m.WeatherstationId == weatherstationid && m.Type == type )
                .ToList();

            foreach (var e in entities)
            {
                var bo = new BadWeatherDayBO();
                var err = BadWeatherDayTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
            }
            return response;
        }

        public GetResponse<BadWeatherDayBO> GetBadWeatherDays(int weatherstationid, int type, int year)
        {
            var response = new GetResponse<BadWeatherDayBO>();
            var q = _uow.BadWeatherDays.GetNoTracking()
                .Where(m => m.WeatherstationId == weatherstationid && m.Type == type && m.Date.Year == year)
                   .ToList();

            foreach (var e in q)
            {
                var bo = new BadWeatherDayBO();
                var err = BadWeatherDayTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
            }
            return response;
        }

        public GetResponse<BadWeatherDayBO> GetClientWeatherDays(int weatherstationid, DateTime startdate, DateTime enddate)
        {
            var response = new GetResponse<BadWeatherDayBO>();
            var q = _uow.BadWeatherDays.GetNoTracking()
                .Where(m => m.WeatherstationId == weatherstationid
                         && m.Date >= DateOnly.FromDateTime(startdate)
                         && m.Date <= DateOnly.FromDateTime(enddate))
                   .ToList();

            foreach (var e in q)
            {
                var bo = new BadWeatherDayBO();
                var err = BadWeatherDayTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
            }
            return response;
        }

        public Response InsertUpdateBadWeatherDay(BadWeatherDayBO bwd)
        {
            var response = new Response();
            if (bwd.BWDate == DateOnly.MinValue) response.AddError("Datum is verplicht");
            if (!response.Success) return response;

            BadWeatherDays entity = bwd.Id == 0
                ? _uow.BadWeatherDays.GetNew()
                : _uow.BadWeatherDays.GetById(bwd.Id);

            if (entity is null)
            {
                response.AddError("Badweatherday not found");
                return response;
            }

            var err = BadWeatherDayTranslator.TranslateBOToEntity(entity, bwd);
            if (err != ErrorCode.Success) response.AddError(err.ToString());

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Slechtweerdag opgeslagen", "Slechtweerdag niet opgeslagen");

            response.Messages.Add(new Message { Type = MessageType.Value, Message = entity.Id.ToString() });
            return response;
        }

        public Response DeleteBadWeatherDays(List<int> ids)
        {
            var response = new Response();
            foreach (var id in ids)
                _uow.BadWeatherDays.DeleteObject(id);

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Record(s) verwijderd", "Geen records verwijderd");
            return response;
        }
        public Response DeleteBadWeatherDays(List<BadWeatherDayBO> bos)
        {
            var ids = bos?.Select(s => s.Id).ToList() ?? new List<int>();
            return DeleteBadWeatherDays(ids);
        }


        // Vacationdays
        public GetResponse<VacationDayBO> GetVacationDays()
        {
            var response = new GetResponse<VacationDayBO>();
            var entities = _uow.VacationDays.GetNoTracking()
                   .Where(m => m.ProjectId == null)
                   .ToList();

            foreach (var e in entities)
            {
                var bo = new VacationDayBO();
                var err = VacationDayTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
            }
            return response;
        }

        public GetResponse<VacationDayBO> GetProjectVacationDays(int projectid)
        {
            var response = new GetResponse<VacationDayBO>();
            var q = _uow.VacationDays.GetNoTracking()
                .Where(m => m.ProjectId == projectid)
                   .ToList();

            foreach (var e in q)
            {
                var bo = new VacationDayBO();
                var err = VacationDayTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
            }
            return response;
        }

        public GetResponse<VacationDayBO> GetVacationDaysGeneral()
        {
            var response = new GetResponse<VacationDayBO>();
            var q = _uow.VacationDays.GetNoTracking()
                .Where(m => m.ProjectId == null || m.ProjectId == 0)
                   .ToList();

            foreach (var e in q)
            {
                var bo = new VacationDayBO();
                var err = VacationDayTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
            }
            return response;
        }

        public Response InsertUpdateVacationDay(VacationDayBO vacationday)
        {
            var response = new Response();
            if (vacationday is null || vacationday.VacationDay == default)
            {
                response.AddError("Datum is verplicht");
                return response;
            }

            // Alleen bij nieuwe invoer (Id == 0): bestond deze datum al?
            if (vacationday.Id == 0)
            {

                // Voeg hier extra keys toe indien per gebruiker/medewerker/project uniek moet zijn
            var existing = _uow.VacationDays.Query()
                        .FirstOrDefault(x => x.VacationDay == vacationday.VacationDay /* && x.EmployeeId == vacationday.EmployeeId */);

                if (existing != null)
                {
                    // Idempotent: geen nieuw record, gewoon het bestaande ID teruggeven (Success = true)
                    response.Messages.Add(new Message { Type = MessageType.Value, Message = existing.Id.ToString() });
                    return response;
                }
            }

            // Nieuw of update pad
            VacationDays entity = vacationday.Id == 0
                ? _uow.VacationDays.GetNew()
                : _uow.VacationDays.GetById(vacationday.Id);

            if (entity is null)
            {
                response.AddError("Vacationday not found");
                return response;
            }

            var err = VacationDayTranslator.TranslateBOToEntity(entity, vacationday);
            if (err != ErrorCode.Success)
            {
                response.AddError(err.ToString());
                return response;
            }

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Verlofdag opgeslagen", "Verlofdag niet opgeslagen");

            response.Messages.Add(new Message { Type = MessageType.Value, Message = entity.Id.ToString() });
            return response;
        }


        public Response DeleteVacationDays(List<int> ids)
        {
            var response = new Response();
            foreach (var id in ids)
                _uow.VacationDays.DeleteObject(id);

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Record(s) verwijderd", "Geen records verwijderd");
            return response;
        }
        public Response DeleteVacationDays(List<VacationDayBO> bos)
        {
            var ids = bos?.Select(s => s.Id).ToList() ?? new List<int>();
            return DeleteVacationDays(ids);
        }

        // ===== Statuses =====
        public GetResponse<ProjectStatusBO> GetStatuses()
        {
            var response = new GetResponse<ProjectStatusBO>();

            var entities = _uow.ProjectStatuses
                .GetNoTracking(); // AsNoTracking()

            foreach (var e in entities)
            {
                var bo = new ProjectStatusBO();
                var err = StatusTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<IdNameBO> GetStatusesForSelect()
        {
            var response = new GetResponse<IdNameBO>();
            var entities = _uow.ProjectStatuses.GetNoTracking();
            foreach (var e in entities) response.AddValue(e.GetIdName());
            return response;
        }

        // ===== Pictures =====
        public GetResponse<ProjectPictureBO> GetPictureById(int id)
        {
            var response = new GetResponse<ProjectPictureBO>();
            var entity = _uow.ProjectPictures.GetById(id);
            var bo = new ProjectPictureBO();

            var err = ProjectPictureTranslator.TranslateEntityToBO(entity, bo);
            if (err == ErrorCode.Success) response.Value = bo;
            else response.AddError(err.ToString());
            return response;
        }

        public GetResponse<ProjectPictureBO> GetPicturesByProjectId(int id)
        {
            var response = new GetResponse<ProjectPictureBO>();
            var entities = _uow.ProjectPictures
                .GetNoTracking()
                .Where(m => m.ProjectId == id);

            foreach (var e in entities)
            {
                var bo = new ProjectPictureBO();
                var err = ProjectPictureTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<ProjectPictureBO> GetPicturesByProjectSlug(string slug)
        {
            var response = new GetResponse<ProjectPictureBO>();
            var entities = _uow.ProjectPictures
                .GetNoTracking()
                .Where(m => m.ProjectNavigation.Slug == slug);

            foreach (var e in entities)
            {
                var bo = new ProjectPictureBO();
                var err = ProjectPictureTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<ProjectPictureBO> GetLatestPictures(int number)
        {
            var response = new GetResponse<ProjectPictureBO>();
            var entities = _uow.ProjectPictures
                .GetNoTracking()
                .OrderByDescending(m => m.Datetimeuploaded)
                .Take(number);

            foreach (var e in entities)
            {
                var bo = new ProjectPictureBO();
                var err = ProjectPictureTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<ProjectPictureBO> GetLatestProjectPictures(int number, int projectid)
        {
            var response = new GetResponse<ProjectPictureBO>();
            var entities = _uow.ProjectPictures
                .GetNoTracking()
                .Where(m => m.ProjectId == projectid && m.Type != 3)
                .OrderByDescending(m => m.Datetimeuploaded)
                .Take(number);

            foreach (var e in entities)
            {
                var bo = new ProjectPictureBO();
                var err = ProjectPictureTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public Response InsertUpdatePicture(ProjectPictureBO picture)
        {
            var response = new Response();
            if (picture == null) { response.AddError("Ongeldige invoer."); return response; }
            if (string.IsNullOrWhiteSpace(picture.Name)) response.AddError("Bestandsnaam is verplicht.");
            if (picture.ProjectId <= 0) response.AddError("ProjectId ontbreekt of is ongeldig.");
            if (!response.Success) return response;

            // 1) Bestaat project in DZÉLFD context/DB?
            var ctx = _uow.Context;
            var exists = ctx.Set<Project>().AsNoTracking().Any(p => p.ProjectId == picture.ProjectId);
            if (!exists) { response.AddError($"Project (id={picture.ProjectId}) bestaat niet."); return response; }

            // 2) Entity ophalen/aanmaken
            ProjectPictures? entity;
            if (picture.Id == 0)
            {
                entity = _uow.ProjectPictures.GetNew() ?? new ProjectPictures();
                // Zorg dat EF dit als INSERT ziet (en niet als detachte entity)
                ctx.Set<ProjectPictures>().Attach(entity);
                ctx.Entry(entity).State = EntityState.Added;
            }
            else
            {
                entity = _uow.ProjectPictures.GetById(picture.Id);
                if (entity == null) { response.AddError("Afbeelding niet gevonden."); return response; }
                // (optioneel) ProjectId niet wijzigen bij update:
                picture.ProjectId = entity.ProjectId ?? picture.ProjectId;
                ctx.Entry(entity).State = EntityState.Modified;
            }

            // 3) Project-relatie expliciet vastleggen (belangrijk)
            entity.ProjectId = picture.ProjectId; // FK-waarde
            var proj = ctx.Set<Project>().Local.FirstOrDefault(p => p.ProjectId == picture.ProjectId)
                       ?? ctx.Attach(new Project { ProjectId = picture.ProjectId }).Entity;
            ctx.Entry(proj).State = EntityState.Unchanged;
            // Als je een referentienavigatie hebt (bvb ProjectNavigation of Project), zet die dan:
            // entity.ProjectNavigation = proj;    // gebruik de juiste propertynaam

            // 4) Overige velden mappen
            entity.Name = picture.Name;
            entity.Caption = picture.Caption;
            entity.Type = (int)picture.Type;
            entity.Datetimeuploaded = picture.DateTimeUploaded == default ? DateTime.Now : picture.DateTimeUploaded;
            entity.FacebookIdCopro = picture.FacebookIdCopro;

            // 5) Opslaan
            try
            {
                var saved = _uow.SaveChanges();
                if (saved <= 0) response.AddError("Geen wijzigingen opgeslagen.");
            }
            catch (Exception ex)
            {
                response.AddError($"Databasefout bij opslaan: {ex.Message}");
            }

            if (response.Success)
                response.Messages.Add(new Message { Type = MessageType.Value, Message = entity.Id.ToString() });

            return response;
        }



        public Response DeletePicture(List<int> ids)
        {
            var response = new Response();
            foreach (var id in ids)
                _uow.ProjectPictures.DeleteObject(id);

            var saved = _uow.SaveChanges();
            response.AddSaveChangesResult(saved, "Record(s) verwijderd", "Geen records verwijderd");
            return response;
        }

        public Response SetDefaultProjectPicture(int projectid, int pictureid)
        {
            var response = new Response();
            if (projectid == 0)
            {
                response.AddError("Er moet een project of foto geselecteerd zijn.");
                return response;
            }

            var project = _uow.Projects.GetById(projectid);
            if (project == null)
            {
                response.AddError("project not found");
                return response;
            }

            if (pictureid == 0)
            {
                if (project.DefaultPicture != null) project.DefaultPictureId = null;
                var saved0 = _uow.SaveChanges();
                response.AddSaveChangesResult(saved0, "Hoofdfoto verwijderd", "Hoofdfoto niet aangepast");
                return response;
            }

            // reset bestaande hoofdfoto's naar nevenfoto
            foreach (var pic in project.ProjectPictures)
                if (pic.Type == (int)PictureType.Hoofdfoto)
                    pic.Type = (int)PictureType.Nevenfoto;

            if (project.DefaultPicture != null)
            {
                var old = _uow.ProjectPictures.GetById(project.DefaultPictureId ?? 0);
                if (old != null) old.Type = (int)PictureType.Nevenfoto;
            }

            project.DefaultPictureId = pictureid;
            var saved = _uow.SaveChanges();
            response.AddSaveChangesResult(saved, "Hoofdfoto ingesteld", "Hoofdfoto niet aangepast");
            return response;
        }

        public string GetFacebookAlbumIdCoproByProjectId(int id)
        {
            var p = _uow.Projects.GetById(id);
            return p?.FacebookAlbumId?.ToString() ?? string.Empty;
        }

        // ===== News =====
        public GetResponse<ProjectNewsBO> GetNewsById(int id)
        {
            var response = new GetResponse<ProjectNewsBO>();
            var entity = _uow.ProjectNews.GetNoTracking()
                .Where(m => m.Id == id)
                .Include(m => m.Picture)
                .FirstOrDefault();
            var bo = new ProjectNewsBO();

            var err = ProjectNewsTranslator.TranslateEntityToBO(entity, bo);
            if (err == ErrorCode.Success) response.Value = bo;
            else response.AddError(err.ToString());
            return response;
        }

        public GetResponse<ProjectNewsBO> GetNewsByProjectId(int id)
        {
            var response = new GetResponse<ProjectNewsBO>();
            var entities = _uow.ProjectNews
                .GetNoTracking()
                .Where(m => m.ProjectId == id)
                .Include(m => m.Picture)
                .OrderByDescending(m => m.Date);

            foreach (var e in entities)
            {
                var bo = new ProjectNewsBO();
                var err = ProjectNewsTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<ProjectNewsBO> GetNewsByProjectSlug(string slug)
        {
            var response = new GetResponse<ProjectNewsBO>();
            var entities = _uow.ProjectNews
                .GetNoTracking()
                .Where(m => m.Project.Slug == slug)
                .OrderByDescending(m => m.Date);

            foreach (var e in entities)
            {
                var bo = new ProjectNewsBO();
                var err = ProjectNewsTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<ProjectNewsBO> GetLatestNews(int number, int builderId = 0)
        {
            var response = new GetResponse<ProjectNewsBO>();
            var q = _uow.ProjectNews.GetNoTracking();

            if (builderId != 0)
                q = q.Where(m => m.Project.BuilderId == builderId);

            var entities = q.OrderByDescending(m => m.Date).Take(number);

            foreach (var e in entities)
            {
                var bo = new ProjectNewsBO();
                var err = ProjectNewsTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<ProjectNewsBO> GetLatestProjectNews(int number, int projectid)
        {
            var response = new GetResponse<ProjectNewsBO>();
            var entities = _uow.ProjectNews
                .GetNoTracking()
                .Where(m => m.ProjectId == projectid)
                .OrderByDescending(m => m.Date)
                .Take(number)
                .Include(m => m.Picture);

            foreach (var e in entities)
            {
                var bo = new ProjectNewsBO();
                var err = ProjectNewsTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public Response InsertUpdateNews(ProjectNewsBO newsItem)
        {
            var response = new Response();
            if (string.IsNullOrWhiteSpace(newsItem.TitleNL))
                response.AddError("Titel is verplicht");
            if (!response.Success) return response;

            ProjectNews entity;
            if (newsItem.Id == 0)
            {
                entity = _uow.ProjectNews.GetNew();
                if (newsItem.Picture is not null)
                    entity.Picture = _uow.ProjectPictures.GetNew();
            }
            else
            {
                entity = _uow.ProjectNews.GetById(newsItem.Id);
                if (newsItem.Picture is not null && newsItem.Picture.Id == 0)
                    entity.Picture = _uow.ProjectPictures.GetNew();
                else if (newsItem.Picture is not null)
                {
                    var errPic = ProjectPictureTranslator
                        .TranslateEntityToBO(_uow.ProjectPictures.GetById(newsItem.Picture.Id), newsItem.Picture);
                    if (errPic != ErrorCode.Success) response.AddError(errPic.ToString());
                }
            }

            if (entity != null)
            {
                var err = ProjectNewsTranslator.TranslateBOToEntity(entity, newsItem, _uow);
                if (err != ErrorCode.Success) response.AddError(err.ToString());
            }
            else response.AddError("Newsitem not found");

            var saved = _uow.SaveChanges();
            response.AddSaveChangesResult(saved, "Nieuws aangepast/toegevoegd", "Nieuws niet aangepast/toegevoegd");
            return response;
        }

        public Response DeleteNews(List<int> ids)
        {
            var response = new Response();
            foreach (var id in ids)
                _uow.ProjectNews.DeleteObject(id);

            var saved = _uow.SaveChanges();
            response.AddSaveChangesResult(saved, "Record(s) verwijderd", "Geen records verwijderd");
            return response;
        }


        // Levels
        public GetResponse<ProjectLevelBO> GetLevelsByProjectId(int id)
        {
            var response = new GetResponse<ProjectLevelBO>();
            var entities = _uow.ProjectLevels.GetNoTracking().Where(x => x.ProjectId == id);

            foreach (var e in entities)
            {
                var bo = new ProjectLevelBO();
                var err = ProjectLevelTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public Response InsertUpdateLevel(ProjectLevelBO level)
        {
            var response = new Response();
            if (string.IsNullOrWhiteSpace(level.Name)) response.AddError("Naam is verplicht");
            if (!response.Success) return response;

            ProjectLevels entity = level.Id == 0
                ? _uow.ProjectLevels.GetNew()
                : _uow.ProjectLevels.GetById(level.Id);

            if (entity != null)
            {
                var err = ProjectLevelTranslator.TranslateBOToEntity(entity, level, _uow);
                if (err != ErrorCode.Success) response.AddError(err.ToString());
            }
            else response.AddError("Level not found");

            var saved = _uow.SaveChanges();
            response.AddSaveChangesResult(saved, "Level opgeslagen", "Level niet opgeslagen");
            return response;
        }

        // Sales settings
        public GetResponse<ProjectSalesSettingsBO> GetSalesSettings(int projectid)
        {
            var response = new GetResponse<ProjectSalesSettingsBO>();
            var entity = _uow.ProjectSalesSettings
                .GetNoTracking()
                .FirstOrDefault(m => m.Projectid == projectid);

            var bo = new ProjectSalesSettingsBO();
            var err = ProjectSalesSettingsTranslator.TranslateEntityToBO(entity, bo);
            if (err == ErrorCode.Success) response.Value = bo;
            else response.AddError(err.ToString());
            return response;
        }

        public GetResponse<ProjectSalesSettingsBO> GetSalesSettings(List<int> ids)
        {
            var response = new GetResponse<ProjectSalesSettingsBO>();
            var q = _uow.ProjectSalesSettings.GetNoTracking().Where(m => ids.Contains(m.Projectid));
            foreach (var e in q)
            {
                var bo = new ProjectSalesSettingsBO();
                var err = ProjectSalesSettingsTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<ProjectSalesDataBO> GetProjectSalesData(List<int> ids)
        {
            var response = new GetResponse<ProjectSalesDataBO>();
            var units = _uow.Units.GetNoTracking().Where(u => ids.Contains(u.ProjectId));

            foreach (var pid in ids)
            {
                // filter 1x per project
                var u = units.Where(m => m.ProjectId == pid && m.AttachedUnit == null && m.LinkedUnit == null);

                var living = u.Where(i => i.Type.GroupId == 1 || i.Type.GroupId == 4);
                var livingCount = living.Count();
                var livingSold = living.Where(i => i.ClientAccountId != null).Count();

                // Sommen expliciet en veilig haakjes geven
                decimal valueForSale = u.Where(m => m.ClientAccountId == null)
                    .Select(m =>
                        (m.LandValue ?? 0)
                        + (m.UnitConstructionValue.Sum(x => (decimal?)x.Value) ?? 0)
                    ).Sum();

                decimal valueSold = u
                    .Select(m =>
                        (m.LandValueSold ?? 0)
                        + (m.UnitConstructionValue.Sum(x => (decimal?)x.ValueSold) ?? 0)
                    ).Sum();

                decimal percentageLiving = livingCount != 0
                    ? (decimal)(livingSold / (double)livingCount * 100)
                    : 100;

                decimal percentageSold = (valueSold + valueForSale) != 0
                    ? (decimal)(valueSold / (valueSold + valueForSale) * 100)
                    : 100;

                int numApts = u.Count(m => m.Type.Id == 1);
                int numHouses = u.Count(m => m.Type.Id == 2);
                int numCommercial = u.Count(m => m.Type.Id == 10);

                // Startprijs: Min( Bouwsom + Grond )
                decimal startingPrice = u.Where(i => i.ClientAccountId == null && (i.Type.GroupId == 1 || i.Type.GroupId == 4))
                    .Select(i => (i.UnitConstructionValue.Where(v => v.UnitId == i.Id).Sum(v => (decimal?)v.Value) ?? 0)
                                 + (i.LandValue ?? 0))
                    .DefaultIfEmpty(0)
                    .Min();

                response.AddValue(new ProjectSalesDataBO
                {
                    ProjectId = pid,
                    LivingUnits = livingCount,
                    LivingUnitsSold = livingSold,
                    PercentageLivingUnitsSold = percentageLiving,
                    ValueForSale = valueForSale,
                    ValueSold = valueSold,
                    PercentageSold = percentageSold,
                    NumberAppartments = numApts,
                    NumberHouses = numHouses,
                    NumberCommercial = numCommercial,
                    StartingPrice = startingPrice
                });
            }
            return response;
        }

        public Response InsertUpdateSalesSettings(ProjectSalesSettingsBO salessettings)
        {
            var response = new Response();
            if (salessettings.ProjectId <= 0) response.AddError("ProjectID is verplicht");
            if (!response.Success) return response;

            ProjectSalesSettings entity = salessettings.SettingsId == 0
                ? _uow.ProjectSalesSettings.GetNew()
                : _uow.ProjectSalesSettings.GetById(salessettings.SettingsId);

            if (entity != null)
            {
                var err = ProjectSalesSettingsTranslator.TranslateBOToEntity(entity, salessettings, _uow);
                if (err != ErrorCode.Success) response.AddError(err.ToString());
            }
            else response.AddError("SalesSettings not found");

            var saved = _uow.SaveChanges();
            response.AddSaveChangesResult(saved, "Sales settings opgeslagen", "Sales settings niet opgeslagen");
            return response;
        }

        public Response DeleteSalesSettings(List<int> ids)
        {
            var response = new Response();
            foreach (var id in ids) _uow.ProjectSalesSettings.DeleteObject(id);
            var saved = _uow.SaveChanges();
            response.AddSaveChangesResult(saved, "Record(s) verwijderd", "Geen records verwijderd");
            return response;
        }

        public Response DeleteSalesSettings(List<ProjectSalesSettingsBO> bos)
            => DeleteSalesSettings(bos.Select(s => s.SettingsId).ToList());

        public decimal GetProjectVatPercentage(int projectid)
        {
            return (decimal)(_uow.ProjectSalesSettings
                .GetNoTracking()
                .Where(m => m.Projectid == projectid)
                .Select(m => m.Vatpercentage)
                .FirstOrDefault() ?? 0m);
        }

        // Docs
        public GetResponse<ProjectDocBO> GetProjectDocs(int projectid, ProjectDocType type = 0)
        {
            var response = new GetResponse<ProjectDocBO>();
            var q = _uow.ProjectDocs.GetNoTracking()
                .Where(m => m.ProjectId == projectid && m.ClientAccountId == null);

            if (type != 0) q = q.Where(m => m.Type == (int)type);

            foreach (var e in q.OrderByDescending(m => m.Name))
            {
                var bo = new ProjectDocBO();
                var err = ProjectDocsTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<IdNameBO> GetProjectDocsForSelect(int projectid, ProjectDocType type = ProjectDocType.Sales)
        {
            var response = new GetResponse<IdNameBO>();
            var entities = _uow.ProjectDocs.GetNoTracking()
                .Where(m => m.ProjectId == projectid && m.Type == (int)type && m.ClientAccountId == null)
                .OrderByDescending(m => m.Name);

            foreach (var e in entities) response.AddValue(e.GetIdName());
            return response;
        }

        public GetResponse<ProjectDocBO> GetProjectDoc(int docid)
        {
            var response = new GetResponse<ProjectDocBO>();
            var entity = _uow.ProjectDocs.GetNoTracking().FirstOrDefault(m => m.Id == docid);

            var bo = new ProjectDocBO();
            var err = ProjectDocsTranslator.TranslateEntityToBO(entity, bo);
            if (err == ErrorCode.Success) response.AddValue(bo);
            else response.AddError(err.ToString());
            return response;
        }

        public GetResponse<ProjectDocBO> GetClientDocs(int clientaccountid)
        {
            var response = new GetResponse<ProjectDocBO>();
            var entities = _uow.ProjectDocs.GetNoTracking()
                .Where(m => m.ClientAccountId == clientaccountid)
                .OrderByDescending(m => m.Name);

            foreach (var e in entities)
            {
                var bo = new ProjectDocBO();
                var err = ProjectDocsTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<ProjectDocBO> GetLatestProjectDocs(int number, int projectid)
        {
            var response = new GetResponse<ProjectDocBO>();
            var entities = _uow.ProjectDocs.GetNoTracking()
                .Where(m => m.ProjectId == projectid && m.ClientAccountId == null)
                .OrderByDescending(m => m.Date)
                .Take(number);

            foreach (var e in entities)
            {
                var bo = new ProjectDocBO();
                var err = ProjectDocsTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<ProjectDocBO> GetLatestClientDocs(int number, int clientaccountid)
        {
            var response = new GetResponse<ProjectDocBO>();
            var entities = _uow.ProjectDocs.GetNoTracking()
                .Where(m => m.ClientAccountId == clientaccountid)
                .OrderByDescending(m => m.Date)
                .Take(number);

            foreach (var e in entities)
            {
                var bo = new ProjectDocBO();
                var err = ProjectDocsTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }


        public Response InsertUpdateProjectDoc(ProjectDocBO projectDoc)
        {
            var response = new Response();

            if (projectDoc.ProjectId <= 0)
                response.AddError("ProjectID is verplicht");

            // Bestandsnaam enkel verplicht bij NIEUW document
            if (projectDoc.Docid == 0 && string.IsNullOrWhiteSpace(projectDoc.Filename))
                response.AddError("Bestandsnaam is verplicht");

            if (!response.Success) return response;

            ProjectDocs entity;
            IdNameBO toDelete = null;

            if (projectDoc.Docid == 0)
            {
                // unicity voor keycombinationcertificate
                if (projectDoc.Type == ProjectDocType.keycombinationcertificate)
                    toDelete = GetProjectDocsForSelect(projectDoc.ProjectId, ProjectDocType.keycombinationcertificate)
                               .Values.FirstOrDefault();

                entity = _uow.ProjectDocs.GetNew();

                // sortorder veilig berekenen (server-side translatable)
                var maxSort = _uow.ProjectDocs.GetNoTracking()
                    .Where(m => m.ProjectId == projectDoc.ProjectId)
                    .Select(m => (int?)m.SortOrder)
                    .Max() ?? 0;
                projectDoc.SortOrder = maxSort + 1;

            }
            else
            {
                entity = _uow.ProjectDocs.GetById(projectDoc.Docid);
                if (entity == null)
                {
                    response.AddError("ProjectDoc not found");
                    return response;
                }
            }

            var err = ProjectDocsTranslator.TranslateBOToEntity(entity, projectDoc, _uow);
            if (err != ErrorCode.Success)
            {
                response.AddError(err.ToString());
                return response;
            }

            if (toDelete != null)
                _uow.ProjectDocs.DeleteObject(toDelete.ID);

            var saved = _uow.SaveChanges();
            response.AddSaveChangesResult(saved, "Document opgeslagen", "Document niet opgeslagen");
            return response;
        }


        public Response DeleteProjectDoc(List<int> ids)
        {
            var response = new Response();
            foreach (var id in ids) _uow.ProjectDocs.DeleteObject(id);
            var saved = _uow.SaveChanges();
            response.AddSaveChangesResult(saved, "Record(s) verwijderd", "Geen records verwijderd");
            return response;
        }

        public Response DeleteProjectDoc(List<ProjectDocBO> bos)
            => DeleteProjectDoc(bos.Select(s => s.Docid).ToList());


        // ===== PaymentGroups =====
        public GetResponse<ProjectPaymentGroupBO> GetProjectPaymentGroups(int projectid)
        {
            var response = new GetResponse<ProjectPaymentGroupBO>();
            var entities = _uow.PaymentGroups
                .GetNoTracking()
                .Where(m => m.ProjectId == projectid)
                .OrderByDescending(m => m.Name);

            foreach (var e in entities)
            {
                var bo = new ProjectPaymentGroupBO();
                var err = ProjectPaymentGroupTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }

            response.Values = response.Values.OrderBy(m => m.Name).ToList();
            return response;
        }

        public GetResponse<ProjectPaymentGroupBO> GetProjectPaymentGroup(int groupid)
        {
            var response = new GetResponse<ProjectPaymentGroupBO>();
            var e = _uow.PaymentGroups.GetNoTracking().FirstOrDefault(m => m.Id == groupid);

            var bo = new ProjectPaymentGroupBO();
            var err = ProjectPaymentGroupTranslator.TranslateEntityToBO(e, bo);
            if (err == ErrorCode.Success) response.AddValue(bo);
            else response.AddError(err.ToString());
            return response;
        }

        public GetResponse<IdNameBO> GetProjectPaymentGroupsForSelect(int projectid)
        {
            var response = new GetResponse<IdNameBO>();
            var entities = _uow.PaymentGroups.GetNoTracking().Where(m => m.ProjectId == projectid);
            foreach (var e in entities) response.AddValue(e.GetIdName());
            response.Values = response.Values.OrderBy(m => m.Display).ToList();
            return response;
        }

        public Response InsertUpdateProjectPaymentGroup(ProjectPaymentGroupBO groupBo)
        {
            var response = new Response();
            if (groupBo.ProjectId <= 0) response.AddError("ProjectID is verplicht");
            if (string.IsNullOrWhiteSpace(groupBo.Name)) response.AddError("Naam is verplicht");
            if (!response.Success) return response;

            InvoicingPaymentGroup entity = groupBo.Id == 0
                ? _uow.PaymentGroups.GetNew()
                : _uow.PaymentGroups.GetById(groupBo.Id);

            if (entity != null)
            {
                var err = ProjectPaymentGroupTranslator.TranslateBOToEntity(entity, groupBo, _uow);
                if (err != ErrorCode.Success) response.AddError(err.ToString());
            }
            else response.AddError("ProjectPaymentGroup not found");

            var saved = _uow.SaveChanges();
            response.AddSaveChangesResult(saved, "Betalingsgroep opgeslagen", "Betalingsgroep niet opgeslagen");
            return response;
        }

        public void LinkPaymentGroupToUnit(int unitid, int paymentgroupid)
        {
            var unit = _uow.Units.GetById(unitid);
            if (unit != null)
            {
                unit.PaymentGroupId = paymentgroupid;
                _uow.SaveChanges();
            }
        }

        public Response DeleteProjectPaymentGroup(List<int> ids)
        {
            var response = new Response();
            foreach (var id in ids) _uow.PaymentGroups.DeleteObject(id);
            var saved = _uow.SaveChanges();
            response.AddSaveChangesResult(saved, "Record(s) verwijderd", "Geen records verwijderd");
            return response;
        }

        public Response DeleteProjectPaymentGroup(List<ProjectPaymentGroupBO> bos)
            => DeleteProjectPaymentGroup(bos.Select(s => s.Id).ToList());


        // ===== PaymentStages =====
        public GetResponse<ProjectPaymentStageBO> GetProjectPaymentStage(int stageid)
        {
            var response = new GetResponse<ProjectPaymentStageBO>();
            var e = _uow.PaymentStages.GetNoTracking().FirstOrDefault(m => m.Id == stageid);

            var bo = new ProjectPaymentStageBO();
            var err = ProjectPaymentStageTranslator.TranslateEntityToBO(e, bo);
            if (err == ErrorCode.Success) response.AddValue(bo);
            else response.AddError(err.ToString());
            return response;
        }

        public GetResponse<UnitWithStagesBO> GetProjectInvoicableUnits(int projectid)
        {
            var response = new GetResponse<UnitWithStagesBO>();

            var units = _uow.Units.GetNoTracking()
                .Where(m =>
                    m.ProjectId == projectid &&
                    m.UnitConstructionValue.Any(l => l.PaymentGroup.InvoicingPaymentStages.Any(i => (bool)i.Invoicable)) &&
                    m.ClientAccountId > 0 &&
                    m.ClientAccount.DateDeedOfSale != null);

            foreach (var unit in units)
            {
                var stages = _uow.PaymentStages.GetNoTracking()
                    .Where(m =>
                        (bool)m.Invoicable &&
                        !m.InvoicesDetails.Any(i => i.UnitId == unit.Id) &&
                        m.Group.UnitConstructionValue.Any(l => l.UnitId == unit.Id));

                if (stages.Any())
                {
                    var bo = new UnitWithStagesBO();
                    var unitBo = new UnitBO();

                    foreach (var stage in stages)
                    {
                        var stageBo = new ProjectPaymentStageBO();
                        var err2 = ProjectPaymentStageTranslator.TranslateEntityToBO(stage, stageBo);
                        if (err2 == ErrorCode.Success) bo.PaymentStages.Add(stageBo);
                        else response.AddError(err2.ToString());
                    }

                    var err = UnitTranslator.TranslateEntityToBO(unit, unitBo);
                    if (err == ErrorCode.Success)
                    {
                        bo.Unit = unitBo;
                        response.AddValue(bo);
                    }
                    else response.AddError(err.ToString());
                }
            }
            return response;
        }

        public Response InsertUpdateProjectPaymentStage(ProjectPaymentStageBO stageBo)
        {
            var response = new Response();
            if (stageBo.GroupId <= 0) response.AddError("GroupID is verplicht");
            if (string.IsNullOrWhiteSpace(stageBo.Name)) response.AddError("Naam is verplicht");
            if (!response.Success) return response;

            InvoicingPaymentStages entity = stageBo.Id == 0
                ? _uow.PaymentStages.GetNew()
                : _uow.PaymentStages.GetById(stageBo.Id);

            if (entity != null)
            {
                var err = ProjectPaymentStageTranslator.TranslateBOToEntity(entity, stageBo, _uow);
                if (err != ErrorCode.Success) response.AddError(err.ToString());
            }
            else response.AddError("ProjectPaymentStage not found");

            var saved = _uow.SaveChanges();
            response.AddSaveChangesResult(saved, "Betalingsfase opgeslagen", "Betalingsfase niet opgeslagen");
            return response;
        }

        public Response UpdateProjectPaymentStageInvoicable(int stageid, bool invoicable)
        {
            var response = new Response();
            if (stageid <= 0) response.AddError("StageId is verplicht");
            if (!response.Success) return response;

            var entity = _uow.PaymentStages.GetById(stageid);
            if (entity != null) entity.Invoicable = invoicable;
            else response.AddError("ProjectPaymentStage not found");

            var saved = _uow.SaveChanges();
            response.AddSaveChangesResult(saved, "Invoicable vlag aangepast", "Invoicable vlag niet aangepast");
            return response;
        }

        public bool CheckProjectPaymentStageDocInUse(int docid)
        {
            var count = _uow.PaymentStages.GetNoTracking().Count(m => m.DocId == docid);
            return count != 0;
        }


        // ===== Invoicing =====
        public GetResponse<ChangeOrderBO> GetProjectInvoicableChangeOrders(int projectid)
        {
            var response = new GetResponse<ChangeOrderBO>();
            var entities = _uow.ChangeOrders.GetNoTracking()
                .Where(m =>
                    m.ContractActivity.Contract.ProjectId == projectid &&
                    m.ChangeOrderDetail.Any(i => (bool)i.Invoicable && !(bool)i.Invoiced) &&
                    m.ClientAccountId > 0 &&
                    m.ClientAccount.DateDeedOfSale != null &&
                    m.ChangeOrderDetail.Count(i => (bool)i.Invoiced) < m.ChangeOrderDetail.Count());

            foreach (var e in entities)
            {
                var bo = new ChangeOrderBO();
                var err = ChangeOrderTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<InvoiceBO> GetInvoicesByUnitIds(List<int> unitIds)
        {
            var response = new GetResponse<InvoiceBO>();
            var entities = _uow.Invoices.GetNoTracking().Where(InvoicesQuery.GetUnitsQuery(unitIds));

            foreach (var e in entities)
            {
                var bo = new InvoiceBO();
                var err = InvoiceTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<UtilityCostBO> GetProjectUtilityCost(int projectid, int clientid)
        {
            var response = new GetResponse<UtilityCostBO>();

            var udao = _uow.Units;
            var numberOfUnits = udao.GetNoTracking()
                .Count(m => m.ProjectId == projectid && (m.Type.GroupId == 1 || m.Type.GroupId == 4));

            // Contractlijnen nuts
            var contractActs = _uow.ContractActivities.GetNoTracking()
                .Where(m => m.Contract.ProjectId == projectid && m.ActivityId == 280);

            foreach (var act in contractActs)
            {
                decimal paid = _uow.IncommingInvoiceDetails.GetNoTracking()
                    .Where(m =>
                        m.ContractAct.ActivityId == 280 &&
                        m.Type == (decimal)IncommingInvoiceType.Contract &&
                        m.IncommingInvoice.ContractId == act.ContractId)
                    .Select(m => (decimal?)m.Price).Sum() ?? 0m;

                if (paid <= (act.Price ?? 0m))
                {
                    // percentages
                    var util = _uow.UtilityPercentages.GetNoTracking();
                    decimal pClient = util.Where(m => m.ContractId == act.ContractId && m.ClientAccountId == clientid)
                                          .Select(s => (decimal?)s.Percentage).Sum() ?? 0m;
                    decimal pOther = util.Where(m => m.ContractId == act.ContractId)
                                          .Select(s => (decimal?)s.Percentage).Sum() ?? 0m;
                    int otherUnits = util.Count(m => m.ContractId == act.ContractId);

                    decimal clientPct = pClient == 0
                        ? (100 - pOther) / (decimal)(Math.Max(1, numberOfUnits - otherUnits))
                        : pClient;

                    response.AddValue(new UtilityCostBO
                    {
                        ProjectId = projectid,
                        Price = (act.Price ?? 0m) - paid,
                        Description = "- NOG TE FACTUREREN -",
                        CompanyName = act.Contract.Company.BedrijfsNaam,
                        Percentage = clientPct
                    });
                }
            }

            // Inkomende facturen (los)
            var details = _uow.IncommingInvoiceDetails.GetNoTracking()
                .Where(m => m.IncommingInvoice.ProjectId == projectid &&
                           (m.ActId == 280 || m.ContractAct.ActivityId == 280));

            foreach (var d in details)
            {
                var util = _uow.UtilityPercentages.GetNoTracking();
                decimal pClient = util.Where(m => m.IncommingInvoiceDetailId == d.Id && m.ClientAccountId == clientid)
                                      .Select(s => (decimal?)s.Percentage).Sum() ?? 0m;
                decimal pOther = util.Where(m => m.IncommingInvoiceDetailId == d.Id)
                                      .Select(s => (decimal?)s.Percentage).Sum() ?? 0m;
                int otherUnits = util.Count(m => m.IncommingInvoiceDetailId == d.Id);

                decimal clientPct = pClient == 0
                    ? (100 - pOther) / (decimal)(Math.Max(1, numberOfUnits - otherUnits))
                    : pClient;

                response.AddValue(new UtilityCostBO
                {
                    ProjectId = projectid,
                    Price = (decimal)(d.Price ?? 0m),
                    Description = d.Description,
                    CompanyName = d.IncommingInvoice.CompanyId == null
                                  ? d.IncommingInvoice.Contract.Company.BedrijfsNaam
                                  : d.IncommingInvoice.Company.BedrijfsNaam,
                    Percentage = clientPct
                });
            }

            return response;
        }

        public Response InsertUpdateProjectInvoice(InvoiceBO invoiceBo)
        {
            var response = new Response();

            if (string.IsNullOrWhiteSpace(invoiceBo.Filename)) response.AddError("Bestandsnaam is verplicht");
            if (invoiceBo.Rows.Count == 0) response.AddError("Er is minstens één detailrij nodig");
            if (!response.Success) return response;

            Invoices entity = invoiceBo.Id == 0
                ? _uow.Invoices.GetNew()
                : _uow.Invoices.GetById(invoiceBo.Id);

            if (entity != null)
            {
                var err = InvoiceTranslator.TranslateBOToEntity(entity, invoiceBo, _uow);
                if (err != ErrorCode.Success) response.AddError(err.ToString());
            }
            else response.AddError("Invoice not found");

            var saved = _uow.SaveChanges();
            response.AddSaveChangesResult(saved, "Factuur opgeslagen", "Factuur niet opgeslagen");
            return response;
        }

        public Response InsertUpdateProjectInvoices(List<InvoiceBO> invoices)
        {
            var response = new Response();

            foreach (var inv in invoices)
            {
                if (string.IsNullOrWhiteSpace(inv.Filename)) response.AddError("Bestandsnaam is verplicht");
                if (inv.Rows.Count == 0) response.AddError("Er is minstens één detailrij nodig");
                if (!response.Success) return response;

                Invoices entity = inv.Id == 0
                    ? _uow.Invoices.GetNew()
                    : _uow.Invoices.GetById(inv.Id);

                if (entity != null)
                {
                    var err = InvoiceTranslator.TranslateBOToEntity(entity, inv, _uow);
                    if (err != ErrorCode.Success) response.AddError(err.ToString());
                }
                else response.AddError("Invoice not found");

                var saved = _uow.SaveChanges();
                response.AddSaveChangesResult(saved, "Factuur opgeslagen", "Factuur niet opgeslagen");
            }

            return response;
        }


        // Contracts
        public GetResponse<ContractBO> GetProjectContracts(int projectid)
        {
            var response = new GetResponse<ContractBO>();
            var entities = _uow.Contracts.GetNoTracking()
                .Where(m => m.ProjectId == projectid)
                .Include(m => m.ContractActivity)
                    .ThenInclude(m => m.Activity)
                        .ThenInclude(m => m.Group)
                .Include(m => m.Company);

            foreach (var e in entities)
            {
                var bo = new ContractBO();
                var err = ContractTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<IdNameBO> GetProjectContractsForSelect(int projectid)
        {
            var response = new GetResponse<IdNameBO>();
            var entities = _uow.Contracts.GetNoTracking()
                .Where(m => m.ProjectId == projectid)
                .Include(m => m.Company)
                .Include(m => m.ContractActivity)
                    .ThenInclude(m => m.Activity);

            foreach (var e in entities) response.AddValue(e.GetIdName());
            return response;
        }

        public GetResponse<IdNameBO> GetProjectContractActivitiesForSelect(int projectid)
        {
            var response = new GetResponse<IdNameBO>();

            var contracts = _uow.Contracts.GetNoTracking().Where(m => m.ProjectId == projectid)
                .Include(m => m.Company)
                .Include(m => m.ContractActivity)
                .ThenInclude(m => m.Activity);

            foreach (var c in contracts)
                foreach (var act in c.ContractActivity)
                {
                    IdNameBO bo = new IdNameBO();
                    bo.ID = act.Id;
                    bo.Display = act.Activity.Omschrijving + " - " + c.Company.BedrijfsNaam;
                    response.AddValue(bo);
                }

                    

            return response;
        }

        public Response InsertUpdateProjectContract(ContractBO contractBo)
        {
            var response = new Response();
            if (contractBo.Company.ID == 0) response.AddError("Bedrijf selecteren is verplicht");
            if (contractBo.Activities.Count == 0) response.AddError("Er is minstens één lot nodig");
            if (!response.Success) return response;

            Contract entity =
                contractBo.Id == 0
                    ? _uow.Contracts.GetNew()
                    : _uow.Contracts.GetNormal()
                        .Where(m => m.Id == contractBo.Id)
                        .Include(m => m.ContractActivity)
                            .ThenInclude(m => m.Activity)
                        .SingleOrDefault();

            if (entity != null)
            {
                var err = ContractTranslator.TranslateBOToEntity(entity, contractBo, _uow);
                if (err != ErrorCode.Success) response.AddError(err.ToString());
            }
            else response.AddError("Contract not found");

            var saved = _uow.SaveChanges();
            response.AddSaveChangesResult(saved, "Contract opgeslagen", "Contract niet opgeslagen");
            return response;
        }

        public GetResponse<ContractBO> GetContract(int contractid)
        {
            var response = new GetResponse<ContractBO>();
            var e = _uow.Contracts.GetNormal()
                .Where(m => m.Id == contractid)
                .Include(m => m.Company)
                .Include(m => m.ContractActivity).ThenInclude(m => m.Activity)
                .Include(m => m.ContractActivity).ThenInclude(m => m.ChangeOrder)
                .FirstOrDefault();

            var bo = new ContractBO();
            var err = ContractTranslator.TranslateEntityToBO(e, bo);
            if (err == ErrorCode.Success) response.AddValue(bo);
            else response.AddError(err.ToString());
            return response;
        }

        public Response DeleteContracts(List<int> ids)
        {
            var response = new Response();
            foreach (var id in ids) _uow.Contracts.DeleteObject(id);
            var saved = _uow.SaveChanges();
            response.AddSaveChangesResult(saved, "Record(s) verwijderd", "Geen records verwijderd");
            return response;
        }

        public GetResponse<IdNameBO> GetContractChangeOrdersForSelect(int contractid)
        {
            var response = new GetResponse<IdNameBO>();
            var entities = _uow.ChangeOrders.GetNoTracking()
                .Where(m => m.ContractActivity.ContractId == contractid);
            foreach (var e in entities) response.AddValue(e.GetIdName());
            return response;
        }

        public GetResponse<ContractBO> GetProjectContractsWithoutInvoices(int projectid, int activityid = 0)
        {
            var response = new GetResponse<ContractBO>();
            IEnumerable<Contract> entities;

            if (activityid == 0)
            {
                entities = _uow.Contracts.GetNoTracking()
                    .Where(m => m.ProjectId == projectid && m.IncommingInvoices.Count == 0)
                    .Include(m => m.ContractActivity).ThenInclude(m => m.Activity).ThenInclude(m => m.Group)
                    .Include(m => m.Company);
            }
            else
            {
                entities = _uow.Contracts.GetNoTracking()
                    .Where(m =>
                        m.ProjectId == projectid &&
                        m.ContractActivity.Any(s => s.ActivityId == activityid) &&
                        m.IncommingInvoices.Count(l => l.IncommingInvoiceDetail.Any(i => i.ContractAct.ActivityId == activityid)) == 0)
                    .Include(m => m.ContractActivity).ThenInclude(m => m.Activity).ThenInclude(m => m.Group)
                    .Include(m => m.Company);
            }

            foreach (var e in entities)
            {
                var bo = new ContractBO();
                var err = ContractTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public decimal GetContractActivityPrice(int contractactid)
        {
            return (decimal)(_uow.ContractActivities.GetById(contractactid).Price ?? 0m);
        }

        public GetResponse<ContractActivityBO> GetProjectContractActivitiesByActivityId(int projectid, int activityid)
        {
            var response = new GetResponse<ContractActivityBO>();
            var entities = _uow.ContractActivities.GetNormal()
                .Where(m => m.Contract.ProjectId == projectid && m.ActivityId == activityid)
                .Include(m => m.Activity).ThenInclude(m => m.Group)
                .Include(m => m.Insurances).ThenInclude(m => m.InsuranceCompany);

            foreach (var e in entities)
            {
                var bo = new ContractActivityBO();
                var err = ContractActivityTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        // Budget
        public GetResponse<BudgetActivityBO> GetProjectBudget(int projectid)
        {
            var response = new GetResponse<BudgetActivityBO>();
            var entities = _uow.Budgets.GetNoTracking()
                .Where(m => m.ProjectId == projectid)
                .Include(m => m.Activity).ThenInclude(m => m.Group);

            foreach (var e in entities)
            {
                var bo = new BudgetActivityBO();
                var err = BudgetTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public Response InsertUpdateProjectBudgetActivity(BudgetActivityBO budgetBo)
        {
            var response = new Response();
            if (budgetBo.ProjectId == 0) response.AddError("Project is niet geselecteerd");
            if (budgetBo.Activity.ID == 0) response.AddError("Er is geen activiteit geselecteerd");
            if (!response.Success) return response;

            ProjectBudget entity = budgetBo.Id == 0
                ? _uow.Budgets.GetNew()
                : _uow.Budgets.GetById(budgetBo.Id);

            if (entity != null)
            {
                var err = BudgetTranslator.TranslateBOToEntity(entity, budgetBo, _uow);
                if (err != ErrorCode.Success) response.AddError(err.ToString());
            }
            else response.AddError("BudgetActivity not found");

            var saved = _uow.SaveChanges();
            response.AddSaveChangesResult(saved, "Budget-activiteit opgeslagen", "Budget-activiteit niet opgeslagen");
            return response;
        }

        public Response InsertUpdateProjectBudgetActivities(List<BudgetActivityBO> budgetActivities, int projectid)
        {
            var response = new Response();

            foreach (var ba in budgetActivities)
            {
                if (ba.ProjectId == 0) response.AddError("Project is niet geselecteerd");
                if (ba.Activity.ID == 0) response.AddError("Er is geen activiteit geselecteerd");
                if (!response.Success) return response;

                ProjectBudget entity = ba.Id == 0 ? _uow.Budgets.GetNew() : _uow.Budgets.GetById(ba.Id);
                if (entity != null)
                {
                    var err = BudgetTranslator.TranslateBOToEntity(entity, ba, _uow);
                    if (err != ErrorCode.Success) response.AddError(err.ToString());
                }
                else response.AddError("BudgetActivity not found");

                var savedInner = _uow.SaveChanges();
                response.AddSaveChangesResult(savedInner, "Budget-activiteit opgeslagen", "Budget-activiteit niet opgeslagen");
            }

            // oude loten verwijderen die niet meer in de lijst zitten
            var existing = _uow.Budgets.GetNoTracking().Where(m => m.ProjectId == projectid).ToList();
            var toDelete = existing.Where(x => !budgetActivities.Any(f => f.Id == x.Id) && !budgetActivities.Any(f => f.Id == 0)).ToList();
            foreach (var x in toDelete) _uow.Budgets.DeleteObject(x.Id);

            var saved = _uow.SaveChanges();
            response.AddSaveChangesResult(saved, "Verwijderingen doorgevoerd", "Geen verwijderingen doorgevoerd");
            return response;
        }

        // ChangeOrders
        public GetResponse<ChangeOrderBO> GetProjectChangeOrders(int projectid)
        {
            var response = new GetResponse<ChangeOrderBO>();
            var entities = _uow.ChangeOrders.GetNoTracking()
                .Where(m => m.ContractActivity.Contract.ProjectId == projectid);

            foreach(var e in entities)
            {
                var bo = new ChangeOrderBO();
                var err = ChangeOrderTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<ChangeOrderBO> GetClientChangeOrders(int number, int clientaccountid)
        {
            var response = new GetResponse<ChangeOrderBO>();


            IQueryable<ChangeOrder> query = _uow.ChangeOrders.GetNoTracking()
                .Where(m => m.ClientAccountId == clientaccountid)
                .Include(m => m.ClientAccount)
                .Include(m => m.ContractActivity).ThenInclude(m => m.Contract)
                .Include(m => m.ChangeOrderDetail);

            // eerst ordenen...
            query = query.OrderByDescending(m => m.Date);

            // ...dan enkel Take toepassen als number > 0
            if (number > 0)
                query = query.Take(number);

            var entities = query.ToList();

            foreach (var e in entities)
            {
                var bo = new ChangeOrderBO();
                var err = ChangeOrderTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<ChangeOrderBO> GetChangeOrder(int changeorderid)
        {
            var response = new GetResponse<ChangeOrderBO>();
            var e = _uow.ChangeOrders.GetNormal()
                .Where(m => m.Id == changeorderid)
                .Include(m => m.ClientAccount)
                .Include(m => m.ContractActivity).ThenInclude(m => m.Contract)
                .Include(m => m.ChangeOrderDetail)
                .FirstOrDefault();

            var bo = new ChangeOrderBO();
            var err = ChangeOrderTranslator.TranslateEntityToBO(e, bo);
            if (err == ErrorCode.Success) response.AddValue(bo);
            else response.AddError(err.ToString());
            return response;
        }

        public Response InsertUpdateProjectChangeOrder(ChangeOrderBO changeorder)
        {
            var response = new Response();
            if (changeorder.ClientAccountID == 0) response.AddError("ClientAccount is niet geselecteerd");
            if (changeorder.ContractActivityID == 0) response.AddError("Er is geen activiteit geselecteerd");
            if (!response.Success) return response;

            ChangeOrder entity = changeorder.Id == 0
                ? _uow.ChangeOrders.GetNew()
                : _uow.ChangeOrders.GetNormal()
                .Where(m => m.Id == changeorder.Id)
                .Include(m => m.ChangeOrderDetail)
                .FirstOrDefault();

            if (entity != null)
            {
                var err = ChangeOrderTranslator.TranslateBOToEntity(entity, changeorder, _uow);
                if (err != ErrorCode.Success) response.AddError(err.ToString());
            }
            else response.AddError("Change Order not found");

            var saved = _uow.SaveChanges();
            response.AddSaveChangesResult(saved, "Meerwerk opgeslagen", "Meerwerk niet opgeslagen");
            return response;
        }

        public Response DeleteChangeOrders(List<int> ids)
        {
            var response = new Response();
            foreach (var id in ids) _uow.ChangeOrders.DeleteObject(id);
            var saved = _uow.SaveChanges();
            response.AddSaveChangesResult(saved, "Record(s) verwijderd", "Geen records verwijderd");
            return response;
        }

        public Response UpdateProjectChangeOrderInvoicable(int COid, bool invoicable)
        {
            var response = new Response();
            if (COid <= 0) response.AddError("COid is verplicht");
            if (!response.Success) return response;

            var details = _uow.ChangeOrderDetails.GetNormal().Where(m => m.ChangeOrderId == COid);
            foreach (var d in details) d.Invoicable = invoicable;

            var saved = _uow.SaveChanges();
            response.AddSaveChangesResult(saved, "Invoicable vlag(-gen) aangepast", "Niets aangepast");
            return response;
        }

        public Response UpdateProjectChangeOrderDetailInvoicable(int CODetailid, bool invoicable)
        {
            var response = new Response();
            if (CODetailid <= 0) response.AddError("CODetailid is verplicht");
            if (!response.Success) return response;

            var entity = _uow.ChangeOrderDetails.GetById(CODetailid);
            if (entity != null) entity.Invoicable = invoicable;
            else response.AddError("ChangeOrderDetail not found");

            var saved = _uow.SaveChanges();
            response.AddSaveChangesResult(saved, "Detail invoicable aangepast", "Detail niet aangepast");
            return response;
        }

        public Response SetChangeOrderDetailInvoiced(int codid)
        {
            var response = new Response();
            if (codid == 0) response.AddError("Geen OrderDetail ingegeven");
            if (!response.Success) return response;

            var e = _uow.ChangeOrderDetails.GetById(codid);
            if (e != null) e.Invoiced = true;
            else response.AddError("Change Order not found");

            var saved = _uow.SaveChanges();
            response.AddSaveChangesResult(saved, "Detail gemarkeerd als gefactureerd", "Detail niet aangepast");
            return response;
        }


        // Incomming Invoices
        public GetResponse<IncommingInvoiceBO> GetIncommingInvoice(int invoiceid)
        {
            var response = new GetResponse<IncommingInvoiceBO>();

            var entity = _uow.IncommingInvoices.GetNoTracking()
                .Where(m => m.Id == invoiceid)
                .Include(m => m.Company)
                .Include(m => m.Project)
                .Include(m => m.IncommingInvoiceDetail)
                    .ThenInclude(d => d.Act)
                        .ThenInclude(a => a.Group)
                .Include(m => m.IncommingInvoiceDetail)
                    .ThenInclude(d => d.ContractAct)
                        .ThenInclude(ca => ca.Activity)
                            .ThenInclude(a => a.Group)
                .Include(m => m.IncommingInvoiceDetail)
                    .ThenInclude(d => d.ChangeOrder)
                        .ThenInclude(co => co.ChangeOrderDetail)
                .Include(m => m.Contract)
                    .ThenInclude(c => c.Company)
                .SingleOrDefault();

            var bo = new IncommingInvoiceBO();
            var err = IncommingInvoiceTranslator.TranslateEntityToBO(entity, bo);
            if (err == ErrorCode.Success) response.AddValue(bo);
            else response.AddError(err.ToString());

            return response;
        }

        public Response InsertUpdateProjectIncommingInvoice(IncommingInvoiceBO invoice)
        {
            var response = new Response();
            if (invoice.ContractID == 0 && invoice.CompanyId == 0)
                response.AddError("De leverancier is niet geselecteerd");
            if (!response.Success) return response;

            IncommingInvoices entity;
            if (invoice.Id == 0)
            {
                entity = _uow.IncommingInvoices.GetNew();
            }
            else
            {
                entity = _uow.IncommingInvoices.GetNormal()
                    .Where(m => m.Id == invoice.Id)
                    .Include(m => m.IncommingInvoiceDetail)
                    .SingleOrDefault();
            }

            if (entity is null)
            {
                response.AddError("Incomming Invoice not found");
                return response;
            }

            var err = IncommingInvoiceTranslator.TranslateBOToEntity(entity, invoice, _uow);
            if (err != ErrorCode.Success) response.AddError(err.ToString());

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Inkomende factuur opgeslagen", "Inkomende factuur niet opgeslagen");
            return response;
        }

        public GetResponse<IncommingInvoiceActivityBO> GetProjectIncommingInvoicesForRecalculation(int projectid)
        {
            var response = new GetResponse<IncommingInvoiceActivityBO>();

            var entities = _uow.IncommingInvoiceDetails.GetNoTracking()
                .Where(m => m.IncommingInvoice.ProjectId == projectid)
                .Include(m => m.IncommingInvoice).ThenInclude(i => i.Contract).ThenInclude(c => c.Company)
                .Include(m => m.IncommingInvoice).ThenInclude(i => i.Company)
                .Include(m => m.ContractAct).ThenInclude(ca => ca.Contract).ThenInclude(c => c.Company)
                .Include(m => m.ContractAct).ThenInclude(ca => ca.Activity).ThenInclude(a => a.Group)
                .Include(m => m.Act).ThenInclude(a => a.Group)
                .Include(m => m.ChangeOrder);

            foreach (var e in entities)
            {
                var bo = new IncommingInvoiceActivityBO();
                var err = IncommingInvoiceActivityTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<IncommingInvoiceActivityBO> GetProjectIncommingInvoicesByActivity(int projectid, int activityid)
        {
            var response = new GetResponse<IncommingInvoiceActivityBO>();

            var entities = _uow.IncommingInvoiceDetails.GetNoTracking()
                .Where(m =>
                    (m.ActId == activityid && m.IncommingInvoice.ProjectId == projectid) ||
                    (m.ContractAct.ActivityId == activityid && m.IncommingInvoice.ProjectId == projectid))
                .Include(m => m.ContractAct).ThenInclude(ca => ca.Contract).ThenInclude(c => c.Company)
                .Include(m => m.ContractAct).ThenInclude(ca => ca.Activity).ThenInclude(a => a.Group)
                .Include(m => m.Act).ThenInclude(a => a.Group)
                .Include(m => m.IncommingInvoice).ThenInclude(i => i.Company);

            foreach (var e in entities)
            {
                var bo = new IncommingInvoiceActivityBO();
                var err = IncommingInvoiceActivityTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<IncommingInvoiceActivityBO> GetProjectIncommingInvoicesByGroup(int projectid, int groupid)
        {
            var response = new GetResponse<IncommingInvoiceActivityBO>();

            var entities = _uow.IncommingInvoiceDetails.GetNoTracking()
                .Where(m =>
                    (m.Act.GroupId == groupid && m.IncommingInvoice.ProjectId == projectid) ||
                    (m.ContractAct.Activity.GroupId == groupid && m.IncommingInvoice.ProjectId == projectid))
                .Include(m => m.ContractAct).ThenInclude(ca => ca.Contract).ThenInclude(c => c.Company)
                .Include(m => m.ContractAct).ThenInclude(ca => ca.Activity).ThenInclude(a => a.Group)
                .Include(m => m.Act).ThenInclude(a => a.Group)
                .Include(m => m.IncommingInvoice).ThenInclude(i => i.Company);

            foreach (var e in entities)
            {
                var bo = new IncommingInvoiceActivityBO();
                var err = IncommingInvoiceActivityTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public Response DeleteIncommingInvoices(List<int> ids)
        {
            var response = new Response();
            foreach (var id in ids)
                _uow.IncommingInvoices.DeleteObject(id);

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Record(s) verwijderd", "Geen records verwijderd");
            return response;
        }

        // Insurances
        public GetResponse<InsuranceBO> GetProjectInsurances(int projectid)
        {
            var response = new GetResponse<InsuranceBO>();

            var entities = _uow.Insurances.GetNoTracking()
                .Where(m => m.ContractActivity.Contract.ProjectId == projectid)
                .Include(m => m.ContractActivity)
                .ThenInclude(m => m.Contract)
                .ThenInclude(m => m.Company)
                .Include(m => m.InsuranceCompany);

            foreach (var e in entities)
            {
                var bo = new InsuranceBO();
                var err = InsuranceTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }


        // HELPERS

        public string GenerateSlug(string phrase)
        {
            string str = RemoveAccent(phrase).ToLower();
            str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
            str = Regex.Replace(str, @"\s+", " ").Trim();
            str = str.Substring(0, str.Length <= 45 ? str.Length : 45).Trim(); // <- toewijzen!
            str = Regex.Replace(str, @"\s", "-");
            return str;
        }

        public string RemoveAccent(string txt)
        {
            var bytes = Encoding.GetEncoding("Cyrillic").GetBytes(txt);
            return Encoding.ASCII.GetString(bytes);
        }

        public DateOnly AddWorkDays(DateOnly date, int workingDays, Array BWDS, Array VDS)
        {
            if (workingDays == 0) return date;

            var newDate = date;
            var remaining = Math.Abs(workingDays);
            var step = workingDays < 0 ? -1 : 1;

            while (remaining > 0)
            {
                newDate = newDate.AddDays(step);
                var isWeekend = newDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                var isBadWeather = Array.IndexOf(BWDS, newDate) >= 0;
                var isVacation = Array.IndexOf(VDS, newDate) >= 0;

                if (!isWeekend && !isBadWeather && !isVacation)
                    remaining--;
            }
            return newDate;
        }

        public int BusinessDaysUntil(DateOnly start, DateOnly end, Array VDS)
        {
            if (end <= start) return 0;

            var d = start.AddDays(1);
            var workdays = 0;
            while (d <= end)
            {
                var isWeekend = d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                var isVacation = Array.IndexOf(VDS, d) >= 0;
                if (!isWeekend && !isVacation) workdays++;
                d = d.AddDays(1);
            }
            return workdays;
        }

        public Response Copyids()
        {
            var response = new Response();

            var details = _uow.InvoiceDetails.GetNormal(); // tracking
            foreach (var det in details)
            {
                var cv = _uow.UnitConstructionValues.GetNoTracking()
                    .FirstOrDefault(m => m.UnitId == det.UnitId &&
                                         m.PaymentGroupId == det.PaymentStage.GroupId);
                if (cv is not null)
                    det.ConstructionValueId = cv.Id;
            }

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "IDs gekopieerd", "IDs niet gekopieerd");
            return response;
        }

    }
}

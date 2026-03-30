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
            public string StatusName { get; set; }
            public string Location { get; set; }
            public string DefaultPic { get; set; }
            public string DefaultPicCaption { get; set; }
            public DateOnly? DeliveryDate { get; set; }
            public DateOnly? StartDateConstruction { get; set; }
            public string CommercialTitleNl { get; set; }
            public string CommercialTextNl { get; set; }
            public string Slug { get; set; }
            public string UserId { get; set; }
            public int? BuilderId { get; set; }
            public int ProjectType { get; set; }
            public int ClientCount { get; set; }
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
                    StatusName = m.Status.StatusName,
                    Location = m.PostalCode.Gemeente,
                    DefaultPic = m.DefaultPicture.Name,
                    DefaultPicCaption = m.DefaultPicture.Caption,
                    DeliveryDate = m.DeliveryDate,
                    StartDateConstruction = m.StartDateConstruction,
                    CommercialTitleNl = m.CommercialTitleNl,
                    CommercialTextNl = m.CommercialTextNl,
                    Slug = m.Slug,
                    UserId = m.AspNetUserId,
                    BuilderId = m.BuilderId,
                    ProjectType = m.ProjectType,
                    ClientCount = m.Units.Count(u => u.ClientAccountId != null)
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
                bo.Status.Name = e.StatusName;
                bo.Postalcode.Gemeente = e.Location;
                bo.StartDateConstruction = e.StartDateConstruction;
                bo.ClientCount = e.ClientCount;

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
                        Type = "danger",
                        Category = "oplevering"
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
                                Type = "danger",
                                Category = "oplevering"
                            });
                        }
                        else if (e.DeliveryDate.Value.AddMonths(11) <= today)
                        {
                            response.AddValue(new WarningBO
                            {
                                ID = e.ProjectId,
                                ProjectId = e.ProjectId,
                                Display = $"De definitieve oplevering van project {e.ProjectName} kan gebeuren vanaf {e.DeliveryDate.Value.AddMonths(11)} , gelieve deze aan te vragen!",
                                Type = "warning",
                                Category = "oplevering"
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
                boDocs.Category = "documenten";

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

        public GetResponse<CompanyInvoiceSummaryBO> GetProjectIncommingInvoiceCompanySummaries(int projectid)
        {
            var response = new GetResponse<CompanyInvoiceSummaryBO>();
            var entities = _uow.IncommingInvoices.GetNoTracking()
                .Where(i => i.ProjectId == projectid)
                .Include(i => i.Contract).ThenInclude(c => c.Company)
                .Include(i => i.Company)
                .ToList();

            var grouped = entities
                .Select(i =>
                {
                    var company = i.Company ?? i.Contract?.Company;
                    return new
                    {
                        Company = company,
                        Total = i.Price
                    };
                })
                .Where(x => x.Company != null)
                .GroupBy(x => new { x.Company.CompanyId, x.Company.BedrijfsNaam })
                .Select(group => new CompanyInvoiceSummaryBO
                {
                    Company = new IdNameBO
                    {
                        ID = group.Key.CompanyId,
                        Display = group.Key.BedrijfsNaam
                    },
                    TotalInvoiced = group.Sum(x => x.Total)
                })
                .ToList();

            foreach (var summary in grouped)
            {
                response.AddValue(summary);
            }

            return response;
        }

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
                entity = new ProjectPictures();
                ctx.Set<ProjectPictures>().Add(entity);
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
            if (ctx.Set<Project>().Local.All(p => p.ProjectId != picture.ProjectId))
                ctx.Attach(new Project { ProjectId = picture.ProjectId });
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

            // Haal alle units van de betrokken projecten in één query
            var projectIds = ids.ToArray(); // belangrijk: local array

            var units = _uow.Units.GetNoTracking()
                .Where(u => projectIds.Contains(u.ProjectId))
                .Select(u => new
                {
                    u.Id,
                    u.ProjectId,
                    u.ClientAccountId,
                    u.AttachedUnit,
                    u.LinkedUnit,
                    u.Type,
                    u.LandValue,
                    u.LandValueSold,
                    UnitConstructionValues = u.UnitConstructionValue
                        .Select(v => new
                        {
                            v.Value,
                            v.ValueSold
                        })
                })
                .ToList(); // ⬅️ materialiseer HIER


            foreach (var pid in ids)
            {
                // Filter 1x per project
                var u = units
                    .Where(m => m.ProjectId == pid && m.AttachedUnit == null && m.LinkedUnit == null)
                    .ToList();

                var living = u.Where(i => i.Type.GroupId == 1 || i.Type.GroupId == 4).ToList();
                var livingCount = living.Count;
                var livingSold = living.Count(i => i.ClientAccountId != null);

                // 💰 Totale waarde te koop (grond + bouwsom)
                decimal valueForSale = u
                    .Where(m => m.ClientAccountId == null)
                    .Select(m =>
                        (m.LandValue ?? 0m) +
                        (m.UnitConstructionValues.Sum(x => (decimal?)x.Value) ?? 0m))
                    .Sum();
                //

                // 💰 Totale waarde verkocht (grond + bouwsom verkocht)
                decimal valueSold = u
                    .Select(m =>
                        (m.LandValueSold ?? 0m) +
                        (m.UnitConstructionValues.Sum(x => (decimal?)x.ValueSold) ?? 0m))
                    .Sum();
                //

                // 📊 Percentages
                decimal percentageLiving = livingCount != 0
                    ? (decimal)(livingSold / (double)livingCount * 100)
                    : 100;

                decimal percentageSold = (valueSold + valueForSale) != 0
                    ? (decimal)(valueSold / (valueSold + valueForSale) * 100)
                    : 100;

                // 🏘️ Aantallen
                int numApts = u.Count(m => m.Type.Id == 1);
                int numHouses = u.Count(m => m.Type.Id == 2);
                int numCommercial = u.Count(m => m.Type.Id == 10);

                // 💶 Startprijs (minimale som van grond + bouw)
                decimal startingPrice = u
                    .Where(i => i.ClientAccountId == null && (i.Type.GroupId == 1 || i.Type.GroupId == 4))
                    .Select(i =>
                        (i.UnitConstructionValues.Sum(v => (decimal?)v.Value) ?? 0m)
                        + (i.LandValue ?? 0m))
                    .DefaultIfEmpty(0m)
                    .Min();

                //

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
        public Response InsertUpdateSalesText(ProjectBO project)
        {
            var response = new Response();
            if (project.Id <= 0) response.AddError("ProjectID is verplicht");
            if (!response.Success) return response;

            Project entity =  _uow.Projects.GetById(project.Id);

            if (entity != null)
            {
                entity.CommercialTextNl = project.CommercialTextNL;
                entity.CommercialTitleNl = project.CommercialTitleNL;
            }
            else response.AddError("Project not found");

            var saved = _uow.SaveChanges();
            response.AddSaveChangesResult(saved, "Project websitedata opgeslagen", "roject websitedata niet opgeslagen");
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

        // ===== Contact Requests =====
        public Response InsertProjectContactRequest(ContactRequestBO request)
        {
            var response = new Response();
            if (request == null)
            {
                response.AddError("Contactaanvraag is leeg");
                return response;
            }

            var entity = _uow.ContactRequests.GetNew();
            var err = ContactRequestTranslator.TranslateBOToEntity(entity, request);
            if (err != ErrorCode.Success)
            {
                response.AddError(err.ToString());
                return response;
            }

            var saved = _uow.SaveChanges();
            response.InsertedId = entity.Id;
            response.AddSaveChangesResult(saved, "Contactaanvraag opgeslagen", "Contactaanvraag niet opgeslagen");
            return response;
        }

        public GetResponse<ContactRequestBO> GetProjectContactRequests(int projectid)
        {
            var response = new GetResponse<ContactRequestBO>();
            var query = _uow.ContactRequests.GetNoTracking();

            if (projectid > 0)
                query = query.Where(c => c.ProjectId == projectid);

            var entities = query.OrderByDescending(c => c.CreatedAt).ToList();

            foreach (var e in entities)
            {
                var bo = new ContactRequestBO();
                var err = ContactRequestTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }

            return response;
        }

        public Response UpdateContactRequestGroup(int projectid, string email, string fullname, string phone, ContactRequestBO updatedValues)
        {
            var response = new Response();

            if (updatedValues == null)
            {
                response.AddError("Contactgegevens ontbreken");
                return response;
            }

            var query = _uow.ContactRequests.GetNormal();

            if (projectid > 0)
                query = query.Where(c => c.ProjectId == projectid);

            var normalizedEmail = NormalizeContactValue(email);
            var normalizedName = NormalizeContactValue(fullname);
            var normalizedPhone = NormalizeContactValue(phone);

            var matches = query.ToList()
                .Where(c => MatchesContactGroup(c, normalizedEmail, normalizedName, normalizedPhone))
                .ToList();

            if (!matches.Any())
            {
                response.AddError("Geen contact gevonden om bij te werken");
                return response;
            }

            foreach (var contact in matches)
            {
                contact.Firstname = updatedValues.Firstname;
                contact.Lastname = updatedValues.Lastname;
                contact.Fullname = !string.IsNullOrWhiteSpace(updatedValues.Fullname)
                    ? updatedValues.Fullname
                    : $"{updatedValues.Firstname} {updatedValues.Lastname}".Trim();
                contact.Email = updatedValues.Email;
                contact.Phone = updatedValues.Phone;
                if (updatedValues.CreatedAt != default)
                    contact.CreatedAt = updatedValues.CreatedAt;
                if (!string.IsNullOrWhiteSpace(updatedValues.SourceSite))
                    contact.SourceSite = updatedValues.SourceSite;
            }

            var saved = _uow.SaveChanges();
            response.AddSaveChangesResult(saved, "Contact bijgewerkt", "Contact niet bijgewerkt");
            return response;
        }

        public Response DeleteContactRequestGroup(int projectid, string email, string fullname, string phone)
        {
            var response = new Response();
            var query = _uow.ContactRequests.GetNormal();

            if (projectid > 0)
                query = query.Where(c => c.ProjectId == projectid);

            var normalizedEmail = NormalizeContactValue(email);
            var normalizedName = NormalizeContactValue(fullname);
            var normalizedPhone = NormalizeContactValue(phone);

            var matches = query.ToList()
                .Where(c => MatchesContactGroup(c, normalizedEmail, normalizedName, normalizedPhone))
                .ToList();

            if (!matches.Any())
            {
                response.AddError("Geen contact gevonden om te verwijderen");
                return response;
            }

            foreach (var contact in matches)
            {
                _uow.ContactRequests.Remove(contact);
            }

            var saved = _uow.SaveChanges();
            response.AddSaveChangesResult(saved, "Contact verwijderd", "Contact niet verwijderd");
            return response;
        }

        private static bool MatchesContactGroup(ContactRequests contact, string normalizedEmail, string normalizedName, string normalizedPhone)
        {
            var contactEmail = NormalizeContactValue(contact.Email);
            var contactName = NormalizeContactValue(GetContactName(contact));
            var contactPhone = NormalizeContactValue(contact.Phone);

            if (!string.IsNullOrWhiteSpace(normalizedEmail))
            {
                return contactEmail == normalizedEmail;
            }

            return contactName == normalizedName && contactPhone == normalizedPhone;
        }

        private static string GetContactName(ContactRequests contact)
        {
            if (!string.IsNullOrWhiteSpace(contact.Fullname))
                return contact.Fullname;

            return $"{contact.Firstname} {contact.Lastname}".Trim();
        }

        private static string NormalizeContactValue(string value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();


        // ===== PaymentGroups =====
        public GetResponse<ProjectPaymentGroupBO> GetProjectPaymentGroups(int projectid)
        {
            var response = new GetResponse<ProjectPaymentGroupBO>();
            var entities = _uow.PaymentGroups
                .GetNormal()
                .Include(m => m.VatType)
                .Include(m => m.InvoicingPaymentStages)
                    .ThenInclude(s => s.Doc)
                .Include(m => m.InvoicingPaymentStages)
                    .ThenInclude(s => s.InvoicesDetails)
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
            var e = _uow.PaymentGroups
                .GetNormal()
                .Include(g => g.VatType)
                .Include(g => g.InvoicingPaymentStages)
                    .ThenInclude(s => s.Doc)
                .Include(g => g.InvoicingPaymentStages)
                    .ThenInclude(s => s.InvoicesDetails)
                .FirstOrDefault(m => m.Id == groupid);

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
            var e = _uow.PaymentStages
                .GetNoTracking()
                .Include(m => m.Group)
                .FirstOrDefault(m => m.Id == stageid);

            var bo = new ProjectPaymentStageBO();
            var err = ProjectPaymentStageTranslator.TranslateEntityToBO(e, bo);
            if (err == ErrorCode.Success) response.AddValue(bo);
            else response.AddError(err.ToString());
            return response;
        }

        public GetResponse<UnitWithStagesBO> GetProjectInvoicableUnits(int projectid)
        {
            var response = new GetResponse<UnitWithStagesBO>();

            // 1) Units materialiseren (enkel klanten met akte-datum t.e.m. morgen)
            var cutoffDate = DateOnly.FromDateTime(DateTime.Today).AddDays(1);
            var units = _uow.Units.GetNoTracking()
                .Where(u =>
                    u.ProjectId == projectid &&
                    u.ClientAccountId > 0 &&
                    u.ClientAccount.DateDeedOfSale != null &&
                    u.ClientAccount.DateDeedOfSale.Value <= cutoffDate)
                .Include(m => m.PaymentGroup)
                .Include(m => m.ClientAccount)
                .ToList();

            if (units.Count == 0) return response;

            var unitIds = units.Select(u => u.Id).ToList();

            // 2) Payment groups per unit (direct en via UnitConstructionValue)
            var groupMap = units
                .ToDictionary(u => u.Id, u => new HashSet<int>());

            foreach (var unit in units)
            {
                if (unit.PaymentGroupId.HasValue)
                    groupMap[unit.Id].Add(unit.PaymentGroupId.Value);
            }

            var cvGroups = _uow.UnitConstructionValues.GetNoTracking()
                .Where(cv => cv.PaymentGroupId != null && unitIds.Contains(cv.UnitId))
                .Select(cv => new { cv.UnitId, GroupId = cv.PaymentGroupId!.Value })
                .ToList();

            foreach (var cv in cvGroups)
                groupMap[cv.UnitId].Add(cv.GroupId);

            // 3) Stages ophalen voor alle relevante groepen
            var groupIds = groupMap.Values.SelectMany(g => g).Distinct().ToList();
            if (groupIds.Count == 0) return response;

            var stages = _uow.PaymentStages.GetNoTracking()
                .Include(s => s.Group)
                .Include(s => s.InvoicesDetails)
                .Where(s => s.Invoicable == true && groupIds.Contains(s.GroupId))
                .ToList();

            if (stages.Count == 0) return response;

            var stageIds = stages.Select(s => s.Id).Distinct().ToList();

            // 4) Gefactureerde combinaties (StageId × UnitId) uitsluiten
            var invoicedPairs = _uow.InvoiceDetails.GetNoTracking()
                .Where(d => d.PaymentStageId != null && stageIds.Contains(d.PaymentStageId.Value)
                            && d.UnitId != null && unitIds.Contains(d.UnitId.Value))
                .Select(d => new { d.PaymentStageId, d.UnitId })
                .ToList() // tuple literals can’t be translated in EF expressions
                .Select(d => (StageId: d.PaymentStageId!.Value, UnitId: d.UnitId!.Value))
                .Distinct()
                .ToHashSet();

            // 5) Units + bijhorende stages materialiseren
            foreach (var unit in units)
            {
                if (!groupMap.TryGetValue(unit.Id, out var unitGroupIds) || unitGroupIds.Count == 0)
                    continue;

                var unitStages = stages
                    .Where(s => unitGroupIds.Contains(s.GroupId) && !invoicedPairs.Contains((s.Id, unit.Id)))
                    .ToList();

                if (unitStages.Count == 0) continue;

                var bo = new UnitWithStagesBO();
                var unitBo = new UnitBO();

                foreach (var stage in unitStages)
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
                    m.Invoiceable &&
                    m.DateAgreement != null &&
                    m.ChangeOrderDetail.Any(i => i.Invoicable != false && i.Invoiced != true) &&
                    m.ClientAccountId > 0 &&
                    m.ChangeOrderDetail.Count(i => i.Invoiced == true) < m.ChangeOrderDetail.Count())
                .Include(m => m.ClientAccount )
                .Include(m => m.ContractActivity)
                .ThenInclude (m => m.Contract)
                .ThenInclude(m => m.Company)
                .Include(m => m.ChangeOrderDetail);
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
                .Where(m => m.ContractActivity.Contract.ProjectId == projectid)
                .Include(m => m.ClientAccount)
                .Include(m => m.ContractActivity)
                    .ThenInclude(m => m.Contract)
                    .ThenInclude(m => m.Company)
                .Include(m => m.ChangeOrderDetail);

            foreach (var e in entities)
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
        public GetResponse<IncommingInvoiceBO> GetProjectIncommingInvoicesByCompany(int projectid, int companyid)
        {
            var response = new GetResponse<IncommingInvoiceBO>();

            var entities = _uow.IncommingInvoices.GetNoTracking()
                .Where(m => m.ProjectId == projectid &&
                    ((m.CompanyId.HasValue && m.CompanyId.Value == companyid) ||
                     (m.ContractId.HasValue && m.Contract.CompanyId == companyid)))
                .Include(m => m.Company)
                .Include(m => m.Project)
                .Include(m => m.Contract)
                    .ThenInclude(c => c.Company);

            foreach (var entity in entities)
            {
                var bo = new IncommingInvoiceBO();
                var err = IncommingInvoiceTranslator.TranslateEntityToBO(entity, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }

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

    public class ProjectSupplierLookupService : IProjectSupplierLookupService
    {
        private readonly cpmRunningContext _db;
        public ProjectSupplierLookupService(cpmRunningContext db) => _db = db;

        // ---------- helpers ----------
        private static string ToLike(string? s)
        {
            s ??= string.Empty;
            // escape % en _
            return $"%{s.Replace("%", "[%]").Replace("_", "[_]")}%";
        }

        // ---------- Projects ----------
        public async Task<IReadOnlyList<(int Id, string Name)>> SearchProjectsAsync(
            string? term, int? clientId = null, int take = 20, CancellationToken ct = default)
        {
            var like = ToLike(term);

            var q = _db.Project.AsNoTracking().AsQueryable();                     // dbo.Project
            if (!string.IsNullOrWhiteSpace(term))
                q = q.Where(p => EF.Functions.Like(p.ProjectName, like));

            if (clientId.HasValue && clientId > 0)
            {
                // filter via Units (indien relevant): toon projecten waar de klant units heeft
                q =
                (from p in q
                 join u in _db.Units.AsNoTracking() on p.ProjectId equals u.ProjectId
                 where u.ClientAccountId == clientId.Value
                 select p).Distinct();
            }

            return await q.OrderBy(p => p.ProjectName)
                          .Take(take)
                          .Select(p => new ValueTuple<int, string>(p.ProjectId, p.ProjectName))
                          .ToListAsync(ct);
        }

        // ---------- Supplier contracts (text = ProjectName) ----------
        public async Task<IReadOnlyList<(int Id, string Name)>> SearchSupplierContractsAsync(
            string? term, int? supplierCompanyId = null, int take = 20, CancellationToken ct = default)
        {
            var like = ToLike(term);

            var q =
                from c in _db.Contract.AsNoTracking()                             // dbo.Contract
                join p in _db.Project.AsNoTracking() on c.ProjectId equals p.ProjectId
                select new { c.Id, c.CompanyId, p.ProjectName };

            if (supplierCompanyId.HasValue && supplierCompanyId > 0)
                q = q.Where(x => x.CompanyId == supplierCompanyId.Value);

            if (!string.IsNullOrWhiteSpace(term))
                q = q.Where(x => EF.Functions.Like(x.ProjectName, like));

            return await q.OrderBy(x => x.ProjectName)
                          .Take(take)
                          .Select(x => new ValueTuple<int, string>(x.Id, x.ProjectName))
                          .ToListAsync(ct);
        }

        // ---------- Stage groups for client (via Units -> PaymentGroupId) ----------
        public async Task<IReadOnlyList<(int Id, string Name)>> SearchStageGroupsForClientAsync(
            int clientId, string? term, int take = 20, CancellationToken ct = default)
        {
            if (clientId <= 0) return Array.Empty<(int, string)>();

            // ✅ deed-guard
            var hasDeed = await _db.ClientAccount
                .AsNoTracking()
                .AnyAsync(c => c.Id == clientId && c.DateDeedOfSale != null, ct);
            if (!hasDeed) return Array.Empty<(int, string)>();

            var like = ToLike(term);

            var direct = from g in _db.InvoicingPaymentGroup.AsNoTracking()
                         join u in _db.Units.AsNoTracking() on g.Id equals u.PaymentGroupId
                         where u.ClientAccountId == clientId
                         select new { g.Id, g.Name };

            var viaCv = from g in _db.InvoicingPaymentGroup.AsNoTracking()
                        join u in _db.Units.AsNoTracking() on g.Id equals u.PaymentGroupId into _ignore // niet nodig
                        from _ in _ignore.DefaultIfEmpty()
                        join u2 in _db.Units.AsNoTracking() on 1 equals 1 // dummy
                        join cv in _db.UnitConstructionValue.AsNoTracking() on u2.Id equals cv.UnitId
                        where u2.ClientAccountId == clientId && cv.PaymentGroupId == g.Id
                        select new { g.Id, g.Name };

            var q = direct.Union(viaCv).Distinct();

            if (!string.IsNullOrWhiteSpace(term))
                q = q.Where(x => EF.Functions.Like(x.Name, like));

            return await q.OrderBy(x => x.Name)
                          .Take(take)
                          .Select(x => new ValueTuple<int, string>(x.Id, x.Name))
                          .ToListAsync(ct);
        }


        // ---------- Stages in group (only invocable) ----------
        public async Task<IReadOnlyList<(int Id, string Name)>> SearchStagesAsync(
            int groupId, string? term, int take = 50, CancellationToken ct = default)
        {
            if (groupId <= 0) return Array.Empty<(int, string)>();
            var like = ToLike(term);

            var q = _db.InvoicingPaymentStages.AsNoTracking()
                      .Where(s => s.GroupId == groupId && s.Invoicable);

            if (!string.IsNullOrWhiteSpace(term))
                q = q.Where(s => EF.Functions.Like(s.Name, like));

            return await q.OrderBy(s => s.Id)
                          .Take(take)
                          .Select(s => new ValueTuple<int, string>(s.Id, s.Name))
                          .ToListAsync(ct);
        }

        // ---------- Validation: are all stages valid for this client? ----------
        public async Task<bool> AreStagesValidForClientAsync(
            int clientId, IEnumerable<int> stageIds, CancellationToken ct = default)
        {
            var ids = stageIds?.Distinct().ToArray() ?? Array.Empty<int>();
            if (clientId <= 0 || ids.Length == 0) return false;

            // ✅ deed-guard
            var hasDeed = await _db.ClientAccount
                .AsNoTracking()
                .AnyAsync(c => c.Id == clientId && c.DateDeedOfSale != null, ct);
            if (!hasDeed) return false;

            var validIds = await (
                from s in _db.InvoicingPaymentStages.AsNoTracking()
                join g in _db.InvoicingPaymentGroup.AsNoTracking() on s.GroupId equals g.Id
                where s.Invoicable && ids.Contains(s.Id) &&
                      (
                        _db.Units.Any(u => u.ClientAccountId == clientId && u.PaymentGroupId == g.Id) // direct
                        ||
                        (from u in _db.Units.AsNoTracking()
                         join cv in _db.UnitConstructionValue.AsNoTracking() on u.Id equals cv.UnitId
                         where u.ClientAccountId == clientId && cv.PaymentGroupId == g.Id
                         select 1).Any() // indirect
                      )
                select s.Id
            ).Distinct().ToListAsync(ct);

            return validIds.Count == ids.Length;
        }


        public async Task<IReadOnlyList<UnitStageRow>> GetUnitsWithInvocableStagesForClientAsync(
            int clientId,
            bool includeZeroOrNegative = false,           // ← zet op true om ook credit-saldi te tonen
            int? invoiceId = null,
            CancellationToken ct = default)
        {
            if (clientId <= 0) return Array.Empty<UnitStageRow>();

            // Alleen klanten met een akte-datum in het verleden
            var today = DateOnly.FromDateTime(DateTime.Today);
            var deedCutoff = today.AddDays(1);

            var hasDeed = await _db.ClientAccount
                .AsNoTracking()
                .AnyAsync(c =>
                    c.Id == clientId &&
                    c.DateDeedOfSale != null &&
                   c.DateDeedOfSale <= deedCutoff, ct);
            if (!hasDeed) return Array.Empty<UnitStageRow>();

            // 1) Stages ophalen (DIRECT via Unit.PaymentGroupId en INDIRECT via UnitConstructionValue.PaymentGroupId)
            var direct = await (
                from u in _db.Units.AsNoTracking()
                where u.ClientAccountId == clientId && u.PaymentGroupId != null
                join g in _db.InvoicingPaymentGroup.AsNoTracking() on u.PaymentGroupId equals g.Id
                join s in _db.InvoicingPaymentStages.AsNoTracking() on g.Id equals s.GroupId
                where s.Invoicable
                select new { u.Id, u.Name, GroupId = g.Id, GroupName = g.Name, StageId = s.Id, StageName = s.Name, StagePercentage = (decimal?)s.Percentage }
            ).ToListAsync(ct);

            var viaCv = await (
                from u in _db.Units.AsNoTracking()
                where u.ClientAccountId == clientId
                join cv in _db.UnitConstructionValue.AsNoTracking() on u.Id equals cv.UnitId
                join g in _db.InvoicingPaymentGroup.AsNoTracking() on cv.PaymentGroupId equals g.Id
                join s in _db.InvoicingPaymentStages.AsNoTracking() on g.Id equals s.GroupId
                where s.Invoicable
                select new { u.Id, u.Name, GroupId = g.Id, GroupName = g.Name, StageId = s.Id, StageName = s.Name, StagePercentage = (decimal?)s.Percentage }
            ).ToListAsync(ct);

            var stages = direct.Concat(viaCv)
                .GroupBy(x => new { x.Id, x.GroupId, x.StageId })
                .Select(g => g.First())
                .ToList();

            if (stages.Count == 0) return Array.Empty<UnitStageRow>();

            var unitIds  = stages.Select(s => s.Id).Distinct().ToList();
            var groupIds = stages.Select(s => s.GroupId).Distinct().ToList();
            var stageIds = stages.Select(s => s.StageId).Distinct().ToList();

            // 1b) Uitsluiten van stages die al een factuurlijn hebben (zelfde check als ad-hoc SQL)
            var invoicedStageUnits = await (
                from d in _db.InvoicesDetails.AsNoTracking()
                where d.PaymentStageId != null && stageIds.Contains(d.PaymentStageId.Value)
                    && d.UnitId != null && unitIds.Contains(d.UnitId.Value)
                       && (!invoiceId.HasValue || d.InvoiceId != invoiceId.Value)
                select new { StageId = d.PaymentStageId!.Value, UnitId = d.UnitId!.Value }
            ).Distinct().ToListAsync(ct);
            var invoicedStageUnitSet = invoicedStageUnits
                .Select(x => (x.StageId, x.UnitId))
                .ToHashSet();

            // 2) Basis per (Unit,Group): som ValueSold
            var baseRows = await (
                from cv in _db.UnitConstructionValue.AsNoTracking()
                where unitIds.Contains(cv.UnitId) && cv.PaymentGroupId != null && groupIds.Contains(cv.PaymentGroupId.Value)
                group cv by new { cv.UnitId, cv.PaymentGroupId } into g
                select new
                {
                    UnitId = g.Key.UnitId,
                    GroupId = g.Key.PaymentGroupId!.Value,
                    BaseAmount = g.Sum(x => (decimal)(x.ValueSold ?? 0m))
                }
            ).ToListAsync(ct);
            var baseLookup = baseRows.ToDictionary(k => (k.UnitId, k.GroupId), v => v.BaseAmount);

            // Totale bouwwaarde per unit
            var unitTotals = await (
                from cv in _db.UnitConstructionValue.AsNoTracking()
                where unitIds.Contains(cv.UnitId)
                group cv by cv.UnitId into g
                select new { UnitId = g.Key, Total = g.Sum(x => (decimal)(x.ValueSold ?? 0m)) }
            ).ToDictionaryAsync(x => x.UnitId, x => x.Total, ct);

            // 3) Alle CV’s per (Unit,Group) — nodig om expected per (Stage×CV) te berekenen
            var cvRows = await (
                from cv in _db.UnitConstructionValue.AsNoTracking()
                where unitIds.Contains(cv.UnitId) && cv.PaymentGroupId != null && groupIds.Contains(cv.PaymentGroupId.Value)
                select new { cv.Id, cv.UnitId, GroupId = cv.PaymentGroupId!.Value, Amount = (decimal)(cv.ValueSold ?? 0m) }
            ).ToListAsync(ct);

            var cvByUnitGroup = cvRows
                .GroupBy(x => (x.UnitId, x.GroupId))
                .ToDictionary(g => g.Key, g => g.ToList());

            var cvIds = cvRows.Select(x => x.Id).Distinct().ToList();

            // 4) Reeds gefactureerd per (StageId, CV.Id)
            var invoicedPairs = await (
                from d in _db.InvoicesDetails.AsNoTracking()
                where d.PaymentStageId != null && stageIds.Contains(d.PaymentStageId.Value)
                    && d.ConstructionValueId != null && cvIds.Contains(d.ConstructionValueId.Value)
                       && (!invoiceId.HasValue || d.InvoiceId != invoiceId.Value)
                group d by new { StageId = d.PaymentStageId!.Value, CvId = d.ConstructionValueId!.Value } into g
                select new { g.Key.StageId, g.Key.CvId, Invoiced = g.Sum(x => (decimal)(x.Price ?? 0m)) }
            ).ToListAsync(ct);
            var invoicedLookup = invoicedPairs.ToDictionary(k => (k.StageId, k.CvId), v => v.Invoiced);

            // 5) Unit/Project meta (voor headerteksten)
            var unitMetaRows = await (
                from u in _db.Units.AsNoTracking()
                where unitIds.Contains(u.Id)
                join p in _db.Project.AsNoTracking() on u.ProjectId equals p.ProjectId
                select new
                {
                    u.Id,
                    UnitType = u.Type.Name,
                    UnitStreet = u.Street,
                    UnitHouse  = u.Housenumber,
                    ProjectName  = p.ProjectName,
                    ProjectStreet= p.Street,
                    ProjectCity  = p.PostalCode.Gemeente
                }
            ).ToListAsync(ct);
            var unitMeta = unitMetaRows.ToDictionary(x => x.Id, x => x);

            // 6) Remaining per stage (som over CV’s van (expected - invoiced))
            const decimal tol = 0.01m; // 1 cent tolerantie
            var result = new List<UnitStageRow>();

            foreach (var st in stages)
            {
                if (invoicedStageUnitSet.Contains((st.StageId, st.Id)))
                    continue;
                var pct = st.StagePercentage ?? 0m;
                if (pct == 0m) continue;

                baseLookup.TryGetValue((st.Id, st.GroupId), out var baseAmount);
                if (!cvByUnitGroup.TryGetValue((st.Id, st.GroupId), out var cvs) || cvs.Count == 0)
                    continue;

                decimal remainingForStage = 0m;

                foreach (var cv in cvs)
                {
                    var expected = Math.Round(cv.Amount * (pct / 100m), 2, MidpointRounding.AwayFromZero);
                    invoicedLookup.TryGetValue((st.StageId, cv.Id), out var already);
                    var rem = expected - already;      // kan positief (factureren) of negatief (credit) zijn
                    remainingForStage += rem;
                }

                // Filter: standaard enkel “te factureren” > tol; met includeZeroOrNegative tonen we ook 0/negatief
                if (!includeZeroOrNegative)
                {
                    if (remainingForStage <= tol) continue;
                }
                else
                {
                    if (Math.Abs(remainingForStage) <= tol) continue; // exact nul → verstoppen
                }

                unitMeta.TryGetValue(st.Id, out var meta);
                unitTotals.TryGetValue(st.Id, out var unitTotal);

                result.Add(new UnitStageRow(
                    UnitId: st.Id,
                    UnitName: st.Name ?? string.Empty,
                    GroupId: st.GroupId,
                    GroupName: st.GroupName ?? string.Empty,
                    StageId: st.StageId,
                    StageName: st.StageName ?? string.Empty,
                    StagePercentage: pct,
                    BaseAmount: baseAmount,                                               // info
                    CalculatedAmount: Math.Round(remainingForStage, 2, MidpointRounding.AwayFromZero), // + = factuur, − = credit
                    Invoicable: true,                                                     // door filter hierboven
                    UnitType: meta?.UnitType,
                    ProjectName: meta?.ProjectName,
                    ProjectStreet: meta?.ProjectStreet,
                    ProjectHouseNumber: null,                                             // project heeft geen huisnr.
                    ProjectCity: meta?.ProjectCity,
                    UnitStreet: meta?.UnitStreet,
                    UnitHouseNumber: meta?.UnitHouse,
                    UnitConstructionTotal: unitTotal
                ));
            }

            return result
                .OrderBy(r => r.UnitName)
                .ThenBy(r => r.GroupName)
                .ThenBy(r => r.StageId)
                .ToList();
        }

        Task<IReadOnlyList<UnitStageRow>> IProjectSupplierLookupService.GetUnitsWithInvocableStagesForClientAsync(
    int clientId,
    bool includeZeroOrNegative,
    int? invoiceId,
    CancellationToken ct)
    => GetUnitsWithInvocableStagesForClientAsync(clientId, includeZeroOrNegative, invoiceId, ct);


        /// <summary>
        /// Geeft goedgekeurde wijzigingsopdrachten (ApprovedOn != null) terug voor de projecten/units
        /// die gekoppeld zijn aan de opgegeven klant. Filtert optioneel op projectId.
        /// </summary>
        public async Task<IReadOnlyList<ChangeOrderRow>> GetApprovedChangeOrdersForClientAsync(
            int clientOrCoOwnerId, int? projectId, CancellationToken ct = default)
        {
            if (clientOrCoOwnerId <= 0) return Array.Empty<ChangeOrderRow>();

            // 1) Resolve hoofdklant (clientaccount óf co-owner → hoofdklant)
            var mainClientId = await _db.ClientAccount
                .AsNoTracking()
                .Where(c => c.Id == clientOrCoOwnerId)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync(ct);

            if (!mainClientId.HasValue)
            {
                mainClientId = await _db.ClientContacts
                    .AsNoTracking()
                    .Where(cc => cc.Id == clientOrCoOwnerId && cc.IsCoOwner && cc.ClientAccountId != null)
                    .Select(cc => (int?)cc.ClientAccountId!)
                    .FirstOrDefaultAsync(ct);
            }

            if (!mainClientId.HasValue) return Array.Empty<ChangeOrderRow>();
            var clientId = mainClientId.Value;

            // 2) Basisquery CO + detail
            var q =
                from co in _db.ChangeOrder.AsNoTracking()
                where co.ClientAccountId == clientId
                   && co.Invoiceable == true
                   && co.DateAgreement != null
                join d in _db.ChangeOrderDetail.AsNoTracking()
                    on co.Id equals d.ChangeOrderId
                where d.Invoicable != false                        // true of NULL
                   && (d.Invoiced == null || d.Invoiced == false)  // nog niet (volledig) gefactureerd
                                                                   // LEFT JOIN ContractActivity → Contract → Project
                join ca0 in _db.ContractActivity.AsNoTracking()
                    on co.ContractActivityId equals ca0.Id into caGrp
                from ca in caGrp.DefaultIfEmpty()
                join c0 in _db.Contract.AsNoTracking()
                    on ca.ContractId equals c0.Id into cGrp
                from c in cGrp.DefaultIfEmpty()
                join p0 in _db.Project.AsNoTracking()
                    on c.ProjectId equals p0.ProjectId into pGrp
                from p in pGrp.DefaultIfEmpty()
                select new
                {
                    co.Id,
                    co.ClientAccountId,
                    CODescription = co.Description,
                    DetailId = d.Id,
                    DetailDescription = d.Description,
                    Price = d.Price,
                    Commission = d.Commission,
                    // NEGATIEF TOEGESTAAN: creditnota's
                    BaseAmountExcl = (d.Invoiced == true ? 0m : ((d.Price * d.Number) + (d.Price * d.Number * d.Commission / 100))),
                    ProjectId = (int?)c.ProjectId,
                    ProjectName = p != null ? p.ProjectName : null,
                    ProjectStreet = p != null ? p.Street : null,
                    ProjectCity = p != null ? p.PostalCode.Gemeente : null,
                    Date = co.Date,
                    DetailNumber = d.Number,
                    DetailUnitPrice = (d.Price) + (d.Price * d.Commission / 100)
                };



            // 6) Optionele filter op ProjectId
            if (projectId.HasValue)
                q = q.Where(x => x.ProjectId == projectId.Value);


            var rowsRaw = await q.ToListAsync(ct);

            // 7) Unitcontext (één unit van deze hoofdklant per project als hint in de UI/header)
            var unitCtx = await _db.Units.AsNoTracking()
                .Where(u => u.ClientAccountId == clientId)
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    UnitTypeName = u.Type != null ? u.Type.Name : null,  // nav → string
                    u.ProjectId
                })
                .ToListAsync(ct);

            var unitByProject = unitCtx
                .GroupBy(u => u.ProjectId)
                .ToDictionary(g => g.Key, g => g.First()); // pick eerste unit per project

            // 8) Materialiseren + bedragen
            var result = rowsRaw.Select(x => new ChangeOrderRow(
                ChangeOrderId: x.Id,
                ChangeOrderDetailId: x.DetailId,
                ClientAccountId: x.ClientAccountId,
                Title: string.IsNullOrWhiteSpace(x.DetailDescription) ? x.CODescription : $"{x.CODescription} – {x.DetailDescription}",
                BaseAmountExcl: x.BaseAmountExcl,   // kan negatief zijn
                VatPercentage: 21m,                 // pas aan indien nodig
                UnitId: null, UnitName: null, UnitType: null,
                ProjectName: x.ProjectName,
                ProjectStreet: x.ProjectStreet,
                ProjectHouseNumber: null,
                ProjectCity: x.ProjectCity,
                ChangeOrderDescription: x.CODescription,
                Date : x.Date,
                Number : x.DetailNumber,
                UnitPrice : x.DetailUnitPrice
                

            )).ToList();

            return result;
        }

        // rows = geselecteerde CO-lijnen met: ChangeOrderDetailId, StagePercentage (0..100), en client-side berekende Price
        public async Task<(bool ok, string? error)> ValidateChangeOrderSelectionAsync(
            int clientId,
            IEnumerable<(int detailId, decimal percent, decimal postedPrice)> rows,
            CancellationToken ct)
        {
            var ids = rows.Select(r => r.detailId).ToList();
            if (ids.Count == 0) return (false, "Geen wijzigingsopdrachten geselecteerd.");

            var map = await _db.ChangeOrderDetail.AsNoTracking()
                .Where(d => ids.Contains(d.Id))
                .Select(d => new {
                    d.Id,
                    Invoiced = d.Invoiced == true,
                    BaseAmountExcl = (d.Invoiced == true ? 0m : ((d.Price) + (d.Commission)))
                })
                .ToDictionaryAsync(x => x.Id, x => x, ct);

            foreach (var r in rows)
            {
                if (!map.TryGetValue(r.detailId, out var src))
                    return (false, "Onbekende wijzigingsopdracht.");

                if (src.Invoiced || src.BaseAmountExcl == 0m)
                    return (false, "Wijzigingsopdracht is al volledig gefactureerd of heeft geen saldo.");

                // herbereken server-side
                var cappedPct = Math.Min(100m, Math.Max(0m, r.percent));
                var expected = Math.Round(src.BaseAmountExcl * (cappedPct / 100m), 2, MidpointRounding.AwayFromZero);

                // Teken moet overeenkomen (credit blijft credit)
                if (Math.Sign(expected) != Math.Sign(r.postedPrice) && expected != 0m)
                    return (false, "Teken van het bedrag klopt niet met het type (factuur/credit).");

                // Niet meer dan basis in absolute waarde
                if (Math.Abs(r.postedPrice) - Math.Abs(expected) > 0.01m)
                    return (false, "Gevraagd bedrag overschrijdt het resterende saldo.");
            }

            return (true, null);
        }

        public async Task<bool> HasDeedOfSaleAsync(int clientId, CancellationToken ct = default)
        {
            if (clientId <= 0) return false;
            return await _db.ClientAccount
                .AsNoTracking()
                .AnyAsync(c => c.Id == clientId && c.DateDeedOfSale != null, ct);
        }
    }

}

using System;
using System.Linq;
using BOCore;
using BOCore.Budget;
using DALCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Budget
{
    public class BudgetPrijsReferentieService : IBudgetPrijsReferentieService
    {
        public const string TypeBouw  = "Bouw";
        public const string TypeGrond = "Grond";

        private readonly UnitOfWorkCore _uow;
        public BudgetPrijsReferentieService(UnitOfWorkCore uow) => _uow = uow;

        public GetResponse<BudgetPrijsReferentieBO> GetAlle()
        {
            var r = new GetResponse<BudgetPrijsReferentieBO>();
            var list = _uow.BudgetPrijsReferentie.GetNoTracking().Include(p => p.Project)
                .OrderBy(p => p.PrijsType).ThenBy(p => p.ProjectId.HasValue).ThenBy(p => p.Project.ProjectName).ThenBy(p => p.Code).ToList();
            foreach (var e in list) r.AddValue(Map(e));
            return r;
        }

        public GetResponse<BudgetPrijsReferentieBO> GetVoorProject(int projectId)
        {
            var r = new GetResponse<BudgetPrijsReferentieBO>();
            var list = _uow.BudgetPrijsReferentie.GetNoTracking().Include(p => p.Project)
                .Where(p => p.ProjectId == null || p.ProjectId == projectId)
                .OrderBy(p => p.PrijsType).ThenBy(p => p.Code).ThenBy(p => p.ProjectId.HasValue).ToList();
            foreach (var e in list) r.AddValue(Map(e));
            return r;
        }

        public Response InsertUpdate(BudgetPrijsReferentieBO bo)
        {
            var response = new Response();
            var type = (bo.PrijsType ?? "").Trim();
            if (type != TypeBouw && type != TypeGrond) { response.AddError("Type moet Bouw of Grond zijn."); return response; }
            if (bo.Code <= 0) { response.AddError("Code moet groter dan 0 zijn."); return response; }
            if (bo.PrijsPerM2 <= 0m) { response.AddError("Prijs per m² moet groter dan 0 zijn."); return response; }

            var bestaat = _uow.BudgetPrijsReferentie.GetNoTracking()
                .Any(p => p.Id != bo.Id && p.PrijsType == type && p.Code == bo.Code && p.ProjectId == bo.ProjectId);
            if (bestaat) { response.AddError($"Code {bo.Code} ({type}) bestaat al" + (bo.ProjectId.HasValue ? " voor dit project." : " als algemene code.")); return response; }

            BudgetPrijsReferentie e;
            if (bo.Id > 0)
            {
                e = _uow.BudgetPrijsReferentie.GetNormal().FirstOrDefault(p => p.Id == bo.Id);
                if (e is null) { response.AddError("Prijsreferentie niet gevonden."); return response; }
            }
            else
            {
                e = new BudgetPrijsReferentie();
                _uow.BudgetPrijsReferentie.Add(e);
            }

            e.ProjectId    = bo.ProjectId;
            e.PrijsType    = type;
            e.Code         = bo.Code;
            e.PrijsPerM2   = Math.Round(bo.PrijsPerM2, 2);
            e.Omschrijving = Trunc(bo.Omschrijving, 200);
            e.Datum        = bo.Datum?.Date;
            e.Bron         = Trunc(bo.Bron, 200);

            _uow.SaveChanges();
            response.InsertedId = e.Id;
            response.AddSuccess("Prijsreferentie opgeslagen.");
            return response;
        }

        public Response Delete(int id)
        {
            var response = new Response();
            var e = _uow.BudgetPrijsReferentie.GetNormal().FirstOrDefault(p => p.Id == id);
            if (e is null) { response.AddError("Prijsreferentie niet gevonden."); return response; }
            // Verkooplijnen verwijzen enkel via het codenummer (CodeBouw/CodeGrond): verwijderen breekt niets, de ingestelde €/m² blijft op de lijn staan.
            _uow.BudgetPrijsReferentie.Remove(e);
            _uow.SaveChanges();
            response.AddSuccess("Prijsreferentie verwijderd.");
            return response;
        }

        private static string Trunc(string s, int max)
        {
            s = s?.Trim();
            if (string.IsNullOrEmpty(s)) return null;
            return s.Length <= max ? s : s.Substring(0, max);
        }

        private static BudgetPrijsReferentieBO Map(BudgetPrijsReferentie e) => new()
        {
            Id           = e.Id,
            ProjectId    = e.ProjectId,
            ProjectNaam  = e.Project?.ProjectName,
            PrijsType    = e.PrijsType,
            Code         = e.Code,
            PrijsPerM2   = e.PrijsPerM2,
            Omschrijving = e.Omschrijving,
            Datum        = e.Datum,
            Bron         = e.Bron
        };
    }
}

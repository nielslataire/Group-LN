using System;
using System.IO;
using System.Linq;
using BOCore;
using BOCore.Budget;
using ClosedXML.Excel;
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
                .OrderBy(p => p.PrijsType).ThenByDescending(p => p.Datum).ThenBy(p => p.Code).ToList();
            var inGebruikBouw  = _uow.BudgetVerkoopLijn.GetNoTracking().Where(l => l.CodeBouw != null).Select(l => l.CodeBouw.Value).Distinct().ToHashSet();
            var inGebruikGrond = _uow.BudgetVerkoopLijn.GetNoTracking().Where(l => l.CodeGrond != null).Select(l => l.CodeGrond.Value).Distinct().ToHashSet();
            foreach (var e in list)
            {
                var bo = Map(e);
                bo.IsInGebruik = (e.PrijsType == TypeGrond ? inGebruikGrond : inGebruikBouw).Contains(e.Code);
                r.AddValue(bo);
            }
            return r;
        }

        public GetResponse<BudgetPrijsReferentieBO> GetVoorProject(int projectId, bool metGearchiveerd = false)
        {
            var r = new GetResponse<BudgetPrijsReferentieBO>();
            var list = _uow.BudgetPrijsReferentie.GetNoTracking().Include(p => p.Project)
                .Where(p => (p.ProjectId == null || p.ProjectId == projectId) && (metGearchiveerd || !p.Gearchiveerd))
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
            if (IsInGebruik(e))
            {
                // 38a: "Codes die in een budget gebruikt zijn, kan je niet verwijderen — wel archiveren."
                response.AddError($"Code {Label(e)} wordt in een budget gebruikt en kan niet verwijderd worden. Archiveer ze in de plaats.");
                return response;
            }
            _uow.BudgetPrijsReferentie.Remove(e);
            _uow.SaveChanges();
            response.AddSuccess("Prijsreferentie verwijderd.");
            return response;
        }

        public Response SetGearchiveerd(int id, bool gearchiveerd)
        {
            var response = new Response();
            var e = _uow.BudgetPrijsReferentie.GetNormal().FirstOrDefault(p => p.Id == id);
            if (e is null) { response.AddError("Prijsreferentie niet gevonden."); return response; }
            e.Gearchiveerd = gearchiveerd;
            _uow.SaveChanges();
            response.AddSuccess(gearchiveerd ? $"Code {Label(e)} gearchiveerd: niet meer kiesbaar op stap 8, bestaande budgetten blijven ongewijzigd." : $"Code {Label(e)} hersteld.");
            return response;
        }

        public int VolgendeCode(string prijsType)
        {
            var max = _uow.BudgetPrijsReferentie.GetNoTracking().Where(p => p.PrijsType == prijsType).Select(p => (int?)p.Code).Max();
            return (max ?? 0) + 1;
        }

        public byte[] ExportXlsx(string prijsType)
        {
            var rows = GetAlle().Values.Where(p => string.Equals(p.PrijsType, prijsType, StringComparison.OrdinalIgnoreCase)).OrderBy(p => p.Code).ToList();
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add(prijsType == TypeGrond ? "Grondprijscodes" : "Bouwprijscodes");
            var kop = new[] { "Code", "Omschrijving", "Prijs per m²", "Datum", "Bron", "Project", "Gearchiveerd", "In gebruik" };
            for (int c = 0; c < kop.Length; c++) ws.Cell(1, c + 1).Value = kop[c];
            ws.Range(1, 1, 1, kop.Length).Style.Font.Bold = true;
            int r = 2;
            foreach (var p in rows)
            {
                ws.Cell(r, 1).Value = p.CodeLabel;
                ws.Cell(r, 2).Value = p.Omschrijving;
                ws.Cell(r, 3).Value = p.PrijsPerM2; ws.Cell(r, 3).Style.NumberFormat.Format = "#,##0.00";
                if (p.Datum.HasValue) { ws.Cell(r, 4).Value = p.Datum.Value; ws.Cell(r, 4).Style.DateFormat.Format = "dd/MM/yyyy"; }
                ws.Cell(r, 5).Value = p.Bron;
                ws.Cell(r, 6).Value = p.ProjectNaam ?? "Algemeen";
                ws.Cell(r, 7).Value = p.Gearchiveerd ? "ja" : "";
                ws.Cell(r, 8).Value = p.IsInGebruik ? "ja" : "";
                r++;
            }
            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        private bool IsInGebruik(BudgetPrijsReferentie e) => e.PrijsType == TypeGrond
            ? _uow.BudgetVerkoopLijn.GetNoTracking().Any(l => l.CodeGrond == e.Code)
            : _uow.BudgetVerkoopLijn.GetNoTracking().Any(l => l.CodeBouw == e.Code);

        private static string Label(BudgetPrijsReferentie e) => (e.PrijsType == TypeGrond ? "G" : "B") + "-" + e.Code.ToString("00");

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
            Bron         = e.Bron,
            Gearchiveerd = e.Gearchiveerd
        };
    }
}

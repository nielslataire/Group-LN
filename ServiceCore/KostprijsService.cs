using BOCore;
using DALCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
namespace ServiceCore;
public class KostprijsService : IKostprijsService {
    private readonly UnitOfWorkCore _uow;
    public KostprijsService(UnitOfWorkCore uow) => _uow = uow;
    public GetResponse<KmIndexTypeBO> GetIndexTypes() {
        var r = new GetResponse<KmIndexTypeBO>();
        foreach (var e in _uow.KmIndexTypes.GetNoTracking().OrderBy(x => x.Naam))
            r.AddValue(new KmIndexTypeBO { Id = e.Id, Code = e.Code, Naam = e.Naam });
        return r;
    }
    public GetResponse<ActivityGroupBO> GetActivityGroepen() {
        var r = new GetResponse<ActivityGroupBO>();
        foreach (var e in _uow.ActivityGroups.GetNoTracking().OrderBy(x => x.Lot).ThenBy(x => x.Name))
            r.AddValue(new ActivityGroupBO { ID = e.GroupId, Name = e.Name, Lot = (int)(e.Lot ?? 0) });
        return r;
    }
    public GetResponse<KostprijsMateriaalBO> GetMaterialen() {
        var r = new GetResponse<KostprijsMateriaalBO>();
        var list = _uow.KostprijsMaterialen.GetNoTracking()
            .Include(m => m.Categorie).Include(m => m.IndexType)
            .OrderBy(m => m.Categorie.Lot).ThenBy(m => m.Volgorde).ThenBy(m => m.Naam).ToList();
        foreach (var e in list)
            r.AddValue(new KostprijsMateriaalBO {
                Id = e.Id, CategorieId = e.CategorieId, CategorieNaam = e.Categorie?.Name,
                LotNummer = (int?)e.Categorie?.Lot, Naam = e.Naam, Eenheid = e.Eenheid,
                Toepassingsgebied = e.Toepassingsgebied,
                KmIndexTypeId = e.KmIndexTypeId, IndexTypeCode = e.IndexType?.Code,
                IndexTypeNaam = e.IndexType?.Naam, ReferentiePrijs = e.ReferentiePrijs,
                ReferentieDatum = e.ReferentieDatum, Volgorde = e.Volgorde
            });
        return r;
    }
    public Response InsertUpdateMateriaal(KostprijsMateriaalBO bo) {
        var r = new Response();
        if (bo.Id == 0) {
            _uow.KostprijsMaterialen.Add(new KostprijsMateriaal {
                CategorieId = bo.CategorieId, Naam = bo.Naam, Eenheid = bo.Eenheid,
                Toepassingsgebied = bo.Toepassingsgebied,
                KmIndexTypeId = bo.KmIndexTypeId, ReferentiePrijs = bo.ReferentiePrijs,
                ReferentieDatum = bo.ReferentieDatum, Volgorde = bo.Volgorde
            });
            r.AddSuccess("Materiaal aangemaakt.");
        } else {
            var e = _uow.KostprijsMaterialen.GetById(bo.Id);
            if (e == null) { r.AddError("Niet gevonden."); return r; }
            e.CategorieId = bo.CategorieId; e.Naam = bo.Naam; e.Eenheid = bo.Eenheid;
            e.Toepassingsgebied = bo.Toepassingsgebied;
            e.KmIndexTypeId = bo.KmIndexTypeId; e.ReferentiePrijs = bo.ReferentiePrijs;
            e.ReferentieDatum = bo.ReferentieDatum; e.Volgorde = bo.Volgorde;
            _uow.KostprijsMaterialen.Update(e);
            r.AddSuccess("Materiaal opgeslagen.");
        }
        _uow.SaveChanges();
        return r;
    }
    public Response DeleteMateriaal(int id) {
        var r = new Response();
        var koppeling = _uow.FormulaKoppelingen.GetNoTracking()
            .FirstOrDefault(k => k.MateriaalId == id);
        if (koppeling != null) {
            r.AddError($"Dit materiaal is gekoppeld aan de formule \"{koppeling.Omschrijving}\" en kan niet worden verwijderd. Verwijder eerst de koppeling in Instellingen → Kostprijsmaterialen → Formule koppelingen.");
            return r;
        }
        _uow.KostprijsMaterialen.DeleteObject(id);
        _uow.SaveChanges();
        r.AddSuccess("Materiaal verwijderd.");
        return r;
    }
    public GetResponse<FormulaKoppelingBO> GetFormulaKoppelingen() {
        var r = new GetResponse<FormulaKoppelingBO>();
        var lijst = _uow.FormulaKoppelingen.GetNoTracking()
            .Include(k => k.Materiaal)
            .OrderBy(k => k.Sleutel)
            .ToList();
        foreach (var k in lijst)
            r.AddValue(new FormulaKoppelingBO {
                Id = k.Id, Sleutel = k.Sleutel, Omschrijving = k.Omschrijving,
                MateriaalId              = k.MateriaalId,
                MateriaalNaam            = k.Materiaal?.Naam,
                MateriaalReferentiePrijs = k.Materiaal?.ReferentiePrijs,
                MateriaalReferentieDatum = k.Materiaal?.ReferentieDatum,
                MateriaalEenheid         = k.Materiaal?.Eenheid
            });
        return r;
    }
    public Response SaveFormulaKoppeling(string sleutel, int? materiaalId) {
        var r = new Response();
        var entity = _uow.FormulaKoppelingen.GetNormal().FirstOrDefault(k => k.Sleutel == sleutel);
        if (entity == null) { r.AddError("Formule-sleutel niet gevonden."); return r; }
        entity.MateriaalId = materiaalId;
        _uow.FormulaKoppelingen.Update(entity);
        _uow.SaveChanges();
        r.AddSuccess("Koppeling opgeslagen.");
        return r;
    }
    public Response SnapshotVoorProject(int projectId) {
        var r = new Response();
        var bestaand = _uow.ProjectKostprijzen.GetNormal().Where(x => x.ProjectId == projectId).ToList();
        foreach (var e in bestaand) _uow.ProjectKostprijzen.Remove(e);
        var materialen = _uow.KostprijsMaterialen.GetNoTracking()
            .Include(m => m.Categorie).Include(m => m.IndexType).ToList();
        foreach (var m in materialen)
            _uow.ProjectKostprijzen.Add(new ProjectKostprijs {
                ProjectId = projectId, KostprijsMateriaalId = m.Id,
                CategorieNaam = m.Categorie?.Name ?? "", LotNummer = (int?)m.Categorie?.Lot,
                Naam = m.Naam, Eenheid = m.Eenheid, IndexTypeCode = m.IndexType?.Code ?? "",
                Prijs = m.ReferentiePrijs, ReferentieDatum = m.ReferentieDatum, SnapshotDatum = DateTime.UtcNow
            });
        _uow.SaveChanges();
        r.AddSuccess("Snapshot aangemaakt.");
        return r;
    }
    public GetResponse<ProjectKostprijsBO> GetProjectKostprijzen(int projectId) {
        var r = new GetResponse<ProjectKostprijsBO>();
        foreach (var e in _uow.ProjectKostprijzen.GetNoTracking().Where(x => x.ProjectId == projectId)
            .OrderBy(x => x.CategorieNaam).ThenBy(x => x.Naam))
            r.AddValue(new ProjectKostprijsBO {
                Id = e.Id, ProjectId = e.ProjectId, KostprijsMateriaalId = e.KostprijsMateriaalId,
                CategorieNaam = e.CategorieNaam, LotNummer = e.LotNummer, Naam = e.Naam,
                Eenheid = e.Eenheid, IndexTypeCode = e.IndexTypeCode, Prijs = e.Prijs,
                ReferentieDatum = e.ReferentieDatum, SnapshotDatum = e.SnapshotDatum
            });
        return r;
    }
    public GetResponse<ProjectKostprijsBO> GetUpdatePreview(int projectId) {
        var r = new GetResponse<ProjectKostprijsBO>();
        var snapshots = _uow.ProjectKostprijzen.GetNoTracking().Where(x => x.ProjectId == projectId).ToList();
        var materialen = _uow.KostprijsMaterialen.GetNoTracking()
            .Include(m => m.Categorie).Include(m => m.IndexType).ToList();
        var snapshotDict = snapshots.ToDictionary(x => x.KostprijsMateriaalId);
        foreach (var m in materialen.OrderBy(m => m.Categorie?.Lot).ThenBy(m => m.Volgorde)) {
            var heeftSnapshot = snapshotDict.TryGetValue(m.Id, out var snap);
            var huidigePrijs = heeftSnapshot ? snap.Prijs : 0m;
            var gewijzigd = !heeftSnapshot || snap.Prijs != m.ReferentiePrijs || snap.ReferentieDatum.Date != m.ReferentieDatum.Date;
            r.AddValue(new ProjectKostprijsBO {
                Id = heeftSnapshot ? snap.Id : 0, ProjectId = projectId, KostprijsMateriaalId = m.Id,
                CategorieNaam = m.Categorie?.Name ?? "", LotNummer = (int?)m.Categorie?.Lot,
                Naam = m.Naam, Eenheid = m.Eenheid, Toepassingsgebied = m.Toepassingsgebied,
                IndexTypeCode = m.IndexType?.Code ?? "",
                Prijs = huidigePrijs, PrijsInstellingen = m.ReferentiePrijs,
                ReferentieDatum = m.ReferentieDatum,
                SnapshotDatum = heeftSnapshot ? snap.SnapshotDatum : DateTime.MinValue,
                Gewijzigd = gewijzigd
            });
        }
        return r;
    }
    public Response BevestigUpdate(int projectId) => SnapshotVoorProject(projectId);
    public GetResponse<BouwkostPercentageGroepBO> GetPercentageGroepen() {
        var r = new GetResponse<BouwkostPercentageGroepBO>();
        foreach (var e in _uow.BouwkostPercentageGroepen.GetNoTracking().OrderBy(x => x.Volgorde).ThenBy(x => x.Naam))
            r.AddValue(new BouwkostPercentageGroepBO { Id = e.Id, Naam = e.Naam, Volgorde = e.Volgorde });
        return r;
    }
    public GetResponse<BouwkostPercentageBO> GetPercentages() {
        var r = new GetResponse<BouwkostPercentageBO>();
        var list = _uow.BouwkostPercentages.GetNoTracking()
            .Include(p => p.Groep)
            .OrderBy(p => p.Groep.Volgorde).ThenBy(p => p.Volgorde).ThenBy(p => p.Naam).ToList();
        foreach (var e in list)
            r.AddValue(new BouwkostPercentageBO {
                Id = e.Id, GroepId = e.GroepId, GroepNaam = e.Groep?.Naam,
                Naam = e.Naam, Percentage = e.Percentage, Volgorde = e.Volgorde
            });
        return r;
    }
    public Response InsertUpdatePercentageGroep(BouwkostPercentageGroepBO bo) {
        var r = new Response();
        if (bo.Id == 0) {
            _uow.BouwkostPercentageGroepen.Add(new BouwkostPercentageGroep { Naam = bo.Naam, Volgorde = bo.Volgorde });
            r.AddSuccess("Groep aangemaakt.");
        } else {
            var e = _uow.BouwkostPercentageGroepen.GetById(bo.Id);
            if (e == null) { r.AddError("Niet gevonden."); return r; }
            e.Naam = bo.Naam; e.Volgorde = bo.Volgorde;
            _uow.BouwkostPercentageGroepen.Update(e);
            r.AddSuccess("Groep opgeslagen.");
        }
        _uow.SaveChanges();
        return r;
    }
    public Response DeletePercentageGroep(int id) {
        var r = new Response();
        _uow.BouwkostPercentageGroepen.DeleteObject(id);
        _uow.SaveChanges();
        r.AddSuccess("Groep verwijderd.");
        return r;
    }
    public Response InsertUpdatePercentage(BouwkostPercentageBO bo) {
        var r = new Response();
        if (bo.Id == 0) {
            _uow.BouwkostPercentages.Add(new BouwkostPercentage {
                GroepId = bo.GroepId, Naam = bo.Naam, Percentage = bo.Percentage, Volgorde = bo.Volgorde
            });
            r.AddSuccess("Percentage aangemaakt.");
        } else {
            var e = _uow.BouwkostPercentages.GetById(bo.Id);
            if (e == null) { r.AddError("Niet gevonden."); return r; }
            e.GroepId = bo.GroepId; e.Naam = bo.Naam; e.Percentage = bo.Percentage; e.Volgorde = bo.Volgorde;
            _uow.BouwkostPercentages.Update(e);
            r.AddSuccess("Percentage opgeslagen.");
        }
        _uow.SaveChanges();
        return r;
    }
    public Response DeletePercentage(int id) {
        var r = new Response();
        _uow.BouwkostPercentages.DeleteObject(id);
        _uow.SaveChanges();
        r.AddSuccess("Percentage verwijderd.");
        return r;
    }
}

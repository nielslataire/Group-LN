using BOCore;
namespace FacadeCore;
public interface IKostprijsService {
    GetResponse<KmIndexTypeBO>        GetIndexTypes();
    GetResponse<ActivityGroupBO>      GetActivityGroepen();
    GetResponse<KostprijsMateriaalBO> GetMaterialen();
    Response InsertUpdateMateriaal(KostprijsMateriaalBO bo);
    Response DeleteMateriaal(int id);
    GetResponse<BouwkostPercentageGroepBO> GetPercentageGroepen();
    GetResponse<BouwkostPercentageBO>      GetPercentages();
    Response InsertUpdatePercentageGroep(BouwkostPercentageGroepBO bo);
    Response DeletePercentageGroep(int id);
    Response InsertUpdatePercentage(BouwkostPercentageBO bo);
    Response DeletePercentage(int id);
    GetResponse<FormulaKoppelingBO> GetFormulaKoppelingen();
    Response SaveFormulaKoppeling(string sleutel, int? materiaalId);
    GetResponse<FormulaKoppelingBO> CreateFormulaKoppeling(string naam, int? materiaalId);
    Response DeleteFormulaKoppeling(int id);
    Response SnapshotVoorProject(int projectId);
    GetResponse<ProjectKostprijsBO> GetProjectKostprijzen(int projectId);
    GetResponse<ProjectKostprijsBO> GetUpdatePreview(int projectId);
    Response BevestigUpdate(int projectId);
}

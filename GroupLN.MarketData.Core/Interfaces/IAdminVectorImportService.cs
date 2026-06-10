namespace GroupLN.MarketData.Core.Interfaces;

public interface IAdminVectorImportService
{
    Task<(int MunicipalitiesImported, int SectionsImported)> ImportAsync(
        string geoPackagePath,
        CancellationToken cancellationToken = default);
}

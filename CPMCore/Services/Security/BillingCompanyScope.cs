using BOCore;
using FacadeCore;

namespace CPMCore.Services.Security;

/// <summary>
/// Welke facturatiebedrijven de ingelogde gebruiker mag lezen voor de functie Facturatie. Zelfde regel als
/// <c>InvoicesController.ResolveInvoicingIssuerScopeAsync</c> en het rail-menu (<c>_RailPartialV2</c>): heeft de gebruiker het recht
/// Facturatie zonder bedrijfsspecifieke rijen (<c>InvoicingByBillingCompany.Company.{id}</c>), dan mag hij alle bedrijven zien,
/// anders enkel de bedrijven met een leesrecht. De verzamelbak voor externe coördinatiecontracten telt nooit mee.
/// </summary>
public static class BillingCompanyScope
{
    /// <summary>Ids van de facturatiebedrijven die de gebruiker mag zien (leeg = geen toegang).</summary>
    public static async Task<IReadOnlyList<IssuerCompanyBO>> ReadableCompaniesAsync(
        IPermissionService permissions, IIssuerCompanyService issuerCompanies, CancellationToken ct = default)
    {
        await permissions.EnsureLoadedAsync(ct);
        if (!permissions.HasRead(PermissionCodes.Invoicing))
            return Array.Empty<IssuerCompanyBO>();

        var scopedIds = permissions.EffectivePermissions
            .Where(x => x.Key.StartsWith(PermissionTreeV2.InvoicingCompanyPrefix, StringComparison.OrdinalIgnoreCase) && x.Value.Read)
            .Select(x => int.TryParse(x.Key[PermissionTreeV2.InvoicingCompanyPrefix.Length..], out var id) ? (int?)id : null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();

        var all = await issuerCompanies.GetAllAsync(ct);
        return all
            .Where(c => !c.IsExternalCoordinationDefault && (scopedIds.Count == 0 || scopedIds.Contains(c.Id)))
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

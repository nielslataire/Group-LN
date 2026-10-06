using System.Threading;
using System.Threading.Tasks;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Services.Signing;

/// <summary>Welk facturatiebedrijf een offerte/wijzigingsopdracht van een project uitgeeft — dezelfde regel als
/// <c>ProjectenController.Coordinatie.EnsureSupplierIssuerLink</c>: facturatiebedrijf-aannemingen van het project;
/// bij een coördinatieproject zonder dat, het coördinatiebedrijf; anders het als "externe standaard" gemarkeerde
/// bedrijf. Gedeeld door de PDF (<see cref="ChangeOrderPdfBuilder"/>) en het scherm (btw-codes van dat bedrijf).</summary>
public static class ChangeOrderIssuerResolver
{
    public static async Task<int?> ResolveIssuerCompanyIdAsync(cpmRunningContext db, Project project, CancellationToken ct = default)
    {
        if (project is null) return null;

        int? id = project.IssuerCompanyIdBuilder
            ?? ((project.IsCoordinationProject || project.IsOnlyCoordinationProject) ? project.CoordinationIssuerCompanyId : null)
            ?? await db.IssuerCompany.AsNoTracking()
                .Where(i => i.IsExternalCoordinationDefault && i.IsActive)
                .Select(i => (int?)i.Id)
                .FirstOrDefaultAsync(ct);

        return id is null or <= 0 ? null : id;
    }
}

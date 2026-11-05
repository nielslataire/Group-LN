using BOCore;
using CPMCore.Helpers;
using CPMCore.Services.Security;
using FacadeCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CPMCore.Controllers
{
    /// <summary>Verwijderen vanuit ChangeOrderDetailV2. Een OFFERTE mag verwijderd worden door iedereen met
    /// verwijderrechten op wijzigingsopdrachten; een WIJZIGINGSOPDRACHT enkel door een admin (bovenop dat
    /// recht). Een lopend of afgerond ondertekendossier blijft vergrendelen, ook voor een admin: eerst
    /// intrekken. Databasefouten (bv. gekoppelde facturen) komen als melding terug via
    /// DbUpdateExceptionFilter.</summary>
    public partial class ProjectenController
    {
        private async Task<bool> IsAdminUserAsync()
        {
            var id = User.GetCpmUserId();
            if (!id.HasValue) return false;
            return await HttpContext.RequestServices.GetRequiredService<ISecurityService>().UserIsAdmin(id.Value.ToString());
        }

        /// <summary>Mag de huidige gebruiker deze offerte/wijzigingsopdracht verwijderen?</summary>
        private async Task<bool> CanDeleteChangeOrderAsync(bool isQuote)
        {
            var ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            if (!ps.HasDelete(PermissionCodes.ProjectsChangeOrders)) return false;
            return isQuote || await IsAdminUserAsync();
        }

        /// <summary>Voorwaarde om te verwijderen: aan geen enkele regel of betaaltermijn hangt een verkoopfactuur (ook geen concept of proforma);
        /// een gefactureerde post verwijder je niet. Geeft de reden terug, of null als verwijderen kan.</summary>
        private async Task<string?> ChangeOrderDeleteBlockerAsync(int changeOrderId)
        {
            var invoiced = await _db.InvoicesDetails.AsNoTracking().AnyAsync(d =>
                (d.ChangeOrderDetailId != null && _db.ChangeOrderDetail.Any(cd => cd.Id == d.ChangeOrderDetailId && cd.ChangeOrderId == changeOrderId))
                || (d.ChangeOrderPaymentTermId != null && _db.ChangeOrderPaymentTerm.Any(t => t.Id == d.ChangeOrderPaymentTermId && t.ChangeOrderId == changeOrderId)));
            return invoiced ? "Er hangt een factuur (of proforma) aan deze wijzigingsopdracht. Verwijder of annuleer die eerst." : null;
        }

        /// <summary>Maakt de verwijzingen naar de wijzigingsopdracht los zodat de database ze niet meer blokkeert: inkoopfactuurregels en documenten
        /// blijven bestaan (zonder koppeling), afgeleide versies verliezen enkel hun verwijzing naar de bron. Geeft het aantal ontkoppelde inkoopregels terug.</summary>
        private async Task<int> UnlinkChangeOrderReferencesAsync(int changeOrderId)
        {
            var purchase = await _db.IncommingInvoiceDetail.Where(d => d.ChangeOrderId == changeOrderId)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.ChangeOrderId, (int?)null));
            // ProjectDocs.ChangeOrderId bestaat in de database maar niet in het EF-model.
            await _db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.ProjectDocs SET ChangeOrderId = NULL WHERE ChangeOrderId = {changeOrderId}");
            await _db.ChangeOrder.Where(c => c.SourceChangeOrderId == changeOrderId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.SourceChangeOrderId, (int?)null));
            return purchase;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeOrderDeleteV2(int projectId, int changeOrderId)
        {
            var co = await _db.ChangeOrder.AsNoTracking().FirstOrDefaultAsync(c => c.Id == changeOrderId);
            if (co is null) return RedirectToAction(nameof(ChangeOrdersV2), new { projectid = projectId });

            if (!await CanDeleteChangeOrderAsync(co.IsQuote))
            {
                AddMessage("error", co.IsQuote ? "U hebt geen rechten om offertes te verwijderen." : "Enkel een admin kan een wijzigingsopdracht verwijderen.", "Geen toegang");
                return BackToChangeOrder(projectId, changeOrderId);
            }
            if (await ActiveSigningCaseAsync(changeOrderId) is not null)
            {
                AddMessage("error", "Er loopt een ondertekening. Trek die eerst in en verwijder daarna.", "Vergrendeld");
                return BackToChangeOrder(projectId, changeOrderId);
            }
            if (await CompletedSigningCaseAsync(changeOrderId) is not null)
            {
                AddMessage("error", SigningCompletedLockedMessage, "Vergrendeld");
                return BackToChangeOrder(projectId, changeOrderId);
            }

            var blocker = await ChangeOrderDeleteBlockerAsync(changeOrderId);
            if (blocker is not null)
            {
                AddMessage("error", blocker, "Niet verwijderd");
                return BackToChangeOrder(projectId, changeOrderId);
            }

            var unlinked = 0;
            BOCore.Response response;
            try
            {
                unlinked = await UnlinkChangeOrderReferencesAsync(changeOrderId);
                response = _projectService.DeleteChangeOrders(new List<int> { changeOrderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Verwijderen van wijzigingsopdracht {ChangeOrderId} mislukt.", changeOrderId);
                AddMessage("error", "Het verwijderen is mislukt: er hangt nog iets anders aan deze wijzigingsopdracht. De fout is gelogd.", "Niet verwijderd");
                return BackToChangeOrder(projectId, changeOrderId);
            }
            if (!response.Success)
            {
                AddMessage("error", "Het verwijderen is mislukt. Probeer opnieuw.", "Niet verwijderd");
                return BackToChangeOrder(projectId, changeOrderId);
            }
            AddMessage("success", (co.IsQuote ? "De offerte" : "De wijzigingsopdracht") + " is verwijderd."
                + (unlinked > 0 ? $" {unlinked} gekoppelde inkoopfactuurregel(s) is/zijn ontkoppeld en blijven bestaan." : ""), "Verwijderd");
            return RedirectToAction(nameof(ChangeOrdersV2), new { projectid = projectId });
        }
    }
}

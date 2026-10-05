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

            var response = _projectService.DeleteChangeOrders(new List<int> { changeOrderId });
            if (!response.Success)
            {
                AddMessage("error", "Het verwijderen is mislukt. Probeer opnieuw.", "Niet verwijderd");
                return BackToChangeOrder(projectId, changeOrderId);
            }
            AddMessage("success", (co.IsQuote ? "De offerte" : "De wijzigingsopdracht") + " is verwijderd.", "Verwijderd");
            return RedirectToAction(nameof(ChangeOrdersV2), new { projectid = projectId });
        }
    }
}

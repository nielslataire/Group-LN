using BOCore;
using CPMCore.Models.Projecten;
using CPMCore.Services;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CPMCore.Controllers
{
    /// <summary>Bijlagen bij een offerte aan de klant (plan, fiche, …; migratie 131): opladen en verwijderen
    /// vanaf het opmaakscherm (AJAX, zonder het formulier te herladen) en meesturen met de offertemail.
    /// Bestanden staan in de DocStorage-map "quotes", naast het bronbestand van de leveranciersofferte.</summary>
    public partial class ProjectenController
    {
        private const string QuoteAttachmentFolder = "quotes";
        private const long QuoteAttachmentMaxBytes = 15 * 1024 * 1024;

        private async Task<List<ChangeOrderAttachmentV2>> LoadAttachmentsAsync(int changeOrderId, DocStorageService storage)
        {
            var rows = await _db.ChangeOrderAttachment.AsNoTracking()
                .Where(a => a.ChangeOrderId == changeOrderId)
                .OrderBy(a => a.Id)
                .ToListAsync();
            var list = new List<ChangeOrderAttachmentV2>();
            foreach (var a in rows)
                list.Add(new ChangeOrderAttachmentV2 { Id = a.Id, FileName = a.FileName, SizeText = SizeText(a.SizeBytes), Url = await storage.GetSignedUrlAsync(a.StoragePath, QuoteAttachmentFolder) });
            return list;
        }

        private static string SizeText(long bytes) => bytes switch
        {
            >= 1024 * 1024 => (bytes / 1024d / 1024d).ToString("0.#", System.Globalization.CultureInfo.GetCultureInfo("nl-BE")) + " MB",
            >= 1024 => Math.Round(bytes / 1024d).ToString("0") + " KB",
            _ => bytes + " B",
        };

        /// <summary>Alleen zolang de offerte nog te bewerken is (niet verzonden, niet omgezet).</summary>
        private static bool QuoteAttachmentsEditable(ChangeOrder co) => co.IsQuote && !co.QuoteConvertedAt.HasValue && !co.DateSendToClient.HasValue;

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(QuoteAttachmentMaxBytes + 1024 * 1024)]
        public async Task<IActionResult> ChangeOrderAttachmentAddV2(int projectId, int changeOrderId, IFormFile file)
        {
            if (!HttpContext.RequestServices.GetRequiredService<IPermissionService>().HasWrite(PermissionCodes.ProjectsChangeOrders)) return Json(new { success = false, message = "Je hebt geen schrijfrecht op offertes." });
            var co = await _db.ChangeOrder.FirstOrDefaultAsync(c => c.Id == changeOrderId && c.ProjectId == projectId);
            if (co is null) return Json(new { success = false, message = "Offerte niet gevonden." });
            if (!QuoteAttachmentsEditable(co)) return Json(new { success = false, message = "Deze offerte is verzonden of omgezet; bijlagen kunnen niet meer wijzigen." });
            if (file is null || file.Length == 0) return Json(new { success = false, message = "Geen bestand ontvangen." });
            if (file.Length > QuoteAttachmentMaxBytes) return Json(new { success = false, message = "Het bestand is te groot (maximaal 15 MB)." });

            var storage = HttpContext.RequestServices.GetRequiredService<DocStorageService>();
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var fileName = Path.GetFileName(string.IsNullOrWhiteSpace(file.FileName) ? "bijlage" : file.FileName);
            if (fileName.Length > 200) fileName = fileName[^200..];
            var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
            var stored = await storage.UploadAsync(ms.ToArray(), fileName, contentType, QuoteAttachmentFolder);
            if (stored is null) return Json(new { success = false, message = "Het bestand kon niet bewaard worden." });

            var row = new ChangeOrderAttachment { ChangeOrderId = co.Id, FileName = fileName, StoragePath = stored, ContentType = contentType, SizeBytes = file.Length, UploadedAt = DateTime.Now };
            _db.ChangeOrderAttachment.Add(row);
            await _db.SaveChangesAsync();
            return Json(new { success = true, id = row.Id, fileName, sizeText = SizeText(row.SizeBytes), url = await storage.GetSignedUrlAsync(stored, QuoteAttachmentFolder) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeOrderAttachmentDeleteV2(int projectId, int changeOrderId, int attachmentId)
        {
            if (!HttpContext.RequestServices.GetRequiredService<IPermissionService>().HasWrite(PermissionCodes.ProjectsChangeOrders)) return Json(new { success = false, message = "Je hebt geen schrijfrecht op offertes." });
            var co = await _db.ChangeOrder.AsNoTracking().FirstOrDefaultAsync(c => c.Id == changeOrderId && c.ProjectId == projectId);
            if (co is null) return Json(new { success = false, message = "Offerte niet gevonden." });
            if (!QuoteAttachmentsEditable(co)) return Json(new { success = false, message = "Deze offerte is verzonden of omgezet; bijlagen kunnen niet meer wijzigen." });
            var row = await _db.ChangeOrderAttachment.FirstOrDefaultAsync(a => a.Id == attachmentId && a.ChangeOrderId == changeOrderId);
            if (row is null) return Json(new { success = true });   // al weg
            _db.ChangeOrderAttachment.Remove(row);
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}

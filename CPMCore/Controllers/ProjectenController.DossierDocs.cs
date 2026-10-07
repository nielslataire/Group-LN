using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CPMCore.Controllers
{
    /// <summary>Documenten van een dossier (design-handoff 31d "Documenten — gedeeld met het documentencentrum"): een bestand dat op het
    /// dossier wordt opgeladen wordt een gewoon document in het documentencentrum (map "Overige", zonder koppeling aan eenheid of klant,
    /// dus nooit zichtbaar voor kopers of op de website) en hangt via ProjectDossierDocument aan het dossier. Staat hier, naast de
    /// documentencode, omdat de opslag- en miniatuur-helpers privé zijn aan ProjectenController. De actienaam bevat "Document":
    /// PermissionResolver vraagt dus schrijfrecht op ProjectsDocuments (POST), bovenop het dossierrecht dat hieronder gecontroleerd wordt.</summary>
    public partial class ProjectenController
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(200_000_000)]
        public async Task<IActionResult> DossierDocumentUploadV2(int projectid, int dossierId, IFormFile? file)
        {
            var ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            if (!ps.HasWrite(PermissionCodes.ProjectsDossiers)) return Forbid();
            if (file == null || file.Length <= 0) return DocJson(DocResult.Fail("Kies eerst een bestand."));
            if (!await _db.ProjectDossier.AsNoTracking().AnyAsync(d => d.Id == dossierId && d.ProjectId == projectid))
                return NotFound();

            var stored = await UploadAssetToStorageAsync(file, "docs");
            if (string.IsNullOrWhiteSpace(stored)) return DocJson(DocResult.Fail("Upload naar de opslag is mislukt."));
            try { await GenerateDocThumbnailViaStorageAsync(stored); }
            catch (Exception ex) { _logger.LogWarning(ex, "Thumbnail voor {File} kon niet gemaakt worden.", stored); }

            var naam = System.IO.Path.GetFileNameWithoutExtension(file.FileName);
            var res = await DocsSvc.Upload(new DocUploadDto
            {
                ProjectId = projectid, Mode = "new", Name = naam,
                UploaderKind = DocumentUploaderKind.Intern, StoredFilename = stored, OriginalFilename = file.FileName,
                SizeBytes = file.Length, UserId = DocUserId, UserName = DocUserName
            });
            if (res.Ok && res.Id.HasValue)
                await HttpContext.RequestServices.GetRequiredService<IProjectDossierService>().LinkDoc(dossierId, res.Id, null, naam, DocUserId);
            return DocJson(res);
        }
    }
}

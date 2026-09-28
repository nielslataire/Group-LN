using System.Net;
using CPMCore.Models.Signing;
using CPMCore.Services;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CPMCore.Controllers;

/// <summary>Publieke ondertekenpagina (geen CPM-login): de klant opent zijn persoonlijke link, bekijkt het PDF, vraagt een code
/// aan (per e-mail), typt zijn naam en bevestigt. Alle beveiliging zit in de token (onraadbaar, enkel gehasht bewaard), de
/// code (10 min, 5 pogingen, 5 codes/uur) en het bewijsdossier — zie ServiceCore.Documents.SigningService.
/// Bewust GEEN BaseController: geen interne permissies, geen gl-v2-layout, geen breadcrumbs.</summary>
[AllowAnonymous]
[Route("ondertekenen")]
public class SigningController : Controller
{
    private readonly ISigningService _signing;
    private readonly IEmailSender _email;
    private readonly IProjectService _projects;
    private readonly DocStorageService _storage;
    private readonly ChangeOrderPdfService _pdf;
    private readonly IConfiguration _config;
    private readonly ILogger<SigningController> _logger;

    public SigningController(ISigningService signing, IEmailSender email, IProjectService projects, DocStorageService storage,
        ChangeOrderPdfService pdf, IConfiguration config, ILogger<SigningController> logger)
    {
        _signing = signing;
        _email = email;
        _projects = projects;
        _storage = storage;
        _pdf = pdf;
        _config = config;
        _logger = logger;
    }

    private void NoIndex()
    {
        Response.Headers["Cache-Control"] = "no-store";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
        Response.Headers["Referrer-Policy"] = "no-referrer";
    }

    private string ClientIp()
    {
        var remote = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
        var xff = Request.Headers["X-Forwarded-For"].ToString();
        var text = string.IsNullOrWhiteSpace(xff) ? remote : remote + "|xff=" + xff.Split(',')[0].Trim();
        return text.Length > 64 ? text[..64] : text;
    }

    // ── pagina ──────────────────────────────────────────────────────────────────────────────────
    [HttpGet("{token}")]
    public async Task<IActionResult> Index(string token)
    {
        NoIndex();
        var info = await _signing.GetInfo(token, markOpened: true);
        if (info == null) return View("Invalid");
        if (info.State == "expired") return View("Expired", info);

        var vm = new SigningPageVm { Token = token, Info = info, ConsentText = _signing.ConsentText };
        if (info.ChangeOrderId.HasValue)
        {
            var co = _projects.GetChangeOrder(info.ChangeOrderId.Value);
            var bo = co.Success ? co.Values.FirstOrDefault() : null;
            if (bo != null)
            {
                var sales = _projects.GetSalesSettings(bo.ProjectId);
                var vat = sales.Success ? (sales.Values.FirstOrDefault()?.VatPercentage ?? 0m) : 0m;
                vm.Description = bo.Description ?? "";
                vm.Date = bo.ChangeOrderDate;
                vm.ExpirationDate = bo.ExpirationDate;
                vm.TotalExcl = bo.Totaal;
                vm.TotalIncl = bo.Totaal + vat * bo.Totaal / 100m;
                vm.HasAmounts = true;
            }
        }
        return View("Index", vm);
    }

    /// <summary>Het PDF wordt via onze server doorgegeven (geen storage-adres bloot, en de iframe blijft same-origin).</summary>
    [HttpGet("{token}/pdf")]
    public async Task<IActionResult> Pdf(string token)
    {
        NoIndex();
        var info = await _signing.GetInfo(token, markOpened: false);
        if (info == null || info.State == "expired" || string.IsNullOrWhiteSpace(info.CurrentFilename)) return NotFound();
        var url = await _storage.GetSignedUrlAsync(info.CurrentFilename, "docs");
        if (string.IsNullOrWhiteSpace(url)) return NotFound();
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            var bytes = await http.GetByteArrayAsync(url);
            Response.Headers["Content-Disposition"] = "inline; filename=\"" + WebUtility.UrlEncode(info.DocumentName) + ".pdf\"";
            return File(bytes, "application/pdf");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PDF ophalen voor ondertekening mislukt.");
            return NotFound();
        }
    }

    // ── code aanvragen ──────────────────────────────────────────────────────────────────────────
    [HttpPost("{token}/code")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestCode(string token)
    {
        NoIndex();
        var res = await _signing.RequestCode(token);
        if (!res.Ok) return Json(new { ok = false, message = res.Message });

        try
        {
            var name = WebUtility.HtmlEncode(res.SignerName ?? "");
            var doc = WebUtility.HtmlEncode(res.DocumentName ?? "");
            await _email.SendEmailAsync(res.Email!, "Uw code om te ondertekenen",
                $"<p>Beste {name},</p><p>Uw code om <strong>{doc}</strong> te ondertekenen is:</p>" +
                $"<p style=\"font-size:28px;letter-spacing:6px;font-weight:700;margin:18px 0\">{res.Code}</p>" +
                $"<p>De code is {SigningServiceCodeMinutes} minuten geldig. Vraagt u dit niet zelf aan, dan kan u deze e-mail negeren en niemand de code geven.</p>");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ondertekeningscode-mail mislukt.");
            return Json(new { ok = false, message = "De e-mail met de code kon niet verstuurd worden. Probeer het later opnieuw." });
        }
        return Json(new { ok = true, message = $"We stuurden een code naar {MaskFor(res.Email!)}." });
    }

    private const int SigningServiceCodeMinutes = ServiceCore.Documents.SigningService.CodeValidMinutes;

    private static string MaskFor(string email)
    {
        var at = email.IndexOf('@');
        if (at < 1) return email;
        return email[..1] + "***" + email[at..];
    }

    // ── tekenen ─────────────────────────────────────────────────────────────────────────────────
    [HttpPost("{token}/sign")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sign(string token, string code, string typedName, bool consent)
    {
        NoIndex();
        var res = await _signing.Sign(token, code, typedName, consent, ClientIp(), Request.Headers.UserAgent.ToString());
        if (!res.Ok) return Json(new { ok = false, message = res.Message });

        // Alles hierna is "best effort": de handtekening staat al vast. Faalt het PDF of de mail, dan ziet de projectleider
        // dat het ondertekende document ontbreekt en kan het opnieuw gemaakt worden.
        byte[]? signedPdf = null; string signedName = "Wijzigingsopdracht (ondertekend).pdf";
        if (res.AllSigned && res.ChangeOrderId.HasValue)
        {
            try
            {
                var evidence = await _signing.GetEvidence(res.ChangeOrderId.Value);
                var pdf = await _pdf.BuildAsync(ControllerContext, res.ChangeOrderId.Value, pendingDigitalSigning: false, evidence);
                if (pdf != null)
                {
                    signedPdf = pdf.Bytes;
                    signedName = pdf.FileName.Replace(".pdf", " - ondertekend.pdf");
                    var stored = await _storage.UploadAsync(pdf.Bytes, signedName, "application/pdf", "docs");
                    if (!string.IsNullOrWhiteSpace(stored))
                        await _signing.AttachSignedRevision(res.DocumentId, stored, pdf.Bytes.Length, "Ondertekende versie met handtekeningblok");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ondertekend PDF maken/bewaren mislukt voor document {DocId}.", res.DocumentId);
            }
        }

        await SendReceipts(res, signedPdf, signedName);
        return Json(new { ok = true, allSigned = res.AllSigned, evidenceRef = res.EvidenceRef });
    }

    private async Task SendReceipts(SigningSignResult res, byte[]? signedPdf, string signedName)
    {
        string E(string? s) => WebUtility.HtmlEncode(s ?? "");
        var when = res.SignedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
        var attachments = signedPdf == null ? null : new[] { new EmailAttachment(signedName, signedPdf, "application/pdf") };
        try
        {
            if (!string.IsNullOrWhiteSpace(res.SignerEmail))
                await _email.SendEmailAsync(res.SignerEmail, "Bevestiging van uw ondertekening — " + res.DocumentName,
                    $"<p>Beste {E(res.SignerName)},</p><p>U ondertekende <strong>{E(res.DocumentName)}</strong> (project {E(res.ProjectName)}) op {when}.</p>" +
                    $"<p>Referentie: <strong>{E(res.EvidenceRef)}</strong>.</p>" +
                    (res.AllSigned ? "<p>Alle partijen tekenden. Het ondertekende document zit als bijlage bij deze e-mail.</p>" : "<p>Zodra alle partijen getekend hebben, ontvangt u het ondertekende document.</p>") +
                    "<p>Bewaar deze e-mail als bewijs.</p>", attachments);
            if (!string.IsNullOrWhiteSpace(res.NotifyEmail))
                await _email.SendEmailAsync(res.NotifyEmail, (res.AllSigned ? "Volledig ondertekend — " : "Ondertekend door " + res.SignerName + " — ") + res.DocumentName,
                    $"<p>{E(res.SignerName)} ondertekende <strong>{E(res.DocumentName)}</strong> (project {E(res.ProjectName)}) op {when}. Referentie {E(res.EvidenceRef)}.</p>" +
                    (res.AllSigned ? "<p>Alle ondertekenaars tekenden; het document is bevroren en staat in de map Contracten.</p>" : "<p>Er wachten nog andere ondertekenaars.</p>"), attachments);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bevestigingsmail na ondertekening mislukt.");
        }
    }
}

using CPMCore.Extensions;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceCore;
using ServiceCore.Invoicing.Pdf;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CPMCore.Controllers
{

    [Authorize(Policy = "CpmAdmin")]
    [CPMCore.Filters.PermissionRead(CPMCore.Services.Security.PermissionCodes.SettingsInvoiceTemplates)]
    public class InvoiceLayoutController : BaseController
    {
        private readonly IInvoicePdfService _pdfService;
        private readonly IInvoiceQueryService _invoices;
        private readonly IIssuerCompanyService _issuers;

        public InvoiceLayoutController(
            IInvoicePdfService pdfService,
            IInvoiceQueryService invoices,
            IIssuerCompanyService issuers)
        {
            _pdfService = pdfService;
            _invoices = invoices;
            _issuers = issuers;
        }

        [HttpPost("Admin/InvoiceLayout/Preview")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Preview([FromForm] LayoutPreviewRequest request, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                var error = ModelState.Values.SelectMany(x => x.Errors).FirstOrDefault()?.ErrorMessage
                    ?? "Ongeldige aanvraag.";
                return BadRequest(new { message = error });
            }

            var detail = await _invoices.GetDetailAsync(request.InvoiceId, ct);
            if (detail == null)
                return NotFound(new { message = "Factuur niet gevonden." });

            var issuer = await _issuers.GetAsync(request.IssuerCompanyId, ct);
            if (issuer == null)
                return NotFound(new { message = "Bedrijf niet gevonden." });

            var vatTypes = await _issuers.ListVatTypeAsync(request.IssuerCompanyId, ct);

            issuer.TemplateKey = string.IsNullOrWhiteSpace(request.TemplateKey)
                ? issuer.TemplateKey
                : request.TemplateKey;
            issuer.TemplateJson = request.TemplateJson;

            if (!string.IsNullOrWhiteSpace(request.BrandPrimaryColor))
                issuer.BrandPrimaryColor = request.BrandPrimaryColor;
            if (!string.IsNullOrWhiteSpace(request.BrandSecondaryColor))
                issuer.BrandSecondaryColor = request.BrandSecondaryColor;
            if (!string.IsNullOrWhiteSpace(request.FontFamily))
                issuer.FontFamily = request.FontFamily;
            if (request.FooterLegalText != null)
                issuer.FooterLegalText = request.FooterLegalText;
            if (request.EpcQrEnabled.HasValue)
                issuer.EpcQrEnabled = request.EpcQrEnabled.Value;

            try
            {
                var dto = detail.ToInvoiceDto(vatTypes);
                var bytes = _pdfService.Render(dto, issuer);
                return File(bytes, "application/pdf", "factuur-preview.pdf");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        public sealed class LayoutPreviewRequest
        {
            [Required]
            [Range(1, int.MaxValue)]
            public int IssuerCompanyId { get; set; }

            [Required]
            [Range(1, int.MaxValue)]
            public int InvoiceId { get; set; }

            public string? TemplateKey { get; set; }
            public string? TemplateJson { get; set; }
            public string? BrandPrimaryColor { get; set; }
            public string? BrandSecondaryColor { get; set; }
            public string? FontFamily { get; set; }
            public string? FooterLegalText { get; set; }
            public bool? EpcQrEnabled { get; set; }
        }
    }
}

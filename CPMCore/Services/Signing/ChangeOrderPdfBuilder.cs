using System.Globalization;
using BOCore;
using CPMCore.Configuration;
using CPMCore.Documents;
using CPMCore.Documents.GlV2;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuestPDF.Drawing;
using QuestPDF.Fluent;

namespace CPMCore.Services.Signing;

/// <summary>
/// Eén plek die een wijzigingsopdracht uit de database haalt en er de QuestPDF-PDF van maakt.
/// Gebruikt door de afdrukactie (<c>ProjectenController.ChangeOrders.ChangeOrderPDF</c>) én door
/// <see cref="ChangeOrderSigningSource"/>, zodat de klant op papier en in het ondertekendossier
/// letterlijk hetzelfde document krijgt. Leest via <see cref="cpmRunningContext"/> (zoals de
/// nieuwere services), niet via de BO-laag: het document heeft velden nodig (postcode van het
/// project, contacten, eenheden per project) die de <c>ChangeOrderBO</c>-vertaling niet meedraagt.
///
/// <see cref="Render"/> kiest op <paramref name="useGlV2Layout"/> tussen de bestaande
/// <see cref="ChangeOrderDocument"/> (ongewijzigd, legacy huisstijl — <see cref="ChangeOrderSigningSource"/>
/// gebruikt enkel deze overload) en <see cref="ChangeOrderDocumentV2"/> (DOCUMENTLAYOUT_VOORTGANG.md,
/// enkel voor gl-v2-sessies). Zo verandert er niets voor wie niet in gl-v2 zit. Het facturatiebedrijf op
/// het gl-v2-document (<c>ChangeOrderPdfModel.IssuerCompany*</c>) volgt dezelfde regel als
/// <c>ProjectenController.Coordinatie.EnsureSupplierIssuerLink</c>: eerst <c>Project.IssuerCompanyIdBuilder</c>
/// (het facturatiebedrijf-aannemingen), bij een coördinatieproject anders <c>Project.CoordinationIssuerCompanyId</c>,
/// anders het als "externe standaard" gemarkeerde facturatiebedrijf. Enkel als geen van die drie iets
/// opleveren (zou niet mogen voorkomen) valt de PDF terug op <see cref="GlV2PdfCompanyOptions"/>.
/// </summary>
public sealed class ChangeOrderPdfBuilder
{
    private readonly cpmRunningContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ChangeOrderPdfBuilder> _logger;
    private readonly IIssuerCompanyService _issuerCompanyService;
    private readonly GlV2PdfCompanyOptions _glV2CompanyFallback;

    public ChangeOrderPdfBuilder(cpmRunningContext db, IWebHostEnvironment env, ILogger<ChangeOrderPdfBuilder> logger,
        IIssuerCompanyService issuerCompanyService, IOptions<GlV2PdfCompanyOptions> glV2CompanyFallback)
    {
        _db = db;
        _env = env;
        _logger = logger;
        _issuerCompanyService = issuerCompanyService;
        _glV2CompanyFallback = glV2CompanyFallback.Value;
    }

    /// <summary>De wijzigingsopdracht met alles wat de PDF en het ondertekendossier nodig hebben; null als ze niet bestaat.</summary>
    public async Task<ChangeOrderPdfModel?> LoadAsync(int changeOrderId, CancellationToken ct = default)
    {
        var co = await _db.ChangeOrder.AsNoTracking()
            .Include(c => c.ChangeOrderDetail)
            .Include(c => c.ChangeOrderPaymentTerm)
            .Include(c => c.ClientAccount).ThenInclude(ca => ca.PostalCode)
            .Include(c => c.ContractActivity).ThenInclude(a => a.Contract).ThenInclude(k => k.Project).ThenInclude(p => p.PostalCode)
            .FirstOrDefaultAsync(c => c.Id == changeOrderId, ct);
        if (co is null) return null;

        var project = co.ContractActivity?.Contract?.Project;
        var projectId = project?.ProjectId ?? 0;

        // Zelfde regel als ClientService.GetClientAccountUnitsNameById, maar beperkt tot dit project.
        var units = await _db.Units.AsNoTracking()
            .Where(u => u.ClientAccountId == co.ClientAccountId && u.ProjectId == projectId && u.Type != null && u.Type.Selectable == true)
            .OrderBy(u => u.Type!.GroupId).ThenBy(u => u.Name)
            .Select(u => new { TypeName = u.Type!.Name, u.Name })
            .ToListAsync(ct);

        var vat = await _db.ProjectSalesSettings.AsNoTracking()
            .Where(s => s.Projectid == projectId)
            .Select(s => s.Vatpercentage)
            .FirstOrDefaultAsync(ct) ?? 0m;

        // Btw-vermeldingen: de factuurvermelding (Vattype.InvoiceMention) van het facturatiebedrijf, dezelfde tekst als
        // op de facturen. Een regel met een gekozen btw-code (migratie 073) levert de vermelding van precies die code;
        // regels zonder code vallen terug op alle codes van het bedrijf met hetzelfde percentage. Niets ingesteld = niets tonen.
        var issuer = await ResolveIssuerCompanyAsync(project, ct);
        var vatMentions = new Dictionary<decimal, string>();
        if (issuer is not null)
        {
            var chosenTypeIds = co.ChangeOrderDetail.Where(d => d.VatTypeId.HasValue).Select(d => d.VatTypeId.Value).Distinct().ToList();
            var types = await _db.Vattype.AsNoTracking()
                .Where(v => v.IssuerCompanyId == issuer.Id || chosenTypeIds.Contains(v.Id))
                .OrderBy(v => v.Id)
                .Select(v => new { v.Id, v.BasePercentage, v.InvoiceMention })
                .ToListAsync(ct);
            var lineRates = co.ChangeOrderDetail.Select(d => d.VatPercentage ?? co.QuoteVatPercentage ?? vat).Distinct().ToList();
            foreach (var rate in lineRates)
            {
                var usedTypeIds = co.ChangeOrderDetail
                    .Where(d => d.VatTypeId.HasValue && (d.VatPercentage ?? co.QuoteVatPercentage ?? vat) == rate)
                    .Select(d => d.VatTypeId.Value).ToHashSet();
                // Eén vermelding per tarief: met gekozen btw-code(s) enkel die van die code(s); zonder code de eerste code met dat percentage die een
                // vermelding heeft (niet alle codes met hetzelfde percentage — dat gaf meerdere vermeldingen naast elkaar).
                var texts = (usedTypeIds.Count > 0
                        ? types.Where(t => usedTypeIds.Contains(t.Id)).Select(t => t.InvoiceMention?.Trim()).Where(t => !string.IsNullOrEmpty(t))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                        : types.Where(t => t.BasePercentage == rate).Select(t => t.InvoiceMention?.Trim()).Where(t => !string.IsNullOrEmpty(t)).Take(1))
                    .ToList();
                if (texts.Count > 0) vatMentions[rate] = string.Join(" ", texts);
            }
        }

        // Facturatieplan (wijzigingsopdracht) en wie moet tekenen.
        var terms = new List<ChangeOrderPdfTerm>();
        var signers = new List<ChangeOrderPdfSigner>();
        var allSignersRequired = false;
        if (!co.IsQuote)
        {
            var stageIds = co.ChangeOrderPaymentTerm.Where(t => t.TriggerStageId.HasValue).Select(t => t.TriggerStageId.Value).Distinct().ToList();
            var stageNames = stageIds.Count == 0 ? new Dictionary<int, string>()
                : await _db.InvoicingPaymentStages.AsNoTracking().Where(s => stageIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s.Name, ct);
            terms = co.ChangeOrderPaymentTerm.OrderBy(t => t.SortOrder).Select(t => new ChangeOrderPdfTerm
            {
                Kind = t.Kind,
                Label = t.Kind switch { 1 => "Voorschot", 3 => "Saldo", _ => "Tussentijdse schijf" },
                TriggerText = t.TriggerType switch
                {
                    1 => "na ondertekening",
                    2 when t.Kind == 3 || !t.TriggerStageId.HasValue => "met de laatste schijf",
                    2 => "bij schijf " + (stageNames.GetValueOrDefault(t.TriggerStageId!.Value) ?? ""),
                    3 => "manueel vrijgegeven",
                    _ => "",
                },
                Percentage = t.Percentage,
                FixedAmount = t.FixedAmount,
            }).ToList();

            // Zelfde voorstel als ChangeOrderSigningSource.SuggestPartiesAsync: eigenaar 1, dan de mede-eigenaars;
            // wie hetzelfde e-mailadres als eigenaar 1 heeft tekent niet apart.
            var acct = co.ClientAccount;
            if (acct is not null)
            {
                signers.Add(new ChangeOrderPdfSigner { Name = DisplayName(acct), Percentage = acct.OwnerPercentage });
                var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(acct.Email)) seenEmails.Add(acct.Email.Trim());
                var contacts = await _db.ClientContacts.AsNoTracking()
                    .Where(c => c.ClientAccountId == co.ClientAccountId && c.IsCoOwner)
                    .OrderByDescending(c => c.IsPrimaryContact).ThenBy(c => c.Id)
                    .ToListAsync(ct);
                foreach (var c in contacts)
                {
                    var email = c.Email?.Trim();
                    if (!string.IsNullOrWhiteSpace(email) && !seenEmails.Add(email)) continue;
                    var name = string.Join(" ", new[] { c.Name, c.Forename }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    if (string.IsNullOrWhiteSpace(name)) name = c.CompanyName ?? "Mede-eigenaar";
                    signers.Add(new ChangeOrderPdfSigner { Name = name, Percentage = c.CoOwnerPercentage });
                }
            }
            // Ondertekenregel: voorkeur van de klant, anders het beleid voor wijzigingsopdrachten, anders "iedereen" (0 Alle, 1 Eén volstaat, 2 Volgorde).
            var policyRule = await _db.SigningPolicy.AsNoTracking()
                .Where(p => p.DocumentType == ChangeOrderSigningSource.Key && p.IsActive).Select(p => (int?)p.SigningRule).FirstOrDefaultAsync(ct);
            var rule = (int?)acct?.DefaultSigningRule ?? policyRule ?? 0;
            allSignersRequired = rule != 1;
        }

        var client = co.ClientAccount;
        string? salutation = null;
        if (client is not null && Enum.TryParse<Salutation>(client.Salutation, out var sal))
            salutation = sal.GetDisplayName();

        return new ChangeOrderPdfModel
        {
            Id = co.Id,
            PublicNumber = co.PublicNumber,
            ProjectId = projectId,
            ClientAccountId = co.ClientAccountId,
            IsQuote = co.IsQuote,
            Date = co.Date,
            ExpirationDate = co.ExpirationDate,
            ProjectName = project?.ProjectName ?? "",
            ProjectAddressLine = project is null ? null : string.Join(" ", new[] { project.Street, project.Number }.Where(s => !string.IsNullOrWhiteSpace(s))),
            ProjectCityLine = project?.PostalCode is null ? null : string.Join(" ", new[] { project.PostalCode.Postcode, project.PostalCode.Gemeente }.Where(s => !string.IsNullOrWhiteSpace(s))),
            ProjectMunicipality = project?.PostalCode?.Gemeente,
            ClientSalutation = salutation,
            ClientName = DisplayName(client),
            ClientEmail = client?.Email,
            UnitsLine = string.Join(" - ", units.Select(u => $"{u.TypeName} {u.Name}".Trim())),
            ClientStreetLine = client is null ? null : string.Join(" ", new[] { client.Street, client.Housenumber }.Where(s => !string.IsNullOrWhiteSpace(s))),
            ClientCityLine = client?.PostalCode is null ? null : string.Join(" ", new[] { client.PostalCode.Postcode, client.PostalCode.Gemeente }.Where(s => !string.IsNullOrWhiteSpace(s))),
            ClientInvoiceExtra = client?.InvoiceExtra,
            Description = co.Description ?? "",
            Subject = co.Subject,
            CommentHtml = co.Comment,
            Conditions = co.IsQuote && string.IsNullOrWhiteSpace(co.ChangeOrderConditions)
                ? ChangeOrderStandardTexts.QuoteConditions(co.ExpirationDate)
                : co.ChangeOrderConditions,
            VatPercentage = vat,
            VatMentions = vatMentions,
            Terms = terms,
            Signers = signers,
            AllSignersRequired = allSignersRequired,
            Lines = co.ChangeOrderDetail.OrderBy(d => d.SortOrder ?? int.MaxValue).ThenBy(d => d.Id).Select(d => new ChangeOrderPdfLine
            {
                Description = d.Description,
                UnitLabel = d.MeasurementUnit.HasValue && Enum.IsDefined(typeof(MeasurementUnit), d.MeasurementUnit.Value) ? ((MeasurementUnit)d.MeasurementUnit.Value).GetDisplayName() : null,
                TypeLabel = d.MeasurementType.HasValue && Enum.IsDefined(typeof(MeasurementType), d.MeasurementType.Value) ? ((MeasurementType)d.MeasurementType.Value).GetDisplayName() : null,
                Number = d.Number,
                Price = d.Price,
                CommissionPercentage = d.Commission,
                VatPercentage = d.VatPercentage ?? co.QuoteVatPercentage,
                VatTypeId = d.VatTypeId,
            }).ToList(),
            IssuerCompanyId = issuer?.Id,
            IssuerCompanyName = issuer?.Name,
            IssuerCompanyLegalLine = issuer is null ? null : string.Join(" ", new[] { issuer.LegalName ?? issuer.Name, issuer.CompanyLegalFormAbbreviation }.Where(s => !string.IsNullOrWhiteSpace(s))),
            IssuerCompanyVatNumber = issuer?.VatNumber,
            IssuerCompanyIban = issuer?.DefaultBankAccountIban,
            IssuerCompanyStreet = issuer is null ? null : string.Join(" ", new[] { issuer.AddressLine1, issuer.AddressLine2 }.Where(s => !string.IsNullOrWhiteSpace(s))),
            IssuerCompanyPostalCity = issuer is null ? null : string.Join(" ", new[] { issuer.PostalCode, issuer.City }.Where(s => !string.IsNullOrWhiteSpace(s))),
            IssuerCompanyPhone = issuer?.Phone,
            IssuerCompanyEmail = issuer?.Email,
            IssuerCompanyWebsite = issuer?.Website,
            IssuerCompanyLogoBytes = issuer?.LogoBytes,
        };
    }

    /// <summary>Zelfde regel als <c>ProjectenController.Coordinatie.EnsureSupplierIssuerLink</c> (zie
    /// die methode voor de volledige uitleg): eerst het facturatiebedrijf-aannemingen van het project
    /// (<see cref="Project.IssuerCompanyIdBuilder"/>); is dit een coördinatieproject, dan diens
    /// <see cref="Project.CoordinationIssuerCompanyId"/>; anders (of als die leeg is) het als "externe
    /// standaard" gemarkeerde facturatiebedrijf. Geeft null als geen van de drie iets opleveren —
    /// <see cref="Render"/> valt dan terug op <see cref="GlV2PdfCompanyOptions"/> in plaats van de PDF
    /// met een halve/foute bedrijfsnaam te tonen.</summary>
    private async Task<IssuerCompanyBO?> ResolveIssuerCompanyAsync(Project? project, CancellationToken ct)
    {
        if (project is null) return null;

        var targetIssuerId = await ChangeOrderIssuerResolver.ResolveIssuerCompanyIdAsync(_db, project, ct);

        if (targetIssuerId is null or <= 0)
        {
            _logger.LogWarning(
                "ChangeOrderPdfBuilder: geen facturatiebedrijf gevonden voor project {ProjectId} (geen bouwheer-facturatiebedrijf en geen 'externe standaard' ingesteld); PDF valt terug op het standaard-bedrijf uit configuratie.",
                project.ProjectId);
            return null;
        }

        return await _issuerCompanyService.GetAsync(targetIssuerId.Value, ct);
    }

    /// <summary>Zelfde regel als ClientAccountBO.DisplayName (migratie 057): achternaam eerst, dan
    /// voornaam indien gekend, anders bedrijfsnaam.</summary>
    public static string DisplayName(ClientAccount? client)
    {
        if (client is null) return "";
        if (string.IsNullOrWhiteSpace(client.Name)) return client.CompanyName ?? "";
        return string.IsNullOrWhiteSpace(client.Forename) ? client.Name : client.Name + " " + client.Forename;
    }

    /// <summary>Bedrijfsgegevens voor kop/voet van een gl-v2-document: het facturatiebedrijf van het project;
    /// enkel als dat niets opleverde (zou niet mogen voorkomen) de terugval uit <see cref="GlV2PdfCompanyOptions"/>.</summary>
    public GlV2PdfCompanyInfo BuildGlV2Company(ChangeOrderPdfModel model)
    {
        var logoPath = Path.Combine(_env.WebRootPath, "Img", "groupln-logo.png");
        byte[]? logo = File.Exists(logoPath) ? File.ReadAllBytes(logoPath) : null;
        return model.IssuerCompanyId is int
                ? new GlV2PdfCompanyInfo
                {
                    Name = model.IssuerCompanyName ?? _glV2CompanyFallback.Name,
                    Street = model.IssuerCompanyStreet ?? _glV2CompanyFallback.Street,
                    PostalCity = model.IssuerCompanyPostalCity ?? _glV2CompanyFallback.PostalCity,
                    Phone = model.IssuerCompanyPhone ?? _glV2CompanyFallback.Phone,
                    Email = model.IssuerCompanyEmail ?? _glV2CompanyFallback.Email,
                    Website = model.IssuerCompanyWebsite ?? _glV2CompanyFallback.Website,
                    LegalForm = model.IssuerCompanyLegalLine ?? _glV2CompanyFallback.LegalForm,
                    VatNumber = model.IssuerCompanyVatNumber ?? _glV2CompanyFallback.VatNumber,
                    Iban = model.IssuerCompanyIban ?? _glV2CompanyFallback.Iban,
                    LogoBytes = model.IssuerCompanyLogoBytes is { Length: > 0 } ? model.IssuerCompanyLogoBytes : logo,
                }
                : new GlV2PdfCompanyInfo
                {
                    Name = _glV2CompanyFallback.Name,
                    Tagline = _glV2CompanyFallback.Tagline,
                    Street = _glV2CompanyFallback.Street,
                    PostalCity = _glV2CompanyFallback.PostalCity,
                    Phone = _glV2CompanyFallback.Phone,
                    Email = _glV2CompanyFallback.Email,
                    Website = _glV2CompanyFallback.Website,
                    LegalForm = _glV2CompanyFallback.LegalForm,
                    VatNumber = _glV2CompanyFallback.VatNumber,
                    Iban = _glV2CompanyFallback.Iban,
                    LogoBytes = logo,
                };
    }

    public byte[] Render(ChangeOrderPdfModel model, bool useGlV2Layout = false)
    {
        var logoPath = Path.Combine(_env.WebRootPath, "Img", "groupln-logo.png");
        byte[]? logo = File.Exists(logoPath) ? File.ReadAllBytes(logoPath) : null;

        if (useGlV2Layout)
        {
            var fontsAvailable = GlV2PdfFonts.EnsureRegistered(_env, _logger);
            var company = BuildGlV2Company(model);
            return new ChangeOrderDocumentV2(model, company, fontsAvailable).GeneratePdf();
        }

        var fontFamily = GroupLnFonts.EnsureAvenirRegistered(_env, _logger);
        return new ChangeOrderDocument(model, logo, fontFamily).GeneratePdf();
    }

    public static string FileName(ChangeOrderPdfModel model)
    {
        var safe = string.Concat((model.ProjectName ?? "Project").Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch)).Trim();
        return $"{(model.IsQuote ? "Offerte" : "Wijzigingsopdracht")}_{safe}_{model.Date:yyyyMMdd}_{model.Id}.pdf";
    }
}

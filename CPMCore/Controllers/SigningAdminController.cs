using BOCore;
using CPMCore.Configuration;
using CPMCore.Helpers;
using CPMCore.Models.Signing;
using DALCore.Models;
using FacadeCore;
using FacadeCore.Signing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServiceCore.Signing;

namespace CPMCore.Controllers;

/// <summary>
/// Interne kant van het elektronisch ondertekenen (ONDERTEKENEN_VOORSTEL.md §5.1, §5.3): een document
/// aanbieden, het dossier opvolgen (status per ondertekenaar, herinnering, nieuwe link, annuleren,
/// downloads, audit trail) en het overzicht per project. Generiek over <c>ISigningDocumentSource</c>:
/// deze controller weet niet wat een wijzigingsopdracht is; hij kent enkel (documentType, sourceId).
///
/// Nieuw scherm, dus enkel gl-v2 (DESIGN.md): de layout wordt hier geforceerd i.p.v. via de
/// preview-cookie, zodat de module er voor iedereen hetzelfde uitziet zodra ze aanstaat.
/// Rechten: PermissionResolver koppelt "SigningAdmin" aan PermissionCodes.Signing (GET = lezen,
/// POST = schrijven). Feature-vlag <c>Features:EnableSigning</c> uit = 404 op elke actie.
/// </summary>
public class SigningAdminController : BaseController
{
    private readonly ISigningService _signing;
    private readonly SigningRegistry _registry;
    private readonly cpmRunningContext _db;
    private readonly IPermissionService _permissions;
    private readonly FeatureFlagsOptions _features;
    private readonly SigningOptions _options;
    private readonly ILogger<SigningAdminController> _logger;

    public SigningAdminController(
        ISigningService signing,
        SigningRegistry registry,
        cpmRunningContext db,
        IPermissionService permissions,
        IOptions<FeatureFlagsOptions> features,
        IOptions<SigningOptions> options,
        ILogger<SigningAdminController> logger)
    {
        _signing = signing;
        _registry = registry;
        _db = db;
        _permissions = permissions;
        _features = features.Value;
        _options = options.Value;
        _logger = logger;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        if (!_features.EnableSigning)
        {
            context.Result = NotFound();
            return;
        }
        ViewData["UseGlV2Layout"] = true;
    }

    private SigningRequestContext Ctx() => new(
        HttpContext.Connection.RemoteIpAddress?.ToString(),
        Request.Headers.UserAgent.ToString(),
        null,
        User.GetCpmUserId(),
        User.GetCpmDisplayName());

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Overzicht
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> Index(int? projectId, int? status, CancellationToken ct)
    {
        var vm = new SigningIndexVm
        {
            ProjectId = projectId,
            StatusFilter = status,
            Cases = await _signing.ListCasesAsync(projectId, status, 300, ct),
            DocumentTypeLabels = _registry.DocumentTypes.ToDictionary(k => k, k => _registry.Source(k).DisplayName),
        };
        if (projectId is > 0)
        {
            var project = await ProjectHeaderAsync(projectId.Value, ct);
            if (project is null) return NotFound();
            vm.ProjectName = project.Value.Name;
            vm.IsCoordinationProject = project.Value.IsCoordination;
            ViewData["BackUrl"] = Url.Action("Detail", "Projecten", new { projectid = projectId });
            ViewData["BreadcrumbNode"] = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "SigningAdmin", "Ondertekeningen")
            {
                Parent = ProjectCrumb(projectId.Value, project.Value.Name),
                RouteValues = new { projectId }
            };
        }
        else
        {
            ViewData["BreadcrumbNode"] = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "SigningAdmin", "Ondertekeningen") { Parent = HomeCrumb() };
        }
        SetPageHeader("ph ph-signature", "Ondertekeningen");
        return View(vm);
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Aanbieden
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> Start(string documentType, int sourceId, string? backUrl, CancellationToken ct)
    {
        var source = _registry.TrySource(documentType ?? "");
        if (source is null || sourceId <= 0) return NotFound();

        var active = await _signing.GetActiveCaseForSourceAsync(source.DocumentType, sourceId, ct);
        if (active is not null)
        {
            AddMessage("info", "Er loopt al een ondertekeningsprocedure voor dit document.", "Al aangeboden");
            return RedirectToAction(nameof(Dossier), new { id = active.CaseId });
        }

        var vm = await BuildStartVmAsync(source, sourceId, backUrl, ct);
        if (vm is null)
        {
            AddMessage("error", "Het document kon niet opgebouwd worden.", "Fout");
            return Redirect(SafeLocal(backUrl) ?? Url.Action("Index")!);
        }
        SetPageHeader("ph ph-signature", "Ter ondertekening aanbieden");
        ViewData["BackUrl"] = vm.BackUrl;
        ViewData["BreadcrumbNode"] = StartCrumb(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(SigningStartPostVm post, CancellationToken ct)
    {
        var source = _registry.TrySource(post.DocumentType ?? "");
        if (source is null || post.SourceId <= 0) return NotFound();

        var parties = post.Parties
            .Where(p => p.Include)
            .Select((p, i) => new SigningPartyInput(p.PartyType, p.SourceRefId, (p.DisplayName ?? "").Trim(), p.Email?.Trim(), p.PhoneMasked, p.Capacity?.Trim(), i))
            .ToList();

        DateTime? expiresAt = post.ExpiresOn.HasValue
            ? post.ExpiresOn.Value.ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Local).ToUniversalTime()
            : null;

        string? error = null;
        if (parties.Count == 0) error = "Kies minstens één ondertekenaar.";
        else if (expiresAt is not null && expiresAt <= DateTime.UtcNow) error = "De vervaldatum moet in de toekomst liggen.";

        if (error is null)
        {
            var userId = User.GetCpmUserId() ?? 0;
            var created = await _signing.CreateCaseAsync(
                new CreateSigningCaseRequest(source.DocumentType, post.SourceId, parties, post.SigningRule, expiresAt, userId, User.GetCpmDisplayName()), Ctx(), ct);
            if (!created.Success) error = created.Error;
            else
            {
                var opened = await _signing.OpenCaseAsync(created.CaseId!.Value, Ctx(), ct);
                if (!opened.Success)
                {
                    // Het dossier bestaat (Draft) maar kon niet aangeboden worden: toon het dossier met de fout,
                    // dan kan de gebruiker annuleren of het opnieuw proberen i.p.v. een tweede dossier te maken.
                    AddMessage("error", opened.Error ?? "Het dossier kon niet aangeboden worden.", "Niet aangeboden");
                    return RedirectToAction(nameof(Dossier), new { id = created.CaseId });
                }
                AddMessage("success", "De uitnodigingen zijn verstuurd.", "Aangeboden ter ondertekening");
                return RedirectToAction(nameof(Dossier), new { id = created.CaseId });
            }
        }

        // Opnieuw tonen mét de ingevulde ondertekenaars (niet het voorstel van de bron).
        var vm = await BuildStartVmAsync(source, post.SourceId, post.BackUrl, ct);
        if (vm is null) return NotFound();
        vm.Parties = post.Parties;
        vm.SigningRule = post.SigningRule;
        if (post.ExpiresOn.HasValue) vm.ExpiresOn = post.ExpiresOn.Value;
        AddMessage("error", error ?? "Onbekende fout.", "Niet aangeboden");
        SetPageHeader("ph ph-signature", "Ter ondertekening aanbieden");
        ViewData["BackUrl"] = vm.BackUrl;
        ViewData["BreadcrumbNode"] = StartCrumb(vm);
        return View(vm);
    }

    /// <summary>Het document zoals het aangeboden zal worden, inline in de browser — de gebruiker ziet
    /// vóór het versturen exact wat de klant zal zien. Bouwt het pakket opnieuw op; bewaart niets.</summary>
    [HttpGet]
    public async Task<IActionResult> Preview(string documentType, int sourceId, CancellationToken ct)
    {
        var source = _registry.TrySource(documentType ?? "");
        if (source is null || sourceId <= 0) return NotFound();
        try
        {
            var package = await source.BuildAsync(sourceId, User.GetCpmUserId() ?? 0, ct);
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{package.FileName}\"";
            return File(package.Pdf, "application/pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Voorvertoning van {DocumentType}/{SourceId} mislukt.", documentType, sourceId);
            return StatusCode(500, "Het document kon niet opgebouwd worden.");
        }
    }

    private async Task<SigningStartVm?> BuildStartVmAsync(ISigningDocumentSource source, int sourceId, string? backUrl, CancellationToken ct)
    {
        SigningDocumentPackage package;
        try { package = await source.BuildAsync(sourceId, User.GetCpmUserId() ?? 0, ct); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Startscherm: pakket van {DocumentType}/{SourceId} kon niet opgebouwd worden.", source.DocumentType, sourceId);
            return null;
        }

        var policy = await _db.SigningPolicy.AsNoTracking().FirstOrDefaultAsync(p => p.DocumentType == source.DocumentType && p.IsActive, ct);
        var linkDays = policy?.LinkValidityDays ?? 30;

        var vm = new SigningStartVm
        {
            DocumentType = source.DocumentType,
            SourceId = sourceId,
            DocumentTypeLabel = source.DisplayName,
            Title = package.Title,
            DocumentNumber = package.DocumentNumber,
            Summary = package.Summary,
            AmountExclVat = package.AmountExclVat,
            VatAmount = package.VatAmount,
            AmountInclVat = package.AmountInclVat,
            ProjectId = package.ProjectId,
            ClientAccountId = package.ClientAccountId,
            Parties = package.SuggestedParties.OrderBy(p => p.SortOrder).Select(p => new SigningStartPartyVm
            {
                Include = true,
                PartyType = p.PartyType,
                SourceRefId = p.SourceRefId,
                DisplayName = p.DisplayName,
                Email = p.Email,
                PhoneMasked = p.PhoneMasked,
                Capacity = p.Capacity,
            }).ToList(),
            SigningRule = package.SuggestedRule ?? policy?.SigningRule ?? (int)SigningRule.All,
            ExpiresOn = DateOnly.FromDateTime(DateTime.Today.AddDays(linkDays)),
            LinkValidityDays = linkDays,
            OtpRequired = policy?.OtpRequired ?? true,
            VerificationMethodLabel = SigningLabels.VerificationMethod(policy?.VerificationMethod),
            ConsentText = policy?.ConsentText ?? "",
            IsTestMode = _options.IsTestMode,
            TestRecipient = _options.TestRecipientOverride,
            BackUrl = SafeLocal(backUrl) ?? (package.ProjectId is > 0 ? Url.Action("DetailsChangeOrder", "Projecten", new { projectid = package.ProjectId }) : Url.Action("Index")),
            PreviewUrl = Url.Action(nameof(Preview), new { documentType = source.DocumentType, sourceId }),
        };

        if (package.ProjectId is > 0)
        {
            var project = await ProjectHeaderAsync(package.ProjectId.Value, ct);
            vm.ProjectName = project?.Name;
            vm.IsCoordinationProject = project?.IsCoordination ?? false;
        }
        return vm;
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Dossier
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> Dossier(int id, CancellationToken ct)
    {
        var status = await _signing.GetCaseStatusAsync(id, ct);
        if (status is null) return NotFound();

        await _permissions.EnsureLoadedAsync(ct);
        var source = _registry.TrySource(status.DocumentType);
        var vm = new SigningDossierVm
        {
            Case = status,
            Events = await _signing.GetEventsAsync(id, ct),
            DocumentTypeLabel = source?.DisplayName ?? status.DocumentType,
            CanWrite = _permissions.HasWrite(PermissionCodes.Signing),
            IsTestMode = _options.IsTestMode,
            VerificationUrl = $"{(_options.PublicBaseUrl ?? "").TrimEnd('/')}/verifieer/{status.PublicVerificationId:D}",
        };
        if (status.ProjectId is > 0)
        {
            var project = await ProjectHeaderAsync(status.ProjectId.Value, ct);
            vm.ProjectName = project?.Name;
            vm.IsCoordinationProject = project?.IsCoordination ?? false;
        }
        // Terug naar de bron — voorlopig enkel wijzigingsopdrachten; een volgende bron voegt hier zijn eigen link toe.
        if (status.DocumentType == Services.Signing.ChangeOrderSigningSource.Key && status.ProjectId is > 0)
        {
            vm.SourceUrl = Url.Action("DetailsChangeOrder", "Projecten", new { projectid = status.ProjectId, clientid = status.ClientAccountId });
            vm.SourceLabel = "Wijzigingsopdrachten";
        }

        ViewData["BackUrl"] = vm.SourceUrl ?? Url.Action(nameof(Index), new { projectId = status.ProjectId });
        SetPageHeader("ph ph-signature", status.DocumentNumber is null ? vm.DocumentTypeLabel : $"{vm.DocumentTypeLabel} {status.DocumentNumber}");
        ViewData["TitleMobile"] = status.DocumentNumber ?? vm.DocumentTypeLabel;
        // DESIGN.md regel 2: de titel zegt wát het is, het kruimelpad wáár het zit.
        var listCrumb = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "SigningAdmin", "Ondertekeningen")
        {
            Parent = status.ProjectId is > 0 ? ProjectCrumb(status.ProjectId.Value, vm.ProjectName ?? "Project") : HomeCrumb(),
            RouteValues = new { projectId = status.ProjectId }
        };
        ViewData["BreadcrumbNode"] = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Dossier", "SigningAdmin", status.DocumentNumber ?? vm.DocumentTypeLabel)
        {
            Parent = listCrumb,
            RouteValues = new { id }
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remind(int caseId, int partyId, CancellationToken ct)
    {
        var r = await _signing.SendReminderAsync(partyId, Ctx(), ct);
        AddMessage(r.Success ? "success" : "error", r.Success ? "De herinnering is verstuurd." : r.Error ?? "Mislukt.", r.Success ? "Herinnering" : "Niet verstuurd");
        return RedirectToAction(nameof(Dossier), new { id = caseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Regenerate(int caseId, int partyId, CancellationToken ct)
    {
        var r = await _signing.RegenerateLinkAsync(partyId, Ctx(), ct);
        AddMessage(r.Success ? "success" : "error", r.Success ? "De oude link is ingetrokken en een nieuwe is verstuurd." : r.Error ?? "Mislukt.", r.Success ? "Nieuwe link" : "Niet verstuurd");
        return RedirectToAction(nameof(Dossier), new { id = caseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int caseId, string? reason, CancellationToken ct)
    {
        var r = await _signing.CancelCaseAsync(caseId, reason ?? "", Ctx(), ct);
        AddMessage(r.Success ? "success" : "error", r.Success ? "Het dossier is geannuleerd; de links werken niet meer." : r.Error ?? "Mislukt.", r.Success ? "Geannuleerd" : "Niet geannuleerd");
        return RedirectToAction(nameof(Dossier), new { id = caseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reopen(int caseId, CancellationToken ct)
    {
        // Een dossier dat als Draft bleef hangen (aanbieden mislukte) alsnog aanbieden.
        var r = await _signing.OpenCaseAsync(caseId, Ctx(), ct);
        AddMessage(r.Success ? "success" : "error", r.Success ? "De uitnodigingen zijn verstuurd." : r.Error ?? "Mislukt.", r.Success ? "Aangeboden" : "Niet aangeboden");
        return RedirectToAction(nameof(Dossier), new { id = caseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyChain(int caseId, CancellationToken ct)
    {
        var r = await _signing.VerifyAuditChainAsync(caseId, ct);
        AddMessage(r.Valid ? "success" : "error",
            r.Valid ? $"De hash-ketting van {r.EventCount} events is intact." : $"{r.Message} (eerste afwijking: event {r.FirstBrokenEventId}).",
            r.Valid ? "Audit trail intact" : "Audit trail NIET intact");
        return RedirectToAction(nameof(Dossier), new { id = caseId });
    }

    /// <summary>Downloads gaan altijd via de service (hash-controle + DocumentDownloaded-event). Kind = BOCore.SigningDocumentKind.</summary>
    [HttpGet]
    public async Task<IActionResult> Download(int caseId, int kind, int? documentId, bool inline, CancellationToken ct)
    {
        var doc = await _signing.GetDocumentAsync(caseId, kind, documentId, Ctx(), ct);
        if (doc is null) return NotFound();
        if (inline)
        {
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{doc.FileName}\"";
            return File(doc.Content, doc.ContentType);
        }
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Hulp
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    private async Task<(string Name, bool IsCoordination)?> ProjectHeaderAsync(int projectId, CancellationToken ct)
    {
        var p = await _db.Project.AsNoTracking().Where(x => x.ProjectId == projectId)
            .Select(x => new { x.ProjectName, x.IsCoordinationProject }).FirstOrDefaultAsync(ct);
        return p is null ? null : (p.ProjectName ?? "", p.IsCoordinationProject);
    }

    private string? SafeLocal(string? url) => !string.IsNullOrWhiteSpace(url) && Url.IsLocalUrl(url) ? url : null;

    // Kruimelpad — zelfde nodes als ProjectenController.DetailsChangeOrder, zodat "Home / Project /
    // Wijzigingsopdrachten / …" hier naadloos verder loopt.
    private static SmartBreadcrumbs.Nodes.MvcBreadcrumbNode HomeCrumb() => new("Index", "Home", "Home");

    private static SmartBreadcrumbs.Nodes.MvcBreadcrumbNode ProjectCrumb(int projectId, string name) =>
        new("Detail", "Projecten", name) { Parent = HomeCrumb(), RouteValues = new { projectid = projectId } };

    private SmartBreadcrumbs.Nodes.MvcBreadcrumbNode StartCrumb(SigningStartVm vm)
    {
        SmartBreadcrumbs.Nodes.MvcBreadcrumbNode parent;
        if (vm.ProjectId is > 0)
        {
            parent = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailsChangeOrder", "Projecten", "Wijzigingsopdrachten")
            {
                Parent = ProjectCrumb(vm.ProjectId.Value, vm.ProjectName ?? "Project"),
                RouteValues = new { projectid = vm.ProjectId }
            };
        }
        else parent = HomeCrumb();
        return new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Start", "SigningAdmin", "Ter ondertekening aanbieden")
        {
            Parent = parent,
            RouteValues = new { documentType = vm.DocumentType, sourceId = vm.SourceId }
        };
    }
}

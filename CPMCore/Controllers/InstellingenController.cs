using BOCore;
using CPMCore.Attributes;
using CPMCore.Models;
using CPMCore.Models.Home;
using CPMCore.Models.Instellingen;
using CPMCore.Models.Invoicing;
using CPMCore.Models.Projecten;
using FacadeCore;
using CPMCore.Services.Octopus;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Build.Definition;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using ServiceCore.Invoicing.Pdf.Templates;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using SystemTextJsonSerializer = System.Text.Json.JsonSerializer;

namespace CPMCore.Controllers;


[Authorize]
[CPMCore.Filters.PermissionRead(PermissionCodes.Settings)]
public partial class InstellingenController : BaseController
{
    private readonly ILogger<HomeController> _logger;
    private readonly IIssuerCompanyService _issuers;
    private readonly IIssuerBankAccountService _bank;
    private readonly IIssuerSeriesService _series;
    private readonly IInvoiceLayoutTemplateService _invoiceTemplates;
    private readonly IOctopusApiClient _octopusClient;
    private readonly IOctopusTokenManager _octopusTokens;
    private readonly IOctopusBookyearService _octopusBookyears;
    private readonly IOctopusRelationSyncService _octopusRelations;
    private readonly IActivityService _activityService;
    private readonly IProjectService _projectService;
    private readonly IKostprijsService _kostprijsService;
    private readonly IBudgetPrijsReferentieService _prijsReferenties;
    private readonly IBudgetReferentieProjectService _referentieProjecten;
    private readonly ServiceCore.Budget.BouwIndexService _bouwIndex;
    private readonly ServiceCore.Budget.SIndexScraperService _sIndexScraper;
    private readonly ServiceCore.Budget.I2021SyncService _i2021Sync;
    private readonly CPMCore.Services.IMarketDataStatusService _marketDataStatus;
    private readonly ServiceCore.Budget.BudgetActivityFormuleService _budgetFormules;

    private static readonly JsonSerializerOptions LayoutSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly string LayoutDefaultsJson =
        System.Text.Json.JsonSerializer.Serialize(new
        {
            layoutA = DefaultLayouts.LayoutA,
        layoutB = DefaultLayouts.LayoutB
    }, LayoutSerializerOptions);

    private static readonly string LayoutSchemaJson = LayoutSchemaProvider.GetSchemaJson();

    public InstellingenController(ILogger<HomeController> logger, IIssuerCompanyService issuers, IIssuerBankAccountService bank, IIssuerSeriesService series, IInvoiceLayoutTemplateService invoiceTemplates, IOctopusApiClient octopusClient, IOctopusTokenManager octopusTokens, IOctopusBookyearService octopusBookyears, IOctopusRelationSyncService octopusRelations, IActivityService activityService, IProjectService projectService, IKostprijsService kostprijsService, IBudgetPrijsReferentieService prijsReferenties, IBudgetReferentieProjectService referentieProjecten, ServiceCore.Budget.BouwIndexService bouwIndex, ServiceCore.Budget.SIndexScraperService sIndexScraper, ServiceCore.Budget.I2021SyncService i2021Sync, CPMCore.Services.IMarketDataStatusService marketDataStatus, ServiceCore.Budget.BudgetActivityFormuleService budgetFormules)
    {
        _logger = logger;
        _issuers = issuers;
        _bank = bank;
        _series = series;
        _invoiceTemplates = invoiceTemplates;
        _octopusClient = octopusClient;
        _octopusTokens = octopusTokens;
        _octopusBookyears = octopusBookyears;
        _octopusRelations = octopusRelations;
        _activityService = activityService;
        _projectService = projectService;
        _kostprijsService = kostprijsService;
        _prijsReferenties = prijsReferenties;
        _referentieProjecten = referentieProjecten;
        _bouwIndex = bouwIndex;
        _sIndexScraper = sIndexScraper;
        _i2021Sync = i2021Sync;
        _marketDataStatus = marketDataStatus;
        _budgetFormules = budgetFormules;
    }

    // Alle acties zijn opgesplitst in partials InstellingenController.<Groep>.cs met views in
    // Views/Instellingen/<Groep>/ (okt. 2026, structureren; zie STRUCTUREREN_VOORTGANG.md).
    // Hier blijven enkel de velden en de constructor over.
    // ===== Algemeen ===== InstellingenController.Algemeen.cs
    // ===== Facturatie ===== InstellingenController.Facturatie.cs
    // ===== Budget ===== InstellingenController.Budget.cs
    // ===== Marktdata ===== InstellingenController.Marktdata.cs
}
public class OctopusRelationLinkRequest
{
    public int IssuerId { get; set; }
    public bool IsSupplier { get; set; }
    public int CandidateId { get; set; }
    public int? OctopusRelationId { get; set; }
}

using BOCore;
using CPMCore.Helpers;
using CPMCore.Models;
using CPMCore.Models.Instellingen;
using CPMCore.Models.Klanten;
using CPMCore.Models.Projecten;
using FacadeCore;
using CPMCore.Services.Octopus;
using CPMCore.Services.Security;
using DALCore.Models;
using DinkToPdf;
using FacadeCore;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using NuGet.Configuration;
using ServiceCore;
using ServiceCore.Signing;
using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;
using static System.Net.Mime.MediaTypeNames;


namespace CPMCore.Controllers
{
    [Authorize]
    [CPMCore.Filters.PermissionRead(PermissionCodes.Customers)]
    public partial class KlantenController : BaseController
    {
        private static readonly JsonSerializerOptions VatLookupSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration Configuration;
        private readonly cpmRunningContext _db; // TODO: vervangen door service methoden
        private readonly IOctopusApiClient _octopusClient;
        private readonly IOctopusTokenManager _octopusTokens;
        private readonly IClientService _clientService;
        private readonly IUnitService _unitService;
        private readonly IProjectService _projectService;
        private readonly ICountryService _countryService;
        private readonly IActivityService _activityService;
        private readonly IContactService _contactService;
        private readonly IInvoiceQueryService _invoiceQueryService;
        private readonly DALCore.UnitOfWorkCore _uow;
        private const string CustomerCompanyPermissionPrefix = "Customers.Company.";

        public KlantenController(ILogger<HomeController> logger, IConfiguration configuration, cpmRunningContext db, IOctopusApiClient octopusClient, IOctopusTokenManager octopusTokens, IClientService clientService, IUnitService unitService, IProjectService projectService, ICountryService countryService, IActivityService activityService, IContactService contactService, IInvoiceQueryService invoiceQueryService, DALCore.UnitOfWorkCore uow)
        {
            _logger = logger;
            Configuration = configuration;
            _db = db;
            _octopusClient = octopusClient;
            _octopusTokens = octopusTokens;
            _clientService = clientService;
            _unitService = unitService;
            _projectService = projectService;
            _countryService = countryService;
            _activityService = activityService;
            _contactService = contactService;
            _invoiceQueryService = invoiceQueryService;
            _uow = uow;
        }

        // Alle acties zijn opgesplitst in partials KlantenController.<Groep>.cs met views in
        // Views/Klanten/<Groep>/ (okt. 2026, structureren; zie STRUCTUREREN_VOORTGANG.md).
        // Hier blijven enkel de velden en de constructor over.
        // ===== Core ===== KlantenController.Core.cs
        // ===== ClientAccount ===== KlantenController.ClientAccount.cs
        // ===== Lookups ===== KlantenController.Lookups.cs
    }
}

using BOCore;
using CPMCore.Helpers;
using CPMCore.Models.Leveranciers;
using CPMCore.Services;
using CPMCore.Services.Octopus;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace CPMCore.Controllers;

[Authorize]
[CPMCore.Filters.PermissionRead(PermissionCodes.Suppliers)]
public partial class LeveranciersController : BaseController
{
    private const string SupplierCompanyPermissionPrefix = "Suppliers.Company.";
    private static readonly JsonSerializerOptions VatLookupSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };
    private readonly cpmRunningContext _db;
    private readonly IOctopusApiClient _octopusClient;
    private readonly IOctopusTokenManager _octopusTokens;
    private readonly IContractorInviteService _contractorInviteService;
    private readonly IEntraGuestInvitationService _guestInvitationService;

    public LeveranciersController(
        cpmRunningContext db,
        IOctopusApiClient octopusClient,
        IOctopusTokenManager octopusTokens,
        IContractorInviteService contractorInviteService,
        IEntraGuestInvitationService guestInvitationService)
    {
        _db = db;
        _octopusClient = octopusClient;
        _octopusTokens = octopusTokens;
        _contractorInviteService = contractorInviteService;
        _guestInvitationService = guestInvitationService;
    }

    // Alle acties zijn opgesplitst in partials LeveranciersController.<Groep>.cs met views in
    // Views/Leveranciers/<Groep>/ (okt. 2026, structureren; zie STRUCTUREREN_VOORTGANG.md).
    // Hier blijven enkel de velden en de constructor over.
    // ===== Core ===== LeveranciersController.Core.cs
    // ===== Portal ===== LeveranciersController.Portal.cs
    // ===== Lookups ===== LeveranciersController.Lookups.cs
}

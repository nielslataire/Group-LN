using BOCore;
using CPMCore.Documents;
using CPMCore.Extensions;
using CPMCore.Models.Invoicing;
using CPMCore.Services;
using CPMCore.Services.Octopus;
using CPMCore.Services.Peppol;
using DALCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using QuestPDF.Fluent;
using ServiceCore;
using ServiceCore.Invoicing;
using ServiceCore.Invoicing.Pdf;
using SmartBreadcrumbs.Nodes;
using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;

namespace CPMCore.Controllers
{
    [Authorize]
    [CPMCore.Filters.PermissionRead(PermissionCodes.Invoicing)]
    public partial class InvoicesController : BaseController
    {
        private const string ControllerName = "Invoices";
        private const string InvoicingCompanyPermissionPrefix = "InvoicingByBillingCompany.Company.";
        private readonly IInvoiceQueryService _invoices;
        private readonly ICompanyQueryService _companies;
        private readonly ILogger<InvoicesController> _logger;
        private readonly IPartyLookupService _lookup;
        private readonly IInvoiceCommandService _cmd;
        private readonly IProjectSupplierLookupService _ps;
        private readonly IIssuerCompanyService _ics;
        private readonly IIssuerBankAccountService _bank;
        private readonly IInvoicePdfService _pdf;
        private readonly IInvoiceCommunicationService _communication;
        private readonly IInvoiceUblBuilder _ublBuilder;
        private readonly IEmailSender _emailSender;
        private readonly TemplateInterpolator _templateInterpolator;
        private readonly IPeppolDirectoryClient _peppolDirectory;
        private readonly IPeppolSender _peppolSender;
        private readonly IOctopusApiClient _octopusClient;
        private readonly IOctopusTokenManager _octopusTokens;
        private readonly UnitOfWorkCore _uow;
        private readonly cpmRunningContext _db;
        private readonly IDataProtector _downloadLinkProtector;
        private static readonly HashSet<string> RefreshableOctopusStates = new(StringComparer.OrdinalIgnoreCase)
        {
            string.Empty,
            "NONE",
            "ERROR",
            "PEPPOL_SENDING_IN_PROGRESS",
            "PEPPOL_SEND_FAILED"
        };
        private const string OctopusWorkflowStateCreated = "CREATED";
        private const string OctopusWorkflowStateAttachmentUploaded = "ATTACHMENT_UPLOADED";
        private const string OctopusWorkflowStateSent = "SENT";
        private static readonly Dictionary<string, int> OctopusWorkflowOrder = new(StringComparer.OrdinalIgnoreCase)
        {
            { OctopusWorkflowStateCreated, 1 },
            { OctopusWorkflowStateAttachmentUploaded, 2 },
            { OctopusWorkflowStateSent, 3 }
        };

        public InvoicesController(
            IInvoiceQueryService invoices,
            ICompanyQueryService companies,
            ILogger<InvoicesController> logger,
            IPartyLookupService lookup,
            IInvoiceCommandService cmd,
            IProjectSupplierLookupService ps,
            IIssuerCompanyService ics,
            IIssuerBankAccountService bank,
            IInvoicePdfService pdf,
            IInvoiceCommunicationService communication,
            IInvoiceUblBuilder ublBuilder,
            IEmailSender emailSender,
            TemplateInterpolator templateInterpolator,
            IPeppolDirectoryClient peppolDirectory,
            IPeppolSender peppolSender,
            IOctopusApiClient octopusClient,
            IOctopusTokenManager octopusTokens,
            IDataProtectionProvider dataProtectionProvider,
            UnitOfWorkCore uow)
        {
            _invoices = invoices;
            _companies = companies;
            _logger = logger;
            _lookup = lookup;
            _cmd = cmd;
            _ps = ps;
            _ics = ics;
            _bank = bank;
            _pdf = pdf;
            _communication = communication;
            _ublBuilder = ublBuilder;
            _emailSender = emailSender;
            _templateInterpolator = templateInterpolator;
            _peppolDirectory = peppolDirectory;
            _peppolSender = peppolSender;
            _octopusClient = octopusClient;
            _octopusTokens = octopusTokens;
            _uow = uow;
            _db = (cpmRunningContext)uow.Context;
            _downloadLinkProtector = dataProtectionProvider.CreateProtector("OctopusInvoiceDownload");
        }

        // Alle acties zijn opgesplitst in partials InvoicesController.<Groep>.cs met views in
        // Views/Invoices/<Groep>/ (okt. 2026, structureren; zie STRUCTUREREN_VOORTGANG.md).
        // Hier blijven enkel de velden en de constructor over.
        // ===== Core ===== InvoicesController.Core.cs
        // ===== Send ===== InvoicesController.Send.cs
        // ===== Editor ===== InvoicesController.Editor.cs
        // ===== Octopus ===== InvoicesController.Octopus.cs
        // ===== Helpers ===== InvoicesController.Helpers.cs
    }

    }

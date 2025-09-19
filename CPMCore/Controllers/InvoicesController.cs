using BOCore;
using CPMCore.Models.Invoicing;
using DALCore;
using FacadeCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using System.Linq;
using UblSharp.CommonAggregateComponents;

namespace CPMCore.Controllers
{
    public class InvoicesController : Controller

    {
        private readonly IInvoiceQueryService _invoices;
        private readonly ICompanyQueryService _companies;
        private readonly ILogger<InvoicesController> _logger;
        private readonly IPartyLookupService _lookup;
        private readonly IInvoiceCommandService _cmd;


        public InvoicesController(IInvoiceQueryService invoices, ICompanyQueryService companies, ILogger<InvoicesController> logger,
        IPartyLookupService lookup, IInvoiceCommandService cmd)
        {
            _invoices = invoices;
            _companies = companies;
            _logger = logger;
            _lookup = lookup;
            _cmd = cmd;
        }
        public async Task<IActionResult> Index(int issuerCompanyId)
        {
            var bos = await _invoices.GetByCompanyAsync(issuerCompanyId);
            var vms = bos.Select(x => new InvoiceListItemVM
            {
                Id = x.Id,
                PublicId = x.PublicId,
                ClientName = x.ClientName,
                InvoiceDate = x.InvoiceDate,
                Status = x.StatusName,
                GrossTotal = x.GrossTotal,
                Balance = x.Balance
            }).ToList();
            ViewBag.CompanyName = await _companies.GetIssuerNameAsync(issuerCompanyId);
            ViewBag.CompanyId = issuerCompanyId; 
            return View(vms);
        }

        // FACTUUR TOEVOEGEN
        // GET /Invoices/Create?issuerId=123
        [HttpGet]
        public async Task<IActionResult> Create(int? issuerId = null, CancellationToken ct = default)
        {
            var vm = new InvoiceCreateVM
            {
                IssuerCompanyId = issuerId ?? await _lookup.GetFirstActiveIssuerIdAsync(ct),
                Issuers = (await _lookup.ListActiveIssuersAsync(ct))
                            .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name })
            };
            return View(vm);
        }

        // AJAX for Select2
        // GET /Invoices/PartyLookup?term=abc&take=20
        [HttpGet]
        public async Task<IActionResult> PartyLookup(string? term, int take = 20, CancellationToken ct = default)
        {
            var rows = await _lookup.SearchPartiesAsync(term ?? "", take, ct);

            var results = rows.Select(x => new {
                id = x.Type switch
                {
                    InvoicePartyType.ClientAccount => $"ca:{x.Id}",
                    InvoicePartyType.ClientContact => $"cc:{x.Id}",
                    InvoicePartyType.Supplier => $"su:{x.Id}",
                    _ => $"x:{x.Id}"
                },
                text = x.Name,       // hier komt dus "Voornaam Naam" voor contacten
                type = x.Type.ToString()
            });

            return Json(new { results });
        }
        // POST /Invoices/CreateDraft
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDraft(InvoiceComposeVM vm, CancellationToken ct)
        {
            if (vm.IssuerCompanyId <= 0)
            {
                AddMessage("error", "Kies een facturatiebedrijf.", "Factuur");
                return RedirectToAction(nameof(Create), new { issuerId = vm.IssuerCompanyId });
            }
            if (vm.PartyType is null || vm.PartyId is null)
            {
                AddMessage("error", "Kies een klant of leverancier.", "Factuur");
                return RedirectToAction(nameof(Create), new { issuerId = vm.IssuerCompanyId });
            }

            try
            {
                var bo = new InvoiceDraftBO
                {
                    IssuerCompanyId = vm.IssuerCompanyId,
                    InvoiceDate = vm.InvoiceDate
                };

                // map partij
                switch (vm.PartyType.Value)
                {
                    case InvoicePartyType.ClientAccount:   // = 1
                        bo.ClientType = 1;
                        bo.ClientId = vm.PartyId;
                        bo.CompanyId = null;
                        break;

                    case InvoicePartyType.ClientContact:   // = 2
                        bo.ClientType = 2;
                        bo.ClientId = vm.PartyId;
                        bo.CompanyId = null;
                        break;

                    case InvoicePartyType.Supplier:        // = 3
                        bo.CompanyId = vm.PartyId;
                        bo.ClientType = null;
                        bo.ClientId = null;
                        break;

                    default:
                        AddMessage("error", "Onbekend partijtype.", "Factuur");
                        return RedirectToAction(nameof(Create), new { issuerId = vm.IssuerCompanyId });
                }

                var id = await _cmd.CreateDraftAsync(bo, ct);
                TempData["wantIssueNow"] = (vm.StartAs == StartStatus.Invoice);
                AddMessage("success", "Conceptfactuur aangemaakt.", "Factuur");
                return RedirectToAction(nameof(Edit), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateDraft failed");
                AddMessage("error", "Aanmaken mislukt: " + ex.Message, "Factuur");
                return RedirectToAction(nameof(Create), new { issuerId = vm.IssuerCompanyId });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(InvoiceComposeVM vm, CancellationToken ct)
        {
            if (vm.IssuerCompanyId <= 0 || vm.PartyId is null || vm.PartyType is null)
            {
                AddMessage("error", "Vul issuer en afnemer in.", "Factuur");
                return await Create(vm.IssuerCompanyId, ct: ct); // herladen met issuer
            }

            var bo = new InvoiceDraftBO
            {
                IssuerCompanyId = vm.IssuerCompanyId,
                InvoiceDate =vm.InvoiceDate,
                // ExpirationDate blijft null -> berekenen via default term
                Lines = vm.Lines.Select((l, idx) => new InvoiceLineBO
                {
                    SortOrder = l.SortOrder > 0 ? l.SortOrder : (idx + 1),
                    Description = l.Description,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    VatRate = l.VatRate,
                    Unit = l.Unit
                }).ToList()
            };

            if (vm.PartyType == InvoicePartyType.Supplier)
                (bo.CompanyId, bo.ClientType, bo.ClientId) = (vm.PartyId, null, null);
            else
                (bo.CompanyId, bo.ClientType, bo.ClientId) = (null, (int)vm.PartyType, vm.PartyId);

            var (id, publicId) = await _cmd.CreateWithLinesAsync(bo, issueNow: vm.StartAs == StartStatus.Invoice, ct);

            if (publicId != null)
                AddMessage("success", $"Factuur uitgegeven: {publicId}", "Factuur");
            else
                AddMessage("success", "Conceptfactuur opgeslagen.", "Factuur");

            // ❗️blijven op dezelfde pagina: laad opnieuw met defaults en toon melding
            return RedirectToAction(nameof(Create), new { issuerId = vm.IssuerCompanyId });
        }


        // FACTUUR BEWERKEN
        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            // heel tijdelijk: alleen de basis ophalen (we maken dit straks rijker)
            var inv = new InvoiceEditVM();
            //var inv = await _db.Invoices
            //    .AsNoTracking()
            //    .Where(i => i.Id == id)
            //    .Select(i => new InvoiceEditVM
            //    {
            //        InvoiceId = i.Id,
            //        IssuerCompanyId = i.IssuerCompanyId ?? 0,
            //        PublicId = i.PublicId,
            //        StatusId = i.StatusId,             // byte?
            //        InvoiceDate = i.Date,              // DateOnly
            //        ExpirationDate = i.ExpirationDate, // DateOnly?
            //        ClientType = i.ClientType,
            //        ClientId = i.ClientId,
            //        CompanyId = i.CompanyID
            //    })
            //    .FirstOrDefaultAsync(ct);

            if (inv == null)
            {
                AddMessage("error", "Factuur niet gevonden.", "Factuur");
                return RedirectToAction(nameof(Create));
            }

            // TODO stap 3: PaymentTerms/Series dropdowns, klant/leverancier display, totals, lijnen...
            return View(inv); // Views/Invoices/Edit.cshtml
        }


        // ========== helper ==========
        public void AddMessage(string messagetype, string message, string messagetitle)
        {
            TempData["Message"] = message;
            TempData["MessageType"] = messagetype;
            TempData["MessageTitle"] = messagetitle;
        }

    }
}

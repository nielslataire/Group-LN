using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Helpers;

namespace ServiceCore.Invoicing
{
    /// <summary>Zet de facturen klaar die bij de akte horen (zie <see cref="IAkteFactuurService"/>).
    /// Regels (Niels, 25/11/2026):
    ///  - Grondfactuur: grondwaarde (Units.LandValueSold) zonder btw, door het facturatiebedrijf van de grondeigenaar (Project.IssuerCompanyIdLandOwner).
    ///  - Kostenfactuur: aandeel basisakte, aandeel verkavelingsakte, landmetingskosten en de eigen kostenposten, met 21 % btw,
    ///    door dezelfde grondvennootschap. NIET: kosten overschrijving (notaris) en aansluitkosten (die komen als afrekening achteraf van de bouwvennootschap).
    ///  - Mede-eigenaars krijgen elk een factuur voor hun aandeel (zoals bij de schijven).
    ///  - Kosten staan per hoofdeenheid (woning, appartement, commercieel), niet per parking of berging.</summary>
    public class AkteFactuurService : IAkteFactuurService
    {
        public const string LineTypeGrond = "AkteGrond";
        public const string LineTypeKosten = "AkteKosten";

        private readonly cpmRunningContext _db;
        private readonly IInvoiceCommandService _cmd;

        public AkteFactuurService(cpmRunningContext db, IInvoiceCommandService cmd) { _db = db; _cmd = cmd; }

        public async Task<AkteFacturenPlan> BerekenAsync(int projectId, IReadOnlyCollection<int> clientAccountIds, CancellationToken ct = default)
        {
            var plan = new AkteFacturenPlan();
            if (clientAccountIds == null || clientAccountIds.Count == 0) return plan;

            var project = await _db.Project.AsNoTracking().FirstOrDefaultAsync(p => p.ProjectId == projectId, ct);
            if (project == null) { plan.Opmerkingen.Add("Het project werd niet gevonden."); return plan; }
            var settings = await _db.ProjectSalesSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Projectid == projectId, ct);
            var extra = await _db.ProjectSalesExtraCost.AsNoTracking().Where(c => c.ProjectId == projectId && c.Amount != 0).OrderBy(c => c.SortOrder).ToListAsync(ct);

            var issuerId = project.IssuerCompanyIdLandOwner;
            if (!issuerId.HasValue || issuerId.Value <= 0)
            {
                plan.Opmerkingen.Add("Geen facturatiebedrijf van de grondeigenaar ingesteld voor dit project (Projectgegevens > Facturatie).");
                return plan;
            }
            // Instellingen van de grondvennootschap (Instellingen > Bedrijven > Facturatie > Aktefacturen); leeg = standaard
            var issuer = await _db.IssuerCompany.AsNoTracking().FirstOrDefaultAsync(i => i.Id == issuerId.Value, ct);
            // kosten (akten, landmeter, eigen kosten): standaard 21 % btw, ongeacht het btw-stelsel van de bouw in de verkoopsinstellingen
            var vatPct = 21m;
            int? kostenVatTypeId = null;
            if (issuer?.AkteKostenVatTypeId is int kv)
            {
                var vt = await _db.Vattype.AsNoTracking().FirstOrDefaultAsync(v => v.Id == kv, ct);
                if (vt != null) { kostenVatTypeId = vt.Id; vatPct = vt.BasePercentage; }
            }
            // grond: standaard 0 %-btwcode van de grondvennootschap, als ze er een heeft (anders blijft het bij het percentage 0)
            var grondVatPct = 0m;
            int? nulBtwTypeId = null;
            if (issuer?.AkteGrondVatTypeId is int gv)
            {
                var vt = await _db.Vattype.AsNoTracking().FirstOrDefaultAsync(v => v.Id == gv, ct);
                if (vt != null) { nulBtwTypeId = vt.Id; grondVatPct = vt.BasePercentage; }
            }
            if (nulBtwTypeId == null)
                nulBtwTypeId = await _db.Vattype.AsNoTracking()
                    .Where(v => v.IssuerCompanyId == issuerId.Value && v.BasePercentage == 0m)
                    .OrderBy(v => v.Id).Select(v => (int?)v.Id).FirstOrDefaultAsync(ct);

            var units = await _db.Units.AsNoTracking().Include(u => u.Type)
                .Where(u => u.ProjectId == projectId && u.ClientAccountId != null && clientAccountIds.Contains(u.ClientAccountId.Value))
                .OrderBy(u => u.Name).ToListAsync(ct);
            var unitIds = units.Select(u => u.Id).ToList();

            // wat al klaarstaat (niet-geannuleerde facturen met een akte-lijn voor die eenheid)
            var cancelled = (byte)InvoiceStatusId.Cancelled;
            var gedaan = await (from d in _db.InvoicesDetails.AsNoTracking()
                                join h in _db.Invoices.AsNoTracking() on d.InvoiceId equals h.Id
                                where (d.LineType == LineTypeGrond || d.LineType == LineTypeKosten) && d.UnitId != null && unitIds.Contains(d.UnitId.Value) && h.StatusId != cancelled
                                select new { d.LineType, UnitId = d.UnitId!.Value }).ToListAsync(ct);
            var gedaanSet = gedaan.Select(x => (x.LineType, x.UnitId)).ToHashSet();

            foreach (var accountId in clientAccountIds.Distinct())
            {
                var account = await _db.ClientAccount.AsNoTracking().FirstOrDefaultAsync(c => c.Id == accountId, ct);
                if (account == null) continue;
                // Enkel tot en met de dag van de akte: zonder aktedatum of na de akte wordt niets meer klaargezet (ook niet voor oudere projecten)
                if (account.DateDeedOfSale == null || account.DateDeedOfSale.Value < DateOnly.FromDateTime(DateTime.Today))
                {
                    plan.Opmerkingen.Add($"{AccountNaam(account)}: " + (account.DateDeedOfSale == null ? "nog geen datum van de akte." : "de akte is al verleden."));
                    continue;
                }
                var mine = units.Where(u => u.ClientAccountId == accountId).ToList();
                if (mine.Count == 0) continue;

                var grondLijnen = new List<(Units Unit, string Naam, decimal Bedrag)>();
                var kostenLijnen = new List<(Units Unit, string Naam, string Tekst, decimal Bedrag)>();
                foreach (var u in mine)
                {
                    var naam = UnitNaam(u);
                    var grond = u.LandValueSold ?? 0m;
                    if (grond > 0m && !gedaanSet.Contains((LineTypeGrond, u.Id)))
                        grondLijnen.Add((u, naam, grond));

                    var isHoofd = u.Type?.GroupId == null || u.Type.GroupId == 1 || u.Type.GroupId == 4;
                    if (isHoofd && !gedaanSet.Contains((LineTypeKosten, u.Id)))
                    {
                        void Kost(decimal? bedrag, string tekst) { if ((bedrag ?? 0m) > 0m) kostenLijnen.Add((u, naam, tekst, bedrag!.Value)); }
                        Kost(settings?.BaseCertificateCost, "Aandeel kosten basisakte");
                        Kost(settings?.ParcelCost, "Aandeel kosten verkavelingsakte");
                        Kost(settings?.SurveyorCost, "Landmetingskosten");
                        foreach (var e in extra) Kost(e.Amount, string.IsNullOrWhiteSpace(e.Name) ? "Kosten" : e.Name.Trim());
                    }
                }

                // eigenaars: hoofdaccount + mede-eigenaars met hun aandeel
                var coowners = await _db.ClientContacts.AsNoTracking()
                    .Where(cc => cc.ClientAccountId == accountId && cc.IsCoOwner && cc.CoOwnerPercentage.HasValue && cc.CoOwnerPercentage > 0)
                    .Select(cc => new { cc.Id, cc.Name, cc.Forename, Pct = cc.CoOwnerPercentage!.Value }).ToListAsync(ct);
                var hoofdAandeel = Math.Max(0m, 100m - coowners.Sum(c => c.Pct));
                var eigenaars = new List<(bool Hoofd, int? ContactId, string Naam, decimal Pct)>();
                if (hoofdAandeel > 0m) eigenaars.Add((true, null, AccountNaam(account), hoofdAandeel));
                eigenaars.AddRange(coowners.Select(c => (false, (int?)c.Id, $"{c.Name} {c.Forename}".Trim(), c.Pct)));

                if (grondLijnen.Count == 0 && kostenLijnen.Count == 0)
                {
                    plan.Opmerkingen.Add($"{AccountNaam(account)}: niets meer klaar te zetten (reeds aangemaakt of geen bedragen ingesteld).");
                    continue;
                }

                foreach (var (hoofd, contactId, eigenaarNaam, pct) in eigenaars)
                {
                    if (grondLijnen.Count > 0)
                    {
                        var d = NieuwDraft(issuerId.Value, projectId, accountId, contactId);
                        d.PaymentTermId = issuer?.AkteGrondPaymentTermId;
                        d.HeaderDescription = $"Voor de aankoop van de grond van {string.Join(", ", grondLijnen.Select(g => g.Naam).Distinct())} in project {project.ProjectName} ingevolge verkoopsovereenkomst.";
                        d.SelectedVatTypeId = nulBtwTypeId;
                        foreach (var g in grondLijnen)
                            d.Lines.Add(new InvoiceLineBO
                            {
                                Text = pct < 100m ? $"Grondwaarde {g.Naam} ({pct:0.##} %)" : $"Grondwaarde {g.Naam}",
                                Price = Math.Round(g.Bedrag * pct / 100m, 2, MidpointRounding.AwayFromZero),
                                VatPercentage = grondVatPct, VatTypeId = nulBtwTypeId, LineType = LineTypeGrond, GroupName = g.Naam, UnitId = g.Unit.Id,
                            });
                        plan.Facturen.Add(new AkteFactuurVoorstel { ClientAccountId = accountId, Soort = "Grond", Eigenaar = eigenaarNaam, Aandeel = pct, IsHoofdeigenaar = hoofd, Draft = d, UnitIds = grondLijnen.Select(g => g.Unit.Id).Distinct().ToList() });
                    }
                    if (kostenLijnen.Count > 0)
                    {
                        var d = NieuwDraft(issuerId.Value, projectId, accountId, contactId);
                        d.PaymentTermId = issuer?.AkteKostenPaymentTermId;
                        d.SelectedVatTypeId = kostenVatTypeId;
                        d.HeaderDescription = $"Kosten van akte en landmeter voor {string.Join(", ", kostenLijnen.Select(k => k.Naam).Distinct())} in project {project.ProjectName} ingevolge verkoopsovereenkomst.";
                        foreach (var k in kostenLijnen)
                            d.Lines.Add(new InvoiceLineBO
                            {
                                Text = pct < 100m ? $"{k.Tekst} ({pct:0.##} %)" : k.Tekst,
                                Price = Math.Round(k.Bedrag * pct / 100m, 2, MidpointRounding.AwayFromZero),
                                VatPercentage = vatPct, VatTypeId = kostenVatTypeId, LineType = LineTypeKosten, GroupName = k.Naam, UnitId = k.Unit.Id,
                            });
                        plan.Facturen.Add(new AkteFactuurVoorstel { ClientAccountId = accountId, Soort = "Kosten", Eigenaar = eigenaarNaam, Aandeel = pct, IsHoofdeigenaar = hoofd, Draft = d, UnitIds = kostenLijnen.Select(k => k.Unit.Id).Distinct().ToList() });
                    }
                }
            }
            return plan;
        }

        public async Task<(int Aangemaakt, List<string> Opmerkingen)> AanmakenAsync(int projectId, IReadOnlyCollection<int> clientAccountIds, CancellationToken ct = default)
        {
            var plan = await BerekenAsync(projectId, clientAccountIds, ct);
            var opm = new List<string>(plan.Opmerkingen);
            var aantal = 0;
            foreach (var f in plan.Facturen)
            {
                try
                {
                    var (invoiceId, _) = await _cmd.CreateWithLinesAsync(f.Draft, issueNow: false, ct);
                    aantal++;
                    if (f.Soort == "Grond" && f.IsHoofdeigenaar)
                    {
                        var ids = f.UnitIds;
                        var units = await _db.Units.Where(u => ids.Contains(u.Id)).ToListAsync(ct);
                        foreach (var u in units) u.LandValueInvoiceId = invoiceId;
                        await _db.SaveChangesAsync(ct);
                    }
                }
                catch (Exception ex)
                {
                    opm.Add($"{f.Soort}factuur voor {f.Eigenaar}: {ex.Message}");
                }
            }
            return (aantal, opm);
        }

        private static InvoiceDraftBO NieuwDraft(int issuerId, int projectId, int accountId, int? contactId)
        {
            var d = new InvoiceDraftBO
            {
                IssuerCompanyId = issuerId,
                InvoiceDate = DateOnly.FromDateTime(DateTime.Today),
                Mode = InvoiceMode.Free,
                ProjectId = projectId,
            };
            // 1 = klantenaccount, 2 = mede-eigenaar/contactpersoon (zelfde codes als InvoicePartyType)
            if (contactId.HasValue) { d.ClientType = 2; d.ClientId = contactId; } else { d.ClientType = 1; d.ClientId = accountId; }
            d.CompanyId = null;
            d.SkipPartyExtraInfo = true;   // geen extra vermelding van de klant (ClientAccount.InvoiceExtra) op grond- en kostenfacturen (Niels, 26/11/2026)
            return d;
        }

        private static string UnitNaam(Units u) => u.Type != null ? $"{u.Type.Name} {u.Name}".Trim() : (u.Name ?? "");
        private static string AccountNaam(ClientAccount a) => (a.Name ?? "").Trim();
    }
}

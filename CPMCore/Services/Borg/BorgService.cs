using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Services.Borg;

/// <summary>Borgen en extra inhoudingen op inkomende facturen (Nacalculatie, design-handoff 42h–42l).
/// Eén plaats voor alle berekeningen, zodat het overzicht (42i), de contractstrook (42b) en het factuurformulier (42c)
/// dezelfde cijfers tonen.
///
/// Borg via inhouding (financiële waarborg): per factuur wordt een bedrag ingehouden (<see cref="IncommingInvoices.BorgBedrag"/>);
/// facturen van vóór deze module (BorgInhouden = NULL) rekenen nog met het contractpercentage, zoals voorheen.
/// Een vrijgegeven extra inhouding laat het borgdeel in de borg. Bankwaarborg: er wordt niets ingehouden, het bedrag is
/// percentage × contractbedrag en de vrijgave gebeurt via een brief bij de definitieve oplevering.</summary>
public class BorgService
{
    private readonly cpmRunningContext _db;
    private readonly IProjectService _projects;

    public BorgService(cpmRunningContext db, IProjectService projects)
    {
        _db = db;
        _projects = projects;
    }

    public const decimal StandaardVoPercentage = 50m;

    // ── Overzicht per contract (42i) ──────────────────────────────────────────────────────────────

    public async Task<BorgOverzicht> GetOverzichtAsync(int projectId)
    {
        var project = await _db.Project.AsNoTracking().Where(p => p.ProjectId == projectId)
            .Select(p => new { p.DeliveryDate, p.DeliveryDateDef }).FirstOrDefaultAsync();
        var vandaag = DateOnly.FromDateTime(DateTime.Today);
        var vo = project?.DeliveryDate;
        var doDatum = project?.DeliveryDateDef;

        var contracten = (_projects.GetProjectContracts(projectId).Values ?? new List<ContractBO>())
            .Where(c => c.GuaranteeType != ContractGuaranteeType.NoGuarantee).ToList();
        var contractIds = contracten.Select(c => c.Id).ToList();
        var facturen = (_projects.GetProjectIncommingInvoicesForRecalculation(projectId).Values ?? new List<IncommingInvoiceActivityBO>())
            .Where(f => f.ContractId > 0 && contractIds.Contains(f.ContractId)).ToList();
        var factuurIds = facturen.Select(f => f.InvoiceId).Distinct().ToList();

        var factuurRijen = await _db.IncommingInvoices.AsNoTracking().Where(i => factuurIds.Contains(i.Id))
            .Select(i => new { i.Id, i.BorgInhouden, i.BorgBedrag, i.InvoiceKind }).ToDictionaryAsync(i => i.Id);
        var afhandelingBorg = await _db.IncommingInvoiceInhoudingAfhandeling.AsNoTracking()
            .Where(a => factuurIds.Contains(a.Inhouding.IncommingInvoiceId))
            .GroupBy(a => a.Inhouding.IncommingInvoiceId).Select(g => new { InvoiceId = g.Key, Borg = g.Sum(a => a.BorgBedrag) })
            .ToDictionaryAsync(g => g.InvoiceId, g => g.Borg);
        var vrijgaven = await _db.ContractBorgVrijgave.AsNoTracking().Where(v => v.ContractId != null && contractIds.Contains(v.ContractId.Value))
            .OrderBy(v => v.Datum).ToListAsync();
        // Borg op facturen zonder contract (aangezet in het factuurformulier): een rij per leverancier.
        var zcFacturen = await _db.IncommingInvoices.AsNoTracking()
            .Where(i => i.ProjectId == projectId && i.ContractId == null && i.CompanyId != null && i.BorgInhouden == true)
            .Select(i => new { i.Id, CompanyId = i.CompanyId!.Value, i.BorgBedrag, i.BorgVoPercentage, i.InvoiceKind }).ToListAsync();
        var zcBedrijven = zcFacturen.Select(f => f.CompanyId).Distinct().ToList();
        var zcNamen = await _db.CompanyInfo.AsNoTracking().Where(c => zcBedrijven.Contains(c.CompanyId)).Select(c => new { c.CompanyId, Name = c.BedrijfsNaam }).ToDictionaryAsync(c => c.CompanyId, c => c.Name);
        var zcVrijgaven = await _db.ContractBorgVrijgave.AsNoTracking().Where(v => v.ContractId == null && v.ProjectId == projectId && v.CompanyId != null)
            .OrderBy(v => v.Datum).ToListAsync();
        var companyIds = contracten.Select(c => c.Company?.ID ?? 0).Where(i => i > 0).Concat(zcBedrijven).Distinct().ToList();
        var geslotenStatussen = new[] { 4, 5, 6, 8 }; // Resolved, Closed, Rejected, Concept
        var openPuntenPerBedrijf = (await _db.ConstructionIssue.AsNoTracking()
            .Where(i => i.ProjectId == projectId && i.ResponsiblePartyId != null && companyIds.Contains(i.ResponsiblePartyId.Value) && !geslotenStatussen.Contains(i.Status))
            .GroupBy(i => i.ResponsiblePartyId.Value).Select(g => new { g.Key, N = g.Count() }).ToListAsync())
            .ToDictionary(g => g.Key, g => g.N);

        var rijen = new List<BorgRij>();
        foreach (var c in contracten)
        {
            var pct = c.GuaranteePercentage ?? 0m;
            var isBank = c.GuaranteeType == ContractGuaranteeType.BankGuarantee;
            var contractBedrag = c.Activities?.Sum(a => a.EffectivePrice) ?? 0m;
            decimal ingehouden;
            if (isBank)
                ingehouden = Math.Round(contractBedrag * pct / 100m, 2);
            else
            {
                ingehouden = 0m;
                foreach (var grp in facturen.Where(f => f.ContractId == c.Id).GroupBy(f => f.InvoiceId))
                {
                    var factuur = factuurRijen.GetValueOrDefault(grp.Key);
                    var teken = factuur?.InvoiceKind == (byte)IncommingInvoiceKind.Creditnota ? -1m : 1m;
                    if (factuur?.BorgInhouden == true) ingehouden += teken * (factuur.BorgBedrag ?? 0m);
                    else if (factuur?.BorgInhouden != false)
                        ingehouden += Math.Round(grp.Where(f => f.IncommingInvoiceType == IncommingInvoiceType.Contract || f.IncommingInvoiceType == IncommingInvoiceType.Meerwerk).Sum(f => f.SignedPrice) * pct / 100m, 2);
                    ingehouden += afhandelingBorg.GetValueOrDefault(grp.Key);
                }
            }

            var vrij = vrijgaven.Where(v => v.ContractId == c.Id).ToList();
            var vrijgegeven = vrij.Sum(v => v.Bedrag);
            var voPct = c.GuaranteeReleaseVOPercentageEffective;
            var voBereikt = vo.HasValue && vo.Value <= vandaag;
            var doBereikt = doDatum.HasValue && doDatum.Value <= vandaag;
            var gerechtigdPct = isBank ? 0m : (doBereikt ? 100m : voBereikt ? voPct : 0m);
            var nuVrij = Math.Max(0m, Math.Round(ingehouden * gerechtigdPct / 100m, 2) - vrijgegeven);
            var open = ingehouden - vrijgegeven;
            var openPunten = openPuntenPerBedrijf.GetValueOrDefault(c.Company?.ID ?? 0);

            string status, tone;
            if (isBank) { status = "Bankwaarborg"; tone = "is-neutral"; }
            else if (ingehouden > 0 && open <= 0.005m) { status = "Vrijgegeven"; tone = "is-positive"; }
            else if (nuVrij > 0 && openPunten > 0) { status = "Open punten"; tone = "is-attention"; }
            else if (nuVrij > 0) { status = "Vrij te geven"; tone = "is-positive"; }
            else if (!voBereikt && !doBereikt) { status = "Wacht op VO"; tone = "is-neutral"; }
            else { status = "Wacht op DO"; tone = "is-neutral"; }

            rijen.Add(new BorgRij
            {
                ContractId = c.Id,
                CompanyId = c.Company?.ID ?? 0,
                CompanyNaam = c.Company?.Display ?? "",
                ContractLabel = string.IsNullOrWhiteSpace(c.ContractName) ? $"Contract #{c.Id}" : c.ContractName,
                Activiteiten = string.Join(", ", (c.Activities ?? new()).Select(a => a.Activity?.Group?.Name).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct()),
                IsBankwaarborg = isBank,
                Percentage = isBank ? null : pct,
                Ingehouden = ingehouden,
                Vrijgegeven = vrijgegeven,
                VoPercentage = voPct,
                VoBereikt = voBereikt,
                DoBereikt = doBereikt,
                NuVrij = isBank ? 0m : nuVrij,
                OpenPunten = openPunten,
                Status = status,
                StatusTone = tone,
                Vrijgaves = vrij.Select(v => new BorgVrijgaveRij { Id = v.Id, Fase = v.Fase, Datum = v.Datum, Bedrag = v.Bedrag, Opmerking = v.Opmerking }).ToList(),
            });
        }

        var voBereiktZc = vo.HasValue && vo.Value <= vandaag;
        var doBereiktZc = doDatum.HasValue && doDatum.Value <= vandaag;
        foreach (var grp in zcFacturen.GroupBy(f => f.CompanyId))
        {
            decimal Teken(byte? kind) => kind == (byte)IncommingInvoiceKind.Creditnota ? -1m : 1m;
            var ingehouden = grp.Sum(f => Teken(f.InvoiceKind) * (f.BorgBedrag ?? 0m));
            var gerechtigd = grp.Sum(f =>
            {
                var voDeel = f.BorgVoPercentage ?? 100m;
                var nu = doBereiktZc ? 100m : voBereiktZc ? voDeel : 0m;
                return Teken(f.InvoiceKind) * (f.BorgBedrag ?? 0m) * nu / 100m;
            });
            var vrij = zcVrijgaven.Where(v => v.CompanyId == grp.Key).ToList();
            var vrijgegeven = vrij.Sum(v => v.Bedrag);
            var nuVrij = Math.Max(0m, Math.Round(gerechtigd, 2) - vrijgegeven);
            var open = ingehouden - vrijgegeven;
            var openPunten = openPuntenPerBedrijf.GetValueOrDefault(grp.Key);
            var voWaarden = grp.Select(f => f.BorgVoPercentage ?? 100m).Distinct().ToList();

            string status, tone;
            if (ingehouden > 0 && open <= 0.005m) { status = "Vrijgegeven"; tone = "is-positive"; }
            else if (nuVrij > 0 && openPunten > 0) { status = "Open punten"; tone = "is-attention"; }
            else if (nuVrij > 0) { status = "Vrij te geven"; tone = "is-positive"; }
            else if (!voBereiktZc && !doBereiktZc) { status = "Wacht op VO"; tone = "is-neutral"; }
            else { status = "Wacht op DO"; tone = "is-neutral"; }

            rijen.Add(new BorgRij
            {
                ContractId = 0,
                ZonderContract = true,
                CompanyId = grp.Key,
                CompanyNaam = zcNamen.GetValueOrDefault(grp.Key, ""),
                ContractLabel = "Zonder contract",
                Activiteiten = "",
                IsBankwaarborg = false,
                Percentage = null,
                Ingehouden = ingehouden,
                Vrijgegeven = vrijgegeven,
                VoPercentage = voWaarden.Count == 1 ? voWaarden[0] : 100m,
                GemengdeRegel = voWaarden.Count > 1,
                VoBereikt = voBereiktZc,
                DoBereikt = doBereiktZc,
                NuVrij = nuVrij,
                OpenPunten = openPunten,
                Status = status,
                StatusTone = tone,
                Vrijgaves = vrij.Select(v => new BorgVrijgaveRij { Id = v.Id, Fase = v.Fase, Datum = v.Datum, Bedrag = v.Bedrag, Opmerking = v.Opmerking }).ToList(),
            });
        }

        var inhoudingen = await GetInhoudingenAsync(projectId);
        return new BorgOverzicht
        {
            ProjectId = projectId,
            VoDatum = vo,
            DoDatum = doDatum,
            Rijen = rijen.OrderBy(r => r.CompanyNaam).ToList(),
            Inhoudingen = inhoudingen,
        };
    }

    // ── Extra inhoudingen (42j) ───────────────────────────────────────────────────────────────────

    public async Task<List<InhoudingRij>> GetInhoudingenAsync(int projectId, int? invoiceId = null)
    {
        var q = _db.IncommingInvoiceInhouding.AsNoTracking().Include(i => i.Afhandelingen).Include(i => i.IncommingInvoice)
            .Where(i => i.IncommingInvoice.ProjectId == projectId);
        if (invoiceId.HasValue) q = q.Where(i => i.IncommingInvoiceId == invoiceId.Value);
        var lijst = await q.OrderByDescending(i => i.CreatedAt).ToListAsync();

        var contractIds = lijst.Select(i => i.IncommingInvoice.ContractId).Where(i => i.HasValue).Select(i => i!.Value).Distinct().ToList();
        var contractInfo = await _db.Contract.AsNoTracking().Where(c => contractIds.Contains(c.Id)).Select(c => new { c.Id, c.CompanyId, c.GuaranteeType, c.GuaranteePercentage }).ToDictionaryAsync(c => c.Id);
        var contractBedrijf = contractInfo.ToDictionary(c => c.Key, c => (int?)c.Value.CompanyId);
        var bedrijfIds = lijst.Select(i => i.IncommingInvoice.CompanyId ?? (i.IncommingInvoice.ContractId.HasValue ? contractBedrijf.GetValueOrDefault(i.IncommingInvoice.ContractId.Value) : null))
            .Where(i => i.HasValue).Select(i => i!.Value).Distinct().ToList();
        var namen = await _db.CompanyInfo.AsNoTracking().Where(c => bedrijfIds.Contains(c.CompanyId)).Select(c => new { c.CompanyId, Name = c.BedrijfsNaam }).ToDictionaryAsync(c => c.CompanyId, c => c.Name);

        var cnIds = lijst.SelectMany(i => i.Afhandelingen).Where(a => a.CreditnotaInvoiceId.HasValue).Select(a => a.CreditnotaInvoiceId!.Value).Distinct().ToList();
        var cnNummers = await _db.IncommingInvoices.AsNoTracking().Where(i => cnIds.Contains(i.Id)).Select(i => new { i.Id, i.ExternalId }).ToDictionaryAsync(i => i.Id, i => i.ExternalId);

        return lijst.Select(i =>
        {
            var inv = i.IncommingInvoice;
            var bedrijfId = inv.CompanyId ?? (inv.ContractId.HasValue ? contractBedrijf.GetValueOrDefault(inv.ContractId.Value) : null);
            var afgehandeld = i.Afhandelingen.Sum(a => a.Bedrag);
            var open = i.Bedrag - afgehandeld;
            string status = afgehandeld <= 0 ? "Open" : open > 0.005m ? "Deels open"
                : i.Afhandelingen.OrderByDescending(a => a.Datum).First().Soort switch { 2 => "Verrekend", 3 => "Afgeboekt", _ => "Vrijgegeven" };
            return new InhoudingRij
            {
                Id = i.Id,
                InvoiceId = inv.Id,
                ContractId = inv.ContractId,
                BorgPercentage = inv.ContractId.HasValue && contractInfo.TryGetValue(inv.ContractId.Value, out var ci) && ci.GuaranteeType == (int)ContractGuaranteeType.FinancialGuarantee ? ci.GuaranteePercentage : null,
                CompanyId = bedrijfId,
                CompanyNaam = bedrijfId.HasValue ? namen.GetValueOrDefault(bedrijfId.Value, "") : (inv.SupplierName ?? ""),
                FactuurNummer = inv.ExternalId ?? $"#{inv.Id}",
                FactuurDatum = inv.Date,
                Reden = i.Reden,
                Toelichting = i.Toelichting,
                Bedrag = i.Bedrag,
                Afgehandeld = afgehandeld,
                Open = Math.Max(0m, open),
                Status = status,
                Afhandelingen = i.Afhandelingen.OrderBy(a => a.Datum).ThenBy(a => a.Id).Select(a => new InhoudingAfhandelingRij
                {
                    Id = a.Id, Datum = a.Datum, Soort = a.Soort, Bedrag = a.Bedrag, BorgBedrag = a.BorgBedrag,
                    CreditnotaInvoiceId = a.CreditnotaInvoiceId, Opmerking = a.Opmerking,
                    CreditnotaNummer = a.CreditnotaInvoiceId.HasValue ? cnNummers.GetValueOrDefault(a.CreditnotaInvoiceId.Value) : null,
                }).ToList(),
            };
        }).ToList();
    }

    public static string RedenTekst(byte reden) => reden switch
    {
        IncommingInvoiceInhouding.RedenCreditnota => "Creditnota te ontvangen",
        IncommingInvoiceInhouding.RedenWerkNietVolledig => "Werk niet volledig uitgevoerd",
        _ => "Andere",
    };

    /// <summary>Creditnota's van dit bedrijf in dit project die nog aan geen enkele afhandeling hangen (42l).</summary>
    public async Task<List<CreditnotaOptie>> GetVrijeCreditnotasAsync(int projectId, int companyId)
    {
        var gebruikt = await _db.IncommingInvoiceInhoudingAfhandeling.AsNoTracking().Where(a => a.CreditnotaInvoiceId != null).Select(a => a.CreditnotaInvoiceId!.Value).ToListAsync();
        var contractIds = await _db.Contract.AsNoTracking().Where(c => c.ProjectId == projectId && c.CompanyId == companyId).Select(c => c.Id).ToListAsync();
        var lijst = await _db.IncommingInvoices.AsNoTracking()
            .Where(i => i.ProjectId == projectId && i.InvoiceKind == (byte)IncommingInvoiceKind.Creditnota && !gebruikt.Contains(i.Id)
                && (i.CompanyId == companyId || (i.ContractId != null && contractIds.Contains(i.ContractId.Value))))
            .OrderByDescending(i => i.Date).Select(i => new CreditnotaOptie { InvoiceId = i.Id, Nummer = i.ExternalId, Datum = i.Date, Bedrag = Math.Abs(i.Price) }).ToListAsync();
        return lijst;
    }

    // ── Schrijven ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>Een extra inhouding (deels) afhandelen. Geeft een foutboodschap terug, of null bij succes.</summary>
    public async Task<string?> AfhandelenAsync(int inhoudingId, byte soort, DateOnly datum, decimal bedrag, int? creditnotaInvoiceId, string? opmerking, string? userId)
    {
        var inh = await _db.IncommingInvoiceInhouding.Include(i => i.Afhandelingen).Include(i => i.IncommingInvoice).FirstOrDefaultAsync(i => i.Id == inhoudingId);
        if (inh == null) return "Die inhouding bestaat niet meer.";
        bedrag = Math.Round(bedrag, 2);
        var open = inh.Bedrag - inh.Afhandelingen.Sum(a => a.Bedrag);
        if (bedrag <= 0) return "Geef een bedrag groter dan 0 op.";
        if (bedrag > open + 0.005m) return $"Je kan niet meer afhandelen dan er open staat (€ {open:N2}).";
        if (soort < 1 || soort > 3) return "Onbekende afhandeling.";

        decimal borg = 0m;
        if (soort == IncommingInvoiceInhoudingAfhandeling.SoortVrijgegeven && inh.IncommingInvoice.ContractId.HasValue)
        {
            var c = await _db.Contract.AsNoTracking().Where(x => x.Id == inh.IncommingInvoice.ContractId.Value).Select(x => new { x.GuaranteeType, x.GuaranteePercentage }).FirstOrDefaultAsync();
            if (c != null && c.GuaranteeType == (int)ContractGuaranteeType.FinancialGuarantee && c.GuaranteePercentage.HasValue)
                borg = Math.Round(bedrag * c.GuaranteePercentage.Value / 100m, 2);
        }
        if (soort == IncommingInvoiceInhoudingAfhandeling.SoortVerrekend)
        {
            if (!creditnotaInvoiceId.HasValue) return "Kies de creditnota die je verrekent.";
            var cn = await _db.IncommingInvoices.AsNoTracking().FirstOrDefaultAsync(i => i.Id == creditnotaInvoiceId.Value);
            if (cn == null || cn.InvoiceKind != (byte)IncommingInvoiceKind.Creditnota) return "Dat is geen creditnota.";
            if (await _db.IncommingInvoiceInhoudingAfhandeling.AnyAsync(a => a.CreditnotaInvoiceId == creditnotaInvoiceId.Value)) return "Die creditnota is al verrekend.";
            if (bedrag > Math.Abs(cn.Price) + 0.005m) return $"De creditnota is maar € {Math.Abs(cn.Price):N2}.";
        }
        else creditnotaInvoiceId = null;

        _db.IncommingInvoiceInhoudingAfhandeling.Add(new IncommingInvoiceInhoudingAfhandeling
        {
            InhoudingId = inhoudingId, Datum = datum, Soort = soort, Bedrag = bedrag, BorgBedrag = borg,
            CreditnotaInvoiceId = creditnotaInvoiceId, Opmerking = string.IsNullOrWhiteSpace(opmerking) ? null : opmerking.Trim(),
            CreatedAt = DateTime.UtcNow, CreatedBy = userId,
        });
        await _db.SaveChangesAsync();
        return null;
    }

    /// <summary>De afhandelstap uit het venster in een keer uitvoeren: vrijgeven, of verrekenen met een creditnota
    /// met daarna het restant vrijgeven / open laten / afboeken (design-handoff 42j/42l). Null = gelukt, anders de foutboodschap.</summary>
    public async Task<string?> AfhandelenStapAsync(int inhoudingId, string modus, decimal bedragVrij, int? creditnotaInvoiceId, decimal bedragVerreken, string restActie, string? opmerking, string? userId)
    {
        var vandaag = DateOnly.FromDateTime(DateTime.Today);
        if (modus == "vrij")
            return await AfhandelenAsync(inhoudingId, IncommingInvoiceInhoudingAfhandeling.SoortVrijgegeven, vandaag, bedragVrij, null, opmerking, userId);

        if (modus != "verreken") return "Onbekende afhandeling.";
        var fout = await AfhandelenAsync(inhoudingId, IncommingInvoiceInhoudingAfhandeling.SoortVerrekend, vandaag, bedragVerreken, creditnotaInvoiceId, opmerking, userId);
        if (fout != null) return fout;
        var inh = await _db.IncommingInvoiceInhouding.AsNoTracking().Include(i => i.Afhandelingen).FirstAsync(i => i.Id == inhoudingId);
        var rest = Math.Round(inh.Bedrag - inh.Afhandelingen.Sum(a => a.Bedrag), 2);
        if (rest <= 0.005m || restActie == "open") return null;
        var soort = restActie == "afboeken" ? IncommingInvoiceInhoudingAfhandeling.SoortAfgeboekt : IncommingInvoiceInhoudingAfhandeling.SoortVrijgegeven;
        return await AfhandelenAsync(inhoudingId, soort, vandaag, rest, null, opmerking, userId);
    }

    /// <summary>Een foute borgvrijgave intrekken: ze telt niet meer mee, de rij blijft als spoor staan. Null = gelukt.</summary>
    public async Task<string?> TrekVrijgaveInAsync(int projectId, int vrijgaveId, string? userId)
    {
        var v = await _db.ContractBorgVrijgave.Include(x => x.Contract).FirstOrDefaultAsync(x => x.Id == vrijgaveId);
        if (v == null) return "Die vrijgave bestaat niet meer of is al ingetrokken.";
        var hoortBij = v.ContractId.HasValue ? v.Contract != null && v.Contract.ProjectId == projectId : v.ProjectId == projectId;
        if (!hoortBij) return "Die vrijgave hoort niet bij dit project.";
        v.IngetrokkenOp = DateTime.UtcNow; v.IngetrokkenDoor = userId;
        await _db.SaveChangesAsync();
        return null;
    }

    /// <summary>Een foute afhandeling van een extra inhouding intrekken (de creditnota is dan weer vrij). Null = gelukt.</summary>
    public async Task<string?> TrekAfhandelingInAsync(int projectId, int afhandelingId, string? userId)
    {
        var a = await _db.IncommingInvoiceInhoudingAfhandeling.Include(x => x.Inhouding).ThenInclude(i => i.IncommingInvoice).FirstOrDefaultAsync(x => x.Id == afhandelingId);
        if (a == null) return "Die afhandeling bestaat niet meer of is al ingetrokken.";
        if (a.Inhouding?.IncommingInvoice?.ProjectId != projectId) return "Die afhandeling hoort niet bij dit project.";
        a.IngetrokkenOp = DateTime.UtcNow; a.IngetrokkenDoor = userId;
        await _db.SaveChangesAsync();
        return null;
    }

    /// <summary>Borg vrijgeven voor de gekozen rijen (sleutel "c{contractId}" of "l{leverancierId}" voor borg zonder contract),
    /// telkens het opgegeven bedrag (leeg = alles wat nu vrij te geven is), nooit meer dan nu vrij te geven is.</summary>
    public async Task<(int Aantal, decimal Totaal)> VrijgevenAsync(int projectId, Dictionary<string, decimal?> keuzes, string? userId)
    {
        var overzicht = await GetOverzichtAsync(projectId);
        var vandaag = DateOnly.FromDateTime(DateTime.Today);
        int aantal = 0; decimal totaal = 0m;
        foreach (var (sleutel, gevraagd) in keuzes)
        {
            var rij = overzicht.Rijen.FirstOrDefault(r => r.Sleutel == sleutel);
            if (rij == null || rij.NuVrij <= 0) continue;
            var bedrag = Math.Round(Math.Min(gevraagd ?? rij.NuVrij, rij.NuVrij), 2);
            if (bedrag <= 0) continue;
            var volledig = bedrag >= rij.NuVrij - 0.005m;
            _db.ContractBorgVrijgave.Add(new ContractBorgVrijgave
            {
                ContractId = rij.ZonderContract ? null : rij.ContractId,
                CompanyId = rij.ZonderContract ? rij.CompanyId : null,
                ProjectId = rij.ZonderContract ? projectId : null,
                // Een deelvrijgave is geen "fase": die krijgt Fase 3 (andere); een volledige vrijgave volgt de oplevering.
                Fase = !volledig ? ContractBorgVrijgave.FaseAndere : rij.DoBereikt ? ContractBorgVrijgave.FaseDefinitief : ContractBorgVrijgave.FaseVoorlopig,
                Datum = vandaag, Bedrag = bedrag, CreatedAt = DateTime.UtcNow, CreatedBy = userId,
            });
            aantal++; totaal += bedrag;
        }
        if (aantal > 0) await _db.SaveChangesAsync();
        return (aantal, totaal);
    }
}

public class BorgOverzicht
{
    public int ProjectId { get; set; }
    public DateOnly? VoDatum { get; set; }
    public DateOnly? DoDatum { get; set; }
    public List<BorgRij> Rijen { get; set; } = new();
    public List<InhoudingRij> Inhoudingen { get; set; } = new();

    public decimal TotaalOpen => Rijen.Sum(r => Math.Max(0m, r.Open));
    public decimal TotaalNuVrij => Rijen.Sum(r => r.NuVrij);
    public int AantalNuVrij => Rijen.Count(r => r.NuVrij > 0);
    /// <summary>Wat nog vastzit maar nog niet vrij te geven is (wacht op een oplevering of op de bankbrief).</summary>
    public decimal TotaalWacht => Rijen.Sum(r => Math.Max(0m, r.Open - r.NuVrij));
    public decimal TotaalVrijgegeven => Rijen.Sum(r => r.Vrijgegeven);
    public int AantalVrijgaves => Rijen.Sum(r => r.Vrijgaves.Count);
    public int AantalOpen => Rijen.Count(r => r.Open > 0.005m);
    public int AantalOpenInhoudingen => Inhoudingen.Count(i => i.Open > 0.005m);
    public decimal TotaalOpenInhoudingen => Inhoudingen.Sum(i => i.Open);
}

public class BorgRij
{
    public int ContractId { get; set; }
    /// <summary>Borg op facturen zonder contract van één leverancier (geen ContractId).</summary>
    public bool ZonderContract { get; set; }
    /// <summary>Unieke sleutel van de rij: "c{contractId}" of "l{leverancierId}".</summary>
    public string Sleutel => ZonderContract ? $"l{CompanyId}" : $"c{ContractId}";
    /// <summary>Zonder contract: de gekozen vrijgaveregel verschilt per factuur.</summary>
    public bool GemengdeRegel { get; set; }
    public int CompanyId { get; set; }
    public string CompanyNaam { get; set; } = "";
    public string ContractLabel { get; set; } = "";
    public string Activiteiten { get; set; } = "";
    public bool IsBankwaarborg { get; set; }
    public decimal? Percentage { get; set; }
    public decimal Ingehouden { get; set; }
    public decimal Vrijgegeven { get; set; }
    public decimal Open => Ingehouden - Vrijgegeven;
    public decimal VoPercentage { get; set; }
    public bool VoBereikt { get; set; }
    public bool DoBereikt { get; set; }
    public decimal NuVrij { get; set; }
    public int OpenPunten { get; set; }
    public string Status { get; set; } = "";
    public string StatusTone { get; set; } = "is-neutral";
    public List<BorgVrijgaveRij> Vrijgaves { get; set; } = new();

    /// <summary>Vrijgaveregel als tekst: "50 % bij VO ✓ 14/07/2026 · 50 % bij DO".</summary>
    public string RegelTekst
    {
        get
        {
            if (IsBankwaarborg) return "bankwaarborg · vrijgave via brief bij DO";
            if (GemengdeRegel) return "per factuur gekozen";
            var vo = VoPercentage;
            if (vo >= 100m) return "100 % bij VO" + VrijgaveVink(1);
            if (vo <= 0m) return "100 % bij DO" + VrijgaveVink(2);
            return $"{vo:0.##} % bij VO{VrijgaveVink(1)} · {100m - vo:0.##} % bij DO{VrijgaveVink(2)}";
        }
    }

    private string VrijgaveVink(byte fase)
    {
        var v = Vrijgaves.Where(x => x.Fase == fase).OrderBy(x => x.Datum).FirstOrDefault();
        return v == null ? "" : $" ✓ {v.Datum:dd/MM/yyyy}";
    }
}

public class BorgVrijgaveRij
{
    public int Id { get; set; }
    public byte Fase { get; set; }
    public DateOnly Datum { get; set; }
    public decimal Bedrag { get; set; }
    public string? Opmerking { get; set; }
}

public class InhoudingRij
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public int? ContractId { get; set; }
    /// <summary>Borgpercentage van het contract (enkel financiële waarborg): bij vrijgave blijft dit deel in de borg.</summary>
    public decimal? BorgPercentage { get; set; }
    public int? CompanyId { get; set; }
    public string CompanyNaam { get; set; } = "";
    public string FactuurNummer { get; set; } = "";
    public DateOnly FactuurDatum { get; set; }
    public byte Reden { get; set; }
    public string? Toelichting { get; set; }
    public decimal Bedrag { get; set; }
    public decimal Afgehandeld { get; set; }
    public decimal Open { get; set; }
    public string Status { get; set; } = "";
    public List<InhoudingAfhandelingRij> Afhandelingen { get; set; } = new();
    public string RedenTekst => BorgService.RedenTekst(Reden);
}

public class InhoudingAfhandelingRij
{
    public int Id { get; set; }
    public DateOnly Datum { get; set; }
    public byte Soort { get; set; }
    public decimal Bedrag { get; set; }
    public decimal BorgBedrag { get; set; }
    public int? CreditnotaInvoiceId { get; set; }
    public string? CreditnotaNummer { get; set; }
    public string? Opmerking { get; set; }
}

public class CreditnotaOptie
{
    public int InvoiceId { get; set; }
    public string? Nummer { get; set; }
    public DateOnly Datum { get; set; }
    public decimal Bedrag { get; set; }
}

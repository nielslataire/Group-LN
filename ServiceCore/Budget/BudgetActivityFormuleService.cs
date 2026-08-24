using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using BOCore.Budget;
using DALCore;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Budget;

// ─────────────────────────────────────────────────────────────────────────────
// Bewerkbare voorstel-formules per activiteit (pagina BudgetActivityLijnen).
// Een formule berekent het TOTAAL voor het project; de aanroeper deelt door
// @aantal_wooncomm voor de prijs per eenheid.
// ─────────────────────────────────────────────────────────────────────────────

public class FormuleParameterInfo
{
    public string   Naam         { get; set; }
    public string   Omschrijving { get; set; }
    public string   Eenheid      { get; set; }
    public string   Categorie    { get; set; }
    public decimal? Waarde       { get; set; }
}

public class BudgetActivityFormuleInfo
{
    public int      Id                   { get; set; }
    public int      ActivityId           { get; set; }
    public string   ActivityOmschrijving { get; set; }
    public string   LotNaam              { get; set; }
    public decimal  LotNummer            { get; set; }
    public string   Formule              { get; set; }
    public string   Omschrijving         { get; set; }
    public bool     Actief               { get; set; }
    public DateTime LaatstGewijzigd      { get; set; }
}

public class FormuleEvaluatieResultaat
{
    public decimal Totaal         { get; set; }
    public string  Label          { get; set; }
    public string  DetailRowsHtml { get; set; }
}

public class FormuleTermRegel
{
    public string  ExprNamen   { get; set; }
    public string  ExprWaarden { get; set; }
    public decimal Waarde      { get; set; }
}

public class FormuleTestResultaat
{
    public bool                   Ok               { get; set; }
    public string                 Fout             { get; set; }
    public decimal                Totaal           { get; set; }
    public decimal                PerEenheid       { get; set; }
    public int                    AantalEenheden   { get; set; }
    public List<FormuleTermRegel> Termen           { get; set; } = new();
    public List<string>           OnbekendeParams  { get; set; } = new();
}

public class BudgetActivityFormuleService
{
    private readonly UnitOfWorkCore _uow;
    private readonly BudgetFormulaService _formulaService;

    public BudgetActivityFormuleService(UnitOfWorkCore uow, BudgetFormulaService formulaService)
    {
        _uow = uow;
        _formulaService = formulaService;
    }

    // ── Vaste parameterdefinities ────────────────────────────────────────────

    private static readonly (string Naam, string Omschrijving, string Eenheid, string Categorie)[] VasteParams =
    {
        ("aantal_eenheden",          "Aantal eenheden (alle rijen op tab Oppervlaktes)",                        "st",   "Aantallen"),
        ("aantal_wooncomm",          "Aantal woon-/commerciële eenheden (deler voor prijs per eenheid)",        "st",   "Aantallen"),
        ("verdiepingen_ondergronds", "Aantal verdiepingen ondergronds (tab Gegevens)",                          "st",   "Aantallen"),
        ("aantal_veluxen",           "Aantal veluxen (tab Dak & afbraak)",                                      "st",   "Aantallen"),

        ("opp_gba",                  "Totale bewoonbare oppervlakte (GBA)",                                     "m²",   "Oppervlaktes"),
        ("opp_ruwbouw",              "Ruwbouwoppervlakte (GBA + garages + kelder + berging + doorrit + gemene delen + 30% zolder + 25% platdak)", "m²", "Oppervlaktes"),
        ("opp_terras_prefab",        "Terras prefab",                                                           "m²",   "Oppervlaktes"),
        ("opp_gevels",               "Gevels (nieuwbouw + bestaand)",                                           "m²",   "Oppervlaktes"),
        ("opp_platdak",              "Platdak",                                                                 "m²",   "Oppervlaktes"),
        ("opp_hellend_dak",          "Hellend dak (horizontale projectie)",                                     "m²",   "Oppervlaktes"),
        ("opp_groendak",             "Groendak",                                                                "m²",   "Oppervlaktes"),
        ("opp_dakoversteken",        "Dakoversteken",                                                           "m²",   "Oppervlaktes"),
        ("opp_onderkant_doorrit",    "Onderkant doorrit",                                                       "m²",   "Oppervlaktes"),
        ("opp_ramen",                "Ramen (nieuwbouw + bestaand)",                                            "m²",   "Oppervlaktes"),
        ("opp_leien",                "Leien gevelbekleding",                                                    "m²",   "Oppervlaktes"),
        ("opp_funderingen",          "Oppervlakte funderingen (tab Gegevens)",                                  "m²",   "Oppervlaktes"),
        ("opp_garberg_ondergronds",  "Garage/berging ondergronds",                                              "m²",   "Oppervlaktes"),
        ("opp_garages_bovengronds",  "Garages/parkings bovengronds",                                            "m²",   "Oppervlaktes"),
        ("opp_berg_gelijkvloers",    "Berging gelijkvloers",                                                    "m²",   "Oppervlaktes"),
        ("opp_doorrit_gvl",          "Doorrit gelijkvloers",                                                    "m²",   "Oppervlaktes"),
        ("opp_gemeenschappelijk",    "Gemeenschappelijke delen",                                                "m²",   "Oppervlaktes"),
        ("opp_zolder",               "Zolder (volledige oppervlakte)",                                          "m²",   "Oppervlaktes"),

        ("lm_berlinerwanden",        "Berlinerwanden (tab Gegevens)",                                           "lm",   "Lengtes"),
        ("lm_secanpalen",            "Secanpalen (tab Gegevens)",                                               "lm",   "Lengtes"),
        ("lm_ballustrades",          "Ballustrades (tab Gevels)",                                               "lm",   "Lengtes"),
        ("lm_zichtschermen",         "Zichtschermen (tab Gevels)",                                              "lm",   "Lengtes"),

        ("m3_onderschoeiingen",      "Onderschoeiingen (tab Gegevens)",                                         "m³",   "Volumes"),
        ("m3_grondwerken",           "Grondwerken: opp funderingen × 0,30 + verd. ondergronds × 3,50 × garage/berging ondergronds", "m³", "Volumes"),

        ("prijs_ruwbouw_basis",      "Nacalc basisprijs ruwbouw uit budget, geïndexeerd",                       "€/m²", "Prijzen budget (geïndexeerd)"),
        ("prijs_gevelmetselwerk",    "Gevelmetselwerkprijs uit budget, geïndexeerd",                            "€/m²", "Prijzen budget (geïndexeerd)"),
        ("prijs_terras",             "Terrasprijs uit budget, geïndexeerd",                                     "€/m²", "Prijzen budget (geïndexeerd)"),
        ("prijs_gipsblokken",        "Gipswerkenprijs uit budget, geïndexeerd",                                 "€/eenh.", "Prijzen budget (geïndexeerd)"),

        ("gewogen_factor",           "Gewogen S/I-indexfactor van het budget",                                  "",     "Indexen"),
        ("i_huidig",                 "Huidige I2021-index van het budget",                                      "",     "Indexen"),
        ("s_huidig",                 "Huidige S-index van het budget",                                          "",     "Indexen"),
    };

    // ── Parametercatalogus ───────────────────────────────────────────────────

    public async Task<List<FormuleParameterInfo>> GetParametersAsync(int? versieId)
    {
        var koppelingen = await _uow.FormulaKoppelingen.GetNoTracking()
            .Include(k => k.Materiaal)
            .Where(k => k.Materiaal != null)
            .OrderBy(k => k.Sleutel)
            .ToListAsync();

        Dictionary<string, decimal> waarden = null;
        if (versieId.HasValue)
            waarden = await BuildParameterWaardenAsync(versieId.Value);

        var lijst = VasteParams
            .Select(p => new FormuleParameterInfo
            {
                Naam         = p.Naam,
                Omschrijving = p.Omschrijving,
                Eenheid      = p.Eenheid,
                Categorie    = p.Categorie,
                Waarde       = waarden != null && waarden.TryGetValue(p.Naam, out var w) ? w : (decimal?)null
            })
            .ToList();

        foreach (var k in koppelingen)
        {
            var naam = "mat_" + k.Sleutel;
            lijst.Add(new FormuleParameterInfo
            {
                Naam         = naam,
                Omschrijving = $"{k.Omschrijving} — materiaal '{k.Materiaal.Naam}', geïndexeerd",
                Eenheid      = "€/" + k.Materiaal.Eenheid,
                Categorie    = "Materiaalprijzen (geïndexeerd)",
                Waarde       = waarden != null && waarden.TryGetValue(naam, out var w) ? w : (decimal?)null
            });
        }
        return lijst;
    }

    // Berekent alle parameterwaarden voor één budgetversie.
    // Zelfde aggregaties als in BudgetActivityService.GetLotGroepenAsync.
    public async Task<Dictionary<string, decimal>> BuildParameterWaardenAsync(int budgetVersieId)
    {
        var versie = await _uow.BudgetVersies.GetNoTracking()
            .Include(v => v.BudgetGegevens)
            .SingleOrDefaultAsync(v => v.Id == budgetVersieId);
        var dbGeg = versie?.BudgetGegevens;

        var gegevensBO = new BudgetGegevensBO
        {
            IIndexHuidig                  = dbGeg?.IIndexHuidig,
            SIndexHuidig                  = dbGeg?.SIndexHuidig,
            IIndexStart                   = dbGeg?.IIndexStart,
            SIndexStart                   = dbGeg?.SIndexStart,
            NacalcBasisprijs              = dbGeg?.NacalcBasisprijs,
            GevelMetselwerkPrijsPerM2     = dbGeg?.GevelMetselwerkPrijsPerM2,
            TerrasPrijsPerM2              = dbGeg?.TerrasPrijsPerM2,
            GipswerkenPrijsPerM2          = dbGeg?.GipswerkenPrijsPerM2,
            LmBerlinerwanden              = dbGeg?.LmBerlinerwanden,
            LmSecanpalen                  = dbGeg?.LmSecanpalen,
            OppFunderingen                = dbGeg?.OppFunderingen,
            AantalVerdiepingenOndergronds = dbGeg?.AantalVerdiepingenOndergronds ?? 0,
            M3Onderschoeiingen            = dbGeg?.M3Onderschoeiingen,
            AantalVeluxen                 = dbGeg?.AantalVeluxen
        };
        var ctx = await _formulaService.BuildContextAsync(budgetVersieId, gegevensBO);

        var opps = await _uow.BudgetOppervlaktes.GetNoTracking()
            .Include(o => o.UnitGroupType)
            .Where(o => o.BudgetVersieId == budgetVersieId)
            .ToListAsync();

        var gevelRijen = await _uow.BudgetGevelElementen.GetNoTracking()
            .Where(g => g.BudgetVersieId == budgetVersieId)
            .ToListAsync();

        static decimal GevelLm(BudgetGevelElementen e) => e.Aantal * (e.Lengte ?? 0m);
        static decimal GevelM2(BudgetGevelElementen e)
        {
            if (e.Hoogte.HasValue && e.Hoogte.Value != 0)
                return e.Aantal * (e.Breedte ?? 0m) * e.Hoogte.Value;
            if (e.Breedte.HasValue && e.Breedte.Value > 0 && e.Lengte.HasValue && e.Lengte.Value > 0)
                return e.Aantal * e.Breedte.Value * e.Lengte.Value;
            return 0m;
        }
        decimal GevelM2Type(params string[] types) =>
            gevelRijen.Where(g => types.Contains(g.ElementType)).Sum(g => GevelM2(g));
        decimal GevelLmType(string type) =>
            gevelRijen.Where(g => g.ElementType == type).Sum(g => GevelLm(g));

        var aantalEenheden = opps.Count;
        var aantalWoonComm = opps.Count(o =>
            o.UnitGroupType != null && (
                o.UnitGroupType.Name.Contains("woon", StringComparison.OrdinalIgnoreCase) ||
                o.UnitGroupType.Name.Contains("commerci", StringComparison.OrdinalIgnoreCase)));
        if (aantalWoonComm == 0) aantalWoonComm = aantalEenheden;

        var totaalPlatDak = GevelM2Type("PlatDak");
        var totOppRuwbouw = opps.Sum(o =>
            o.BewoonbareOpp + o.GaragesParkingsBovenGr + o.GarBergOndergronds +
            o.BergGelijkvloers + o.DoorritGVL + o.GemeenschappelijkeDelen + o.Zolder * 0.30m)
            + totaalPlatDak * 0.25m;

        var m2Funder      = gegevensBO.OppFunderingen ?? 0m;
        var totaalGarBerg = opps.Sum(o => o.GarBergOndergronds);

        var waarden = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["aantal_eenheden"]          = aantalEenheden,
            ["aantal_wooncomm"]          = aantalWoonComm,
            ["verdiepingen_ondergronds"] = gegevensBO.AantalVerdiepingenOndergronds,
            ["aantal_veluxen"]           = gegevensBO.AantalVeluxen ?? 0,

            ["opp_gba"]                  = opps.Sum(o => o.BewoonbareOpp),
            ["opp_ruwbouw"]              = totOppRuwbouw,
            ["opp_terras_prefab"]        = opps.Sum(o => o.TerrasPrefab),
            ["opp_gevels"]               = GevelM2Type("GevelNieuwbouw", "GevelBestaand"),
            ["opp_platdak"]              = totaalPlatDak,
            ["opp_hellend_dak"]          = GevelM2Type("HellendDak"),
            ["opp_groendak"]             = GevelM2Type("GroenDak"),
            ["opp_dakoversteken"]        = GevelM2Type("Dakoversteken"),
            ["opp_onderkant_doorrit"]    = GevelM2Type("OnderkantDoorrit"),
            ["opp_ramen"]                = GevelM2Type("RaamNieuwbouw", "RaamBestaand"),
            ["opp_leien"]                = GevelM2Type("Leien"),
            ["opp_funderingen"]          = m2Funder,
            ["opp_garberg_ondergronds"]  = totaalGarBerg,
            ["opp_garages_bovengronds"]  = opps.Sum(o => o.GaragesParkingsBovenGr),
            ["opp_berg_gelijkvloers"]    = opps.Sum(o => o.BergGelijkvloers),
            ["opp_doorrit_gvl"]          = opps.Sum(o => o.DoorritGVL),
            ["opp_gemeenschappelijk"]    = opps.Sum(o => o.GemeenschappelijkeDelen),
            ["opp_zolder"]               = opps.Sum(o => o.Zolder),

            ["lm_berlinerwanden"]        = gegevensBO.LmBerlinerwanden ?? 0m,
            ["lm_secanpalen"]            = gegevensBO.LmSecanpalen     ?? 0m,
            ["lm_ballustrades"]          = GevelLmType("Ballustrade"),
            ["lm_zichtschermen"]         = GevelLmType("Zichtscherm"),

            ["m3_onderschoeiingen"]      = gegevensBO.M3Onderschoeiingen ?? 0m,
            ["m3_grondwerken"]           = m2Funder * 0.3m
                                         + gegevensBO.AantalVerdiepingenOndergronds * 3.5m * totaalGarBerg,

            ["prijs_ruwbouw_basis"]      = (gegevensBO.NacalcBasisprijs          ?? 0m) * ctx.MIndexFactor(FormulaSleutels.NacalcRuwbouwBasis),
            ["prijs_gevelmetselwerk"]    = (gegevensBO.GevelMetselwerkPrijsPerM2 ?? 0m) * ctx.MIndexFactor(FormulaSleutels.BovenbouwGevelmetselwerk),
            ["prijs_terras"]             = (gegevensBO.TerrasPrijsPerM2          ?? 0m) * ctx.MIndexFactor(FormulaSleutels.BovenbouwTerras),
            ["prijs_gipsblokken"]        = (gegevensBO.GipswerkenPrijsPerM2      ?? 0m) * ctx.MIndexFactor(FormulaSleutels.BovenbouwGipsblokken),

            ["gewogen_factor"]           = ctx.GewogenIndex,
            ["i_huidig"]                 = ctx.IHuidig,
            ["s_huidig"]                 = ctx.SHuidig,
        };

        // Geïndexeerde materiaalprijzen per formule-slot: mat_<sleutel>
        var koppelingen = await _uow.FormulaKoppelingen.GetNoTracking()
            .Where(k => k.MateriaalId != null)
            .Select(k => k.Sleutel)
            .ToListAsync();
        foreach (var sleutel in koppelingen)
        {
            waarden["mat_" + sleutel] = ctx.HeeftMateriaal(sleutel)
                ? ctx.M(sleutel) * ctx.MIndexFactor(sleutel)
                : 0m;
        }

        return waarden;
    }

    // ── CRUD ─────────────────────────────────────────────────────────────────

    public async Task<List<BudgetActivityFormuleInfo>> GetFormulesAsync()
    {
        return await _uow.BudgetActivityFormules.GetNoTracking()
            .Include(f => f.Activity).ThenInclude(a => a.Group)
            .OrderBy(f => f.Activity.Group.Lot)
            .ThenBy(f => f.Activity.Omschrijving)
            .Select(f => new BudgetActivityFormuleInfo
            {
                Id                   = f.Id,
                ActivityId           = f.ActivityId,
                ActivityOmschrijving = f.Activity.Omschrijving,
                LotNaam              = f.Activity.Group.Name,
                LotNummer            = f.Activity.Group.Lot ?? 0m,
                Formule              = f.Formule,
                Omschrijving         = f.Omschrijving,
                Actief               = f.Actief,
                LaatstGewijzigd      = f.LaatstGewijzigd
            })
            .ToListAsync();
    }

    public async Task<Response> SaveAsync(int activityId, string formule, string omschrijving, bool actief)
    {
        var response = new Response();

        // Valideer syntax
        FormuleNode node;
        try
        {
            node = FormuleParser.Parse(formule);
        }
        catch (FormuleParseException ex)
        {
            response.AddError("Formule ongeldig: " + ex.Message);
            return response;
        }

        // Valideer parameternamen tegen de catalogus
        var bekend = (await GetParametersAsync(null))
            .Select(p => p.Naam)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var onbekend = FormuleParser.GetParameterNamen(node)
            .Where(n => !bekend.Contains(n))
            .ToList();
        if (onbekend.Count > 0)
        {
            response.AddError("Onbekende parameter(s): " + string.Join(", ", onbekend.Select(n => "@" + n)));
            return response;
        }

        var bestaand = await _uow.BudgetActivityFormules.GetNormal()
            .SingleOrDefaultAsync(f => f.ActivityId == activityId);
        if (bestaand != null)
        {
            bestaand.Formule         = formule.Trim();
            bestaand.Omschrijving    = string.IsNullOrWhiteSpace(omschrijving) ? null : omschrijving.Trim();
            bestaand.Actief          = actief;
            bestaand.LaatstGewijzigd = DateTime.UtcNow;
        }
        else
        {
            _uow.BudgetActivityFormules.Add(new BudgetActivityFormule
            {
                ActivityId      = activityId,
                Formule         = formule.Trim(),
                Omschrijving    = string.IsNullOrWhiteSpace(omschrijving) ? null : omschrijving.Trim(),
                Actief          = actief,
                LaatstGewijzigd = DateTime.UtcNow
            });
        }

        int affected = await _uow.SaveChangesAsync();
        response.AddSaveChangesResult(affected, "Formule opgeslagen.", "Geen wijzigingen opgeslagen.");
        return response;
    }

    public async Task<Response> DeleteAsync(int id)
    {
        var response = new Response();
        var formule = await _uow.BudgetActivityFormules.GetNormal()
            .SingleOrDefaultAsync(f => f.Id == id);
        if (formule == null)
        {
            response.AddError("Formule niet gevonden.");
            return response;
        }
        _uow.BudgetActivityFormules.Remove(formule);
        int affected = await _uow.SaveChangesAsync();
        response.AddSaveChangesResult(affected, "Formule verwijderd.", "Geen wijzigingen opgeslagen.");
        return response;
    }

    // Alle activiteiten voor de "nieuwe formule"-dropdown.
    public async Task<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> GetActiviteitenAsync()
    {
        var activities = await _uow.Activities.GetNoTracking()
            .Include(a => a.Group)
            .Where(a => a.Group != null)
            .OrderBy(a => a.Group.Lot)
            .ThenBy(a => a.Omschrijving)
            .ToListAsync();

        return activities
            .Select(a => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = a.ActivityId.ToString(),
                Text  = $"Lot {a.Group.Lot:G} — {a.Omschrijving}"
            })
            .ToList();
    }

    // Budgetversies voor de test-dropdown op de instellingenpagina.
    public async Task<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> GetTestVersiesAsync()
    {
        var versies = await _uow.BudgetVersies.GetNoTracking()
            .Include(v => v.Project)
            .Include(v => v.BudgetMaster)
            .OrderBy(v => v.ProjectId)
            .ThenByDescending(v => v.Versienummer)
            .ToListAsync();

        return versies
            .Select(v => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = v.Id.ToString(),
                Text  = $"{v.Project?.ProjectName ?? ("Project " + v.ProjectId)} — {v.BudgetMaster?.Naam} v{v.Versienummer}"
                        + (v.IsHuidig ? " (huidig)" : "")
            })
            .ToList();
    }

    // ── Evaluatie ────────────────────────────────────────────────────────────

    private static readonly CultureInfo NlBe = new("nl-BE");
    private static string F(decimal v) => v.ToString("N2", NlBe);

    // Evalueert alle actieve formules voor een budgetversie.
    // Resultaat per ActivityId: totaal project + detailregels voor de popover.
    public async Task<Dictionary<int, FormuleEvaluatieResultaat>> EvaluateAlleAsync(int budgetVersieId)
    {
        var resultaten = new Dictionary<int, FormuleEvaluatieResultaat>();

        var formules = await _uow.BudgetActivityFormules.GetNoTracking()
            .Include(f => f.Activity)
            .Where(f => f.Actief)
            .ToListAsync();
        if (formules.Count == 0) return resultaten;

        var waarden = await BuildParameterWaardenAsync(budgetVersieId);

        foreach (var f in formules)
        {
            FormuleNode node;
            try { node = FormuleParser.Parse(f.Formule); }
            catch (FormuleParseException) { continue; } // ongeldige formule overslaan

            var termen = BouwTermRegels(node, waarden);
            var totaal = termen.Sum(t => t.Waarde);

            resultaten[f.ActivityId] = new FormuleEvaluatieResultaat
            {
                Totaal         = totaal,
                Label          = string.IsNullOrWhiteSpace(f.Omschrijving) ? f.Activity.Omschrijving : f.Omschrijving,
                DetailRowsHtml = BouwDetailRowsHtml(termen)
            };
        }
        return resultaten;
    }

    // Test één formule (editor op de instellingenpagina) tegen een budgetversie.
    public async Task<FormuleTestResultaat> TestAsync(int budgetVersieId, string formule)
    {
        var result = new FormuleTestResultaat();

        FormuleNode node;
        try { node = FormuleParser.Parse(formule); }
        catch (FormuleParseException ex)
        {
            result.Fout = ex.Message;
            return result;
        }

        var waarden = await BuildParameterWaardenAsync(budgetVersieId);
        var bekend  = (await GetParametersAsync(null))
            .Select(p => p.Naam)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        result.OnbekendeParams = FormuleParser.GetParameterNamen(node)
            .Where(n => !bekend.Contains(n))
            .Select(n => "@" + n)
            .ToList();

        result.Termen = BouwTermRegels(node, waarden);
        result.Totaal = result.Termen.Sum(t => t.Waarde);
        var eenheden = waarden.TryGetValue("aantal_wooncomm", out var n2) ? (int)n2 : 0;
        result.AantalEenheden = eenheden;
        result.PerEenheid = eenheden > 0 ? Math.Round(result.Totaal / eenheden, 2) : 0m;
        result.Ok = true;
        return result;
    }

    private static List<FormuleTermRegel> BouwTermRegels(FormuleNode node, Dictionary<string, decimal> waarden)
    {
        return FormuleParser.GetTermen(node)
            .Select(t =>
            {
                var teken = t.Teken < 0 ? "− " : "";
                return new FormuleTermRegel
                {
                    ExprNamen   = teken + t.Node.ToDisplay(n => "@" + n),
                    ExprWaarden = teken + t.Node.ToDisplay(n => F(waarden.TryGetValue(n, out var w) ? w : 0m)),
                    Waarde      = t.Teken * t.Node.Evaluate(waarden)
                };
            })
            .ToList();
    }

    // Detailregels in dezelfde stijl als de bestaande VoorstelEnkelDetail-rijen.
    private static string BouwDetailRowsHtml(List<FormuleTermRegel> termen)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < termen.Count; i++)
        {
            var t = termen[i];
            var border = i == termen.Count - 1 ? ";border-bottom:1px dashed #ccc" : "";
            sb.Append($"<tr style='font-size:.75rem;color:#6c757d{border}'>")
              .Append($"<td style='white-space:normal'>{t.ExprNamen}</td>")
              .Append($"<td colspan='2' style='padding:0 6px;text-align:right'>{t.ExprWaarden}</td>")
              .Append($"<td style='padding-left:8px;text-align:right'>= € {F(t.Waarde)}</td></tr>");
        }
        return sb.ToString();
    }
}

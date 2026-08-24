using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BOCore;
using BOCore.Budget;
using DALCore;
using DALCore.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Budget
{
    public class BudgetActivityService
    {
        private const int ActivityIdRuwbouw          = 186;
        private const int ActivityIdBerlinerwanden    = 177;
        private const int ActivityIdSecanpalen        = 178;
        private const int ActivityIdFunderingen       = 183;
        private const int ActivityIdGrondwerken       = 173;
        private const int ActivityIdOnderschoeiingen  = 179;
        private const int ActivityIdPlatdak              = 197;
        private const int ActivityIdGroendak             = 202;
        private const int ActivityIdDaktimmerwerk        = 194;
        private const int ActivityIdDakBedekking         = 195;
        private const int ActivityIdKelderRuwbouw        = 181;
        private const int ActivityIdBuitenschrijnwerk    = 205;
        private const int ActivityIdGevelsluitingCombi   = 217;
        private const int ActivityIdLeien                = 215;

        private readonly UnitOfWorkCore _uow;
        private readonly BouwIndexService _bouwIndex;
        private readonly BudgetFormulaService _formulaService;
        private readonly BudgetActivityFormuleService _activityFormules;

        public BudgetActivityService(UnitOfWorkCore uow, BouwIndexService bouwIndex, BudgetFormulaService formulaService, BudgetActivityFormuleService activityFormules)
        {
            _uow = uow;
            _bouwIndex = bouwIndex;
            _formulaService = formulaService;
            _activityFormules = activityFormules;
        }

        private static decimal GevelLm(DALCore.Models.BudgetGevelElementen e)
            => e.Aantal * (e.Lengte ?? 0m);

        private static decimal GevelM2(DALCore.Models.BudgetGevelElementen e)
        {
            if (e.Hoogte.HasValue && e.Hoogte.Value != 0)
                return e.Aantal * (e.Breedte ?? 0m) * e.Hoogte.Value;
            if (e.Breedte.HasValue && e.Breedte.Value > 0 && e.Lengte.HasValue && e.Lengte.Value > 0)
                return e.Aantal * e.Breedte.Value * e.Lengte.Value;
            return 0m;
        }

        public async Task<List<BudgetLotGroepBO>> GetLotGroepenAsync(int budgetVersieId)
        {
            var versie = await _uow.BudgetVersies.GetNoTracking()
                .Include(v => v.BudgetGegevens)
                .SingleOrDefaultAsync(v => v.Id == budgetVersieId);

            var sStart  = versie?.BudgetGegevens?.SIndexStart  ?? 0m;
            var sHuidig = versie?.BudgetGegevens?.SIndexHuidig ?? 0m;
            var iStart  = versie?.BudgetGegevens?.IIndexStart  ?? 0m;
            var iHuidig = versie?.BudgetGegevens?.IIndexHuidig ?? 0m;
            var gewogenFactor = _bouwIndex.BerekenGewogenFactor(sStart, sHuidig, iStart, iHuidig);

            // Oppervlaktes (met UnitGroupType voor eenhedentelling)
            var opps = await _uow.BudgetOppervlaktes.GetNoTracking()
                .Include(o => o.UnitGroupType)
                .Where(o => o.BudgetVersieId == budgetVersieId)
                .ToListAsync();

            var aantalEenheden = opps.Count;
            var totaalGBA = opps.Sum(o => o.BewoonbareOpp);

            // ── Ruwbouw-voorstel berekening ────────────────────────────────────────
            // Dezelfde formule als de live sidebar in BudgetGegevens:
            //   basisprijs × ctx.MIndexFactor(sleutel)
            //   = basisprijs × (IHuidig/MateriaalIRef × 0.4 + SHuidig/MateriaalSRef × 0.4 + 0.2)
            // ctx.Gegevens bevat de volledige BudgetGegevensBO zodat IHuidig/SHuidig en
            // de basisprijzen beschikbaar zijn via dezelfde context als in BudgetGegevens.
            var dbGeg = versie?.BudgetGegevens;
            var gegevensBO = new BudgetGegevensBO
            {
                IIndexHuidig                    = dbGeg?.IIndexHuidig,
                SIndexHuidig                    = dbGeg?.SIndexHuidig,
                IIndexStart                     = dbGeg?.IIndexStart,
                SIndexStart                     = dbGeg?.SIndexStart,
                NacalcBasisprijs                = dbGeg?.NacalcBasisprijs,
                GevelMetselwerkPrijsPerM2       = dbGeg?.GevelMetselwerkPrijsPerM2,
                TerrasPrijsPerM2                = dbGeg?.TerrasPrijsPerM2,
                GipswerkenPrijsPerM2            = dbGeg?.GipswerkenPrijsPerM2,
                LmBerlinerwanden                = dbGeg?.LmBerlinerwanden,
                LmSecanpalen                    = dbGeg?.LmSecanpalen,
                OppFunderingen                  = dbGeg?.OppFunderingen,
                AantalVerdiepingenOndergronds   = dbGeg?.AantalVerdiepingenOndergronds ?? 0,
                M3Onderschoeiingen              = dbGeg?.M3Onderschoeiingen,
                AantalVeluxen                   = dbGeg?.AantalVeluxen
            };
            var formulaCtx = await _formulaService.BuildContextAsync(budgetVersieId, gegevensBO);

            var ruwbouwPrijsGeind = (gegevensBO.NacalcBasisprijs          ?? 0m) * formulaCtx.MIndexFactor(FormulaSleutels.NacalcRuwbouwBasis);
            var gevelPrijsGeind   = (gegevensBO.GevelMetselwerkPrijsPerM2 ?? 0m) * formulaCtx.MIndexFactor(FormulaSleutels.BovenbouwGevelmetselwerk);
            var terrasPrijsGeind  = (gegevensBO.TerrasPrijsPerM2          ?? 0m) * formulaCtx.MIndexFactor(FormulaSleutels.BovenbouwTerras);
            var gipsPrijsGeind    = (gegevensBO.GipswerkenPrijsPerM2      ?? 0m) * formulaCtx.MIndexFactor(FormulaSleutels.BovenbouwGipsblokken);

            // ── Onderbouw: geïndexeerde catalogusprijzen per materiaal ─────────
            decimal MatGeind(string sleutel) =>
                formulaCtx.HeeftMateriaal(sleutel)
                    ? formulaCtx.M(sleutel) * formulaCtx.MIndexFactor(sleutel)
                    : 0m;

            var berlinerPrijsGeind   = MatGeind(FormulaSleutels.OnderbouwBerlinerwanden);
            var secanPrijsGeind      = MatGeind(FormulaSleutels.OnderbouwSecanpalen);
            var funderPrijsGeind     = MatGeind(FormulaSleutels.OnderbouwFunderingen);
            var grondwerkPrijsGeind  = MatGeind(FormulaSleutels.OnderbouwGrondwerken);
            var onderschPrijsGeind   = MatGeind(FormulaSleutels.OnderbouwOnderschoeiingen);

            // ── Dakwerken: geïndexeerde catalogusprijzen ──────────────────────
            var platdakPrijsGeind      = MatGeind(FormulaSleutels.DakwerkenPlatdak);
            var groendakPrijsGeind     = MatGeind(FormulaSleutels.DakwerkenGroendak);
            var timmerPrijsGeind       = MatGeind(FormulaSleutels.DakwerkenDaktimmerwerk);
            var overstPrijsGeind       = MatGeind(FormulaSleutels.DakwerkenDakoversteken);
            var doorritPrijsGeind      = MatGeind(FormulaSleutels.DakwerkenOnderkantDoorrit);
            var bekkingPrijsGeind      = MatGeind(FormulaSleutels.DakwerkenHellendDakBedekking);
            var veluxPrijsGeind          = MatGeind(FormulaSleutels.DakwerkenVeluxen);

            // ── Gevelsluiting: geïndexeerde catalogusprijzen ──────────────────
            var kelderRuwbouwPrijsGeind  = MatGeind(FormulaSleutels.NacalcRuwbouwBasis);
            var buitenschrPrijsGeind     = MatGeind(FormulaSleutels.GevelsluitingBuitenschrijnwerk);
            var ballustradePrijsGeind    = MatGeind(FormulaSleutels.GevelsluitingBallustrades);
            var zichtschermenPrijsGeind  = MatGeind(FormulaSleutels.GevelsluitingZichtschermen);
            var leienPrijsGeind          = MatGeind(FormulaSleutels.GevelsluitingLeien);

            // ── Hoeveelheden onderbouw ────────────────────────────────────────
            var lmBerliner     = gegevensBO.LmBerlinerwanden    ?? 0m;
            var lmSecan        = gegevensBO.LmSecanpalen        ?? 0m;
            var m2Funder       = gegevensBO.OppFunderingen      ?? 0m;
            var m3Onderschoei  = gegevensBO.M3Onderschoeiingen  ?? 0m;
            var totaalGarBerg  = opps.Sum(o => o.GarBergOndergronds);
            var m3Grondwerk    = m2Funder * 0.3m
                               + gegevensBO.AantalVerdiepingenOndergronds * 3.5m * totaalGarBerg;

            var totOppRuwbouw = opps.Sum(o =>
                o.BewoonbareOpp + o.GaragesParkingsBovenGr + o.GarBergOndergronds +
                o.BergGelijkvloers + o.DoorritGVL + o.GemeenschappelijkeDelen + o.Zolder * 0.30m);

            var totTerrasPrefab = opps.Sum(o => o.TerrasPrefab);

            var gevelRijen = await _uow.BudgetGevelElementen.GetNoTracking()
                .Where(g => g.BudgetVersieId == budgetVersieId)
                .ToListAsync();
            var totaalGevels  = gevelRijen
                .Where(g => g.ElementType == "GevelNieuwbouw" || g.ElementType == "GevelBestaand")
                .Sum(g => GevelM2(g));
            var totaalPlatDak = gevelRijen
                .Where(g => g.ElementType == "PlatDak")
                .Sum(g => GevelM2(g));
            totOppRuwbouw += totaalPlatDak * 0.25m;

            // ── Dak hoeveelheden ──────────────────────────────────────────────
            var totaalHellendDak      = gevelRijen.Where(g => g.ElementType == "HellendDak").Sum(g => GevelM2(g));
            var totaalGroenDak        = gevelRijen.Where(g => g.ElementType == "GroenDak").Sum(g => GevelM2(g));
            var totaalDakoversteken   = gevelRijen.Where(g => g.ElementType == "Dakoversteken").Sum(g => GevelM2(g));
            var totaalOnderkantDoorrit= gevelRijen.Where(g => g.ElementType == "OnderkantDoorrit").Sum(g => GevelM2(g));
            var aantalVeluxen         = (decimal)(gegevensBO.AantalVeluxen ?? 0);

            // ── Gevelsluiting hoeveelheden ────────────────────────────────────
            var totaalRamen           = gevelRijen
                .Where(g => g.ElementType == "RaamNieuwbouw" || g.ElementType == "RaamBestaand")
                .Sum(g => GevelM2(g));
            var totaalBallustrade     = gevelRijen.Where(g => g.ElementType == "Ballustrade").Sum(g => GevelLm(g));
            var totaalZichtscherm     = gevelRijen.Where(g => g.ElementType == "Zichtscherm").Sum(g => GevelLm(g));
            var totaalLeien           = gevelRijen.Where(g => g.ElementType == "Leien").Sum(g => GevelM2(g));

            var aantalWoonComm = opps.Count(o =>
                o.UnitGroupType != null && (
                    o.UnitGroupType.Name.Contains("woon", StringComparison.OrdinalIgnoreCase) ||
                    o.UnitGroupType.Name.Contains("commerci", StringComparison.OrdinalIgnoreCase)));
            if (aantalWoonComm == 0) aantalWoonComm = aantalEenheden;

            var ruwbouwVoorstelTotaal = ruwbouwPrijsGeind * totOppRuwbouw
                                      + terrasPrijsGeind  * totTerrasPrefab
                                      + gevelPrijsGeind   * totaalGevels
                                      + gipsPrijsGeind    * aantalWoonComm;
            var ruwbouwVoorstelPerEenheid = aantalWoonComm > 0
                ? Math.Round(ruwbouwVoorstelTotaal / aantalWoonComm, 2)
                : 0m;
            // ── Einde ruwbouw-voorstel ─────────────────────────────────────────────

            // Bewerkbare formules (Instellingen → Budgetformules) hebben voorrang
            // op de hardgecodeerde voorstellen hieronder.
            var formuleEvaluaties = await _activityFormules.EvaluateAlleAsync(budgetVersieId);

            // Alle activiteiten inclusief lot-groep
            var activities = await _uow.Activities.GetNoTracking()
                .Include(a => a.Group)
                .Where(a => a.Group != null)
                .OrderBy(a => a.Group.Lot)
                .ThenBy(a => a.Omschrijving)
                .ToListAsync();

            // Bestaande lijnen voor deze versie
            var bestaandeLijnen = await _uow.BudgetActivityLijnen.GetNoTracking()
                .Where(l => l.BudgetVersieId == budgetVersieId)
                .ToListAsync();

            var lijnenByActivity = bestaandeLijnen.ToDictionary(l => l.ActivityId, l => l);

            // Groepeer per ActivityGroup
            var groepen = activities
                .GroupBy(a => new
                {
                    a.GroupId,
                    a.Group.Name,
                    Lot = a.Group.Lot ?? 0m
                })
                .OrderBy(g => g.Key.Lot)
                .Select(g =>
                {
                    var groep = new BudgetLotGroepBO
                    {
                        LotNummer = g.Key.Lot,
                        LotNaam   = g.Key.Name
                    };

                    foreach (var activity in g)
                    {
                        bool isRuwbouw = activity.ActivityId == ActivityIdRuwbouw;

                        var bo = new BudgetActivityLijnBO
                        {
                            BudgetVersieId        = budgetVersieId,
                            ActivityId            = activity.ActivityId,
                            ActivityOmschrijving  = activity.Omschrijving,
                            LotNummer             = g.Key.Lot,
                            LotNaam               = g.Key.Name,
                            GroupId               = activity.GroupId,
                            AantalEenheden        = aantalEenheden,
                            TotaalOppervlakte     = totaalGBA,
                            Correctiefactor       = 1m,
                            SIndexStart           = sStart,
                            SIndexHuidig          = sHuidig,
                            IIndexStart           = iStart,
                            IIndexHuidig          = iHuidig,
                            GewogenIndexFactor    = gewogenFactor
                        };

                        bool heeftBestaandeLijn = lijnenByActivity.TryGetValue(activity.ActivityId, out var lijn);
                        if (heeftBestaandeLijn)
                        {
                            bo.Id                          = lijn.Id;
                            bo.AlternatievePrijsPerEenheid = lijn.AlternatievePrijsPerEenheid ?? 0m;
                            bo.NacalcPrijsPerEenheid       = lijn.NacalcPrijsPerEenheid       ?? 0m;
                            bo.Correctiefactor             = lijn.Correctiefactor;
                            bo.IsManueel                   = lijn.IsManueel;
                        }

                        bool viaFormule = formuleEvaluaties.TryGetValue(activity.ActivityId, out var fEval)
                                          && fEval.Totaal > 0 && aantalWoonComm > 0;
                        if (viaFormule)
                        {
                            var fPerEenheid = Math.Round(fEval.Totaal / aantalWoonComm, 2);
                            bo.VoorgesteldePrijsPerEenheid = fPerEenheid;
                            bo.VoorstelEnkelPrijs          = fPerEenheid;
                            bo.VoorstelEnkelHoeveelheid    = aantalWoonComm;
                            bo.VoorstelEnkelEenheid        = "eenh.";
                            bo.VoorstelEnkelLabel          = fEval.Label;
                            bo.VoorstelEnkelDetail         = fEval.DetailRowsHtml;
                            bo.VoorstelAantalEenheden      = aantalWoonComm;
                            if (!heeftBestaandeLijn)
                                bo.AlternatievePrijsPerEenheid = fPerEenheid;
                        }

                        if (!viaFormule && isRuwbouw && ruwbouwVoorstelPerEenheid > 0)
                        {
                            bo.VoorgesteldePrijsPerEenheid  = ruwbouwVoorstelPerEenheid;
                            bo.VoorstelRuwbouwPrijs         = ruwbouwPrijsGeind;
                            bo.VoorstelRuwbouwOpp           = totOppRuwbouw;
                            bo.VoorstelTerrasPrijs          = terrasPrijsGeind;
                            bo.VoorstelTerrasOpp            = totTerrasPrefab;
                            bo.VoorstelGevelPrijs           = gevelPrijsGeind;
                            bo.VoorstelGevelOpp             = totaalGevels;
                            bo.VoorstelGipsPrijs            = gipsPrijsGeind;
                            bo.VoorstelGipsOpp              = aantalWoonComm;
                            bo.VoorstelGipsAantalEenheden   = aantalWoonComm;
                            bo.VoorstelAantalEenheden       = aantalWoonComm;
                            if (!heeftBestaandeLijn)
                                bo.AlternatievePrijsPerEenheid = ruwbouwVoorstelPerEenheid;
                        }

                        (decimal prijs, decimal hoev, string eenh, string label) enkel = activity.ActivityId switch
                        {
                            ActivityIdBerlinerwanden   => (berlinerPrijsGeind,     lmBerliner,     "lm",  "Berlinerwanden"),
                            ActivityIdSecanpalen       => (secanPrijsGeind,        lmSecan,        "lm",  "Secanpalen"),
                            ActivityIdFunderingen      => (funderPrijsGeind,       m2Funder,       "m²",  "Funderingen"),
                            ActivityIdGrondwerken      => (grondwerkPrijsGeind,    m3Grondwerk,    "m³",  "Grondwerken"),
                            ActivityIdOnderschoeiingen => (onderschPrijsGeind,     m3Onderschoei,  "m³",  "Onderschoeiingen"),
                            ActivityIdPlatdak          => (platdakPrijsGeind,      totaalPlatDak,  "m²",  "Platdak"),
                            ActivityIdGroendak         => (groendakPrijsGeind,     totaalGroenDak, "m²",  "Groendak"),
                            ActivityIdKelderRuwbouw    => (kelderRuwbouwPrijsGeind,totaalGarBerg,  "m²",  "Kelderruwbouw"),
                            ActivityIdBuitenschrijnwerk=> (buitenschrPrijsGeind,   totaalRamen,    "m²",  "Buitenschrijnwerk"),
                            ActivityIdLeien            => (leienPrijsGeind,        totaalLeien,    "m²",  "Leien"),
                            _                          => (0m, 0m, string.Empty, string.Empty)
                        };

                        // ── Composite: ballustrades + zichtschermen (217) ─────
                        if (!viaFormule && activity.ActivityId == ActivityIdGevelsluitingCombi)
                        {
                            var ballDeel  = ballustradePrijsGeind  * totaalBallustrade;
                            var zichtDeel = zichtschermenPrijsGeind * totaalZichtscherm;
                            var totaalProject = ballDeel + zichtDeel;
                            if (totaalProject > 0 && aantalWoonComm > 0)
                            {
                                var nlBE3 = new System.Globalization.CultureInfo("nl-BE");
                                string F3(decimal v) => v.ToString("N2", nlBE3);
                                var ePerEenheid = Math.Round(totaalProject / aantalWoonComm, 2);
                                bo.VoorgesteldePrijsPerEenheid = ePerEenheid;
                                bo.VoorstelEnkelPrijs          = ePerEenheid;
                                bo.VoorstelEnkelHoeveelheid    = aantalWoonComm;
                                bo.VoorstelEnkelEenheid        = "eenh.";
                                bo.VoorstelEnkelLabel          = "Gevelsluiting";
                                bo.VoorstelEnkelDetail         =
                                    $"<tr style='font-size:.75rem;color:#6c757d'><td>Ballustrades</td>" +
                                    $"<td style='padding:0 6px;text-align:right'>{F3(totaalBallustrade)} lm</td>" +
                                    $"<td style='text-align:right'>€ {F3(ballustradePrijsGeind)}/lm</td>" +
                                    $"<td style='padding-left:8px;text-align:right'>= € {F3(ballDeel)}</td></tr>" +
                                    $"<tr style='font-size:.75rem;color:#6c757d;border-bottom:1px dashed #ccc'><td>Zichtschermen</td>" +
                                    $"<td style='padding:0 6px;text-align:right'>{F3(totaalZichtscherm)} lm</td>" +
                                    $"<td style='text-align:right'>€ {F3(zichtschermenPrijsGeind)}/lm</td>" +
                                    $"<td style='padding-left:8px;text-align:right'>= € {F3(zichtDeel)}</td></tr>";
                                bo.VoorstelAantalEenheden = aantalWoonComm;
                                if (!heeftBestaandeLijn)
                                    bo.AlternatievePrijsPerEenheid = ePerEenheid;
                            }
                        }

                        // ── Composiete dak activiteiten (meerdere materialen) ─
                        bool isDaktimmerwerk = activity.ActivityId == ActivityIdDaktimmerwerk;
                        bool isDakBedekking  = activity.ActivityId == ActivityIdDakBedekking;

                        if (!viaFormule && (isDaktimmerwerk || isDakBedekking))
                        {
                            decimal totaalProject;
                            string  dakLabel;
                            string  dakDetail;
                            var nlBE2 = new System.Globalization.CultureInfo("nl-BE");
                            string F2(decimal v) => v.ToString("N2", nlBE2);

                            if (isDaktimmerwerk)
                            {
                                // hellend dak horizontale projectie × 1.42 × 0.45 + dakoversteken + onderkant doorrit
                                var timmerM2   = totaalHellendDak * 1.42m * 0.45m;
                                var timmerDeel = timmerPrijsGeind  * timmerM2;
                                var overstDeel = overstPrijsGeind  * totaalDakoversteken;
                                var doorDeel   = doorritPrijsGeind * totaalOnderkantDoorrit;
                                totaalProject = timmerDeel + overstDeel + doorDeel;
                                dakLabel = "Daktimmerwerk";
                                dakDetail =
                                    $"<tr style='font-size:.75rem;color:#6c757d'><td>Timmerwerk hellend</td>" +
                                    $"<td style='padding:0 6px;text-align:right'>{F2(totaalHellendDak)} m² × 1,42 × 0,45</td>" +
                                    $"<td style='text-align:right'>€ {F2(timmerPrijsGeind)}/m²</td>" +
                                    $"<td style='padding-left:8px;text-align:right'>= € {F2(timmerDeel)}</td></tr>" +
                                    $"<tr style='font-size:.75rem;color:#6c757d'><td>Dakoversteken</td>" +
                                    $"<td style='padding:0 6px;text-align:right'>{F2(totaalDakoversteken)} m²</td>" +
                                    $"<td style='text-align:right'>€ {F2(overstPrijsGeind)}/m²</td>" +
                                    $"<td style='padding-left:8px;text-align:right'>= € {F2(overstDeel)}</td></tr>" +
                                    $"<tr style='font-size:.75rem;color:#6c757d;border-bottom:1px dashed #ccc'><td>Onderkant doorrit</td>" +
                                    $"<td style='padding:0 6px;text-align:right'>{F2(totaalOnderkantDoorrit)} m²</td>" +
                                    $"<td style='text-align:right'>€ {F2(doorritPrijsGeind)}/m²</td>" +
                                    $"<td style='padding-left:8px;text-align:right'>= € {F2(doorDeel)}</td></tr>";
                            }
                            else // isDakBedekking
                            {
                                // hellend dak × 1.42 (schuine oppervlakte) + veluxen
                                var schuin    = totaalHellendDak * 1.42m;
                                var bekkDeel  = bekkingPrijsGeind * schuin;
                                var veluxDeel = veluxPrijsGeind   * aantalVeluxen;
                                totaalProject = bekkDeel + veluxDeel;
                                dakLabel = "Hellend dak bedekking";
                                dakDetail =
                                    $"<tr style='font-size:.75rem;color:#6c757d'><td>Hellend dak</td>" +
                                    $"<td style='padding:0 6px;text-align:right'>{F2(totaalHellendDak)} m² × 1,42</td>" +
                                    $"<td style='text-align:right'>€ {F2(bekkingPrijsGeind)}/m²</td>" +
                                    $"<td style='padding-left:8px;text-align:right'>= € {F2(bekkDeel)}</td></tr>" +
                                    $"<tr style='font-size:.75rem;color:#6c757d;border-bottom:1px dashed #ccc'><td>Veluxen</td>" +
                                    $"<td style='padding:0 6px;text-align:right'>{(int)aantalVeluxen} st</td>" +
                                    $"<td style='text-align:right'>€ {F2(veluxPrijsGeind)}/st</td>" +
                                    $"<td style='padding-left:8px;text-align:right'>= € {F2(veluxDeel)}</td></tr>";
                            }

                            if (totaalProject > 0 && aantalWoonComm > 0)
                            {
                                var ePerEenheid = Math.Round(totaalProject / aantalWoonComm, 2);
                                bo.VoorgesteldePrijsPerEenheid = ePerEenheid;
                                bo.VoorstelEnkelPrijs          = ePerEenheid;
                                bo.VoorstelEnkelHoeveelheid    = aantalWoonComm;
                                bo.VoorstelEnkelEenheid        = "eenh.";
                                bo.VoorstelEnkelLabel          = dakLabel;
                                bo.VoorstelEnkelDetail         = dakDetail;
                                bo.VoorstelAantalEenheden      = aantalWoonComm;
                                if (!heeftBestaandeLijn)
                                    bo.AlternatievePrijsPerEenheid = ePerEenheid;
                            }
                        }

                        if (!viaFormule && enkel.prijs > 0 && enkel.hoev > 0)
                        {
                            var ePerEenheid = aantalWoonComm > 0
                                ? Math.Round(enkel.prijs * enkel.hoev / aantalWoonComm, 2)
                                : 0m;
                            bo.VoorgesteldePrijsPerEenheid = ePerEenheid;
                            bo.VoorstelEnkelPrijs          = enkel.prijs;
                            bo.VoorstelEnkelHoeveelheid    = enkel.hoev;
                            bo.VoorstelEnkelEenheid        = enkel.eenh;
                            bo.VoorstelEnkelLabel          = enkel.label;
                            bo.VoorstelAantalEenheden      = aantalWoonComm;
                            if (!heeftBestaandeLijn)
                                bo.AlternatievePrijsPerEenheid = ePerEenheid;

                            if (activity.ActivityId == ActivityIdGrondwerken)
                            {
                                var nlBE = new System.Globalization.CultureInfo("nl-BE");
                                string F(decimal v) => v.ToString("N2", nlBE);
                                var funderDeel = m2Funder * 0.3m;
                                var ondergDeel = (decimal)gegevensBO.AantalVerdiepingenOndergronds * 3.5m * totaalGarBerg;
                                bo.VoorstelEnkelDetail =
                                    $"<tr style='font-size:.75rem;color:#6c757d'>" +
                                    $"<td>Funderingen</td><td style='padding:0 6px;text-align:right'>{F(m2Funder)} m²</td>" +
                                    $"<td style='text-align:right'>× 0,30</td>" +
                                    $"<td style='padding-left:8px;text-align:right'>= {F(funderDeel)} m³</td></tr>" +
                                    $"<tr style='font-size:.75rem;color:#6c757d'>" +
                                    $"<td>Ondergronds</td><td style='padding:0 6px;text-align:right'>{gegevensBO.AantalVerdiepingenOndergronds} verd. × {F(totaalGarBerg)} m²</td>" +
                                    $"<td style='text-align:right'>× 3,50</td>" +
                                    $"<td style='padding-left:8px;text-align:right'>= {F(ondergDeel)} m³</td></tr>" +
                                    $"<tr style='font-size:.75rem;color:#6c757d;border-bottom:1px dashed #ccc'>" +
                                    $"<td colspan='3'>Volume totaal</td>" +
                                    $"<td style='padding-left:8px;text-align:right'>= {F(enkel.hoev)} m³</td></tr>";
                            }
                        }

                        groep.Lijnen.Add(bo);
                    }

                    return groep;
                })
                .ToList();

            return groepen;
        }

        public async Task<Response> SaveLijnenAsync(int budgetVersieId, List<BudgetActivityLijnBO> lijnen)
        {
            var response = new Response();

            var bestaandeLijnen = await _uow.BudgetActivityLijnen.GetNormal()
                .Where(l => l.BudgetVersieId == budgetVersieId)
                .ToListAsync();

            var bestaandeByActivity = bestaandeLijnen.ToDictionary(l => l.ActivityId, l => l);

            foreach (var bo in lijnen)
            {
                if (bestaandeByActivity.TryGetValue(bo.ActivityId, out var bestaande))
                {
                    bestaande.AlternatievePrijsPerEenheid = bo.AlternatievePrijsPerEenheid;
                    bestaande.NacalcPrijsPerEenheid       = bo.NacalcPrijsPerEenheid;
                    bestaande.Correctiefactor             = bo.Correctiefactor;
                    bestaande.IsManueel                   = bo.IsManueel;
                }
                else
                {
                    _uow.BudgetActivityLijnen.Add(new BudgetActivityLijnen
                    {
                        BudgetVersieId             = budgetVersieId,
                        ActivityId                 = bo.ActivityId,
                        AlternatievePrijsPerEenheid = bo.AlternatievePrijsPerEenheid,
                        NacalcPrijsPerEenheid       = bo.NacalcPrijsPerEenheid,
                        Correctiefactor             = bo.Correctiefactor,
                        IsManueel                  = bo.IsManueel,
                        VerhogingsPerc             = 0m
                    });
                }
            }

            int affected = await _uow.SaveChangesAsync();
            response.AddSaveChangesResult(affected,
                "Activiteitenlijnen opgeslagen.",
                "Geen wijzigingen opgeslagen.");
            return response;
        }

        public async Task<IEnumerable<SelectListItem>> GetProjectenVoorNacalcAsync()
        {
            // Geeft alle unieke projecten die budget masters hebben
            var projecten = await _uow.BudgetMasters.GetNoTracking()
                .Include(m => m.Project)
                .Where(m => m.Project != null && !m.IsGearchiveerd)
                .Select(m => new { m.ProjectId, m.Project })
                .Distinct()
                .OrderBy(x => x.ProjectId)
                .ToListAsync();

            return projecten
                .DistinctBy(x => x.ProjectId)
                .Select(x => new SelectListItem
                {
                    Value = x.ProjectId.ToString(),
                    Text  = x.Project.ProjectName ?? $"Project {x.ProjectId}"
                })
                .ToList();
        }
    }
}

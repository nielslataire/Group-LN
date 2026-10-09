using System;
using System.Collections.Generic;
using System.Linq;
using BOCore;
using DALCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Translators;

namespace ServiceCore
{
    public class BudgetWizardService : IBudgetService
    {
        private readonly UnitOfWorkCore _uow;

        public BudgetWizardService(UnitOfWorkCore uow)
        {
            _uow = uow;
        }

        // ── BudgetMaster ──────────────────────────────────────────────────────

        public GetResponse<BudgetMasterBO> GetBudgetMasters(int projectId)
        {
            var response = new GetResponse<BudgetMasterBO>();

            var entities = _uow.BudgetMasters.GetNoTracking()
                .Where(m => m.ProjectId == projectId && !m.IsGearchiveerd)
                .Include(m => m.BudgetVersies)
                .OrderBy(m => m.Id)
                .ToList();

            foreach (var entity in entities)
                response.AddValue(BudgetWizardTranslator.TranslateMasterToBO(entity));

            return response;
        }

        public GetResponse<BudgetMasterBO> GetBudgetMaster(int masterId)
        {
            var response = new GetResponse<BudgetMasterBO>();

            var entity = _uow.BudgetMasters.GetNoTracking()
                .Where(m => m.Id == masterId)
                .Include(m => m.BudgetVersies)
                .SingleOrDefault();

            if (entity == null)
            {
                response.AddError("Budget master niet gevonden.");
                return response;
            }

            response.Value = BudgetWizardTranslator.TranslateMasterToBO(entity);
            return response;
        }

        public Response CreateBudgetMaster(BudgetMasterBO master, int userId)
        {
            var response = new Response();

            if (string.IsNullOrWhiteSpace(master.Naam))
            {
                response.AddError("Naam is verplicht.");
                return response;
            }

            var masterEntity = new BudgetMaster
            {
                ProjectId       = master.ProjectId,
                Naam            = master.Naam,
                Omschrijving    = master.Omschrijving,
                IsActief        = true,
                IsGearchiveerd  = false,
                CreatedAt       = DateTime.Now,
                CreatedByUserId = userId
            };

            _uow.BudgetMasters.Add(masterEntity);
            _uow.SaveChanges();

            var versieEntity = new BudgetVersie
            {
                BudgetMasterId  = masterEntity.Id,
                ProjectId       = master.ProjectId,
                Versienummer    = 1,
                VersieNaam      = null,
                Status          = "Concept",
                IsHuidig        = true,
                CreatedAt       = DateTime.Now,
                CreatedByUserId = userId
            };

            _uow.BudgetVersies.Add(versieEntity);
            _uow.SaveChanges();

            var gegevensEntity = new BudgetGegevens
            {
                BudgetVersieId              = versieEntity.Id,
                GevelMetselwerkPrijsPerM2   = 165m,
                GipswerkenPrijsPerM2        = 2759m
            };

            _uow.BudgetGegevens.Add(gegevensEntity);
            _uow.SaveChanges();

            response.InsertedId = versieEntity.Id;
            response.AddSuccess("Budget aangemaakt.");
            return response;
        }

        public Response UpdateBudgetMaster(BudgetMasterBO master)
        {
            var response = new Response();

            var entity = _uow.BudgetMasters.GetNoTracking()
                .SingleOrDefault(m => m.Id == master.Id);

            if (entity == null)
            {
                response.AddError("Budget master niet gevonden.");
                return response;
            }

            entity.Naam         = master.Naam;
            entity.Omschrijving = master.Omschrijving;

            _uow.BudgetMasters.Update(entity);
            int affected = _uow.SaveChanges();
            response.AddSaveChangesResult(affected, "Budget bijgewerkt.", "Geen wijzigingen opgeslagen.");
            return response;
        }

        public Response ArchiveBudgetMaster(int masterId)
        {
            var response = new Response();

            var entity = _uow.BudgetMasters.GetNoTracking()
                .SingleOrDefault(m => m.Id == masterId);

            if (entity == null)
            {
                response.AddError("Budget master niet gevonden.");
                return response;
            }

            entity.IsGearchiveerd = true;
            entity.IsActief = false;

            _uow.BudgetMasters.Update(entity);
            int affected = _uow.SaveChanges();
            response.AddSaveChangesResult(affected, "Budget gearchiveerd.", "Geen wijzigingen opgeslagen.");
            return response;
        }

        // ── BudgetVersie ──────────────────────────────────────────────────────

        public GetResponse<BudgetVersieBO> GetBudgetVersies(int masterId)
        {
            var response = new GetResponse<BudgetVersieBO>();

            var entities = _uow.BudgetVersies.GetNoTracking()
                .Where(v => v.BudgetMasterId == masterId)
                .OrderByDescending(v => v.Versienummer)
                .ToList();

            foreach (var entity in entities)
                response.AddValue(BudgetWizardTranslator.TranslateVersieToBO(entity));

            return response;
        }

        public GetResponse<BudgetVersieBO> GetActiefVersie(int masterId)
        {
            var response = new GetResponse<BudgetVersieBO>();

            var entity = _uow.BudgetVersies.GetNoTracking()
                .SingleOrDefault(v => v.BudgetMasterId == masterId && v.IsHuidig);

            if (entity == null)
            {
                response.AddError("Geen actieve versie gevonden.");
                return response;
            }

            response.Value = BudgetWizardTranslator.TranslateVersieToBO(entity);
            return response;
        }

        public Response CreateNieuweVersie(int masterId, string versieNaam, string notitie, int userId)
        {
            var response = new Response();

            var master = _uow.BudgetMasters.GetNoTracking()
                .SingleOrDefault(m => m.Id == masterId);

            if (master == null)
            {
                response.AddError("Budget master niet gevonden.");
                return response;
            }

            var huidigeVersie = _uow.BudgetVersies.GetNoTracking()
                .SingleOrDefault(v => v.BudgetMasterId == masterId && v.IsHuidig);

            int volgendNummer = _uow.BudgetVersies.GetNoTracking()
                .Where(v => v.BudgetMasterId == masterId)
                .Max(v => (int?)v.Versienummer) ?? 0;
            volgendNummer++;

            // Deactiveer huidige versie
            if (huidigeVersie != null)
            {
                huidigeVersie.IsHuidig = false;
                _uow.BudgetVersies.Update(huidigeVersie);
            }

            var nieuweVersie = new BudgetVersie
            {
                BudgetMasterId  = masterId,
                ProjectId       = master.ProjectId,
                Versienummer    = volgendNummer,
                VersieNaam      = versieNaam,
                Status          = "Concept",
                IsHuidig        = true,
                Notitie         = notitie,
                CreatedAt       = DateTime.Now,
                CreatedByUserId = userId
            };

            _uow.BudgetVersies.Add(nieuweVersie);
            _uow.SaveChanges();

            if (huidigeVersie != null)
            {
                // Volledige kopie van de huidige versie — dezelfde helper als "Herstel als nieuwe versie" (okt. 2026:
                // voordien gingen activiteitenlijnen, parameters en verkooplijnen hier verloren).
                var kopie = KopieerVersieInhoud(huidigeVersie.Id, nieuweVersie.Id);
                if (!kopie.Success) return kopie;
            }
            else
            {
                // Eerste versie van een master: lege gegevensrij met de historische startwaarden.
                _uow.BudgetGegevens.Add(new BudgetGegevens
                {
                    BudgetVersieId            = nieuweVersie.Id,
                    GevelMetselwerkPrijsPerM2 = 165m,
                    GipswerkenPrijsPerM2      = 2759m
                });
                _uow.SaveChanges();
            }

            response.InsertedId = nieuweVersie.Id;
            response.AddSuccess($"Versie v{volgendNummer} aangemaakt.");
            return response;
        }

        /// <summary>Kopieert álle inhoud van een versie naar een bestaande doelversie: gegevens (elk veld), oppervlaktes, sanitair,
        /// gevel-/dakelementen, activiteitenlijnen (alt.-/nacalc-prijs, correctie), parameters en verkooplijnen. Nieuwe kolommen op een
        /// van deze tabellen horen hier bij — dit is de enige plaats waar een versie gekopieerd wordt.</summary>
        public Response KopieerVersieInhoud(int bronVersieId, int doelVersieId)
        {
            var response = new Response();
            if (bronVersieId == doelVersieId)
            {
                response.AddError("Bron en doel zijn dezelfde versie.");
                return response;
            }

            // Aanpasbare VMSW-factoren en bevestigde aandachtspunten reizen mee (migratie 078); status/vastzetting niet.
            var bronVersie = _uow.BudgetVersies.GetNoTracking().FirstOrDefault(x => x.Id == bronVersieId);
            var doelVersie = _uow.BudgetVersies.GetNormal().FirstOrDefault(x => x.Id == doelVersieId);
            if (bronVersie != null && doelVersie != null)
            {
                doelVersie.VmswFactoren = bronVersie.VmswFactoren;
                doelVersie.WaarschuwingenBevestigd = bronVersie.WaarschuwingenBevestigd;
                doelVersie.LaatsteStap = bronVersie.LaatsteStap;
            }

            var g = _uow.BudgetGegevens.GetNoTracking().FirstOrDefault(x => x.BudgetVersieId == bronVersieId);
            if (g != null && !_uow.BudgetGegevens.GetNoTracking().Any(x => x.BudgetVersieId == doelVersieId))
            {
                _uow.BudgetGegevens.Add(new BudgetGegevens
                {
                    BudgetVersieId                 = doelVersieId,
                    Naam                           = g.Naam,
                    Adres                          = g.Adres,
                    BouwheerCompanyId              = g.BouwheerCompanyId,
                    AantalLiften                   = g.AantalLiften,
                    AantalBinnentrappen            = g.AantalBinnentrappen,
                    AantalBovengrondseVerdiepingen = g.AantalBovengrondseVerdiepingen,
                    AantalVerdiepingenOndergronds  = g.AantalVerdiepingenOndergronds,
                    TypePoorten                    = g.TypePoorten,
                    TypeDak                        = g.TypeDak,
                    GevelLeienSidings              = g.GevelLeienSidings,
                    OppFunderingen                 = g.OppFunderingen,
                    M3Grondwerk                    = g.M3Grondwerk,
                    LmBerlinerwanden               = g.LmBerlinerwanden,
                    LmSecanpalen                   = g.LmSecanpalen,
                    M3Onderschoeiingen             = g.M3Onderschoeiingen,
                    AantalVeluxen                  = g.AantalVeluxen,
                    AantalTrapzalen                = g.AantalTrapzalen,
                    AantalToegangspoorten          = g.AantalToegangspoorten,
                    AantalAanTeBouwenBuren         = g.AantalAanTeBouwenBuren,
                    NacalcBasisprijs               = g.NacalcBasisprijs,
                    NacalcBasisJaar                = g.NacalcBasisJaar,
                    SIndexStart                    = g.SIndexStart,
                    SIndexHuidig                   = g.SIndexHuidig,
                    IIndexStart                    = g.IIndexStart,
                    IIndexHuidig                   = g.IIndexHuidig,
                    GevelMetselwerkPrijsPerM2      = g.GevelMetselwerkPrijsPerM2,
                    GipswerkenPrijsPerM2           = g.GipswerkenPrijsPerM2,
                    TerrasPrijsPerM2               = g.TerrasPrijsPerM2
                });
            }

            foreach (var o in _uow.BudgetOppervlaktes.GetNoTracking().Where(x => x.BudgetVersieId == bronVersieId).OrderBy(x => x.SortOrder).ToList())
                _uow.BudgetOppervlaktes.Add(new BudgetOppervlaktes
                {
                    BudgetVersieId          = doelVersieId,
                    EenheidNaam             = o.EenheidNaam,
                    UnitGroupTypeId         = o.UnitGroupTypeId,
                    UnitTypeId              = o.UnitTypeId,
                    SortOrder               = o.SortOrder,
                    BewoonbareOpp           = o.BewoonbareOpp,
                    Tuin                    = o.Tuin,
                    TerrasPrefab            = o.TerrasPrefab,
                    TerrasGelijkvloers      = o.TerrasGelijkvloers,
                    Dakterras               = o.Dakterras,
                    GaragesParkingsBovenGr  = o.GaragesParkingsBovenGr,
                    GarBergOndergronds      = o.GarBergOndergronds,
                    BergGelijkvloers        = o.BergGelijkvloers,
                    Carports                = o.Carports,
                    DoorritGVL              = o.DoorritGVL,
                    Zolder                  = o.Zolder,
                    GemeenschappelijkeDelen = o.GemeenschappelijkeDelen,
                    Wegenis                 = o.Wegenis,
                    Grondopp                = o.Grondopp
                });

            foreach (var s in _uow.BudgetSanitair.GetNoTracking().Where(x => x.BudgetVersieId == bronVersieId).OrderBy(x => x.SortOrder).ToList())
                _uow.BudgetSanitair.Add(new BudgetSanitair
                {
                    BudgetVersieId     = doelVersieId,
                    EenheidNaam        = s.EenheidNaam,
                    UnitTypeId         = s.UnitTypeId,
                    SortOrder          = s.SortOrder,
                    Badkamer           = s.Badkamer,
                    ToiletInBadkamer   = s.ToiletInBadkamer,
                    AfzonderlijkToilet = s.AfzonderlijkToilet,
                    DoucheInBadkamer   = s.DoucheInBadkamer,
                    Douchekamer        = s.Douchekamer
                });

            foreach (var e in _uow.BudgetGevelElementen.GetNoTracking().Where(x => x.BudgetVersieId == bronVersieId).OrderBy(x => x.ElementType).ThenBy(x => x.SortOrder).ToList())
                _uow.BudgetGevelElementen.Add(new BudgetGevelElementen
                {
                    BudgetVersieId = doelVersieId,
                    ElementType    = e.ElementType,
                    EenheidNaam    = e.EenheidNaam,
                    Beschrijving   = e.Beschrijving,
                    Aantal         = e.Aantal,
                    Breedte        = e.Breedte,
                    Hoogte         = e.Hoogte,
                    Lengte         = e.Lengte,
                    SortOrder      = e.SortOrder
                });

            foreach (var l in _uow.BudgetActivityLijnen.GetNoTracking().Where(x => x.BudgetVersieId == bronVersieId).ToList())
                _uow.BudgetActivityLijnen.Add(new BudgetActivityLijnen
                {
                    BudgetVersieId              = doelVersieId,
                    ActivityId                  = l.ActivityId,
                    AlternatievePrijsPerEenheid = l.AlternatievePrijsPerEenheid,
                    NacalcPrijsPerEenheid       = l.NacalcPrijsPerEenheid,
                    Correctiefactor             = l.Correctiefactor,
                    IsManueel                   = l.IsManueel,
                    VerhogingsPerc              = l.VerhogingsPerc,
                    Omschrijving                = l.Omschrijving
                });

            var p = _uow.BudgetParams.GetNoTracking().FirstOrDefault(x => x.BudgetVersieId == bronVersieId);
            if (p != null && !_uow.BudgetParams.GetNoTracking().Any(x => x.BudgetVersieId == doelVersieId))
                _uow.BudgetParams.Add(new BudgetParams
                {
                    BudgetVersieId            = doelVersieId,
                    ProjectcoordinatiePerc    = p.ProjectcoordinatiePerc,
                    ArchitectPerc             = p.ArchitectPerc,
                    VeiligheidscoordEPBPerc   = p.VeiligheidscoordEPBPerc,
                    VentVerslaggeverForfait   = p.VentVerslaggeverForfait,
                    StudieIRPerc              = p.StudieIRPerc,
                    OpmetingSonderingForfait  = p.OpmetingSonderingForfait,
                    DecennaleGeslRuwbouwPerc  = p.DecennaleGeslRuwbouwPerc,
                    ABRPlaatsbeschrPerc       = p.ABRPlaatsbeschrPerc,
                    InfrastructuurForfait     = p.InfrastructuurForfait,
                    LiftPrijsPerStuk          = p.LiftPrijsPerStuk,
                    WetBreynePerc             = p.WetBreynePerc,
                    WetBreyneMaanden          = p.WetBreyneMaanden,
                    StraightloanGebouwPerc    = p.StraightloanGebouwPerc,
                    StraightloanGebouwMaanden = p.StraightloanGebouwMaanden,
                    StraightloanGrondPerc     = p.StraightloanGrondPerc,
                    StraightloanGrondMaanden  = p.StraightloanGrondMaanden,
                    AankoopprijsGrond         = p.AankoopprijsGrond,
                    OnvoorzienPerc            = p.OnvoorzienPerc,
                    PubliciteitForfait        = p.PubliciteitForfait,
                    DoelMargePerc             = p.DoelMargePerc,
                    GrondMargePerc            = p.GrondMargePerc
                });

            foreach (var v in _uow.BudgetVerkoopLijn.GetNoTracking().Where(x => x.BudgetVersieId == bronVersieId).OrderBy(x => x.SortOrder).ToList())
                _uow.BudgetVerkoopLijn.Add(new BudgetVerkoopLijn
                {
                    BudgetVersieId = doelVersieId,
                    EenheidNaam    = v.EenheidNaam,
                    UnitId         = v.UnitId,
                    CodeBouw       = v.CodeBouw,
                    CodeGrond      = v.CodeGrond,
                    OppTuin        = v.OppTuin,
                    OppTerras      = v.OppTerras,
                    OppDakterras   = v.OppDakterras,
                    Grondwaarde    = v.Grondwaarde,
                    Bouwwaarde     = v.Bouwwaarde,
                    Vraagprijs     = v.Vraagprijs,
                    IsRuil         = v.IsRuil,
                    ExtraForfait   = v.ExtraForfait,
                    BouwPrijsPerM2  = v.BouwPrijsPerM2,   // migratie 075
                    GrondPrijsPerM2 = v.GrondPrijsPerM2,
                    PrijsBron       = v.PrijsBron,
                    SortOrder      = v.SortOrder
                });

            // Nacalc: dezelfde referentieprojecten vergelijken (migratie 076)
            foreach (var r in _uow.BudgetVersieNacalcReferenties.GetNoTracking().Where(x => x.BudgetVersieId == bronVersieId).ToList())
                _uow.BudgetVersieNacalcReferenties.Add(new BudgetVersieNacalcReferentie { BudgetVersieId = doelVersieId, ReferentieProjectId = r.ReferentieProjectId });

            _uow.SaveChanges();
            response.AddSuccess("Versie gekopieerd.");
            return response;
        }

        // ── Versiestatus (migratie 078) ───────────────────────────────────────

        public bool IsVergrendeld(int versieId)
            => _uow.BudgetVersies.GetNoTracking().Any(v => v.Id == versieId && v.Status == BudgetVersie.StatusDefinitief);

        public Response AfrondenVersie(int versieId)
        {
            var response = new Response();
            var v = _uow.BudgetVersies.GetNormal().FirstOrDefault(x => x.Id == versieId);
            if (v == null) { response.AddError("Versie niet gevonden."); return response; }
            if (v.Status == BudgetVersie.StatusDefinitief) { response.AddError("Een definitieve versie is al afgerond en vergrendeld."); return response; }
            v.Status = BudgetVersie.StatusAfgerond;
            v.LaatsteStap = 9;
            _uow.SaveChanges();
            response.AddSuccess($"Versie v{v.Versienummer} afgerond.");
            return response;
        }

        public Response MaakDefinitief(int versieId, string door)
        {
            var response = new Response();
            var v = _uow.BudgetVersies.GetNormal().FirstOrDefault(x => x.Id == versieId);
            if (v == null) { response.AddError("Versie niet gevonden."); return response; }

            // Eén definitieve versie per project (ook over budgetten heen): een eerdere wordt weer "Afgerond" en bewerkbaar.
            foreach (var andere in _uow.BudgetVersies.GetNormal().Where(x => x.ProjectId == v.ProjectId && x.Id != versieId && x.Status == BudgetVersie.StatusDefinitief).ToList())
            {
                andere.Status = BudgetVersie.StatusAfgerond;
                andere.VastgezetOp = null;
                andere.VastgezetDoor = null;
            }
            v.Status = BudgetVersie.StatusDefinitief;
            v.VastgezetOp = DateTime.Now;
            v.VastgezetDoor = door;
            v.LaatsteStap = 9;
            _uow.SaveChanges();
            response.AddSuccess($"Versie v{v.Versienummer} is nu het definitieve budget: alleen-lezen en basis voor facturen en contracten.");
            return response;
        }

        public Response OntgrendelDefinitief(int versieId)
        {
            var response = new Response();
            var v = _uow.BudgetVersies.GetNormal().FirstOrDefault(x => x.Id == versieId);
            if (v == null) { response.AddError("Versie niet gevonden."); return response; }
            if (v.Status != BudgetVersie.StatusDefinitief) { response.AddInfo("Deze versie is niet definitief."); return response; }
            v.Status = BudgetVersie.StatusAfgerond;
            v.VastgezetOp = null;
            v.VastgezetDoor = null;
            _uow.SaveChanges();
            response.AddSuccess($"Versie v{v.Versienummer} is niet langer definitief en weer bewerkbaar.");
            return response;
        }

        public void RegistreerStap(int versieId, int stap)
        {
            if (stap < 1 || stap > 9) return;
            var v = _uow.BudgetVersies.GetNormal().FirstOrDefault(x => x.Id == versieId);
            if (v == null || v.Status == BudgetVersie.StatusDefinitief) return;
            if ((v.LaatsteStap ?? 0) >= stap) return;
            v.LaatsteStap = (byte)stap;
            _uow.SaveChanges();
        }

        public BOCore.Budget.VmswFactorenBO GetVmswFactoren(int versieId)
        {
            var json = _uow.BudgetVersies.GetNoTracking().Where(v => v.Id == versieId).Select(v => v.VmswFactoren).FirstOrDefault();
            return BOCore.Budget.VmswFactorenBO.VanJson(json);
        }

        public Response SetVmswFactoren(int versieId, BOCore.Budget.VmswFactorenBO factoren)
        {
            var response = new Response();
            var v = _uow.BudgetVersies.GetNormal().FirstOrDefault(x => x.Id == versieId);
            if (v == null) { response.AddError("Versie niet gevonden."); return response; }
            if (factoren == null) factoren = BOCore.Budget.VmswFactorenBO.Standaard;
            if (factoren.Lijst().Any(f => f.Item3 < 0m || f.Item3 > 2m)) { response.AddError("Een reductiefactor ligt tussen 0 en 2."); return response; }
            v.VmswFactoren = factoren.NaarJson();   // NULL als alles standaard is
            _uow.SaveChanges();
            response.AddSuccess(factoren.IsAangepast ? "Reductiefactoren aangepast voor deze versie." : "Standaard VMSW-factoren hersteld.");
            return response;
        }

        public Response BevestigWaarschuwing(int versieId, string sleutel, bool bevestigd, string door = null)
        {
            var response = new Response();
            var v = _uow.BudgetVersies.GetNormal().FirstOrDefault(x => x.Id == versieId);
            if (v == null) { response.AddError("Versie niet gevonden."); return response; }
            // 39k "Negeren bewaart naam en datum": formaat code~naam~yyyy-MM-dd (zie BudgetControleService.ZetGenegeerd)
            v.WaarschuwingenBevestigd = Budget.BudgetControleService.ZetGenegeerd(v.WaarschuwingenBevestigd, sleutel, bevestigd, door);
            _uow.SaveChanges();
            response.AddSuccess("Opgeslagen.");
            return response;
        }

        public Response ActiveerVersie(int versieId)
        {
            var response = new Response();

            var versie = _uow.BudgetVersies.GetNoTracking()
                .SingleOrDefault(v => v.Id == versieId);

            if (versie == null)
            {
                response.AddError("Versie niet gevonden.");
                return response;
            }

            var andereVersies = _uow.BudgetVersies.GetNoTracking()
                .Where(v => v.BudgetMasterId == versie.BudgetMasterId && v.IsHuidig)
                .ToList();

            foreach (var andere in andereVersies)
            {
                andere.IsHuidig = false;
                _uow.BudgetVersies.Update(andere);
            }

            versie.IsHuidig = true;
            _uow.BudgetVersies.Update(versie);
            _uow.SaveChanges();

            response.AddSuccess("Versie geactiveerd.");
            return response;
        }

        // ── BudgetGegevens ────────────────────────────────────────────────────

        public GetResponse<BudgetGegevensBO> GetBudgetGegevens(int versieId)
        {
            var response = new GetResponse<BudgetGegevensBO>();

            var entity = _uow.BudgetGegevens.GetNoTracking()
                .SingleOrDefault(g => g.BudgetVersieId == versieId);

            if (entity == null)
            {
                response.AddError("Gegevens niet gevonden.");
                return response;
            }

            response.Value = BudgetWizardTranslator.TranslateGegevensToBO(entity);
            return response;
        }

        private decimal? SnapIndex(string type, decimal? waarde)
        {
            if (!waarde.HasValue || waarde.Value == 0m) return waarde;
            var rij = Budget.BudgetControleService.ZoekIndexRij(_uow.BouwIndex.GetNoTracking().Where(x => x.IndexType == type).ToList(), waarde.Value);
            return rij != null ? rij.IndexWaarde : waarde;
        }

        public Response SaveBudgetGegevens(BudgetGegevensBO bo, int versieId)
        {
            var response = new Response();

            // De modelbinder rondt af op 2 decimalen; een gekozen indexwaarde uit de historiek (4 decimalen) wordt hier weer
            // op de exacte rij gezet, zodat de peildatum terugvindbaar blijft en de berekening niet afwijkt.
            bo.SIndexHuidig = SnapIndex("S", bo.SIndexHuidig);
            bo.IIndexHuidig = SnapIndex("I2021", bo.IIndexHuidig);

            var entity = _uow.BudgetGegevens.GetNoTracking()
                .SingleOrDefault(g => g.BudgetVersieId == versieId);

            if (entity == null)
            {
                entity = new BudgetGegevens { BudgetVersieId = versieId };
                BudgetWizardTranslator.ApplyGegevensBOToEntity(bo, entity);
                _uow.BudgetGegevens.Add(entity);
            }
            else
            {
                BudgetWizardTranslator.ApplyGegevensBOToEntity(bo, entity);
                _uow.BudgetGegevens.Update(entity);
            }

            int affected = _uow.SaveChanges();
            response.AddSaveChangesResult(affected, "Gegevens opgeslagen.", "Geen wijzigingen opgeslagen.");
            return response;
        }

        // ── BudgetOppervlaktes ────────────────────────────────────────────────

        public GetResponse<BudgetOppervlaktesBO> GetBudgetOppervlaktes(int versieId)
        {
            var response = new GetResponse<BudgetOppervlaktesBO>();

            var entities = _uow.BudgetOppervlaktes.GetNoTracking()
                .Where(o => o.BudgetVersieId == versieId)
                .Include(o => o.UnitGroupType)
                .Include(o => o.UnitType)
                .OrderBy(o => o.SortOrder)
                .ToList();

            var factoren = GetVmswFactoren(versieId);
            foreach (var e in entities)
            {
                var bo = BudgetWizardTranslator.TranslateOppervlaktesToBO(e);
                bo.Factoren = factoren;
                response.AddValue(bo);
            }

            return response;
        }

        public GetResponse<BudgetOppervlaktesTotaalBO> GetBudgetOppervlaktesTotaal(int versieId)
        {
            var response = new GetResponse<BudgetOppervlaktesTotaalBO>();

            var rows = GetBudgetOppervlaktes(versieId);
            response.Value = BudgetWizardTranslator.BuildTotalen(rows.Values ?? new List<BudgetOppervlaktesBO>());
            return response;
        }

        public Response SaveBudgetOppervlaktes(List<BudgetOppervlaktesBO> rows, int versieId)
        {
            var response = new Response();

            var existing = _uow.BudgetOppervlaktes.GetNormal()
                .Where(o => o.BudgetVersieId == versieId)
                .ToList();

            foreach (var e in existing)
                _uow.BudgetOppervlaktes.Remove(e);

            int sort = 0;
            foreach (var bo in rows)
            {
                var entity = new BudgetOppervlaktes { BudgetVersieId = versieId, SortOrder = sort++ };
                BudgetWizardTranslator.ApplyOppervlaktesBOToEntity(bo, entity);
                _uow.BudgetOppervlaktes.Add(entity);
            }

            _uow.SaveChanges();
            response.AddSuccess("Oppervlaktes opgeslagen.");
            return response;
        }

        public Response AddBudgetOppervlaktesRij(BudgetOppervlaktesBO rij, int versieId)
        {
            var response = new Response();

            int maxSort = _uow.BudgetOppervlaktes.GetNoTracking()
                .Where(o => o.BudgetVersieId == versieId)
                .Max(o => (int?)o.SortOrder) ?? -1;

            var entity = new BudgetOppervlaktes
            {
                BudgetVersieId = versieId,
                SortOrder      = maxSort + 1,
                EenheidNaam    = rij.EenheidNaam ?? ""
            };
            BudgetWizardTranslator.ApplyOppervlaktesBOToEntity(rij, entity);
            entity.BudgetVersieId = versieId;

            _uow.BudgetOppervlaktes.Add(entity);
            _uow.SaveChanges();

            response.InsertedId = entity.Id;
            response.AddSuccess("Rij toegevoegd.");
            return response;
        }

        public Response DeleteBudgetOppervlaktesRij(int rijId, int versieId)
        {
            var response = new Response();

            var entity = _uow.BudgetOppervlaktes.GetNoTracking()
                .SingleOrDefault(o => o.Id == rijId && o.BudgetVersieId == versieId);

            if (entity == null)
            {
                response.AddError("Rij niet gevonden.");
                return response;
            }

            _uow.BudgetOppervlaktes.Remove(entity);
            _uow.SaveChanges();
            response.AddSuccess("Rij verwijderd.");
            return response;
        }

        public Response UpdateBudgetOppervlaktesRij(BudgetOppervlaktesBO rij)
        {
            var response = new Response();

            var entity = _uow.BudgetOppervlaktes.GetNoTracking()
                .SingleOrDefault(o => o.Id == rij.Id);

            if (entity == null)
            {
                response.AddError("Rij niet gevonden.");
                return response;
            }

            BudgetWizardTranslator.ApplyOppervlaktesBOToEntity(rij, entity);
            _uow.BudgetOppervlaktes.Update(entity);
            _uow.SaveChanges();
            response.AddSuccess("Rij bijgewerkt.");
            return response;
        }

        public Response ReorderBudgetOppervlaktes(List<int> orderedIds, int versieId)
        {
            var response = new Response();

            var entities = _uow.BudgetOppervlaktes.GetNormal()
                .Where(o => o.BudgetVersieId == versieId)
                .ToList();

            for (int i = 0; i < orderedIds.Count; i++)
            {
                var e = entities.SingleOrDefault(o => o.Id == orderedIds[i]);
                if (e != null) e.SortOrder = i;
            }

            _uow.SaveChanges();
            response.AddSuccess("Volgorde bijgewerkt.");
            return response;
        }

        // ── BudgetGevelElementen ──────────────────────────────────────────────

        public GetResponse<BudgetGevelElementBO> GetBudgetGevelElementen(int versieId, string elementType = null)
        {
            var response = new GetResponse<BudgetGevelElementBO>();

            var query = _uow.BudgetGevelElementen.GetNoTracking()
                .Where(g => g.BudgetVersieId == versieId);

            if (!string.IsNullOrEmpty(elementType))
                query = query.Where(g => g.ElementType == elementType);

            var entities = query
                .OrderBy(g => g.ElementType)
                .ThenBy(g => g.SortOrder)
                .ToList();

            foreach (var e in entities)
                response.AddValue(BudgetWizardTranslator.TranslateGevelToBO(e));

            return response;
        }

        public GetResponse<BudgetGevelTotaalBO> GetBudgetGevelTotaal(int versieId)
        {
            var response = new GetResponse<BudgetGevelTotaalBO>();

            var rows = GetBudgetGevelElementen(versieId);
            response.Value = BudgetWizardTranslator.BuildGevelTotalen(
                rows.Values ?? new List<BudgetGevelElementBO>());

            return response;
        }

        public Response AddBudgetGevelElement(BudgetGevelElementBO element, int versieId)
        {
            var response = new Response();

            int maxSort = _uow.BudgetGevelElementen.GetNoTracking()
                .Where(g => g.BudgetVersieId == versieId && g.ElementType == element.ElementType)
                .Max(g => (int?)g.SortOrder) ?? -1;

            var entity = new BudgetGevelElementen
            {
                BudgetVersieId = versieId,
                ElementType    = element.ElementType,
                EenheidNaam    = element.EenheidNaam,
                Beschrijving   = element.Beschrijving,
                Aantal         = element.Aantal > 0 ? element.Aantal : 1m,
                SortOrder      = maxSort + 1
            };

            _uow.BudgetGevelElementen.Add(entity);
            _uow.SaveChanges();

            response.InsertedId = entity.Id;
            response.AddSuccess("Element toegevoegd.");
            return response;
        }

        public Response UpdateBudgetGevelElement(BudgetGevelElementBO element)
        {
            var response = new Response();

            var entity = _uow.BudgetGevelElementen.GetNoTracking()
                .SingleOrDefault(g => g.Id == element.Id);

            if (entity == null)
            {
                response.AddError("Element niet gevonden.");
                return response;
            }

            BudgetWizardTranslator.ApplyGevelBOToEntity(element, entity);
            _uow.BudgetGevelElementen.Update(entity);
            _uow.SaveChanges();

            response.AddSuccess("Element bijgewerkt.");
            return response;
        }

        public Response DeleteBudgetGevelElement(int elementId, int versieId)
        {
            var response = new Response();

            var entity = _uow.BudgetGevelElementen.GetNoTracking()
                .SingleOrDefault(g => g.Id == elementId && g.BudgetVersieId == versieId);

            if (entity == null)
            {
                response.AddError("Element niet gevonden.");
                return response;
            }

            _uow.BudgetGevelElementen.Remove(entity);
            _uow.SaveChanges();
            response.AddSuccess("Element verwijderd.");
            return response;
        }

        public Response ReorderBudgetGevelElementen(List<int> orderedIds, int versieId, string elementType)
        {
            var response = new Response();

            var entities = _uow.BudgetGevelElementen.GetNormal()
                .Where(g => g.BudgetVersieId == versieId && g.ElementType == elementType)
                .ToList();

            for (int i = 0; i < orderedIds.Count; i++)
            {
                var e = entities.SingleOrDefault(g => g.Id == orderedIds[i]);
                if (e != null) e.SortOrder = i;
            }

            _uow.SaveChanges();
            response.AddSuccess("Volgorde bijgewerkt.");
            return response;
        }

        // ── BudgetSanitair ────────────────────────────────────────────────────

        public GetResponse<BudgetSanitairBO> GetBudgetSanitair(int versieId)
        {
            var response = new GetResponse<BudgetSanitairBO>();

            var entities = _uow.BudgetSanitair.GetNoTracking()
                .Where(s => s.BudgetVersieId == versieId)
                .OrderBy(s => s.SortOrder)
                .ToList();

            foreach (var e in entities)
                response.AddValue(BudgetWizardTranslator.TranslateSanitairToBO(e));

            return response;
        }

        public GetResponse<BudgetSanitairTotaalBO> GetBudgetSanitairTotaal(int versieId)
        {
            var response = new GetResponse<BudgetSanitairTotaalBO>();

            var rows = GetBudgetSanitair(versieId);
            response.Value = BudgetWizardTranslator.BuildSanitairTotalen(
                rows.Values ?? new List<BudgetSanitairBO>());

            return response;
        }

        public Response AddBudgetSanitairRij(BudgetSanitairBO rij, int versieId)
        {
            var response = new Response();

            int maxSort = _uow.BudgetSanitair.GetNoTracking()
                .Where(s => s.BudgetVersieId == versieId)
                .Max(s => (int?)s.SortOrder) ?? -1;

            var entity = new BudgetSanitair
            {
                BudgetVersieId = versieId,
                EenheidNaam    = rij.EenheidNaam ?? "",
                UnitTypeId     = rij.UnitTypeId,
                SortOrder      = maxSort + 1
            };

            _uow.BudgetSanitair.Add(entity);
            _uow.SaveChanges();

            response.InsertedId = entity.Id;
            response.AddSuccess("Rij toegevoegd.");
            return response;
        }

        public Response UpdateBudgetSanitairRij(BudgetSanitairBO rij)
        {
            var response = new Response();

            var entity = _uow.BudgetSanitair.GetNoTracking()
                .SingleOrDefault(s => s.Id == rij.Id);

            if (entity == null)
            {
                response.AddError("Rij niet gevonden.");
                return response;
            }

            BudgetWizardTranslator.ApplySanitairBOToEntity(rij, entity);
            _uow.BudgetSanitair.Update(entity);
            _uow.SaveChanges();

            response.AddSuccess("Rij bijgewerkt.");
            return response;
        }

        public Response DeleteBudgetSanitairRij(int rijId, int versieId)
        {
            var response = new Response();

            var entity = _uow.BudgetSanitair.GetNoTracking()
                .SingleOrDefault(s => s.Id == rijId && s.BudgetVersieId == versieId);

            if (entity == null)
            {
                response.AddError("Rij niet gevonden.");
                return response;
            }

            _uow.BudgetSanitair.Remove(entity);
            _uow.SaveChanges();
            response.AddSuccess("Rij verwijderd.");
            return response;
        }

        public Response SyncSanitairVanOppervlaktes(int versieId)
        {
            var response = new Response();

            var oppRows = _uow.BudgetOppervlaktes.GetNoTracking()
                .Where(o => o.BudgetVersieId == versieId && o.BewoonbareOpp > 0)
                .OrderBy(o => o.SortOrder)
                .ToList();

            var bestaandeNamen = _uow.BudgetSanitair.GetNoTracking()
                .Where(s => s.BudgetVersieId == versieId)
                .Select(s => s.EenheidNaam)
                .ToHashSet();

            int maxSort = _uow.BudgetSanitair.GetNoTracking()
                .Where(s => s.BudgetVersieId == versieId)
                .Max(s => (int?)s.SortOrder) ?? -1;

            bool added = false;
            foreach (var opp in oppRows)
            {
                if (bestaandeNamen.Contains(opp.EenheidNaam))
                    continue;

                _uow.BudgetSanitair.Add(new BudgetSanitair
                {
                    BudgetVersieId = versieId,
                    EenheidNaam    = opp.EenheidNaam,
                    UnitTypeId     = opp.UnitTypeId,
                    SortOrder      = ++maxSort
                });
                added = true;
            }

            if (added)
                _uow.SaveChanges();

            response.AddSuccess("Synchronisatie voltooid.");
            return response;
        }
    }
}

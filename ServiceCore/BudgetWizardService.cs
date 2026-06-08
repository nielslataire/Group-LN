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
                .Include(v => v.BudgetGegevens)
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

            // Deep copy BudgetOppervlaktes
            var bronOppervlaktes = huidigeVersie != null
                ? _uow.BudgetOppervlaktes.GetNoTracking()
                    .Where(o => o.BudgetVersieId == huidigeVersie.Id)
                    .OrderBy(o => o.SortOrder)
                    .ToList()
                : new List<BudgetOppervlaktes>();

            foreach (var src in bronOppervlaktes)
            {
                _uow.BudgetOppervlaktes.Add(new BudgetOppervlaktes
                {
                    BudgetVersieId          = nieuweVersie.Id,
                    EenheidNaam             = src.EenheidNaam,
                    UnitGroupTypeId         = src.UnitGroupTypeId,
                    UnitTypeId              = src.UnitTypeId,
                    SortOrder               = src.SortOrder,
                    BewoonbareOpp           = src.BewoonbareOpp,
                    Tuin                    = src.Tuin,
                    TerrasPrefab            = src.TerrasPrefab,
                    TerrasGelijkvloers      = src.TerrasGelijkvloers,
                    Dakterras               = src.Dakterras,
                    GaragesParkingsBovenGr  = src.GaragesParkingsBovenGr,
                    GarBergOndergronds      = src.GarBergOndergronds,
                    BergGelijkvloers        = src.BergGelijkvloers,
                    Carports                = src.Carports,
                    DoorritGVL              = src.DoorritGVL,
                    Zolder                  = src.Zolder,
                    GemeenschappelijkeDelen = src.GemeenschappelijkeDelen,
                    Wegenis                 = src.Wegenis,
                    Grondopp                = src.Grondopp
                });
            }

            // Deep copy van gegevens van de huidige versie
            var gegevensEntity = new BudgetGegevens
            {
                BudgetVersieId              = nieuweVersie.Id,
                GevelMetselwerkPrijsPerM2   = 165m,
                GipswerkenPrijsPerM2        = 2759m
            };

            if (huidigeVersie?.BudgetGegevens != null)
            {
                var src = huidigeVersie.BudgetGegevens;
                gegevensEntity.Naam                             = src.Naam;
                gegevensEntity.Adres                            = src.Adres;
                gegevensEntity.BouwheerCompanyId                = src.BouwheerCompanyId;
                gegevensEntity.AantalLiften                     = src.AantalLiften;
                gegevensEntity.AantalBinnentrappen              = src.AantalBinnentrappen;
                gegevensEntity.AantalBovengrondseVerdiepingen   = src.AantalBovengrondseVerdiepingen;
                gegevensEntity.AantalVerdiepingenOndergronds    = src.AantalVerdiepingenOndergronds;
                gegevensEntity.TypePoorten                      = src.TypePoorten;
                gegevensEntity.TypeDak                          = src.TypeDak;
                gegevensEntity.GevelLeienSidings                = src.GevelLeienSidings;
                gegevensEntity.OppFunderingen                   = src.OppFunderingen;
                gegevensEntity.M3Grondwerk                      = src.M3Grondwerk;
                gegevensEntity.LmBerlinerwanden                 = src.LmBerlinerwanden;
                gegevensEntity.LmSecanpalen                     = src.LmSecanpalen;
                gegevensEntity.NacalcBasisprijs                 = src.NacalcBasisprijs;
                gegevensEntity.NacalcBasisJaar                  = src.NacalcBasisJaar;
                gegevensEntity.SIndexStart                      = src.SIndexStart;
                gegevensEntity.SIndexHuidig                     = src.SIndexHuidig;
                gegevensEntity.IIndexStart                      = src.IIndexStart;
                gegevensEntity.IIndexHuidig                     = src.IIndexHuidig;
                gegevensEntity.GevelMetselwerkPrijsPerM2        = src.GevelMetselwerkPrijsPerM2;
                gegevensEntity.GipswerkenPrijsPerM2             = src.GipswerkenPrijsPerM2;
                gegevensEntity.TerrasPrijsPerM2                 = src.TerrasPrijsPerM2;
            }

            _uow.BudgetGegevens.Add(gegevensEntity);
            _uow.SaveChanges();

            // Deep copy BudgetGevelElementen
            var bronGevel = huidigeVersie != null
                ? _uow.BudgetGevelElementen.GetNoTracking()
                    .Where(g => g.BudgetVersieId == huidigeVersie.Id)
                    .OrderBy(g => g.ElementType).ThenBy(g => g.SortOrder)
                    .ToList()
                : new List<BudgetGevelElementen>();

            foreach (var src in bronGevel)
            {
                _uow.BudgetGevelElementen.Add(new BudgetGevelElementen
                {
                    BudgetVersieId = nieuweVersie.Id,
                    ElementType    = src.ElementType,
                    EenheidNaam    = src.EenheidNaam,
                    Beschrijving   = src.Beschrijving,
                    Aantal         = src.Aantal,
                    Breedte        = src.Breedte,
                    Hoogte         = src.Hoogte,
                    Lengte         = src.Lengte,
                    SortOrder      = src.SortOrder
                });
            }

            // Deep copy BudgetSanitair
            var bronSanitair = huidigeVersie != null
                ? _uow.BudgetSanitair.GetNoTracking()
                    .Where(s => s.BudgetVersieId == huidigeVersie.Id)
                    .OrderBy(s => s.SortOrder)
                    .ToList()
                : new List<BudgetSanitair>();

            foreach (var src in bronSanitair)
            {
                _uow.BudgetSanitair.Add(new BudgetSanitair
                {
                    BudgetVersieId      = nieuweVersie.Id,
                    EenheidNaam         = src.EenheidNaam,
                    UnitTypeId          = src.UnitTypeId,
                    SortOrder           = src.SortOrder,
                    Badkamer            = src.Badkamer,
                    ToiletInBadkamer    = src.ToiletInBadkamer,
                    AfzonderlijkToilet  = src.AfzonderlijkToilet,
                    DoucheInBadkamer    = src.DoucheInBadkamer,
                    Douchekamer         = src.Douchekamer
                });
            }
            _uow.SaveChanges();

            response.InsertedId = nieuweVersie.Id;
            response.AddSuccess($"Versie v{volgendNummer} aangemaakt.");
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

        public Response SaveBudgetGegevens(BudgetGegevensBO bo, int versieId)
        {
            var response = new Response();

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

            foreach (var e in entities)
                response.AddValue(BudgetWizardTranslator.TranslateOppervlaktesToBO(e));

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

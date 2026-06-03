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
                gegevensEntity.AbexBasisIndex                   = src.AbexBasisIndex;
                gegevensEntity.AbexHuidigIndex                  = src.AbexHuidigIndex;
                gegevensEntity.GevelMetselwerkPrijsPerM2        = src.GevelMetselwerkPrijsPerM2;
                gegevensEntity.GipswerkenPrijsPerM2             = src.GipswerkenPrijsPerM2;
            }

            _uow.BudgetGegevens.Add(gegevensEntity);
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
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using BOCore;
using DALCore.Models;

namespace ServiceCore.Translators
{
    internal static class BudgetWizardTranslator
    {
        internal static BudgetMasterBO TranslateMasterToBO(BudgetMaster entity)
        {
            if (entity == null) return null;

            var bo = new BudgetMasterBO();
            bo.Id               = entity.Id;
            bo.ProjectId        = entity.ProjectId;
            bo.Naam             = entity.Naam;
            bo.Omschrijving     = entity.Omschrijving;
            bo.IsActief         = entity.IsActief;
            bo.IsGearchiveerd   = entity.IsGearchiveerd;
            bo.CreatedAt        = entity.CreatedAt;
            bo.CreatedByUserId  = entity.CreatedByUserId;

            if (entity.BudgetVersies != null)
            {
                bo.Versies = entity.BudgetVersies
                    .OrderByDescending(v => v.Versienummer)
                    .Select(v => TranslateVersieToBO(v))
                    .ToList();
            }

            return bo;
        }

        internal static BudgetVersieBO TranslateVersieToBO(BudgetVersie entity)
        {
            if (entity == null) return null;

            var bo = new BudgetVersieBO();
            bo.Id               = entity.Id;
            bo.BudgetMasterId   = entity.BudgetMasterId;
            bo.ProjectId        = entity.ProjectId;
            bo.Versienummer     = entity.Versienummer;
            bo.VersieNaam       = entity.VersieNaam;
            bo.Status           = entity.Status;
            bo.IsHuidig         = entity.IsHuidig;
            bo.Notitie          = entity.Notitie;
            bo.CreatedAt        = entity.CreatedAt;
            bo.CreatedByUserId  = entity.CreatedByUserId;

            return bo;
        }

        internal static BudgetGegevensBO TranslateGegevensToBO(BudgetGegevens entity)
        {
            if (entity == null) return null;

            var bo = new BudgetGegevensBO();
            bo.Id                               = entity.Id;
            bo.BudgetVersieId                   = entity.BudgetVersieId;
            bo.Naam                             = entity.Naam;
            bo.Adres                            = entity.Adres;
            bo.BouwheerCompanyId                = entity.BouwheerCompanyId;
            bo.AantalLiften                     = entity.AantalLiften;
            bo.AantalBinnentrappen              = entity.AantalBinnentrappen;
            bo.AantalBovengrondseVerdiepingen   = entity.AantalBovengrondseVerdiepingen;
            bo.AantalVerdiepingenOndergronds    = entity.AantalVerdiepingenOndergronds;
            bo.TypePoorten                      = entity.TypePoorten;
            bo.TypeDak                          = entity.TypeDak;
            bo.GevelLeienSidings                = entity.GevelLeienSidings;
            bo.OppFunderingen                   = entity.OppFunderingen;
            bo.M3Grondwerk                      = entity.M3Grondwerk;
            bo.LmBerlinerwanden                 = entity.LmBerlinerwanden;
            bo.LmSecanpalen                     = entity.LmSecanpalen;
            bo.NacalcBasisprijs                 = entity.NacalcBasisprijs;
            bo.NacalcBasisJaar                  = entity.NacalcBasisJaar;
            bo.ABEXBasisIndex                   = entity.AbexBasisIndex;
            bo.ABEXHuidigIndex                  = entity.AbexHuidigIndex;
            bo.GevelMetselwerkPrijsPerM2        = entity.GevelMetselwerkPrijsPerM2;
            bo.GipswerkenPrijsPerM2             = entity.GipswerkenPrijsPerM2;
            bo.UpdatedAt                        = entity.UpdatedAt;

            return bo;
        }

        internal static void ApplyGegevensBOToEntity(BudgetGegevensBO bo, BudgetGegevens entity)
        {
            entity.Naam                             = bo.Naam;
            entity.Adres                            = bo.Adres;
            entity.BouwheerCompanyId                = bo.BouwheerCompanyId;
            entity.AantalLiften                     = bo.AantalLiften;
            entity.AantalBinnentrappen              = bo.AantalBinnentrappen;
            entity.AantalBovengrondseVerdiepingen   = bo.AantalBovengrondseVerdiepingen;
            entity.AantalVerdiepingenOndergronds    = bo.AantalVerdiepingenOndergronds;
            entity.TypePoorten                      = bo.TypePoorten;
            entity.TypeDak                          = bo.TypeDak;
            entity.GevelLeienSidings                = bo.GevelLeienSidings;
            entity.OppFunderingen                   = bo.OppFunderingen;
            entity.M3Grondwerk                      = bo.M3Grondwerk;
            entity.LmBerlinerwanden                 = bo.LmBerlinerwanden;
            entity.LmSecanpalen                     = bo.LmSecanpalen;
            entity.NacalcBasisprijs                 = bo.NacalcBasisprijs;
            entity.NacalcBasisJaar                  = bo.NacalcBasisJaar;
            entity.AbexBasisIndex                   = bo.ABEXBasisIndex;
            entity.AbexHuidigIndex                  = bo.ABEXHuidigIndex;
            entity.GevelMetselwerkPrijsPerM2        = bo.GevelMetselwerkPrijsPerM2;
            entity.GipswerkenPrijsPerM2             = bo.GipswerkenPrijsPerM2;
            entity.UpdatedAt                        = DateTime.Now;
        }
    }
}

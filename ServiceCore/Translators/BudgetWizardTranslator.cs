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
            bo.SIndexStart                      = entity.SIndexStart;
            bo.SIndexHuidig                     = entity.SIndexHuidig;
            bo.IIndexStart                      = entity.IIndexStart;
            bo.IIndexHuidig                     = entity.IIndexHuidig;
            bo.GevelMetselwerkPrijsPerM2        = entity.GevelMetselwerkPrijsPerM2;
            bo.GipswerkenPrijsPerM2             = entity.GipswerkenPrijsPerM2;
            bo.TerrasPrijsPerM2                 = entity.TerrasPrijsPerM2;
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
            entity.SIndexStart                      = bo.SIndexStart;
            entity.SIndexHuidig                     = bo.SIndexHuidig;
            entity.IIndexStart                      = bo.IIndexStart;
            entity.IIndexHuidig                     = bo.IIndexHuidig;
            entity.GevelMetselwerkPrijsPerM2        = bo.GevelMetselwerkPrijsPerM2;
            entity.GipswerkenPrijsPerM2             = bo.GipswerkenPrijsPerM2;
            entity.TerrasPrijsPerM2                 = bo.TerrasPrijsPerM2;
            entity.UpdatedAt                        = DateTime.Now;
        }

        internal static BudgetOppervlaktesBO TranslateOppervlaktesToBO(BudgetOppervlaktes entity)
        {
            if (entity == null) return null;

            var bo = new BudgetOppervlaktesBO();
            bo.Id                       = entity.Id;
            bo.BudgetVersieId           = entity.BudgetVersieId;
            bo.EenheidNaam              = entity.EenheidNaam;
            bo.UnitGroupTypeId          = entity.UnitGroupTypeId;
            bo.UnitTypeId               = entity.UnitTypeId;
            bo.SortOrder                = entity.SortOrder;
            bo.BewoonbareOpp            = entity.BewoonbareOpp;
            bo.Tuin                     = entity.Tuin;
            bo.TerrasPrefab             = entity.TerrasPrefab;
            bo.TerrasGelijkvloers       = entity.TerrasGelijkvloers;
            bo.Dakterras                = entity.Dakterras;
            bo.GaragesParkingsBovenGr   = entity.GaragesParkingsBovenGr;
            bo.GarBergOndergronds       = entity.GarBergOndergronds;
            bo.BergGelijkvloers         = entity.BergGelijkvloers;
            bo.Carports                 = entity.Carports;
            bo.DoorritGVL               = entity.DoorritGVL;
            bo.Zolder                   = entity.Zolder;
            bo.GemeenschappelijkeDelen  = entity.GemeenschappelijkeDelen;
            bo.Wegenis                  = entity.Wegenis;
            bo.Grondopp                 = entity.Grondopp;

            bo.GroupTypeName  = entity.UnitGroupType?.Name;
            bo.TypeName       = entity.UnitType?.Name;
            bo.TypeShortcode  = entity.UnitType?.Shortcode;

            return bo;
        }

        internal static void ApplyOppervlaktesBOToEntity(BudgetOppervlaktesBO bo, BudgetOppervlaktes entity)
        {
            entity.EenheidNaam              = bo.EenheidNaam;
            entity.UnitGroupTypeId          = bo.UnitGroupTypeId;
            entity.UnitTypeId               = bo.UnitTypeId;
            entity.SortOrder                = bo.SortOrder;
            entity.BewoonbareOpp            = bo.BewoonbareOpp;
            entity.Tuin                     = bo.Tuin;
            entity.TerrasPrefab             = bo.TerrasPrefab;
            entity.TerrasGelijkvloers       = bo.TerrasGelijkvloers;
            entity.Dakterras                = bo.Dakterras;
            entity.GaragesParkingsBovenGr   = bo.GaragesParkingsBovenGr;
            entity.GarBergOndergronds       = bo.GarBergOndergronds;
            entity.BergGelijkvloers         = bo.BergGelijkvloers;
            entity.Carports                 = bo.Carports;
            entity.DoorritGVL               = bo.DoorritGVL;
            entity.Zolder                   = bo.Zolder;
            entity.GemeenschappelijkeDelen  = bo.GemeenschappelijkeDelen;
            entity.Wegenis                  = bo.Wegenis;
            entity.Grondopp                 = bo.Grondopp;
        }

        internal static BudgetGevelElementBO TranslateGevelToBO(BudgetGevelElementen entity)
        {
            if (entity == null) return null;

            var bo = new BudgetGevelElementBO();
            bo.Id             = entity.Id;
            bo.BudgetVersieId = entity.BudgetVersieId;
            bo.ElementType    = entity.ElementType;
            bo.EenheidNaam    = entity.EenheidNaam;
            bo.Beschrijving   = entity.Beschrijving;
            bo.Aantal         = entity.Aantal;
            bo.Breedte        = entity.Breedte;
            bo.Hoogte         = entity.Hoogte;
            bo.Lengte         = entity.Lengte;
            bo.SortOrder      = entity.SortOrder;

            return bo;
        }

        internal static void ApplyGevelBOToEntity(BudgetGevelElementBO bo, BudgetGevelElementen entity)
        {
            entity.ElementType   = bo.ElementType;
            entity.EenheidNaam   = bo.EenheidNaam;
            entity.Beschrijving  = bo.Beschrijving;
            entity.Aantal        = bo.Aantal;
            entity.Breedte       = bo.Breedte;
            entity.Hoogte        = bo.Hoogte;
            entity.Lengte        = bo.Lengte;
            entity.SortOrder     = bo.SortOrder;
        }

        internal static BudgetGevelTotaalBO BuildGevelTotalen(IEnumerable<BudgetGevelElementBO> rows)
        {
            var t = new BudgetGevelTotaalBO();
            foreach (var r in rows)
            {
                switch (r.ElementType)
                {
                    case "GevelNieuwbouw":    t.TotaalGevelNieuwbouw   += r.ResultaatM2;  break;
                    case "GevelBestaand":     t.TotaalGevelBestaand    += r.ResultaatM2;  break;
                    case "RaamNieuwbouw":     t.TotaalRaamNieuwbouw    += r.ResultaatM2;  break;
                    case "RaamBestaand":      t.TotaalRaamBestaand     += r.ResultaatM2;  break;
                    case "Ballustrade":       t.TotaalBallustrade      += r.ResultaatLm;  break;
                    case "Zichtscherm":       t.TotaalZichtscherm      += r.ResultaatLm;  break;
                    case "PlatDak":           t.TotaalPlatDak          += r.ResultaatM2;  break;
                    case "HellendDak":        t.TotaalHellendDak       += r.ResultaatM2;  break;
                    case "GroenDak":          t.TotaalGroenDak         += r.ResultaatM2;  break;
                    case "Dakoversteken":     t.TotaalDakoversteken    += r.ResultaatM2;  break;
                    case "OnderkantDoorrit":  t.TotaalOnderkantDoorrit += r.ResultaatM2;  break;
                    case "Afbraak":           t.TotaalAfbraak          += r.ResultaatM2;  break;
                }
            }
            return t;
        }

        internal static BudgetSanitairBO TranslateSanitairToBO(BudgetSanitair entity)
        {
            if (entity == null) return null;

            var bo = new BudgetSanitairBO();
            bo.Id                   = entity.Id;
            bo.BudgetVersieId       = entity.BudgetVersieId;
            bo.EenheidNaam          = entity.EenheidNaam;
            bo.UnitTypeId           = entity.UnitTypeId;
            bo.SortOrder            = entity.SortOrder;
            bo.Badkamer             = entity.Badkamer;
            bo.ToiletInBadkamer     = entity.ToiletInBadkamer;
            bo.AfzonderlijkToilet   = entity.AfzonderlijkToilet;
            bo.DoucheInBadkamer     = entity.DoucheInBadkamer;
            bo.Douchekamer          = entity.Douchekamer;

            return bo;
        }

        internal static void ApplySanitairBOToEntity(BudgetSanitairBO bo, BudgetSanitair entity)
        {
            entity.EenheidNaam          = bo.EenheidNaam;
            entity.UnitTypeId           = bo.UnitTypeId;
            entity.SortOrder            = bo.SortOrder;
            entity.Badkamer             = bo.Badkamer;
            entity.ToiletInBadkamer     = bo.ToiletInBadkamer;
            entity.AfzonderlijkToilet   = bo.AfzonderlijkToilet;
            entity.DoucheInBadkamer     = bo.DoucheInBadkamer;
            entity.Douchekamer          = bo.Douchekamer;
        }

        internal static BudgetSanitairTotaalBO BuildSanitairTotalen(IEnumerable<BudgetSanitairBO> rows)
        {
            var t = new BudgetSanitairTotaalBO();
            foreach (var r in rows)
            {
                t.TotaalBadkamers               += r.Badkamer;
                t.TotaalToilettenInBadkamer      += r.ToiletInBadkamer;
                t.TotaalAfzonderlijkeToiletten   += r.AfzonderlijkToilet;
                t.TotaalDouchesInBadkamer        += r.DoucheInBadkamer;
                t.TotaalDouchekamers             += r.Douchekamer;
                t.AantalEenheden++;
            }
            return t;
        }

        internal static BudgetOppervlaktesTotaalBO BuildTotalen(IEnumerable<BudgetOppervlaktesBO> rows)
        {
            var totalen = new BudgetOppervlaktesTotaalBO();
            foreach (var r in rows)
            {
                totalen.TotaalBewoonbaar  += r.BewoonbareOpp;
                totalen.TotaalGereduceerd += r.OppGereduceerd;
                totalen.TotaalGrondopp    += r.Grondopp;
                totalen.AantalTotaal++;

                var grp = (r.GroupTypeName ?? "").ToLowerInvariant();
                var sc  = (r.TypeShortcode ?? "").ToUpperInvariant();

                if (grp.Contains("woon"))
                    totalen.AantalWooneenheden++;
                else if (grp.Contains("park") || sc == "GAR" || sc == "CAR" || sc == "PARK")
                    totalen.AantalParkeerplaatsen++;
                else if (grp.Contains("commerc"))
                    totalen.AantalCommercieel++;
            }
            return totalen;
        }
    }
}

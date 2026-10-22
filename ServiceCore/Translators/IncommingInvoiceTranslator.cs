using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using DALCore;
using DALCore.Models;

namespace ServiceCore.Translators
{
    public class IncommingInvoiceTranslator
    {
        internal static ErrorCode TranslateEntityToBO(IncommingInvoices _entity, IncommingInvoiceBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Id = _entity.Id;
            bo.ProjectId = _entity.Project.ProjectId;
            if (_entity.Contract is not null)
                bo.ContractID = _entity.ContractId;
            if (_entity.Company is not null)
                bo.CompanyId = _entity.CompanyId;
            bo.IncommingInvoiceDate = _entity.Date;
            if (_entity.ExternalId is not null)
                bo.InvoiceExternalId = _entity.ExternalId;
            bo.InvoicePrice = _entity.Price;
            bo.DueDate = _entity.DueDate;
            bo.StatusId = _entity.StatusId;
            bo.Kind = _entity.InvoiceKind.HasValue ? (IncommingInvoiceKind)_entity.InvoiceKind.Value : (IncommingInvoiceKind?)null;
            bo.BorgInhouden = _entity.BorgInhouden;
            bo.BorgPercentage = _entity.BorgPercentage;
            bo.BorgBedrag = _entity.BorgBedrag;
            bo.BorgAfwijkReden = _entity.BorgAfwijkReden;
            bo.BorgVoPercentage = _entity.BorgVoPercentage;
            bo.KortingPercentage = _entity.KortingPercentage;
            bo.KortingDagen = _entity.KortingDagen;
            var inhouding = _entity.Inhoudingen?.OrderBy(i => i.Id).FirstOrDefault();
            if (inhouding != null)
            {
                bo.InhoudingId = inhouding.Id;
                bo.InhoudingBedrag = inhouding.Bedrag;
                bo.InhoudingReden = inhouding.Reden;
                bo.InhoudingToelichting = inhouding.Toelichting;
                bo.InhoudingAfgehandeld = inhouding.Afhandelingen?.Sum(a => a.Bedrag) ?? 0m;
                bo.InhoudingVergrendeld = bo.InhoudingAfgehandeld > 0m;
            }
            foreach (var x in _entity.IncommingInvoiceDetail)
            {
                IncommingInvoiceDetailBO bou = new IncommingInvoiceDetailBO();
                var err = IncommingInvoiceDetailTranslator.TranslateEntityToBO(x, bou);
                bo.Details.Add(bou);
            }
            return ErrorCode.Success;
        }
        internal static ErrorCode TranslateBOToEntity(IncommingInvoices _entity, IncommingInvoiceBO bo, UnitOfWorkCore uow)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            if (bo.ProjectId != 0)
                _entity.ProjectId = bo.ProjectId;
            if (bo.ContractID != 0)
                _entity.ContractId = bo.ContractID;
            if (bo.CompanyId != 0)
                _entity.CompanyId = bo.CompanyId;
            _entity.Date = bo.IncommingInvoiceDate;
            _entity.ExternalId = bo.InvoiceExternalId;
            _entity.Price = bo.InvoicePrice;
            // DueDate/StatusId zijn bewust enkel in TranslateEntityToBO gezet (alleen-lezen): het Add/Edit-
            // formulier heeft geen veld voor deze twee, dus bij een gewone POST zou de BO ze op hun CLR-default
            // (Nothing/0) binden — ze hier terugschrijven zou de verrijkings-/Octopus-pijplijn se eigen waarden
            // overschrijven bij elke handmatige bewerking. Enkel die pijplijn (InvoiceEnrichmentPipelineService,
            // OctopusIncomingInvoiceSyncService) schrijft deze kolommen, rechtstreeks op de EF-entity.
            // Kind (Vordering/Eindvordering/Creditnota) is wél beide richtingen aangesloten, maar
            // VOORWAARDELIJK geschreven: bo.Kind == Nothing betekent "dit formulier heeft er geen veld voor"
            // (het legacy Add/EditIncommingInvoice-scherm) — dan blijft de bestaande DB-waarde staan i.p.v.
            // terugvallen op "Vordering". Enkel het nieuwe V2-formulier stuurt altijd een expliciete waarde.
            if (bo.Kind.HasValue)
                _entity.InvoiceKind = (byte)bo.Kind.Value;
            if (bo.FormV2)
            {
                // Borg (42h): alleen het gl-v2-formulier stuurt dit mee. BorgInhouden = false wist het bedrag.
                _entity.BorgInhouden = bo.BorgInhouden ?? false;
                _entity.BorgPercentage = _entity.BorgInhouden == true ? bo.BorgPercentage : null;
                _entity.BorgBedrag = _entity.BorgInhouden == true ? bo.BorgBedrag : null;
                _entity.BorgAfwijkReden = _entity.BorgInhouden == true && !string.IsNullOrWhiteSpace(bo.BorgAfwijkReden) ? bo.BorgAfwijkReden.Trim() : null;
                _entity.BorgVoPercentage = _entity.BorgInhouden == true ? bo.BorgVoPercentage : null;
                // Korting contant: enkel zonder contract (met contract komt ze uit het contract zelf).
                var zonderContract = bo.ContractID.GetValueOrDefault() <= 0;
                var kortingGeldig = zonderContract && bo.KortingPercentage.GetValueOrDefault() > 0m && bo.KortingDagen.GetValueOrDefault() > 0;
                _entity.KortingPercentage = kortingGeldig ? bo.KortingPercentage : null;
                _entity.KortingDagen = kortingGeldig ? bo.KortingDagen : null;
                // Vervaldatum: enkel overschrijven als het formulier er een meestuurt (zonder contract zelf in te vullen,
                // met contract berekend uit de betaaltermijn); leeg laat een door de verrijkingspijplijn gevulde waarde staan.
                if (bo.DueDate.HasValue)
                    _entity.DueDate = bo.DueDate;

                // Extra inhouding (42c/42l): een per factuur. Zodra er iets van afgehandeld is, blijven bedrag en reden vergrendeld.
                var bestaand = _entity.Inhoudingen.OrderBy(i => i.Id).FirstOrDefault();
                var heeftAfhandeling = bestaand != null && bestaand.Afhandelingen.Count > 0;
                var gewenst = bo.InhoudingBedrag.GetValueOrDefault();
                if (!heeftAfhandeling)
                {
                    if (gewenst > 0m)
                    {
                        if (bestaand == null)
                        {
                            bestaand = new IncommingInvoiceInhouding { CreatedAt = DateTime.UtcNow };
                            _entity.Inhoudingen.Add(bestaand);
                        }
                        bestaand.Bedrag = gewenst;
                        bestaand.Reden = bo.InhoudingReden ?? IncommingInvoiceInhouding.RedenAndere;
                        bestaand.Toelichting = string.IsNullOrWhiteSpace(bo.InhoudingToelichting) ? null : bo.InhoudingToelichting.Trim();
                    }
                    else if (bestaand != null)
                    {
                        _entity.Inhoudingen.Remove(bestaand);
                    }
                }
                else
                {
                    // Enkel de toelichting mag nog wijzigen.
                    bestaand.Toelichting = string.IsNullOrWhiteSpace(bo.InhoudingToelichting) ? null : bo.InhoudingToelichting.Trim();
                }
            }
            if (string.IsNullOrEmpty(_entity.CostContextType))
                _entity.CostContextType = "Unknown";

            var err = HandleRows(_entity, bo.Details);
            if ((err != ErrorCode.Success))
                return err;
            return ErrorCode.Success;
        }

        private static ErrorCode HandleRows(IncommingInvoices _entity, List<IncommingInvoiceDetailBO> rows)
        {
            if ((rows.Count == 0))
                return ErrorCode.Success;
            foreach (var x in rows)
            {
                ErrorCode err;
                if ((x.Id == 0))
                {
                    // insert
                    IncommingInvoiceDetail row = new IncommingInvoiceDetail();
                    err = IncommingInvoiceDetailTranslator.TranslateBOToEntity(row, x);
                    _entity.IncommingInvoiceDetail.Add(row);
                }
                else
                {
                    // update
                    var row = _entity.IncommingInvoiceDetail.FirstOrDefault(f => f.Id == x.Id);
                    if ((row != null))
                        err = IncommingInvoiceDetailTranslator.TranslateBOToEntity(row, x);
                }
            }
            // delete
            List<IncommingInvoiceDetail> delList = new List<IncommingInvoiceDetail>();
            foreach (var x in _entity.IncommingInvoiceDetail)
            {
                if ((!rows.Any(f => f.Id == x.Id)))
                    delList.Add(x);
            }
            foreach (var x in delList)
                _entity.IncommingInvoiceDetail.Remove(x);
            return ErrorCode.Success;
        }
    }
}

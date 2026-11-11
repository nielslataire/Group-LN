#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

/// <summary>Migratie 140 (design-handoff 28t/28u) — een groep regels van één leverancier binnen een offerte of
/// wijzigingsopdracht. De klant ziet één document met de groepstitels als tussenkop; de leverancier, het contract,
/// de bron-offerte en "via ons gefactureerd" horen bij de groep. ChangeOrder.ContractActivityId/QuoteSource*/Invoiceable
/// spiegelen de eerste groep.</summary>
public partial class ChangeOrderGroup
{
    public int Id { get; set; }

    public int ChangeOrderId { get; set; }

    public int SortOrder { get; set; }

    /// <summary>De leverancier (CompanyInfo). Leeg kan enkel op een oude rij zonder leverancier.</summary>
    public int? SupplierCompanyId { get; set; }

    /// <summary>Tussenkop op het document voor de klant (bv. "Schilderwerken") — nooit de leveranciersnaam.</summary>
    public string Title { get; set; }

    /// <summary>Het contract van deze leverancier in dit project; leeg = "Geen contract · koppelen".</summary>
    public int? ContractActivityId { get; set; }

    public string QuoteSupplierReference { get; set; }

    public string QuoteSourcePath { get; set; }

    public string QuoteSourceFileName { get; set; }

    /// <summary>aan = wij factureren (facturatiebedrijf aannemer) · uit = de onderaannemer factureert zelf.</summary>
    public bool InvoicedByUs { get; set; } = true;

    public virtual ChangeOrder ChangeOrder { get; set; }

    public virtual CompanyInfo SupplierCompany { get; set; }

    public virtual ContractActivity ContractActivity { get; set; }

    public virtual ICollection<ChangeOrderDetail> Details { get; set; } = new List<ChangeOrderDetail>();
}

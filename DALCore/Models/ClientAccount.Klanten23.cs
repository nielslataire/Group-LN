#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

// Migratie 057_KlantenaccountVoornaamEnFacturatie.sql (design-handoff 23 "Klant toevoegen").
// ClientAccount blijft het hoofdeigenaar-/accountrecord; deze partial voegt de nieuwe voornaam- en
// facturatie-/ondertekenvoorkeuren toe. Alles nullable of met een default die het bestaande gedrag
// (enkel Name gevuld, geen voorkeuren) exact bewaart.
public partial class ClientAccount
{
    /// <summary>Voornaam van eigenaar 1 — mirrort ClientContacts.Forename. Leeg bij bestaande accounts;
    /// Name (achternaam of volledige naam bij oude rijen) blijft dan de enige bron voor de weergavenaam.</summary>
    public string Forename { get; set; }

    // Migratie 058_KlantenaccountTelefoon.sql: eigenaar 1 had tot dan enkel Email — elke mede-eigenaar/
    // contactpersoon (ClientContacts) heeft al Phone/Cellphone, dit dichtte het gat voor het account
    // zelf. Zelfde kolomlengte (50) als ClientContacts.Phone/Cellphone.
    public string Phone { get; set; }
    public string Cellphone { get; set; }

    /// <summary>Zie BOCore.ClientInvoicingMode (0 Joint, 1 PerOwner). Enkel bewaard, stuurt de
    /// facturatie-generatie nog niet aan. Geen entity-eigen enum: BOCore is de bron van waarheid.</summary>
    public byte InvoicingMode { get; set; }

    /// <summary>Zie BOCore.ClientBilledToType (0 AllOwners, 1 SpecificOwner, 2 Company). Null = niet
    /// ingesteld (gedraagt zich als AllOwners).</summary>
    public byte? BilledToType { get; set; }

    /// <summary>Enkel gevuld bij BilledToType = SpecificOwner en een mede-eigenaar (niet eigenaar 1 zelf).</summary>
    public int? BilledToClientContactId { get; set; }

    /// <summary>Zie BOCore.Enum.Signing.SigningRule (0 All, 1 Any, 2 Ordered) — hergebruikt, geen eigen enum.
    /// Null = ongewijzigd gedrag: ChangeOrderSigningSource stelt vandaag altijd "Alle eigenaars" voor.</summary>
    public byte? DefaultSigningRule { get; set; }

    /// <summary>"Uitnodigen voor het klantenportaal" — enkel onthouden, er bestaat nog geen klantenportaal
    /// dat hierop reageert.</summary>
    public bool PortalInviteRequested { get; set; }

    public virtual ClientContacts BilledToClientContact { get; set; }
    public virtual ICollection<ClientAccountInvoiceRecipient> InvoiceRecipients { get; set; } = new List<ClientAccountInvoiceRecipient>();
}

/// <summary>"Verzenden naar" — extra e-mailontvangers van een klantenaccount, los van de eigenaars/
/// mede-eigenaars zelf (bv. een boekhouder). Migratie 057.</summary>
public partial class ClientAccountInvoiceRecipient
{
    public int Id { get; set; }
    public int ClientAccountId { get; set; }
    public int? ClientContactId { get; set; }
    public string Email { get; set; }
    public string DisplayName { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedDate { get; set; }

    public virtual ClientAccount ClientAccount { get; set; }
    public virtual ClientContacts ClientContact { get; set; }
}

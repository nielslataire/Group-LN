#nullable disable
using System;

namespace DALCore.Models;

/// <summary>Een gesynchroniseerde mail van een gebruiker (migratie 122). Enkel metadata en een korte voorvertoning: nooit bijlagen of de volledige tekst.</summary>
public partial class OutlookMail
{
    public int Id { get; set; }

    public int UserId { get; set; }

    /// <summary>"in" (Postvak IN en submappen) of "uit" (Verzonden items).</summary>
    public string Richting { get; set; }

    public string GraphId { get; set; }

    /// <summary>Dedup-sleutel, ook voor mails die CPM zelf verstuurde.</summary>
    public string InternetMessageId { get; set; }

    public string ConversationId { get; set; }

    public string Afzender { get; set; }

    public string AfzenderNaam { get; set; }

    /// <summary>Ontvangers (aan en cc), gescheiden door ";".</summary>
    public string Ontvangers { get; set; }

    public string Onderwerp { get; set; }

    /// <summary>UTC.</summary>
    public DateTime OntvangenOp { get; set; }

    /// <summary>Maximaal 300 tekens.</summary>
    public string Voorvertoning { get; set; }

    public string WebLink { get; set; }

    public int? ProjectId { get; set; }

    /// <summary>ClientAccount.Id van de klant uit afzender of ontvanger.</summary>
    public int? KlantId { get; set; }

    /// <summary>CompanyInfo.CompanyId van de leverancier uit afzender of ontvanger.</summary>
    public int? LeverancierId { get; set; }

    /// <summary>Voorstel van CPM voor een mail die nog te koppelen is (migratie 123); de gebruiker bevestigt of kiest zelf.</summary>
    public int? VoorgesteldProjectId { get; set; }

    /// <summary>nieuw, afgehandeld of te-koppelen (zie OutlookMailStatus).</summary>
    public string Status { get; set; } = "nieuw";

    public bool IsPrive { get; set; }

    public bool VerwijderdInOutlook { get; set; }

    public DateTime AangemaaktOp { get; set; }

    public virtual Users User { get; set; }

    public virtual Project Project { get; set; }
}

/// <summary>Een afzender die een gebruiker blijvend negeert (migratie 122).</summary>
public partial class OutlookGeblokkeerdeAfzender
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Email { get; set; }

    public DateTime AangemaaktOp { get; set; }
}

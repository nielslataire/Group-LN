using CPMCore.Models.GlV2;

namespace CPMCore.Models.Projecten;

/// <summary>gl-v2 — Nacalculatie, categorie/activiteit-detail (design-handoff punt 42b "Categorie
/// Bovenbouw" + 42f "Subcategorieën — Technieken Elektro"). Zelfde pagina voor beide: 42f is gewoon
/// 42b met een extra tier-2 tabbar zodra de categorie meer dan 1 activiteit heeft (zie
/// <see cref="SubTabs"/> — leeg = geen tweede tabbar, exact zoals het ontwerp dat voor "Bovenbouw"
/// (1 activiteit) ook niet toont). Vooraf berekend in de controller
/// (<c>ProjectenController.Contracts.BuildRecalculationDetailV2Vm</c>) uit dezelfde bronnen als het
/// legacy-scherm (<see cref="ProjectRecalculationDetailModel"/>).</summary>
public class RecalculationDetailV2Vm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public bool CanWrite { get; set; }

    public int GroupId { get; set; }
    public string GroupNaam { get; set; } = "";
    /// <summary>Null = "Alle activiteiten" (of de categorie heeft maar 1 activiteit, dan is er geen keuze).</summary>
    public int? ActivityId { get; set; }
    public string? ActivityNaam { get; set; }

    /// <summary>Tier-1 tabbar — alle 11 categorieën, zelfde volgorde/signaal als 42a.</summary>
    public List<RecalcTabCategorieV2> Categorieen { get; set; } = new();
    /// <summary>Tier-2 tabbar — enkel gevuld als deze categorie >1 activiteit heeft; dan altijd een
    /// "Alle activiteiten"-item vooraan (<see cref="RecalcTabActiviteitV2.ActivityId"/> == null).</summary>
    public List<RecalcTabActiviteitV2> SubTabs { get; set; } = new();

    public List<GlV2KpiItemVm> Kpis { get; set; } = new();
    /// <summary>Enkel gevuld wanneer <see cref="SubTabs"/> niet leeg is én "Alle activiteiten" actief
    /// is (42f se eigen "Activiteiten"-kaart, klikbare rijen die de leverancierskaarten filteren).</summary>
    public List<RecalcActiviteitRijV2> ActiviteitRijen { get; set; } = new();

    public List<RecalcLeverancierKaartV2> Leveranciers { get; set; } = new();
}

public class RecalcTabCategorieV2
{
    public int GroupId { get; set; }
    public string Naam { get; set; } = "";
    public bool HeeftSignaal { get; set; }
    public bool IsActief { get; set; }
}

public class RecalcTabActiviteitV2
{
    /// <summary>Null = "Alle activiteiten".</summary>
    public int? ActivityId { get; set; }
    public string Naam { get; set; } = "";
    public bool IsActief { get; set; }
    /// <summary>Telling-chip op de pill (design-handoff 42f): bij "Alle activiteiten" het aantal
    /// activiteiten in de categorie, bij een specifieke activiteit het aantal factuurregels ervoor.</summary>
    public int Aantal { get; set; }
}

/// <summary>Eén rij in 42f se "Activiteiten"-kaart (enkel bij "Alle activiteiten").</summary>
public class RecalcActiviteitRijV2
{
    public int ActivityId { get; set; }
    public string Naam { get; set; } = "";
    public decimal Begroot { get; set; }
    public decimal Gecontracteerd { get; set; }
    public decimal Gefactureerd { get; set; }
    public decimal VerschilContractFactuur => Gecontracteerd - Gefactureerd;
    public decimal VoortgangPercent => Gecontracteerd > 0 ? Gefactureerd / Gecontracteerd * 100m : 0m;
    /// <summary>"Gegund" (is er een contract) · "Nog te gunnen" (geen contract) — design-handoff 42f.</summary>
    public bool IsGegund { get; set; }
}

/// <summary>Eén leverancierskaart (design-handoff 42b/42f): contractstrook + facturentabel per type.
/// Zonder contract (<see cref="HasContract"/> false) toont enkel een infokader + de "Geen contract"-
/// factuurgroep — geen contractstrook, geen borg/betaaltermijn (die bestaan niet zonder contract).</summary>
public class RecalcLeverancierKaartV2
{
    public int CompanyId { get; set; }
    public string CompanyNaam { get; set; } = "";
    public string ActiviteitNaam { get; set; } = "";
    public bool HasContract { get; set; }
    public int ContractId { get; set; }
    /// <summary>Contractnaam als die is ingevuld, anders "Contract #id" — ContractBO heeft geen
    /// genummerd veld zoals "C-2025-004" uit het ontwerp.</summary>
    public string ContractLabel { get; set; } = "";
    public bool ContractSigned { get; set; }
    public DateOnly? ContractSentDate { get; set; }

    /// <summary>= Contracten.Activities.Sum(EffectivePrice) voor deze activiteit/categorie-scope (incl. bijbestellingen).</summary>
    public decimal ContractBedrag { get; set; }
    /// <summary>Som van IncommingInvoiceType.Meerwerk-facturen (niet Meerwerk_Klant) voor deze leverancier/scope.</summary>
    public decimal MeerwerkBedrag { get; set; }
    /// <summary>= ContractBedrag + MeerwerkBedrag-facturen, d.w.z. Contract- en Meerwerk-type facturen samen.</summary>
    public decimal GefactureerdBedrag { get; set; }
    public decimal Saldo => ContractBedrag - GefactureerdBedrag;
    public decimal VoortgangPercent => ContractBedrag > 0 ? GefactureerdBedrag / ContractBedrag * 100m : 0m;

    public decimal? BorgPercentage { get; set; }
    /// <summary>Enkel bij GuaranteeType == FinancialGuarantee — bij BankGuarantee wordt er niets op de factuur ingehouden.</summary>
    public decimal BorgIngehouden { get; set; }
    public string? BetaaltermijnTekst { get; set; }

    /// <summary>Borg over het héle contract uit BorgService (42i): wat er al vrijgegeven werd en wat nu vrij te geven is.
    /// <see cref="BorgIngehouden"/> is hier het totaal over het hele contract, niet enkel deze categorie.</summary>
    public decimal BorgVrijgegeven { get; set; }
    public decimal BorgNuVrij { get; set; }
    public bool BorgIsBankwaarborg { get; set; }
    public string? BorgStatus { get; set; }
    public decimal BorgNogIngehouden => BorgIngehouden - BorgVrijgegeven;

    public List<RecalcFactuurGroepV2> FactuurGroepen { get; set; } = new();
}

public class RecalcFactuurGroepV2
{
    /// <summary>"CONTRACT" · "MEERWERK" · "MEERWERK KLANT" · "MEERWERK ALGEMEEN" · "GEEN CONTRACT".</summary>
    public string Label { get; set; } = "";
    public decimal Totaal { get; set; }
    public List<RecalcFactuurRijV2> Rijen { get; set; } = new();
}

public class RecalcFactuurRijV2
{
    public int InvoiceId { get; set; }
    public string FactuurNummer { get; set; } = "";
    public DateOnly Datum { get; set; }
    public string Omschrijving { get; set; } = "";
    /// <summary>Al met het juiste teken (<c>IncommingInvoiceActivityBO.SignedPrice</c>) — een creditnota is hier al negatief.</summary>
    public decimal Bedrag { get; set; }
    public bool IsCreditnota { get; set; }
    /// <summary>"Betaald" (is-positive) · "Vervallen" (is-blocked) · null = geen badge (meeste rijen).</summary>
    public string? BetaalLabel { get; set; }
    public string BetaalTone { get; set; } = "";
    /// <summary>"Korting tot dd/mm" — enkel bij een contract met korting-contant én nog niet betaald/vervallen.</summary>
    public string? KortingTekst { get; set; }
}

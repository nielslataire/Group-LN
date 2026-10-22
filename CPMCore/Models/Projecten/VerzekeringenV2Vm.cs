using CPMCore.Models.GlV2;

namespace CPMCore.Models.Projecten;

/// <summary>gl-v2 — Projecten/DetailInsurancesV2 (design-handoff punt 43a/43b): overzicht van de verzekeringen van een project.
/// Alles wordt berekend door <see cref="CPMCore.Services.Verzekeringen.VerzekeringService"/>.</summary>
public class VerzekeringenV2Vm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public bool CanWrite { get; set; }
    public bool CanDelete { get; set; }
    /// <summary>"polissen" (43a) of "attesten" (43b).</summary>
    public string Tab { get; set; } = "polissen";
    public List<GlV2KpiItemVm> Kpis { get; set; } = new();
    public List<VzAandachtspunt> Aandachtspunten { get; set; } = new();
    public List<VzPolisRij> Polissen { get; set; } = new();
    public VzTijdlijn Tijdlijn { get; set; } = new();
    public List<VzAttestRij> Attesten { get; set; } = new();
    /// <summary>Keuze voor het hele project: null per aannemer afgeleid, 1 via projectpolis.</summary>
    public int? TjProjectBron { get; set; }
    public string? TjProjectOpmerking { get; set; }
    /// <summary>De woningen van het project, voor de keuze "voor welke woningen geldt dit attest".</summary>
    public List<VzWoning> Woningen { get; set; } = new();
    public int AantalContractenAttest => Attesten.Count;
    public int AantalOntbrekend => Polissen.Count(p => p.IsOntbrekend);
    public int AantalPolissen => Polissen.Count(p => !p.IsOntbrekend);
    public int AantalOpen => Aandachtspunten.Count(a => !a.Opgelost);
    public int AantalOpgelost => Aandachtspunten.Count(a => a.Opgelost);
}

/// <summary>Een aandachtspunt, automatisch afgeleid uit polissen, contracten, definitief budget en opleveringsdatum.
/// Tone: "danger" (rood), "warning" (goud), "info" (grijs), "success" (opgelost).</summary>
public class VzAandachtspunt
{
    public string Sleutel { get; set; } = "";
    public string Tone { get; set; } = "warning";
    public string Titel { get; set; } = "";
    public string Kicker { get; set; } = "";
    public string Tekst { get; set; } = "";
    public bool Opgelost { get; set; }
    public List<VzActie> Acties { get; set; } = new();
}

public class VzActie
{
    public string Label { get; set; } = "";
    public string Href { get; set; } = "";
    public string Icon { get; set; } = "ph-arrow-right";
    /// <summary>De actie wijzigt gegevens: de knop is een formulier (POST naar <see cref="Href"/> met het projectid) i.p.v. een link.</summary>
    public bool Post { get; set; }
}

public class VzWoning
{
    public int Id { get; set; }
    public string Naam { get; set; } = "";
    public string Koper { get; set; } = "";
}

public class VzPolisRij
{
    public int Id { get; set; }
    /// <summary>abr · brand · tienjarige · ba · andere · attest · ontbrekend (voor het typefilter en het icoon).</summary>
    public string TypeKey { get; set; } = "";
    public string Soort { get; set; } = "";
    public string Maatschappij { get; set; } = "";
    public string MaatschappijSub { get; set; } = "";
    public string Polisnummer { get; set; } = "";
    public string Waarde { get; set; } = "—";
    public string Dekking { get; set; } = "—";
    public string DekkingSub { get; set; } = "";
    public string DekkingSub2 { get; set; } = "";
    public string Waarborg { get; set; } = "—";
    public string WaarborgSub { get; set; } = "";
    public string Status { get; set; } = "";
    public string StatusTone { get; set; } = "is-neutral";
    /// <summary>lopend · aandacht · beeindigd · ontbreekt · attest — voor de statusfilter.</summary>
    public string StatusKey { get; set; } = "lopend";
    public bool IsOntbrekend { get; set; }
    public bool IsAttest { get; set; }
    public bool IsBeeindigd { get; set; }
    public int? DocumentId { get; set; }
    /// <summary>Waar "Toevoegen" naartoe gaat bij een ontbrekende polis.</summary>
    public string? ToevoegenHref { get; set; }
    public string Zoek { get; set; } = "";
}

public class VzTijdlijn
{
    public List<int> Jaren { get; set; } = new();
    public double VandaagPct { get; set; } = -1;
    public List<VzTijdlijnRij> Rijen { get; set; } = new();
}

public class VzTijdlijnRij
{
    public string Label { get; set; } = "";
    public string Sub { get; set; } = "";
    public List<VzBalk> Balken { get; set; } = new();
    public List<VzMarker> Markers { get; set; } = new();
}

public class VzBalk
{
    public double LinksPct { get; set; }
    public double BreedtePct { get; set; }
    /// <summary>werf · polis · verlengd · waarborg · ontbrekend · tienjarige · beeindigd</summary>
    public string Soort { get; set; } = "polis";
    public string Tekst { get; set; } = "";
    public bool Pijl { get; set; }
}

public class VzMarker
{
    public double Pct { get; set; }
    public string Label { get; set; } = "";
}

/// <summary>Eén contract op het tabblad "Attesten aannemers" (43b).</summary>
public class VzAttestRij
{
    public int ContractId { get; set; }
    public string Bedrijf { get; set; } = "";
    public string BedrijfEmail { get; set; } = "";
    public string ContractLabel { get; set; } = "";
    public string Lot { get; set; } = "";
    public bool TjVereist { get; set; }
    /// <summary>De aannemer valt onder de globale polis (keuze op het contract, of van het project als het contract niets koos).</summary>
    public bool ViaProject { get; set; }
    public string TjStatus { get; set; } = "";
    public string TjTone { get; set; } = "is-neutral";
    public string TjSub { get; set; } = "";
    public bool TjOntbreekt { get; set; }
    public string BaStatus { get; set; } = "";
    public string BaTone { get; set; } = "is-neutral";
    public string BaSub { get; set; } = "";
    public bool BaOntbreekt { get; set; }
    public string Laatst { get; set; } = "nog niet";
    public bool IsOpgevraagd { get; set; }
    /// <summary>Er ontbreekt minstens één attest: de rij kan geselecteerd worden en krijgt "Opvragen" of "Herinneren".</summary>
    public bool KanOpvragen => TjOntbreekt || BaOntbreekt;
    public string Zoek { get; set; } = "";
    public int TjUnitsRequired { get; set; }
    public int TjUnitsReceived { get; set; }
    /// <summary>Huidige gegevens per soort voor het venster "Attest vastleggen" (als JSON op de knop).</summary>
    public VzAttestPrefill Tj { get; set; } = new();
    public VzAttestPrefill Ba { get; set; } = new();
}

public class VzAttestPrefill
{
    /// <summary>0 ontbreekt · 1 ontvangen · 2 niet vereist.</summary>
    public int Status { get; set; }
    public int UnitsRequired { get; set; }
    public int UnitsReceived { get; set; }
    public string ValidUntil { get; set; } = "";
    public string Insurer { get; set; } = "";
    public string PolicyNumber { get; set; } = "";
    public string Note { get; set; } = "";
    public int? DocumentId { get; set; }
    /// <summary>De woningen waarvoor dit tienjarige-attest geldt.</summary>
    public List<int> UnitIds { get; set; } = new();
}

/// <summary>De velden van het formulier "Verzekering toevoegen/bewerken" (43c).</summary>
public class VzPolisInput
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    /// <summary>1 ABR · 2 Brand · 3 Tienjarige · 4 BA · 5 Andere.</summary>
    public int Type { get; set; } = 1;
    /// <summary>0 bestaand gebouw · 1 nieuwbouw (enkel bij Brand).</summary>
    public int? Building { get; set; }
    public int? UnitId { get; set; }
    /// <summary>Enkel bij een nieuwe tienjarige polis: de eenheden waarvoor elk een eigen polis wordt aangemaakt (leeg = één polis voor het hele project).</summary>
    public List<int>? UnitIds { get; set; }
    public string? AppliesTo { get; set; }
    public decimal? InsuredValue { get; set; }
    public int? InsuranceCompanyId { get; set; }
    public int? BrokerCompanyId { get; set; }
    public string? Polisnummer { get; set; }
    public DateOnly? Startdate { get; set; }
    public bool StartAtDelivery { get; set; }
    public DateOnly? Enddate { get; set; }
    public int? Period { get; set; }
    public int? ExtensionPeriod { get; set; }
    public int? GuaranteePeriod { get; set; }
    public int? DurationYears { get; set; }
    public decimal? Premium { get; set; }
    public string? Note { get; set; }
    public bool RemindStart { get; set; } = true;
    public bool RemindSale { get; set; } = true;
    public bool RemindExpiry { get; set; } = true;
    /// <summary>Knop "Bewaren als aangevraagd".</summary>
    public bool Requested { get; set; }
    public string? From { get; set; }

    public VzPolisInput Kopie() => (VzPolisInput)MemberwiseClone();
}

/// <summary>Weergave van het formulier (43c).</summary>
public class VzPolisFormVm
{
    public VzPolisInput Input { get; set; } = new();
    public string ProjectName { get; set; } = "";
    public bool IsNew => Input.Id == 0;
    public bool HasBroker { get; set; }
    public string? BrokerName { get; set; }
    public int? DocumentId { get; set; }
    public List<GlV2SelectItem> Maatschappijen { get; set; } = new();
    public List<GlV2SelectItem> Makelaars { get; set; } = new();
    public List<GlV2SelectItem> Eenheden { get; set; } = new();
    public DateOnly? VoDatum { get; set; }
    public decimal? BudgetBouwkost { get; set; }
    public string? BudgetBron { get; set; }
    /// <summary>De aandachtspunten die dit type oplost (lijst rechts onderaan).</summary>
    public List<string> LostOp { get; set; } = new();
    public string? Fout { get; set; }
}

using System.Collections.Generic;
using System.Linq;

namespace CPMCore.Models.Projecten;

/// <summary>gl-v2 "Betalingsgroep bewerken / Nieuwe betalingsgroep" (design-handoff punt 26b/26c).</summary>
public class PaymentGroupEditV2Vm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public int GroupId { get; set; }
    public string Name { get; set; } = "";
    public int? VatTypeId { get; set; }
    public List<PaymentGroupVatOptionV2> VatOptions { get; set; } = new();
    public List<PaymentGroupEditStageV2> Stages { get; set; } = new();
    /// <summary>Eenheden die aan de groep gekoppeld zijn (chips).</summary>
    public List<PaymentGroupEditUnitV2> Units { get; set; } = new();
    /// <summary>Alle andere eenheden van het project (keuzelijst achter "+ eenheid").</summary>
    public List<PaymentGroupEditUnitV2> OtherUnits { get; set; } = new();
    /// <summary>Andere groepen van dit project, voor "Schijven overnemen uit andere groep".</summary>
    public List<PaymentGroupSourceV2> OtherGroups { get; set; } = new();
    public bool IsNew => GroupId == 0;
    /// <summary>Verwijderknop tonen (bestaande groep + verwijderrecht).</summary>
    public bool CanDelete { get; set; }
    /// <summary>Gevuld = verwijderen kan niet (er is al gefactureerd, een schijf is bereikt of een
    /// wijzigingsopdracht hangt eraan); de knop blijft dan zichtbaar maar uitgeschakeld, met deze uitleg.</summary>
    public string? DeleteBlockReason { get; set; }
    /// <summary>Aantal eenheden dat bij verwijderen van de groep losgekoppeld wordt.</summary>
    public int DeleteUnitCount { get; set; }
    /// <summary>Bij een nieuwe groep die uit een bron voorgevuld werd: de naam van die bron (voor de hint).</summary>
    public string? CopiedFromName { get; set; }
    public decimal Total => Stages.Sum(s => s.Percentage);
}

public class PaymentGroupVatOptionV2
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
}

public class PaymentGroupEditStageV2
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Percentage { get; set; }
    public bool Invoicable { get; set; }
    /// <summary>Gefactureerd of per eenheid bereikt: vast (niet verwijderen, niet verplaatsen, % niet verlagen).</summary>
    public bool IsLocked { get; set; }
    /// <summary>Gemarkeerd als kopie tot de gebruiker ze aanpast (26d).</summary>
    public string? CopyOfHint { get; set; }
}

public class PaymentGroupEditUnitV2
{
    public int UnitId { get; set; }
    public string Name { get; set; } = "";
    /// <summary>Naam van de groep waar de eenheid nu aan hangt (enkel bij OtherUnits).</summary>
    public string? CurrentGroupName { get; set; }
    public bool InGroup { get; set; }
    /// <summary>Er bestaat al een factuurregel voor deze eenheid in de groep: koppeling ligt vast.</summary>
    public bool IsLocked { get; set; }
}

public class PaymentGroupSourceV2
{
    public int GroupId { get; set; }
    public string Name { get; set; } = "";
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public string ProjectMunicipality { get; set; } = "";
    public bool IsThisProject { get; set; }
    public decimal VatPercentage { get; set; }
    public int? VatTypeId { get; set; }
    public int StageCount { get; set; }
    public int UnitCount { get; set; }
    public List<PaymentGroupEditStageV2> Stages { get; set; } = new();
}

public class NewPaymentGroupModalV2Vm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public int? PreselectGroupId { get; set; }
    public List<PaymentGroupSourceV2> Sources { get; set; } = new();
    public List<PaymentGroupVatOptionV2> VatOptions { get; set; } = new();
}

/// <summary>Postmodel van de bewerkpagina. Arrays staan parallel (zelfde index = zelfde rij).</summary>
public class PaymentGroupEditV2Post
{
    public int ProjectId { get; set; }
    public int GroupId { get; set; }
    public string? Name { get; set; }
    public int? VatTypeId { get; set; }
    public List<int> StageIds { get; set; } = new();
    public List<string?> StageNames { get; set; } = new();
    public List<string?> StagePercentages { get; set; } = new();
    public List<bool> StageInvoicable { get; set; } = new();
    public List<int> UnitIds { get; set; } = new();
}

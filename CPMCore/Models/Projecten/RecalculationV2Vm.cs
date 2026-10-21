using CPMCore.Models.GlV2;

namespace CPMCore.Models.Projecten;

/// <summary>gl-v2 — Nacalculatie overzicht (design-handoff punt 42a). Vooraf berekend in de controller
/// (<c>ProjectenController.Contracts.RecalculationV2Vm</c>) uit dezelfde bronnen als het legacy-scherm
/// (<see cref="ProjectContractsModel"/>: contracten, budget, inkomende facturen) — exact dezelfde
/// sommen en statuslogica, enkel de weergave is nieuw.</summary>
public class RecalculationIndexV2Vm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public bool CanWrite { get; set; }
    /// <summary>Aantal contracten waarvan de borg nu vrij te geven is — de badge op de knop "Borgen & inhoudingen" (42a).</summary>
    public int BorgenNuVrij { get; set; }
    public List<GlV2KpiItemVm> Kpis { get; set; } = new();
    public List<RecalculationCategoryV2> Categorieen { get; set; } = new();

    public int AantalMetSignaal => Categorieen.Count(c => c.BadgeLabel != null);

    /// <summary>Waarschuwingstekst boven de tabel (design-handoff 42a: "Bovenbouw is 10,7 % boven contract
    /// gefactureerd · Onderbouw en Omgevingswerken hebben facturen zonder contract · …") — telkens opgebouwd
    /// uit de badges van <see cref="Categorieen"/>, niet hardcoded.</summary>
    public string? WaarschuwingTekst
    {
        get
        {
            var delen = new List<string>();
            foreach (var c in Categorieen.Where(c => c.BadgeLabel == "Overschrijding"))
            {
                var pct = c.Gecontracteerd > 0 ? (c.Gefactureerd / c.Gecontracteerd - 1m) * 100m : 0m;
                delen.Add($"{c.Naam} is {pct:N1} % boven contract gefactureerd");
            }
            var zonderContract = Categorieen.Where(c => c.BadgeLabel == "Zonder contract").Select(c => c.Naam).ToList();
            if (zonderContract.Count == 1) delen.Add($"{zonderContract[0]} heeft facturen zonder contract");
            else if (zonderContract.Count > 1) delen.Add($"{string.Join(" en ", zonderContract)} hebben facturen zonder contract");
            var nietBegroot = Categorieen.Where(c => c.BadgeLabel == "Niet begroot").Select(c => c.Naam).ToList();
            if (nietBegroot.Count == 1) delen.Add($"{nietBegroot[0]} heeft facturen maar geen budget");
            else if (nietBegroot.Count > 1) delen.Add($"{string.Join(", ", nietBegroot)} hebben facturen maar geen budget");
            return delen.Count == 0 ? null : string.Join(" · ", delen);
        }
    }
}

public class RecalculationCategoryV2
{
    public int GroupId { get; set; }
    public string Naam { get; set; } = "";
    public decimal Begroot { get; set; }
    public decimal Gecontracteerd { get; set; }
    public decimal Gefactureerd { get; set; }
    public decimal MeerwerkKlant { get; set; }
    public decimal VerschilBegrootContract => Begroot - Gecontracteerd;
    public decimal VerschilContractFactuur => Gecontracteerd - Gefactureerd;
    /// <summary>Rode stip op de categorietab/-kop (design-handoff 42a/42f): er is meer gefactureerd dan gecontracteerd.</summary>
    public bool HeeftSignaal => VerschilContractFactuur < 0;
    /// <summary>Voortgang = gefactureerd/gecontracteerd (design-handoff 42a, niet t.o.v. begroot) — 0% als er niets
    /// gecontracteerd is, ook al zijn er facturen (zie Onderbouw/Omgevingswerken/Bureel algemeen in het ontwerp).</summary>
    public decimal VoortgangPercent => Gecontracteerd > 0 ? Gefactureerd / Gecontracteerd * 100m : 0m;
    /// <summary>Schilderwerken-achtige rij in 42a: geen enkele activiteit met budget/contract/factuur → gedimd, niet
    /// uitklapbaar, en bewust GEEN badge (ook al is Begroot dan toevallig 0) — zie <see cref="BadgeLabel"/>.</summary>
    public bool IsLeeg => Activiteiten.Count == 0;
    /// <summary>Prioriteit zoals in 42a: eerst "Niet begroot", dan "Zonder contract", dan "Overschrijding" — anders geen badge.</summary>
    public string? BadgeLabel => IsLeeg ? null : Begroot == 0 ? "Niet begroot" : Gecontracteerd == 0 ? "Zonder contract" : VerschilContractFactuur < 0 ? "Overschrijding" : null;
    /// <summary>gl-v2-badge tonen: is-blocked (rood) · is-attention (amber) — "is-danger"/"is-warning" bestaan niet als badge-klasse.</summary>
    public string BadgeTone => BadgeLabel switch { "Niet begroot" => "is-blocked", "Overschrijding" => "is-blocked", "Zonder contract" => "is-attention", _ => "" };
    public List<RecalculationActivityRowV2> Activiteiten { get; set; } = new();
}

public class RecalculationActivityRowV2
{
    public int ActivityId { get; set; }
    public string Naam { get; set; } = "";
    /// <summary>Null = (nog) niet op het budget gezet — de rij toont dan een streepje i.p.v. € 0,00.</summary>
    public decimal? Begroot { get; set; }
    public decimal Gecontracteerd { get; set; }
    public decimal Gefactureerd { get; set; }
    public decimal MeerwerkKlant { get; set; }
    public decimal VerschilBegrootContract => (Begroot ?? 0m) - Gecontracteerd;
    public decimal VerschilContractFactuur => Gecontracteerd - Gefactureerd;
    /// <summary>Volledig (precies gefactureerd wat gecontracteerd is) · Gedeeltelijk (minder) · Over (meer).</summary>
    public string Status { get; set; } = "";
    public string StatusTone { get; set; } = "is-neutral";
}

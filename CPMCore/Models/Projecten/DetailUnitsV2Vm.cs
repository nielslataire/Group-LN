namespace CPMCore.Models.Projecten;

/// <summary>gl-v2 view-model voor Projecten/DetailUnitsV2 (design-handoff punt 16, opties 16a
/// "Eenhedenlijst — hoofdeenheden met hun berging en parking eronder" en 16d "Eenhedenlijst zonder
/// basisakte — elk lot een eigen perceel, koper in plaats van aandeel"). Eén view-model voor beide
/// varianten: <see cref="HasBasisakte"/> bepaalt welke kolom/KPI/projectregel de view rendert, niet
/// de gebruiker — exact 16d's eigen regel 1 ("Het projecttype bepaalt de kolommen").
/// Hangt als <c>DetailUnitsModel.GlV2</c> naast het legacy model, zodat Views/Projecten/DetailUnits.cshtml
/// (en de AddUnit/EditUnit-paden die <c>FillDetailUnitModel</c> delen) ongemoeid blijven.</summary>
public class DetailUnitsV2Vm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";

    /// <summary>True zodra het project een totaal aandeel basisakte heeft (ProjectBO.TotalLandShare),
    /// of er al aandelen over de eenheden verdeeld zijn. False = 16d (losse loten, geen mede-eigendom):
    /// de kolom BASISAKTE verdwijnt en wordt KOPER, de waarschuwing wordt een neutrale projectregel en
    /// de vierde KPI wordt grondoppervlakte.</summary>
    public bool HasBasisakte { get; set; }

    /// <summary>Totaal aantal aandelen volgens de projectinstellingen (bv. 1.000 of 10.000).</summary>
    public decimal LandshareTotal { get; set; }

    /// <summary>Som van de aandelen die daadwerkelijk aan een eenheid hangen.</summary>
    public decimal LandshareAssigned { get; set; }

    public bool IsCoordinationProject { get; set; }

    /// <summary>Is er nog een berging/parking die aan geen enkel lot hangt? De koppelregel in de
    /// tabel (16a §2) heeft enkel zin zolang dat zo is.</summary>
    public bool HasAttachableUnits { get; set; }

    public List<DetailUnitsV2Group> Groups { get; set; } = new();

    /// <summary>Alle rijen (hoofdeenheden én gekoppelde eenheden) in renderorde — voor tellers en de
    /// mobiele kaartenlijst, zodat die niet opnieuw door de groepsboom moet lopen.</summary>
    public List<DetailUnitsV2Row> AllRows { get; set; } = new();

    // ── KPI-strook (16a/16d §1) ──────────────────────────────────────────────────────────────────
    /// <summary>Hoofdeenheden: wooneenheden + commerciële ruimtes.</summary>
    public int MainCount { get; set; }
    /// <summary>Nevenruimtes: bergingen + parkeergelegenheden (gekoppeld of los).</summary>
    public int SecondaryCount { get; set; }
    public int SoldMainCount { get; set; }
    public decimal SalesValueTotal { get; set; }
    public decimal LivingSurfaceTotal { get; set; }
    public decimal GroundSurfaceTotal { get; set; }
    /// <summary>Aantal eenheden met een eigen grondoppervlakte — 16d's "N percelen".</summary>
    public int ParcelCount { get; set; }

    // ── Filterchips (16a §6) — tellers per type en per status ─────────────────────────────────────
    public int WoningCount { get; set; }
    public int CommercialCount { get; set; }
    public int StorageCount { get; set; }
    public int ParkingCount { get; set; }
    public int AvailableCount { get; set; }
    public int SoldCount { get; set; }
}

/// <summary>Eén groepskop in de eenhedenlijst ("Woningen", "Commerciële ruimtes", "Los te koop") —
/// design-handoff 16a §6: "Groepen zijn inklapbaar en losse nevenruimtes die nog niet gekoppeld zijn,
/// staan apart onder 'Los te koop'".</summary>
public class DetailUnitsV2Group
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    /// <summary>Phosphor-klasse zonder "ph "-prefix.</summary>
    public string IconClass { get; set; } = "ph-buildings";
    /// <summary>Enkel de hoofdeenheden; hun gekoppelde eenheden zitten in <see cref="DetailUnitsV2Row.Children"/>.</summary>
    public List<DetailUnitsV2Row> Rows { get; set; } = new();

    /// <summary>Aantal rijen dat de groepskop toont — hoofdeenheden én hun gekoppelde eenheden.</summary>
    public int TotalRowCount => Rows.Count + Rows.Sum(r => r.Children.Count);
}

/// <summary>Eén rij in de eenhedenlijst — een hoofdeenheid of (met <see cref="IsAttached"/>) een
/// eenheid die eronder hangt.</summary>
public class DetailUnitsV2Row
{
    public int UnitId { get; set; }
    public string Name { get; set; } = "";
    /// <summary>Adres + kadaster onder de naam (16a §3), of "gekoppeld aan Lot 1" bij een gekoppelde
    /// eenheid — de view kiest niet, de controller vult hier al de juiste regel in.</summary>
    public string SubLine { get; set; } = "";

    /// <summary>"Wooneenheid" / "Commerciële ruimte" / "Nevenruimte" — de bovenste regel in de
    /// TYPE-kolom.</summary>
    public string TypeGroupLabel { get; set; } = "";
    /// <summary>Het echte subtype ("Woning", "Berging", "Staanplaats") — de tweede regel.</summary>
    public string TypeName { get; set; } = "";
    public int TypeGroupId { get; set; }

    public string LevelLabel { get; set; } = "—";
    public decimal? Surface { get; set; }
    public decimal? GroundSurface { get; set; }

    /// <summary>De prijs die als hoofdbedrag in de kolom VERKOOPPRIJS staat: bij een verkochte eenheid
    /// de verkoopprijs, anders grondwaarde + basisbouwwaarde (zelfde definitie als de Eenheden-tabel
    /// op Projecten/DetailV2), en bij een hoofdeenheid mét gekoppelde eenheden inclusief die
    /// gekoppelde bedragen.</summary>
    public decimal Price { get; set; }
    /// <summary>De prijs van de eenheid zelf, zonder wat eraan gekoppeld hangt — de tweede regel
    /// "€ 975.000 + 2 gekoppeld".</summary>
    public decimal OwnPrice { get; set; }
    public int AttachedCount { get; set; }
    /// <summary>Goedkoopste afwerkingsoptie bovenop <see cref="Price"/> ("vanaf € 820.000").</summary>
    public decimal? PriceFrom { get; set; }
    /// <summary>Duurste afwerkingsoptie bovenop <see cref="Price"/> ("afgewerkt € 995.000").</summary>
    public decimal? PriceFinished { get; set; }

    public decimal? Landshare { get; set; }

    public string Status { get; set; } = "";
    /// <summary>Toonklasse voor .gl-v2-badge (is-positive/is-neutral/is-attention/is-info).</summary>
    public string StatusTone { get; set; } = "is-neutral";

    public int? ClientId { get; set; }
    public string? ClientName { get; set; }
    /// <summary>Datum compromis — 16d's tweede regel onder de koper.</summary>
    public DateOnly? SalesAgreementDate { get; set; }

    public bool IsAttached { get; set; }
    public string? ParentName { get; set; }
    public int? ParentUnitId { get; set; }

    /// <summary>Een legacy "KOPPELING"-eenheid (Units.IsLink) — een samengestelde verkoopeenheid uit
    /// het oude koppelmechanisme. Krijgt een eigen markering i.p.v. stil door te gaan voor een gewone
    /// eenheid, en kan niet als koppeldoel of koppelbron dienen.</summary>
    public bool IsLink { get; set; }

    /// <summary>Mag hier een berging/parking aan gekoppeld worden (de "Berging of parking koppelen aan
    /// Lot 2"-regel uit 16a §2)? Enkel bij een hoofdeenheid die geen legacy koppeling is.</summary>
    public bool CanAttach { get; set; }

    public List<DetailUnitsV2Row> Children { get; set; } = new();

    /// <summary>Filterwaarde voor de type-chips: woning/commercieel/berging/parking.</summary>
    public string TypeFilterKey { get; set; } = "";
    /// <summary>Filterwaarde voor de status-chips: beschikbaar/verkocht/gekoppeld/los.</summary>
    public string StatusFilterKey { get; set; } = "";
    /// <summary>Kleingeletterde zoekstring (naam, adres, kadaster, type, koper).</summary>
    public string SearchText { get; set; } = "";
}

# Development Notes — Group-LN CPMCore

## Recente wijzigingen (juni 2026)

---

## 1. KostprijsMaterialen — Categorieën vervangen door ActivityGroup

**Probleem:** Kostprijsmaterialen hadden een eigen `KostprijsCategorie`-entiteit. Dit is vervangen door de bestaande `ActivityGroup`-entiteit (zelfde als gebruikt in Activities/Planning).

**Wat gewijzigd:**
- `KostprijsMateriaal.Categorie` is nu van type `ActivityGroup` (ipv `KostprijsCategorie`)
- FK kolom `CategorieId` verwijst nu naar `ActivityGroup.GroupID`
- `KostprijsCategorie.Materialen` nav property verwijderd (veroorzaakte EF shadow FK)
- `DbSet<KostprijsCategorie>` verwijderd uit context
- `modelBuilder.Ignore<KostprijsCategorie>()` toegevoegd
- `KostprijsCategorieen` repository verwijderd uit `UnitOfWorkCore`

**SQL uitvoeren (als nog niet gedaan):**
```
DALCore/Migrations/KostprijsMaterialen_ActivityGroep.sql
```
Let op: eerst `ProjectKostprijs` en `KostprijsMateriaal` leegmaken wegens FK conflict.

---

## 2. S-index scraper — Enkel 2B + Categorie A

**Bestand:** `ServiceCore/Budget/SIndexScraperService.cs`

**Wijziging:** De scraper van arch-index.be importeert nu alleen:
- Rijen met `(2B)` in de datumkolom → 1 waarde per maand
- Alleen de eerste datakolom (Categorie A)

**SQL om bestaande S-index data te wissen voor herinitialisatie:**
```sql
DELETE FROM [dbo].[BouwIndex] WHERE [IndexType] = 'S';
```

---

## 3. BudgetFormuleEngine — NIEUW

Dit is het grootste nieuwe onderdeel. Het koppelt velden in de budget-wizard aan prijzen uit KostprijsMaterialen via een formule-systeem.

### Architectuur

**Twee lagen:**

1. **Database** — `KostprijsFormulaKoppeling`: koppelt een logische sleutel (bv. `nacalc_ruwbouw_basis`) aan een `KostprijsMateriaal`. Beheerbaar via UI.
2. **C# code** — `BudgetFormulaRegistry`: de developer schrijft formules als C# lambda's.

### Nieuwe bestanden

| Bestand | Doel |
|---|---|
| `DALCore/Migrations/KostprijsFormulaKoppeling.sql` | **UITVOEREN in SSMS** — tabel + seed |
| `DALCore/Models/KostprijsFormulaKoppeling.cs` | EF entity |
| `DALCore/Models/cpmRunningContext.KostprijsMaterialen.cs` | DbSet + EF config toegevoegd |
| `DALCore/UnitOfWorkCore.cs` | `FormulaKoppelingen` repository toegevoegd |
| `BOCore/BO/Company/FormulaKoppelingBO.vb` | BO |
| `ServiceCore/Budget/FormulaSleutels.cs` | Constanten voor sleutels |
| `ServiceCore/Budget/BudgetFormulaContext.cs` | Context met alle variabelen |
| `ServiceCore/Budget/BudgetFormulaRegistry.cs` | Formule-definities |
| `ServiceCore/Budget/BudgetFormulaService.cs` | Bouwt context + evalueert formules |
| `FacadeCore/IKostprijsService.cs` | `GetFormulaKoppelingen` + `SaveFormulaKoppeling` toegevoegd |
| `ServiceCore/KostprijsService.cs` | Implementatie + deletion protection |
| `CPMCore/Program.cs` | DI registratie toegevoegd |
| `CPMCore/Models/Instellingen/KostprijsMaterialenViewModel.cs` | `FormulaKoppelingen` + `FormulaKoppelingAjaxRequest` |
| `CPMCore/Models/Projecten/ProjectModel.cs` | `FormulaVoorstellingen` op `BudgetGegevensModel` |
| `CPMCore/Controllers/InstellingenController.cs` | Koppelingen laden + 2 nieuwe acties |
| `CPMCore/Controllers/ProjectenController.cs` | `BudgetFormulaService` geïnjecteerd + gebruikt in GET |
| `CPMCore/Views/Projecten/BudgetGegevens.cshtml` | Voorstel-badge + Overnemen-knop |
| `CPMCore/Views/Instellingen/KostprijsMaterialen.cshtml` | Tab "Formule koppelingen" |

### SQL uitvoeren (verplicht op nieuwe PC)

```sql
-- 1. KostprijsFormulaKoppeling tabel aanmaken
-- Voer uit: DALCore/Migrations/KostprijsFormulaKoppeling.sql
```

### Eerste gebruik

1. Ga naar **Instellingen → Kostprijzen materialen → tab "Formule koppelingen"**
2. Koppel een materiaal aan `nacalc_ruwbouw_basis`
3. Open een project → Budget → stap 1 → het veld "Nacalc basisprijs ruwbouw" toont nu een voorstel met de geïndexeerde prijs en een knop om over te nemen

### Volgende formule toevoegen (developer)

**Voorbeeld: Gevelmetselwerk koppelen**

**Stap 1** — `ServiceCore/Budget/FormulaSleutels.cs`:
```csharp
public const string GevelMetselwerk = "gevel_metselwerk";
```

**Stap 2** — `ServiceCore/Budget/BudgetFormulaRegistry.cs`, in de constructor:
```csharp
Register(
    FormulaSleutels.GevelMetselwerk,
    "Gevelmetselwerk (€/m²)",
    ctx => ctx.M(FormulaSleutels.GevelMetselwerk) * ctx.GewogenIndex
);
```

**Stap 3** — SQL uitvoeren in SSMS:
```sql
INSERT INTO [dbo].[KostprijsFormulaKoppeling] ([Sleutel], [Omschrijving])
VALUES ('gevel_metselwerk', 'Gevelmetselwerk (€/m²)');
```

**Stap 4** — In de view het voorstel tonen (zelfde patroon als `NacalcBasisprijs` in `BudgetGegevens.cshtml`):
```razor
@{
    Model.FormulaVoorstellingen.TryGetValue(FormulaSleutels.GevelMetselwerk, out var fGevel);
}
@if (fGevel?.Waarde != null) {
    <div class="form-text text-primary">
        Formulewaarde: <strong>€ @fGevel.Waarde.Value.ToString("N2", nlBE)</strong>/m²
        <button type="button" onclick="...overnemen...">↔</button>
    </div>
}
```

### Beschikbare helpers in formules (`ctx.`)

| Helper | Omschrijving |
|---|---|
| `ctx.M("sleutel")` | ReferentiePrijs van het gekoppelde materiaal |
| `ctx.MNaam("sleutel")` | Naam van het gekoppelde materiaal |
| `ctx.HeeftMateriaal("sleutel")` | True als er een materiaal gekoppeld is |
| `ctx.GewogenIndex` | I×0.4 + S×0.4 + 0.2 (berekend uit gegevens) |
| `ctx.AantalEenheden("Woning")` | Aantal rijen in BudgetOppervlaktes met die EenheidNaam |
| `ctx.TotaleOpp("Woning")` | Som BewoonbareOpp voor die groep |
| `ctx.TotaleOpp()` | Totale bewoonbare opp alle eenheden |
| `ctx.Pct("naam")` | BouwkostPercentage op naam / 100 |
| `ctx.F("andere_sleutel")` | Resultaat van een andere formule (lazy, circulaire refs worden afgebroken) |
| `ctx.Gegevens.AantalLiften` | Rechtstreeks veld uit BudgetGegevens |
| `ctx.Gegevens.TypeDak` | Etc. — alle BudgetGegevensBO properties |

**Voorbeeld met condities:**
```csharp
Register("dakprijs_totaal", "Dakprijs totaal",
    ctx => ctx.Gegevens.TypeDak == "Plat dak"
        ? ctx.M("plat_dak_per_m2")  * ctx.TotaleOpp() * ctx.GewogenIndex
        : ctx.M("hellend_dak_per_m2") * ctx.TotaleOpp() * ctx.GewogenIndex
);
```

**Voorbeeld met meerdere unit-types:**
```csharp
Register("badkamers_totaal", "Totaal badkamers (€)",
    ctx =>   ctx.AantalEenheden("Woning")      * ctx.M("badkamer_woning") * ctx.GewogenIndex
           + ctx.AantalEenheden("Appartement") * ctx.M("badkamer_app")    * ctx.GewogenIndex
);
```

### Deletion protection

Als een materiaal gekoppeld is aan een formule-slot, wordt verwijderen geblokkeerd met de melding:
> "Dit materiaal is gekoppeld aan de formule '...' en kan niet worden verwijderd. Verwijder eerst de koppeling in Instellingen → Kostprijsmaterialen → Formule koppelingen."

---

## 4. Bouwkost Percentages — NIEUW

Aparte tabel voor bouwkost-percentages, beheerbaar via derde tab in KostprijsMaterialen.

**SQL uitvoeren (als nog niet gedaan):**
```
DALCore/Migrations/BouwkostPercentages.sql
```

Entiteiten: `BouwkostPercentageGroep` + `BouwkostPercentage`

---

## Te doen / pending

- [ ] SQL migraties uitvoeren op productie/andere PC:
  - `KostprijsMaterialen_ActivityGroep.sql`
  - `BouwkostPercentages.sql`
  - `KostprijsFormulaKoppeling.sql`
- [ ] Materiaal koppelen in Instellingen voor `nacalc_ruwbouw_basis`
- [ ] Zelfde voorstel-badge patroon implementeren voor `GevelMetselwerkPrijsPerM2` en `GipswerkenPrijsPerM2`
- [ ] Verdere formules toevoegen naargelang budget-stappen dat vereisen

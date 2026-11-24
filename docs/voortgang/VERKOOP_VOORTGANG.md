# Verkoop gl-v2 (design-handoff punt 45) — voortgang en overdracht

Laatst bijgewerkt: 23/10/2026. Niets gecommit (branch `layout-experiment`), niets in de browser getest door mij; Niels heeft de pagina's wel gezien en feedback gegeven (verwerkt, zie onder).
Ontwerp: `design-handoff/CRM Verkoop.dc.html` (45a t/m 45h). Tekstdump: `%TEMP%\verkoop_tekst.txt` (kan weg zijn; opnieuw te maken door de html te strippen).

## Afspraken met Niels
- In fasen met een stop na elke fase. Fase 1 en 2 zijn gebouwd, fase 3 (afdrukken/exporteren) niet.
- 45c Eenheid berekenen: **enkel rekenen en bekijken**, niets bewaren, geen "Prijs opslaan", **geen Code/Eigen-schakelaar** (de prijs is altijd een eigen bedrag). Enkel grondwaarde, bouwwaarde, registratierechten en btw zijn aanpasbaar; oppervlaktes, prijs per m² en kosten koper zijn uitgeschakeld. Verkochte eenheden zijn vergrendeld (grijze pill, niet klikbaar).
- Ontbrekende gegevens zelf gebouwd: optiedatum, optant, makelaar/herkomst, online sinds, geïnteresseerden (uit contacten), "Kost toevoegen".
- Websitetekst gebruikt de bestaande projectvelden (`Project.CommercialTitleNl/CommercialTextNl`).
- Help volgens CPMCore/DESIGN.md/HELP_SCHRIJFGIDS.md, met screenshots (die nog ontbreken, zie onder).

## Wat er gebouwd is
**Data:** migratie `_migrations/115_VerkoopEenhedenEnKostenKoper.sql` (`Units.OptionUntil/OptionHolder/SaleVia/OnlineSince`, tabel `ProjectSalesExtraCost`); entiteiten in DALCore (`Units.cs`, `ProjectSalesExtraCost.cs`, `cpmRunningContext.Verkoop.cs`).

**Service:** `CPMCore/Services/Verkoop/VerkoopService.cs` (+ `VerkoopViewModels.cs`): instellingen lezen/bewaren (rechtstreeks via de context, niet via BO), `Bereken` (grond + registratie + bouw + btw + kosten koper + btw erop + notaris), index-VM met KPI's en T.o.v. budget, bulkacties (te koop / optie / online), `NotarisTarief`.

**Controller:** `ProjectenController.VerkoopV2.cs` (Te koop, Instellingen, opslaan via `SalesSettingsSaveV2`, bulk via `SalesBulkV2`), `ProjectenController.VerkoopBerekenV2.cs` (`SalesCalculateV2`), V2-takken in `ProjectenController.Sales.cs` (DetailSales en SalesSettings; `?classic=true` blijft het oude scherm).

**Views/css/js:** `Views/Projecten/Sales/DetailSalesV2`, `SalesSettingsV2`, `SalesCalculateV2`; `css/gl-v2-projecten-verkoop*.css` en `-verkoopberekenen.css`; `js/gl-v2-projecten-verkoop*.js` en `-verkoopberekenen.js`; menu-item **Instellingen** onder Verkoop in `_ProjectInnerMenuV2.cshtml`.

**Help:** `HelpContent/projecten/te-koop-overzicht.md`, `verkoopinstellingen-aanpassen.md`, `eenheid-berekenen.md` (schermen `projecten-te-koop`, `projecten-verkoopinstellingen`, `projecten-eenheid-berekenen`). **Screenshots ontbreken nog.**

**CPMCore/DESIGN.md:** sectie "Verkoop gl-v2" met fase 1, kosten/notaris en fase 2.

## Rekenregels (belangrijk, bewust zo)
- **Prijzen:** verkocht = `LandValueSold + Σ ConstructionValues.ValueSold` (exact zoals Klanten/Detail, `UnitBO.TotalValueSold`); niet verkocht = grond + bouwwaarde van de **standaardafwerking** (`ServiceCore.Helpers.UnitPricing`, afwerkingen zijn alternatieven en worden niet opgeteld).
- **Status:** verkocht = klantaccount gekoppeld; optie = optievlag; anders te koop.
- **Regime:** gemengd (btw op bouw, registratie op grond) bij `RegistrationType` 2 of `MixedVatRegistration`; type 1 = registratierechten op alles; anders btw op alles. In de UI: "Verkoop onder" + "Type registratie" als twee segmented controls.
- **Kosten koper:** 21 % btw op basisakte, verkaveling, landmeter en eigen posten; aansluitkosten aan het project-btw. "Vaste aktekost" is vervallen (overlapt met de notaris; kolom blijft in de database). Hypotheekkantoor heet nu **Kosten overschrijving** en hoort bij de notaris (geen btw).
- **Notaris** (nagerekend met de rekenmodule van de notarissen, Lot 3: € 5.611,89): ereloon volgens **schaal Jbis op de volledige prijs (grond + bouw)**, plus administratieve kosten 855, uitgaven aan derden 309, kosten overschrijving (= hypotheekkantoor), recht op geschriften 100, registratierecht op bijlagen 100; 21 % btw op ereloon + administratieve kosten + uitgaven + recht op geschriften (niet op overschrijving en registratierecht op bijlagen; Niels bevestigde het laatste, het eerste volgt uit de cijfers van de website). **Jbis is de standaard** (schakelaar "Enige eigen woning" staat aan; uit = schaal J). Tarief 2026; vaste gedeelte en forfaits worden om de 2 jaar op 1 januari geïndexeerd, volgende keer **1/1/2028**. Klasse `NotarisTarief` in `VerkoopService.cs`, gespiegeld in beide JS-bestanden.
- **Tegenover het budget (45c):** kostprijs en minimum uit `VerkoopVoorstelService.BerekenAsync`; verkoopkosten makelaar volgens `VerkoopResultaat` (btw enkel op het grondaandeel; overige verkoopkosten niet per eenheid); netto marge = vraagprijs − makelaar − kostprijs. Enkel als de eenheid in stap 8 aan een verkooplijn van het definitieve budget gekoppeld is.
- **Bouwoppervlakte** op 45c = gereduceerde oppervlakte uit het budget, anders woonoppervlakte (staat erbij).

## Wat Niels nog moet doen (op de andere pc eerst dit)
1. **Migratie 115 draaien op `db_ab5fbb_testdb` VOOR de app te starten** (anders falen alle eenheids- en verkooppagina's). Controleer ook dat 113 en 114 gedraaid zijn.
2. App herstarten (C#/Razor); css/js laden live.
3. Testen: Te koop (tabs, zoeken, selectiebalk: status/optie/website, Berekenen), Instellingen (voorbeeld rekent mee, Kost toevoegen, projectrekening, tekst, opslaan), Eenheid berekenen (grond/bouw aanpassen, notaris, vergelijking met budget; Lot 3 moet op notaris € 5.611,89 uitkomen).
4. Stuur screenshots als iets er niet goed uitziet: de kaartopmaak is nagebouwd uit het ontwerp maar nooit door mij in de browser bekeken.
5. Daarna: screenshots voor de drie helpartikels laten maken (Playwright, anonimiseren).
6. Commit/push staat open (veel bestanden, ook van andere sessies: facturen, nacalculatie, contacten).

## Open / nog niet gedaan
- **Fase 3 = 45d t/m 45h:** afdrukken-dialoog (één of meerdere eenheden, versie koper/intern, voorbeeld), berekening-PDF A4 (koper + intern, overzichtspagina bij meerdere), prijslijst A4 landscape (PDF), Excel-export met tabs, Exporteren-modal; knoppen **Exporteren** en **Berekening afdrukken** in de koptekst, **Prijslijst (PDF)** in de selectiebalk (verwijst nu naar de bestaande `SalesListPdf`).
- Filters-knop uit 45a (nu enkel tabs en zoeken).
- Onzeker: de btw-basis van de notaris (welk van de twee 100-bedragen) is afgeleid; een tweede voorbeeld van de website zou het bevestigen. Ook het forfait "uitgaven aan derden" (309) kan afhangen van de situatie.
- Beginstand op 45c is de huidige prijs; de prijscode uit het budget wordt niet vooringevuld.
- De oude kaart `CalculateCosts`/`PrintCalculation` (legacy) gebruikt nog de oude, foute notarisschaal; hij blijft staan voor `?classic=true`.
- Eventueel: Jbis/J per koper op de berekenpagina is er (schakelaar), maar wordt nergens bewaard (enkel rekenen).

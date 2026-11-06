# Terugkerende facturen — voortgang (design-handoff punt 49 en 50)

Gebouwd op 06/11/2026 (branch `layout-experiment`), **niet in de browser getest, niets gecommit**. Compileert (CPMCore + ServiceCore.Tests, 15 nieuwe tests groen voor de rekenkern).

## Vóór de app opnieuw start
1. **Draai `_migrations/133_TerugkerendeFacturen.sql` én `135_Gezondheidsindex.sql` op de live database.** De app leest de nieuwe kolommen `Invoices.RecurringScheduleId/-Occurrence/-Label/-PeriodText` bij elke factuurquery: zonder migratie falen de facturenpagina's. De migratie is idempotent en voegt alleen toe.

## Wat er is
- **Data**: tabellen `RecurringInvoiceSchedule`, `-ScheduleLine`, `-Event`, `-Settings` + 4 kolommen op `Invoices` + unieke index (schema + factuurdatum). Entiteiten in `DALCore/Models/RecurringInvoice.cs`, `Invoices.Recurring.cs`, `cpmRunningContext.Recurring.cs`.
- **Rekenkern**: `ServiceCore/Invoicing/RecurringScheduleCalculator.cs` (datums, weekend en Belgische feestdagen, periode, variabelen, indexering).
- **Service**: `FacadeCore/IRecurringInvoiceService.cs` + `ServiceCore/Invoicing/RecurringInvoiceService.cs` (concept klaarzetten via `CreateWithLinesAsync`, pauzeren, hervatten, stopzetten, indexatie bevestigen, instellingen). `CPMCore/Services/RecurringInvoiceHostedService.cs` loopt elk uur (start na 5 min).
- **UI**: schakelaar + ↻-label + "Ter controle"-pil in `Invoices/Core/IndexV2`; `Views/Invoices/Recurring/` (overzicht, zijpaneel Instellingen, schemadetail, stap 2/3/4-onderdelen voor `Editor/CreateV2`); JS `gl-v2-invoices-recurring*.js`, hooks in `gl-v2-invoices-create.js`; CSS `gl-v2-invoices-recurring.css`.
- **Help**: 3 artikels `terugkerende-factuur-aanmaken`, `-facturen-beheren`, `-facturen-instellen` (+ FAQ in `factuur-nummeren-en-verzenden`, `HELP_DEKKING.md`). **DESIGN.md**: sectie "Terugkerende facturen".

## Bewust niet gebouwd of anders dan het ontwerp
- **Automatisch nummeren en versturen**: bestaat niet (de nummerstap hangt aan de controller en Octopus). De optie staat uitgeschakeld; het CRM zet altijd een concept klaar ter controle.
- **Index**: gezondheidsindex komt van Statbel (migratie 135, ophalen op de laatste dag van de maand, mail naar niels.lataire@groupln.be bij mislukken); consumptieprijzen en vast percentage met zelf ingevuld percentage.
- Eerste factuur moet vandaag of later vallen (voorkomt een reeks oude concepten).
- Het ontwerp toont "Ook bereikbaar via Instellingen › Facturatie › Terugkerend": die pagina bestaat niet, die zin staat er niet.
- Meldingen bij klaarstaande concepten gaan per e-mail naar de gekozen personen (`RecurringInvoiceSettings.NotifyUsers`); de personenkiezer gebruikt `/MijnTaken/UitnodigingZoek` (vraagt een gekoppelde agenda; een volledig getypt e-mailadres werkt altijd).

## Te testen in de browser
1. `Invoices?issuerCompanyId=…&view=recurring` en terug via de schakelaar; lege staat, filters, jaar, zoeken.
2. Nieuw schema: stap 2 (Soort → Terugkerend, herhaal, factuurdag, eerste factuur, stopt, indexeren), planning rechts, stap 3 (variabelen, per lijn opties, voorbeeld), stap 4 → Schema activeren en Opslaan als draft.
3. Schemadetail: Pauzeren, Hervatten (met gemiste periodes), Nu factuur maken (concept verschijnt als Proforma met ↻-label), indexatie bevestigen, stopzetten, Bewerken.
4. Instellingen-zijpaneel: opslaan en heropenen (collega's voorgevuld).
5. Achtergrondtaak: schema met eerste factuur binnen "concept klaar" en 5 minuten wachten (of de service handmatig aanroepen).

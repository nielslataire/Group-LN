# Overdracht 23/10/2026 (werk van de sessies "contacten" en "facturatie")

Branch `layout-experiment`. **Niets is gecommit.** Niets hieronder is in de browser getest (de build geeft geen CS/RZ/BC-fouten; MSB3021/CS2012 zijn bestandsblokkades van de draaiende app).
Bouwcontrole: `dotnet build CPMCore/CPMCore.csproj 2>&1 | grep -E "error (CS|RZ|BC)"`, JS met `node --check`.

## 1. Eerst doen op de nieuwe pc / vóór gebruik
Migraties handmatig draaien op de live database, in deze volgorde (allemaal idempotent):
| Nr | Bestand | Waarvoor |
|---|---|---|
| 100 | PuntRondgang | Punten (nog niet gedraaid?) |
| 104 | Werfverslag EindUur | Verslagen (nog niet gedraaid?) |
| 105 | `105_CrmContacten.sql` | contacten (tabellen CrmContact*, CrmExportLog) |
| 113 | `113_CrmContactMakelaarsPostcode.sql` | ProjectBroker, BrokerCompanyId, AssignedCompanyId, PostcodeId + omzetting website-formulieren. **Hernoemd van 111** (botste met `111_VerzekeringenAttestEenhedenEnTienjarigeKeuze.sql`); opnieuw uitvoerbaar |
| 114 | `114_CrmContactVerwijderd.sql` | CrmContactLegacySkip (verwijderd contact kwam terug door de import) |
Controleer ook of 110/111/112 (verzekeringen) en 115 (verkoop/Te koop, andere werkstroom) gedraaid zijn.
Nieuw recht `Projects.ContactsExport` ("Contacten exporteren") toekennen aan rollen bij Instellingen › Rollen (admins hebben het vanzelf).
App herstarten na elke build (ze draait met de oude dll). Chrome-iconen: harde refresh + snelkoppeling opnieuw maken.

## 2. Contacten gl-v2 (design-handoff 44a-d) - gebouwd
- Pagina's: lijst (`Projecten/DetailContacts`, oude via `?classic=true`), detail met opvolging (`ContactV2`), toevoegen/bewerken (`ContactFormV2`), opgeslagen lijsten en export.
- Makelaars zijn bedrijven: koppelen bij Project bewerken › Partners (`ProjectBroker`, chips). Toegewezen aan = interne medewerker of makelaar van het project (zoekveld; interne = geen UserCompanyAccess/gast/aannemer).
- Gemeente/postcode via zoekveld (zoals Klanten); Omzetten naar klant vult naam, partner, e-mail, telefoon, adres, gemeente, 1 eenheid in (`Klanten/AddClientAccount?contactId=`).
- Export (Excel/CSV/PDF): recht `Projects.ContactsExport`, nieuwsbriefexport, contactdatum-kolom, gesorteerd op contactdatum (oudste eerst); PDF-lijst heeft geen datumkolom.
- Telefoon/e-mail gevalideerd (GlV2Telefoon/GlV2Email + Foutoverzicht); trimmen van spaties, datumveld-typen en telefoon-plakken zijn **project-wijd** opgelost in `gl-v2-shell.js`.
- Help: `HelpContent/projecten/contacten-overzicht|contact-opvolgen|contact-toevoegen|contactenlijsten-en-export.md`. Details: `CONTACTEN_VOORTGANG.md` en CPMCore/DESIGN.md ("Contacten gl-v2").
- Open/bewust niet gedaan: commissies aan makelaars, partner als mede-eigenaar bij omzetten, filters enkelvoudig, geen bulk-verwijderen.

## 3. Facturen bewerken en verzenden gl-v2 (design-handoff 11f en 11g) - gebouwd
- **Edit** (`Invoices/Editor/EditV2.cshtml`, definitieve factuur): 2 stappen, vergrendelde velden, omschrijving, detailomschrijving en **bijkomende vermelding** (= ExtraInfo) aanpasbaar; de btw-vermelding per tarief is vast (uit Instellingen) en niet getoond als invoer. Live A4-voorbeeld (`Views/Invoices/Partials/_InvoiceSheetV2.cshtml`) uit dezelfde gegevens als de pdf (`IInvoicePdfService.BuildPreview`), toont de eerste pagina, altijd layoutA. "Creditnota maken" = `Invoices/Create?duplicateInvoiceId=&creditNote=true`. Conceptfacturen gaan nog naar het oude `EditDraft`/`Create`.
- **Send** (`Invoices/Send/SendV2.cshtml`): kanaal E-mail / Peppol via Octopus / Beide (`InvoiceSendVM.Channel`). Peppol via Octopus = doorsturen naar de eigen Octopus (`SendInvoiceToOctopusAsync`, zonder klassieke mail), enkel als Octopus gekoppeld is en de factuur er nog niet is of de verzending mislukte. Ontvangers als tags, bericht als opgemaakte tekst (contenteditable), bijlagetegels, "Kopie naar mezelf", verzendgeschiedenis met kanaal (Octopus-regels = kanaal Peppol).
- Peppol-controle van de klant gebruikte een niet-bestaand adres; nu `directory.peppol.eu/search/1.0/json` (+ `0208:`+10 cijfers). **Nog niet bevestigd dat GROUP LN nu "geregistreerd" toont** (herstart + test).
- Help: `HelpContent/facturatie-wijzigingen/factuur-bewerken.md` en `factuur-verzenden.md`; CPMCore/DESIGN.md ("Facturen bewerken en verzenden gl-v2").
- Te testen/beslissen: of "Peppol via Octopus" in de praktijk goed hervat na een mislukking; het oude Send-scherm blijft de rechtstreekse Peppol-verzender gebruiken; bestandsgrootte bij bijlagen en een oogje per geschiedenisregel zijn niet gebouwd.

## 4. Iconen (favicon / snelkoppeling)
`wwwroot/favicon.ico` en `wwwroot/Img/favicon.ico` zijn vervangen door een meerformaat-icoon (16 tot 256) uit `icons/icon-512.png`; alle layouts (ook `_Base` van de aanmeldpagina en `Account/Shared/_Layout`) verwijzen naar 192/512-png's en hebben `sizes` op de ico-link. Chrome onthoudt iconen: oude snelkoppeling verwijderen en opnieuw maken.

## 5. Eerder in deze periode (kort, zie de eigen voortgangsdocumenten)
Punten (alle fases werken), nacalculatie, budget, verzekeringen, Verkoop/Te koop (migratie 115, andere werkstroom). Memory-index: `C:\Users\latni\.claude\projects\e--TFS\memory\MEMORY.md` (nieuwe entries: contacten_crm_gl_v2, facturen_edit_send_gl_v2); op een andere pc staat die map niet: de inhoud staat hier en in de *_VOORTGANG.md-bestanden.

## 6. Werkafspraken (voor wie verder werkt)
Nederlandse UI-tekst, eigen gl-v2-modals i.p.v. browserdialogen, migraties als handmatige idempotente SQL in `_migrations/NNN_*.sql` (nooit EF-migraties, enkel additief), sleutels nooit in appsettings, help volgens CPMCore/DESIGN.md ("Een nieuw help-artikel schrijven") en CPMCore/HELP_SCHRIJFGIDS.md (je-vorm, geen gedachtestreepjes, onbekende logica als `[VRAAG: ...]`), pagina-CSS laadt vóór shell-CSS (extra `.gl-v2`-specificiteit), shell-script met apostrofen faalt: schrijf python-scripts met de Write-tool.
Openstaande help-vragen in totaal: zoek met `grep -rn "\[VRAAG" CPMCore/HelpContent`.
